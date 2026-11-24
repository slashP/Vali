// Daily-rotating file logging for production diagnostics. Errors (including
// panics/crashes) from the Rust host, the .NET engine's stderr, and the webview
// all funnel into one daily file under the OS log directory, so a crash in the
// field leaves a trail for debugging.

use std::path::PathBuf;
use std::sync::OnceLock;

use tracing_appender::non_blocking::WorkerGuard;

// The non-blocking writer's guard must live for the whole process, otherwise
// buffered lines are dropped on exit. Held here for the app's lifetime.
static GUARD: OnceLock<WorkerGuard> = OnceLock::new();

/// Initialize daily file logging + a panic hook. Idempotent; safe to call once
/// at startup. `log_dir` is created if missing.
pub fn init(log_dir: PathBuf) {
    if GUARD.get().is_some() {
        return;
    }
    let _ = std::fs::create_dir_all(&log_dir);

    // One file per day: vali-studio.log.YYYY-MM-DD
    let appender = tracing_appender::rolling::daily(&log_dir, "vali-studio.log");
    let (writer, guard) = tracing_appender::non_blocking(appender);
    let _ = GUARD.set(guard);

    let subscriber = tracing_subscriber::fmt()
        .with_writer(writer)
        .with_ansi(false)
        .with_target(true)
        .finish();
    let _ = tracing::subscriber::set_global_default(subscriber);

    install_panic_hook();
    tracing::info!(target: "app", "Vali Studio logging started ({})", log_dir.display());
}

/// Log panics with a backtrace before the default hook runs, so an unexpected
/// crash is always captured to the daily file.
fn install_panic_hook() {
    let previous = std::panic::take_hook();
    std::panic::set_hook(Box::new(move |info| {
        let location = info
            .location()
            .map(|l| format!("{}:{}", l.file(), l.line()))
            .unwrap_or_else(|| "unknown".to_string());
        let message = info
            .payload()
            .downcast_ref::<&str>()
            .map(|s| s.to_string())
            .or_else(|| info.payload().downcast_ref::<String>().cloned())
            .unwrap_or_else(|| "non-string panic payload".to_string());
        let backtrace = std::backtrace::Backtrace::force_capture();
        tracing::error!(target: "panic", "panic at {location}: {message}\n{backtrace}");
        previous(info);
    }));
}

/// Append a message from the webview (uncaught errors, unhandled rejections,
/// React error boundary, surfaced store errors) to the same daily log.
#[tauri::command]
pub fn log_message(level: String, message: String) {
    match level.as_str() {
        "error" => tracing::error!(target: "frontend", "{message}"),
        "warn" => tracing::warn!(target: "frontend", "{message}"),
        _ => tracing::info!(target: "frontend", "{message}"),
    }
}
