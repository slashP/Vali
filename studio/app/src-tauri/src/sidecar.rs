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

    let rel_release = [
        "..", "..", "sidecar", "Vali.Host", "bin", "Release", "net10.0", "Vali.Host.exe",
    ];
    let rel_debug = [
        "..", "..", "sidecar", "Vali.Host", "bin", "Debug", "net10.0", "Vali.Host.exe",
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

    let mut child = cmd
        .spawn()
        .map_err(|e| format!("failed to spawn sidecar {exe:?}: {e}"))?;

    let stdout = child.stdout.take().ok_or("sidecar has no stdout")?;
    let stderr = child.stderr.take().ok_or("sidecar has no stderr")?;
    let stdin = child.stdin.take().ok_or("sidecar has no stdin")?;

    let (writer_tx, mut writer_rx) = mpsc::unbounded_channel::<String>();

    {
        let state = app.state::<SidecarState>();
        let mut inner = state.inner.lock().map_err(|_| "state poisoned")?;
        inner.child = Some(child);
        inner.writer_tx = Some(writer_tx);
        inner.pending.clear();
        inner.alive = true;
    }

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

    // Reader task — parses protocol lines, routes events + responses.
    let app_out = app.clone();
    tauri::async_runtime::spawn(async move {
        let mut lines = BufReader::new(stdout).lines();
        while let Ok(Some(line)) = lines.next_line().await {
            handle_stdout_line(&app_out, &line);
        }
        // stdout closed → the process is gone.
        if let Some(state) = app_out.try_state::<SidecarState>() {
            if let Ok(mut inner) = state.inner.lock() {
                inner.alive = false;
            }
        }
        let _ = app_out.emit("sidecar-exit", ());
    });

    // Stderr task — surfaces log lines to the webview.
    let app_err = app.clone();
    tauri::async_runtime::spawn(async move {
        let mut lines = BufReader::new(stderr).lines();
        while let Ok(Some(line)) = lines.next_line().await {
            let _ = app_err.emit("sidecar-log", line);
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

    let writer = {
        let inner = state.inner.lock().map_err(|_| "state poisoned")?;
        if !inner.alive {
            return Err("Sidecar is not running".to_string());
        }
        inner.writer_tx.clone()
    };
    let writer = writer.ok_or("Sidecar stdin is unavailable")?;

    let (tx, rx) = oneshot::channel();
    {
        let mut inner = state.inner.lock().map_err(|_| "state poisoned")?;
        inner.pending.insert(id.clone(), tx);
    }

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
        let message = value
            .get("error")
            .and_then(|e| e.get("message"))
            .and_then(|m| m.as_str())
            .unwrap_or("Unknown sidecar error")
            .to_string();
        Err(message)
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
