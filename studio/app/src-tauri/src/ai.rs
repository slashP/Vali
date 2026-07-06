use std::process::Stdio;
use std::sync::Mutex;

use serde::Serialize;
use tauri::State;
use tokio::io::{AsyncBufReadExt, AsyncReadExt, AsyncWriteExt, BufReader};
use tokio::process::Command;
use tokio::sync::oneshot;

#[cfg(windows)]
const CREATE_NO_WINDOW: u32 = 0x0800_0000;

const AI_TIMEOUT_SECS: u64 = 300;

/// Holds the cancel signal for the in-flight AI request (only one runs at a
/// time), so the UI can abort a slow/stuck model.
#[derive(Default)]
pub struct AiState {
    cancel: Mutex<Option<oneshot::Sender<()>>>,
}

struct Provider {
    id: &'static str,
    label: &'static str,
    /// Candidate CLI names to probe on PATH.
    binaries: &'static [&'static str],
}

// Local AI CLIs Vali can drive. The prompt is always sent on stdin (it embeds
// the ~30 KB schema, too large to pass as an argument).
const PROVIDERS: &[Provider] = &[
    Provider { id: "claude", label: "Claude", binaries: &["claude"] },
    Provider { id: "codex", label: "Codex", binaries: &["codex"] },
    Provider { id: "gemini", label: "Gemini", binaries: &["gemini"] },
    Provider { id: "copilot", label: "Copilot", binaries: &["copilot"] },
];

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
pub struct ProviderInfo {
    id: String,
    label: String,
    available: bool,
}

fn find_provider(id: &str) -> Option<&'static Provider> {
    PROVIDERS.iter().find(|p| p.id == id)
}

/// One-shot args per provider; the prompt goes on stdin in every case.
fn provider_args(id: &str) -> Vec<&'static str> {
    match id {
        // Claude streams NDJSON incl. partial text deltas — we relay those live.
        "claude" => vec![
            "-p",
            "--output-format",
            "stream-json",
            "--verbose",
            "--include-partial-messages",
        ],
        "codex" => vec!["exec"],
        "gemini" => vec![],
        // Experimental — Copilot CLI is interactive-first; may need adjustment.
        "copilot" => vec!["-p"],
        _ => vec![],
    }
}

// ---- binary resolution --------------------------------------------------

fn resolve_binary(name: &str) -> Option<String> {
    #[cfg(windows)]
    {
        resolve_binary_windows(name)
    }
    #[cfg(not(windows))]
    {
        resolve_binary_unix(name)
    }
}

/// Windows: `where <name>`, then the official installer location
/// `%USERPROFILE%\.local\bin\<name>.{exe,cmd}`. GUI apps inherit the system
/// PATH here, so `where` usually succeeds.
#[cfg(windows)]
fn resolve_binary_windows(name: &str) -> Option<String> {
    use std::os::windows::process::CommandExt;

    let mut cmd = std::process::Command::new("where");
    cmd.arg(name);
    cmd.creation_flags(CREATE_NO_WINDOW);
    if let Ok(output) = cmd.output() {
        if output.status.success() {
            let stdout = String::from_utf8_lossy(&output.stdout);
            if let Some(first) = stdout.lines().map(str::trim).find(|l| !l.is_empty()) {
                return Some(first.to_string());
            }
        }
    }

    if let Ok(home) = std::env::var("USERPROFILE") {
        for ext in ["exe", "cmd"] {
            let mut p = std::path::PathBuf::from(&home);
            p.push(".local");
            p.push("bin");
            p.push(format!("{name}.{ext}"));
            if p.exists() {
                return Some(p.to_string_lossy().into_owned());
            }
        }
    }

    None
}

/// macOS/Linux: a Dock-launched GUI app doesn't inherit the shell PATH, so probe
/// common install locations first, then `which`, then the login shell.
#[cfg(not(windows))]
fn resolve_binary_unix(name: &str) -> Option<String> {
    if let Ok(home) = std::env::var("HOME") {
        for rel in [".local/bin", ".npm-global/bin", ".bun/bin", ".yarn/bin"] {
            let p = std::path::Path::new(&home).join(rel).join(name);
            if p.exists() {
                return Some(p.to_string_lossy().into_owned());
            }
        }
    }

    for dir in ["/opt/homebrew/bin", "/usr/local/bin", "/usr/bin"] {
        let p = std::path::Path::new(dir).join(name);
        if p.exists() {
            return Some(p.to_string_lossy().into_owned());
        }
    }

    if let Some(p) = run_capture("which", &[name]) {
        return Some(p);
    }

    resolve_via_login_shell(name)
}

#[cfg(not(windows))]
fn run_capture(program: &str, args: &[&str]) -> Option<String> {
    let out = std::process::Command::new(program).args(args).output().ok()?;
    if !out.status.success() {
        return None;
    }
    let stdout = String::from_utf8_lossy(&out.stdout);
    stdout
        .lines()
        .map(str::trim)
        .find(|l| l.starts_with('/'))
        .map(String::from)
}

#[cfg(not(windows))]
fn resolve_via_login_shell(name: &str) -> Option<String> {
    let shell = std::env::var("SHELL").unwrap_or_else(|_| "/bin/zsh".to_string());
    let script = format!("command -v {name}");
    run_capture(&shell, &["-lc", script.as_str()])
}

// ---- command building ---------------------------------------------------

/// Build a command for `path` with `args`. On Windows this routes `.cmd`/`.bat`
/// shims (and extensionless npm shims) through `cmd /C`.
#[cfg(windows)]
fn build_command(path: &str, args: &[&str]) -> Command {
    let lower = path.to_ascii_lowercase();
    let file_name = std::path::Path::new(path)
        .file_name()
        .map(|s| s.to_string_lossy().into_owned())
        .unwrap_or_default();
    let extensionless = !file_name.contains('.');

    let (program, use_cmd) = if lower.ends_with(".exe") {
        (path.to_string(), false)
    } else if lower.ends_with(".cmd") || lower.ends_with(".bat") {
        (path.to_string(), true)
    } else if extensionless {
        let exe = format!("{path}.exe");
        let cmd = format!("{path}.cmd");
        if std::path::Path::new(&exe).exists() {
            (exe, false)
        } else if std::path::Path::new(&cmd).exists() {
            (cmd, true)
        } else {
            (path.to_string(), true)
        }
    } else {
        (path.to_string(), false)
    };

    if use_cmd {
        let mut c = Command::new("cmd");
        c.raw_arg("/C");
        c.raw_arg(format!("\"\"{program}\" {}\"", args.join(" ")));
        c
    } else {
        let mut c = Command::new(&program);
        for a in args {
            c.arg(a);
        }
        c
    }
}

/// Build a command for `path` with `args` (native binary).
#[cfg(not(windows))]
fn build_command(path: &str, args: &[&str]) -> Command {
    let mut c = Command::new(path);
    for a in args {
        c.arg(a);
    }
    c
}

/// Give a Dock-launched app a usable PATH so a node/npm-shim CLI can find its
/// interpreter and helpers at runtime.
#[cfg(not(windows))]
fn augment_path(command: &mut Command, bin: &str) {
    let mut paths: Vec<String> = Vec::new();
    if let Some(dir) = std::path::Path::new(bin).parent() {
        let d = dir.to_string_lossy();
        if !d.is_empty() {
            paths.push(d.into_owned());
        }
    }
    if let Ok(home) = std::env::var("HOME") {
        paths.push(format!("{home}/.local/bin"));
        paths.push(format!("{home}/.npm-global/bin"));
    }
    for p in ["/opt/homebrew/bin", "/usr/local/bin", "/usr/bin", "/bin"] {
        paths.push(p.to_string());
    }
    if let Ok(existing) = std::env::var("PATH") {
        if !existing.is_empty() {
            paths.push(existing);
        }
    }
    command.env("PATH", paths.join(":"));
}

// ---- output helpers -----------------------------------------------------

/// Strip a leading ```json / ``` fence and a trailing ``` fence, then trim.
fn strip_fences(s: &str) -> String {
    let mut t = s.trim();
    if let Some(rest) = t.strip_prefix("```json") {
        t = rest.trim_start();
    } else if let Some(rest) = t.strip_prefix("```") {
        t = rest.trim_start();
    }
    if let Some(rest) = t.strip_suffix("```") {
        t = rest.trim_end();
    }
    t.trim().to_string()
}

/// Pull the JSON document out of arbitrary CLI output (some tools print chatter
/// around it): first `{` to last `}`, after fence-stripping.
fn extract_json(s: &str) -> String {
    let t = strip_fences(s);
    if let (Some(a), Some(b)) = (t.find('{'), t.rfind('}')) {
        if b > a {
            return t[a..=b].to_string();
        }
    }
    t
}

fn tail_chars(s: &str, n: usize) -> String {
    let chars: Vec<char> = s.chars().collect();
    let start = chars.len().saturating_sub(n);
    chars[start..].iter().collect()
}

// ---- commands -----------------------------------------------------------

/// Report which local AI CLIs are installed, so the UI can offer a picker.
#[tauri::command]
pub fn ai_providers() -> Vec<ProviderInfo> {
    PROVIDERS
        .iter()
        .map(|p| ProviderInfo {
            id: p.id.to_string(),
            label: p.label.to_string(),
            available: p.binaries.iter().any(|b| resolve_binary(b).is_some()),
        })
        .collect()
}

/// Abort the in-flight AI request, if any.
#[tauri::command]
pub fn ai_cancel(state: State<'_, AiState>) {
    if let Ok(mut guard) = state.cancel.lock() {
        if let Some(tx) = guard.take() {
            let _ = tx.send(());
        }
    }
}

/// Run the chosen local AI CLI with `prompt` and return the final JSON document,
/// fences stripped. Progress is conveyed by the caller's phase text, not a stream.
#[tauri::command]
pub async fn ai_run(
    state: State<'_, AiState>,
    provider: String,
    prompt: String,
) -> Result<String, String> {
    let p = find_provider(&provider).ok_or_else(|| format!("Unknown AI provider '{provider}'"))?;
    let bin = p
        .binaries
        .iter()
        .find_map(|b| resolve_binary(b))
        .ok_or_else(|| format!("{} CLI not found on PATH", p.label))?;
    let stream_json = provider == "claude";

    let mut command = build_command(&bin, &provider_args(&provider));
    command
        .stdin(Stdio::piped())
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .kill_on_drop(true);
    #[cfg(windows)]
    command.creation_flags(CREATE_NO_WINDOW);
    #[cfg(not(windows))]
    augment_path(&mut command, &bin);

    let mut child = command
        .spawn()
        .map_err(|e| format!("failed to launch {} ({bin}): {e}", p.label))?;

    let (cancel_tx, cancel_rx) = oneshot::channel::<()>();
    if let Ok(mut guard) = state.cancel.lock() {
        *guard = Some(cancel_tx);
    }

    let mut stdin = child.stdin.take().ok_or("AI CLI has no stdin")?;
    let writer = tokio::spawn(async move {
        let _ = stdin.write_all(prompt.as_bytes()).await;
        let _ = stdin.shutdown().await;
    });

    let mut stderr = child.stderr.take().ok_or("AI CLI has no stderr")?;
    let stderr_task = tokio::spawn(async move {
        let mut buf = Vec::new();
        let _ = stderr.read_to_end(&mut buf).await;
        String::from_utf8_lossy(&buf).into_owned()
    });

    let stdout = child.stdout.take().ok_or("AI CLI has no stdout")?;
    let read = async move {
        let mut reader = BufReader::new(stdout);
        let mut line = Vec::new();
        let mut acc = String::new();
        let mut result: Option<String> = None;
        loop {
            line.clear();
            match reader.read_until(b'\n', &mut line).await {
                Ok(0) | Err(_) => break,
                Ok(_) => {
                    let raw = String::from_utf8_lossy(&line);
                    if stream_json {
                        // Claude NDJSON: relay text deltas, capture final result.
                        let Ok(v) =
                            serde_json::from_str::<serde_json::Value>(raw.trim())
                        else {
                            continue;
                        };
                        match v.get("type").and_then(|t| t.as_str()) {
                            Some("stream_event") => {
                                if let Some(chunk) =
                                    v.pointer("/event/delta/text").and_then(|t| t.as_str())
                                {
                                    acc.push_str(chunk);
                                }
                            }
                            Some("result") => {
                                if let Some(r) = v.get("result").and_then(|r| r.as_str()) {
                                    result = Some(r.to_string());
                                }
                            }
                            _ => {}
                        }
                    } else {
                        // Other CLIs: no structured stream; accumulate raw output.
                        acc.push_str(raw.as_ref());
                    }
                }
            }
        }
        (result, acc)
    };

    let dur = std::time::Duration::from_secs(AI_TIMEOUT_SECS);
    let outcome = tokio::select! {
        r = tokio::time::timeout(dur, read) => r,
        _ = cancel_rx => {
            if let Ok(mut guard) = state.cancel.lock() { *guard = None; }
            return Err("cancelled".to_string());
        }
    };
    if let Ok(mut guard) = state.cancel.lock() {
        *guard = None;
    }

    let (result, acc) = outcome.map_err(|_| format!("{} timed out", p.label))?;
    let _ = writer.await;
    let status = child.wait().await;
    let stderr_text = stderr_task.await.unwrap_or_default();

    let text = if stream_json {
        result.or(if acc.is_empty() { None } else { Some(acc) })
    } else if acc.trim().is_empty() {
        None
    } else {
        Some(extract_json(&acc))
    };

    if let Some(t) = text {
        let cleaned = strip_fences(&t);
        if !cleaned.is_empty() {
            return Ok(cleaned);
        }
    }

    let code = status.ok().and_then(|s| s.code());
    Err(format!(
        "{} produced no usable output (exit {code:?}). {}",
        p.label,
        tail_chars(stderr_text.trim(), 500)
    ))
}
