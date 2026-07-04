use std::process::Stdio;

use tokio::io::AsyncWriteExt;
use tokio::process::Command;

#[cfg(windows)]
const CREATE_NO_WINDOW: u32 = 0x0800_0000;

const AI_TIMEOUT_SECS: u64 = 300;

/// Locate the `claude` CLI on PATH. Returns the resolved path (the first line
/// of `where`/`which`), which may be a `.cmd` shim on Windows.
fn resolve_claude() -> Option<String> {
    #[cfg(windows)]
    let (prog, arg) = ("where", "claude");
    #[cfg(not(windows))]
    let (prog, arg) = ("which", "claude");

    let mut cmd = std::process::Command::new(prog);
    cmd.arg(arg);
    #[cfg(windows)]
    {
        use std::os::windows::process::CommandExt;
        cmd.creation_flags(CREATE_NO_WINDOW);
    }

    let output = cmd.output().ok()?;
    if !output.status.success() {
        return None;
    }
    let stdout = String::from_utf8_lossy(&output.stdout);
    let first = stdout.lines().next()?.trim().to_string();
    if first.is_empty() {
        None
    } else {
        Some(first)
    }
}

fn build_prompt(instruction: &str, definition_json: &str) -> String {
    format!(
        "You are editing a Vali map-definition JSON file (Vali generates GeoGuessr maps).
Apply this instruction to the JSON and return ONLY the complete edited JSON document, no explanations, no markdown fences.
Preserve all existing properties you are not asked to change. Preserve JSON comments if present.

INSTRUCTION: {instruction}

CURRENT JSON:
{definition_json}
"
    )
}

/// True if the `claude` CLI resolves on PATH.
#[tauri::command]
pub fn ai_available() -> bool {
    resolve_claude().is_some()
}

/// Ask the local Claude Code CLI to apply `instruction` to `definition_json`.
/// Returns the edited JSON (the `result` field of `claude`'s JSON output, or
/// the raw stdout if that cannot be parsed).
#[tauri::command]
pub async fn ai_edit(instruction: String, definition_json: String) -> Result<String, String> {
    let claude = resolve_claude().ok_or("claude CLI not found on PATH")?;
    let prompt = build_prompt(&instruction, &definition_json);

    // On Windows `claude` may be a `.cmd` shim, so invoke through `cmd /C`.
    let mut command = if cfg!(windows) {
        let mut c = Command::new("cmd");
        c.arg("/C").arg(&claude);
        c
    } else {
        Command::new(&claude)
    };
    command
        .arg("-p")
        .arg("--output-format")
        .arg("json")
        .stdin(Stdio::piped())
        .stdout(Stdio::piped())
        .stderr(Stdio::piped());
    #[cfg(windows)]
    command.creation_flags(CREATE_NO_WINDOW);

    let mut child = command
        .spawn()
        .map_err(|e| format!("failed to launch claude: {e}"))?;

    {
        let mut stdin = child.stdin.take().ok_or("claude has no stdin")?;
        stdin
            .write_all(prompt.as_bytes())
            .await
            .map_err(|e| e.to_string())?;
        stdin.shutdown().await.ok();
    }

    let dur = std::time::Duration::from_secs(AI_TIMEOUT_SECS);
    let output = tokio::time::timeout(dur, child.wait_with_output())
        .await
        .map_err(|_| "claude timed out".to_string())?
        .map_err(|e| e.to_string())?;

    if !output.status.success() {
        let stderr = String::from_utf8_lossy(&output.stderr);
        return Err(format!("claude exited with an error: {}", stderr.trim()));
    }

    let stdout = String::from_utf8_lossy(&output.stdout).into_owned();
    match serde_json::from_str::<serde_json::Value>(stdout.trim()) {
        Ok(value) => match value.get("result").and_then(|r| r.as_str()) {
            Some(result) => Ok(result.to_string()),
            None => Ok(stdout),
        },
        Err(_) => Ok(stdout),
    }
}
