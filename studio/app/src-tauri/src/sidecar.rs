use std::collections::HashMap;
use std::path::PathBuf;
use std::process::Stdio;
use std::sync::Mutex;

use tauri::{AppHandle, Emitter, Manager, State};
use tokio::io::{AsyncBufReadExt, AsyncWriteExt, BufReader};
use tokio::process::{Child, Command};
use tokio::sync::{mpsc, oneshot};

#[cfg(windows)]
const CREATE_NO_WINDOW: u32 = 0x0800_0000;

/// The sidecar executable's file name. .NET emits a native apphost per platform:
/// `Vali.Host.exe` on Windows, extensionless `Vali.Host` elsewhere.
#[cfg(windows)]
const HOST_BINARY: &str = "Vali.Host.exe";
#[cfg(not(windows))]
const HOST_BINARY: &str = "Vali.Host";

/// Whether a bundled file name is the host *executable* (not its .dll / .pdb /
/// .deps.json siblings). Tauri's externalBin may keep a `-<target-triple>` suffix.
fn is_host_candidate(name: &str) -> bool {
    let Some(rest) = name.strip_prefix("Vali.Host") else {
        return false;
    };
    #[cfg(windows)]
    {
        // "Vali.Host.exe" or "Vali.Host-<triple>.exe"
        rest.eq_ignore_ascii_case(".exe")
            || (rest.starts_with('-') && rest.to_ascii_lowercase().ends_with(".exe"))
    }
    #[cfg(not(windows))]
    {
        // Extensionless apphost only: "Vali.Host" or "Vali.Host-<triple>".
        // A dot after the stem means a managed dll / deps.json / pdb — skip it.
        rest.is_empty() || (rest.starts_with('-') && !rest.contains('.'))
    }
}

const DEFAULT_TIMEOUT_MS: u64 = 600_000;

/// Managed state for the single Vali.Host sidecar process.
#[derive(Default)]
pub struct SidecarState {
    inner: Mutex<SidecarInner>,
}

#[derive(Default)]
struct SidecarInner {
    child: Option<Child>,
    /// Serializes writes to the child's stdin (a dedicated writer task owns it).
    writer_tx: Option<mpsc::UnboundedSender<String>>,
    /// In-flight requests keyed by their app-supplied id.
    pending: HashMap<String, oneshot::Sender<serde_json::Value>>,
    alive: bool,
    /// Incremented on each spawn; reader/stderr tasks capture their own value so
    /// a stale task (from a killed/restarted child) can't touch current state.
    epoch: u64,
}

fn remove_pending(state: &SidecarState, id: &str) {
    if let Ok(mut inner) = state.inner.lock() {
        inner.pending.remove(id);
    }
}

/// Resolve the Vali.Host executable: `VALI_HOST_PATH` env var, else the default
/// Release/Debug build outputs relative to the app's src-tauri directory (dev)
/// or the running executable's directory (installed).
fn resolve_sidecar_path() -> PathBuf {
    if let Ok(p) = std::env::var("VALI_HOST_PATH") {
        if !p.trim().is_empty() {
            return PathBuf::from(p);
        }
    }

    // Installed layout: the sidecar is bundled next to the app executable. Check
    // the exact name first, then scan for any bundled Vali.Host* (Tauri may keep
    // a target-triple suffix on the externalBin file name).
    if let Ok(exe) = std::env::current_exe() {
        if let Some(dir) = exe.parent() {
            let sibling = dir.join(HOST_BINARY);
            if sibling.exists() {
                return sibling;
            }
            if let Ok(entries) = std::fs::read_dir(dir) {
                for entry in entries.flatten() {
                    let name = entry.file_name();
                    if is_host_candidate(&name.to_string_lossy()) && entry.path().is_file() {
                        return entry.path();
                    }
                }
            }
        }
    }

    let rel_release = [
        "..", "..", "sidecar", "Vali.Host", "bin", "Release", "net10.0", HOST_BINARY,
    ];
    let rel_debug = [
        "..", "..", "sidecar", "Vali.Host", "bin", "Debug", "net10.0", HOST_BINARY,
    ];

    let mut bases: Vec<PathBuf> = vec![PathBuf::from(env!("CARGO_MANIFEST_DIR"))];
    if let Ok(exe) = std::env::current_exe() {
        if let Some(dir) = exe.parent() {
            bases.push(dir.to_path_buf());
        }
    }

    for base in &bases {
        for rel in [&rel_release, &rel_debug] {
            let mut candidate = base.clone();
            for seg in rel.iter() {
                candidate.push(seg);
            }
            if candidate.exists() {
                return candidate;
            }
        }
    }

    // Nothing found — return the manifest-based Release path so errors are legible.
    let mut fallback = PathBuf::from(env!("CARGO_MANIFEST_DIR"));
    for seg in rel_release.iter() {
        fallback.push(seg);
    }
    fallback
}

fn handle_stdout_line(app: &AppHandle, line: &str) {
    let trimmed = line.trim();
    if trimmed.is_empty() {
        return;
    }
    let value: serde_json::Value = match serde_json::from_str(trimmed) {
        Ok(v) => v,
        Err(_) => return, // ignore non-protocol noise
    };

    // Unsolicited event → forward to the webview.
    if value.get("event").is_some() {
        let _ = app.emit("sidecar-event", value);
        return;
    }

    // Response → complete the matching pending request.
    if let Some(id) = value.get("id").and_then(|v| v.as_str()) {
        let id = id.to_string();
        if let Some(state) = app.try_state::<SidecarState>() {
            let sender = state
                .inner
                .lock()
                .ok()
                .and_then(|mut inner| inner.pending.remove(&id));
            if let Some(tx) = sender {
                let _ = tx.send(value);
            }
        }
    }
}

/// Spawn (or replace) the sidecar and wire up its stdio.
pub fn spawn_sidecar(app: &AppHandle) -> Result<(), String> {
    let exe = resolve_sidecar_path();

    let mut cmd = Command::new(&exe);
    cmd.stdin(Stdio::piped())
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .kill_on_drop(true);
    #[cfg(windows)]
    cmd.creation_flags(CREATE_NO_WINDOW);

    // On Windows the engine's default (C:\ProgramData\Vali) is writable and shared
    // with the CLI, so we leave it alone. On macOS/Linux the default resolves to a
    // non-writable location (e.g. /usr/share) which makes the engine fall back to
    // an interactive stdin prompt — fatal for our stdio protocol. Point it at the
    // per-user OS data dir instead (the engine appends "Vali"), unless the user has
    // already chosen a folder via the environment.
    #[cfg(not(windows))]
    {
        let already_set = std::env::var("VALI_DOWNLOAD_FOLDER")
            .map(|v| !v.trim().is_empty())
            .unwrap_or(false);
        if !already_set {
            match app.path().data_dir().or_else(|_| app.path().app_data_dir()) {
                Ok(dir) => {
                    cmd.env("VALI_DOWNLOAD_FOLDER", &dir);
                }
                Err(e) => {
                    // Without a writable dir the engine falls back to a
                    // non-writable default and blocks on an interactive prompt.
                    tracing::error!(target: "sidecar", "no writable data dir: {e}");
                }
            }
        }
    }

    let mut child = cmd
        .spawn()
        .map_err(|e| format!("failed to spawn sidecar {exe:?}: {e}"))?;

    let stdout = child.stdout.take().ok_or("sidecar has no stdout")?;
    let stderr = child.stderr.take().ok_or("sidecar has no stderr")?;
    let stdin = child.stdin.take().ok_or("sidecar has no stdin")?;

    let (writer_tx, mut writer_rx) = mpsc::unbounded_channel::<String>();

    let epoch = {
        let state = app.state::<SidecarState>();
        let mut inner = state.inner.lock().map_err(|_| "state poisoned")?;
        inner.child = Some(child);
        inner.writer_tx = Some(writer_tx);
        inner.pending.clear();
        inner.alive = true;
        inner.epoch = inner.epoch.wrapping_add(1);
        inner.epoch
    };

    // Writer task — owns stdin, drains the channel.
    tauri::async_runtime::spawn(async move {
        let mut stdin = stdin;
        while let Some(line) = writer_rx.recv().await {
            if stdin.write_all(line.as_bytes()).await.is_err()
                || stdin.write_all(b"\n").await.is_err()
            {
                break;
            }
            let _ = stdin.flush().await;
        }
    });

    // Reader task — parses protocol lines, routes events + responses. Read raw
    // bytes + lossy-decode so a stray non-UTF-8 byte can't be mistaken for the
    // stream ending (which would falsely mark the engine dead).
    let app_out = app.clone();
    tauri::async_runtime::spawn(async move {
        let mut reader = BufReader::new(stdout);
        let mut buf = Vec::new();
        loop {
            buf.clear();
            match reader.read_until(b'\n', &mut buf).await {
                Ok(0) => break,      // EOF → child gone
                Err(_) => break,     // genuine IO error → child gone
                Ok(_) => handle_stdout_line(&app_out, String::from_utf8_lossy(&buf).trim_end()),
            }
        }
        // stdout closed → this child is gone. Only act if we're still the
        // current child (a restart bumps the epoch), so a stale reader can't
        // mark a freshly-spawned engine dead or emit a spurious exit.
        if let Some(state) = app_out.try_state::<SidecarState>() {
            let mut orphaned: Vec<oneshot::Sender<serde_json::Value>> = Vec::new();
            let is_current = match state.inner.lock() {
                Ok(mut inner) if inner.epoch == epoch => {
                    inner.alive = false;
                    orphaned.extend(inner.pending.drain().map(|(_, tx)| tx));
                    true
                }
                _ => false,
            };
            if is_current {
                tracing::warn!(target: "sidecar", "engine stdout closed; process ended");
                // Fail in-flight requests immediately instead of waiting out
                // their timeout.
                for tx in orphaned {
                    let _ = tx.send(serde_json::json!({
                        "ok": false,
                        "error": { "message": "The engine exited unexpectedly" }
                    }));
                }
                let _ = app_out.emit("sidecar-exit", ());
            }
        }
    });

    // Stderr task — surfaces log lines to the webview and the daily log file.
    let app_err = app.clone();
    tauri::async_runtime::spawn(async move {
        let mut reader = BufReader::new(stderr);
        let mut buf = Vec::new();
        loop {
            buf.clear();
            match reader.read_until(b'\n', &mut buf).await {
                Ok(0) | Err(_) => break,
                Ok(_) => {
                    let line = String::from_utf8_lossy(&buf).trim_end().to_string();
                    if line.is_empty() {
                        continue;
                    }
                    tracing::info!(target: "sidecar", "{line}");
                    let _ = app_err.emit("sidecar-log", line);
                }
            }
        }
    });

    Ok(())
}

/// Kill the sidecar and clear pending state.
pub fn kill_sidecar(app: &AppHandle) {
    if let Some(state) = app.try_state::<SidecarState>() {
        if let Ok(mut inner) = state.inner.lock() {
            if let Some(mut child) = inner.child.take() {
                let _ = child.start_kill();
            }
            inner.writer_tx = None;
            inner.pending.clear();
            inner.alive = false;
            // Bump the epoch so the outgoing child's reader task can't take the
            // "engine exited" branch and emit a spurious exit during a restart.
            inner.epoch = inner.epoch.wrapping_add(1);
        }
    }
}

/// Send one request to the sidecar and await its response.
#[tauri::command]
pub async fn sidecar_request(
    state: State<'_, SidecarState>,
    id: String,
    cmd: String,
    params: serde_json::Value,
    timeout_ms: Option<u64>,
) -> Result<serde_json::Value, String> {
    let envelope = serde_json::json!({ "id": id, "cmd": cmd, "params": params });
    let line = serde_json::to_string(&envelope).map_err(|e| e.to_string())?;

    // Check liveness, clone the writer, and register the pending response under
    // one lock. If they were separate locks, a child that dies in between could
    // leave this request registered with no reader left to fail it — hanging for
    // the full timeout instead of erroring out promptly.
    let (writer, rx) = {
        let mut inner = state.inner.lock().map_err(|_| "state poisoned")?;
        if !inner.alive {
            return Err("Sidecar is not running".to_string());
        }
        let writer = inner
            .writer_tx
            .clone()
            .ok_or("Sidecar stdin is unavailable")?;
        let (tx, rx) = oneshot::channel();
        inner.pending.insert(id.clone(), tx);
        (writer, rx)
    };

    if writer.send(line).is_err() {
        remove_pending(&state, &id);
        return Err("Failed to write to sidecar".to_string());
    }

    let dur = std::time::Duration::from_millis(timeout_ms.unwrap_or(DEFAULT_TIMEOUT_MS));
    let value = match tokio::time::timeout(dur, rx).await {
        Ok(Ok(v)) => v,
        Ok(Err(_)) => {
            remove_pending(&state, &id);
            return Err("Sidecar closed before responding".to_string());
        }
        Err(_) => {
            remove_pending(&state, &id);
            return Err("Request timed out".to_string());
        }
    };

    let ok = value.get("ok").and_then(|v| v.as_bool()).unwrap_or(false);
    if ok {
        Ok(value
            .get("result")
            .cloned()
            .unwrap_or(serde_json::Value::Null))
    } else {
        // Return the FULL error object as a JSON string so the frontend can read
        // its `code`/`data` — {message, detail?, code?, data?}. The frontend
        // parses this; a non-JSON Err string (from the arms above) is treated as
        // a bare message.
        let error = value.get("error").cloned().unwrap_or_else(|| {
            serde_json::json!({ "message": "Unknown sidecar error" })
        });
        Err(serde_json::to_string(&error)
            .unwrap_or_else(|_| "Unknown sidecar error".to_string()))
    }
}

/// Whether the sidecar child is currently alive.
#[tauri::command]
pub fn sidecar_status(state: State<'_, SidecarState>) -> bool {
    let mut inner = match state.inner.lock() {
        Ok(g) => g,
        Err(_) => return false,
    };
    let exited = match inner.child.as_mut() {
        Some(child) => matches!(child.try_wait(), Ok(Some(_))),
        None => true,
    };
    if exited {
        inner.alive = false;
    }
    inner.alive
}

/// Kill and respawn the sidecar.
#[tauri::command]
pub async fn sidecar_restart(app: AppHandle) -> Result<(), String> {
    kill_sidecar(&app);
    spawn_sidecar(&app)
}
