use std::time::UNIX_EPOCH;

use serde::Serialize;
use tauri::{AppHandle, Manager};
use tauri_plugin_dialog::DialogExt;
use tokio::sync::oneshot;

#[cfg(windows)]
const CREATE_NO_WINDOW: u32 = 0x0800_0000;

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
pub struct DirEntryInfo {
    name: String,
    is_dir: bool,
    mtime_ms: f64,
    bytes: u64,
}

#[tauri::command]
pub fn read_text_file(path: String) -> Result<String, String> {
    let text = std::fs::read_to_string(&path).map_err(|e| format!("{path}: {e}"))?;
    // Strip a leading UTF-8 BOM so imported/edited definitions match what the
    // .NET engine sees — File.ReadAllText drops the BOM, but a BOM left in the
    // string makes the engine's JsonNode.Parse throw, wrongly failing validation
    // for a file the CLI accepts. (PowerShell 5 / Notepad write a BOM by default.)
    Ok(text.strip_prefix('\u{feff}').map(str::to_owned).unwrap_or(text))
}

#[tauri::command]
pub fn write_text_file(path: String, content: String) -> Result<(), String> {
    std::fs::write(&path, content).map_err(|e| format!("{path}: {e}"))
}

#[tauri::command]
pub fn ensure_dir(path: String) -> Result<(), String> {
    std::fs::create_dir_all(&path).map_err(|e| format!("{path}: {e}"))
}

/// True if `target` resolves to a location inside `base` (symlink/`..`-safe).
fn is_within(base: &std::path::Path, target: &std::path::Path) -> bool {
    let base = base.canonicalize().unwrap_or_else(|_| base.to_path_buf());
    match target.canonicalize() {
        Ok(resolved) => resolved.starts_with(&base),
        Err(_) => target.starts_with(&base),
    }
}

/// Remove a file, or a directory recursively — restricted to the managed
/// workspace (app data). This command only ever deletes a map folder, so
/// refusing paths outside app data keeps a recursive delete from being turned
/// against arbitrary user files.
#[tauri::command]
pub fn remove_path(app: AppHandle, path: String) -> Result<(), String> {
    let base = app.path().app_data_dir().map_err(|e| e.to_string())?;
    let target = std::path::Path::new(&path);
    if !is_within(&base, target) {
        return Err(format!("Refusing to remove a path outside the workspace: {path}"));
    }
    if target.is_dir() {
        std::fs::remove_dir_all(target).map_err(|e| format!("{path}: {e}"))
    } else {
        std::fs::remove_file(target).map_err(|e| format!("{path}: {e}"))
    }
}

/// List a directory's immediate entries (files and folders) with size + mtime.
#[tauri::command]
pub fn list_dir(path: String) -> Result<Vec<DirEntryInfo>, String> {
    let mut out = Vec::new();
    let read = std::fs::read_dir(&path).map_err(|e| format!("{path}: {e}"))?;
    for entry in read.flatten() {
        let Ok(meta) = entry.metadata() else { continue };
        let mtime_ms = meta
            .modified()
            .ok()
            .and_then(|t| t.duration_since(UNIX_EPOCH).ok())
            .map(|d| d.as_millis() as f64)
            .unwrap_or(0.0);
        out.push(DirEntryInfo {
            name: entry.file_name().to_string_lossy().into_owned(),
            is_dir: meta.is_dir(),
            mtime_ms,
            bytes: if meta.is_dir() { 0 } else { meta.len() },
        });
    }
    Ok(out)
}

/// The managed workspace root: `<appDataDir>/maps` (created if missing).
#[tauri::command]
pub fn workspace_root(app: AppHandle) -> Result<String, String> {
    let dir = app.path().app_data_dir().map_err(|e| e.to_string())?;
    let maps = dir.join("maps");
    std::fs::create_dir_all(&maps).map_err(|e| format!("{}: {e}", maps.display()))?;
    Ok(maps.to_string_lossy().into_owned())
}

/// Open the OS file manager with `path` selected (Windows Explorer / macOS
/// Finder / Linux file manager).
#[tauri::command]
pub fn reveal_in_explorer(path: String) -> Result<(), String> {
    // Each arm is a mutually-exclusive tail expression, so exactly one survives
    // cfg-expansion per target and becomes the function's return value.
    #[cfg(windows)]
    {
        use std::os::windows::process::CommandExt;
        // raw_arg keeps the `/select,"..."` shape Explorer expects, even with spaces.
        std::process::Command::new("explorer")
            .raw_arg(format!("/select,\"{path}\""))
            .creation_flags(CREATE_NO_WINDOW)
            .spawn()
            .map_err(|e| e.to_string())?;
        Ok(())
    }
    #[cfg(target_os = "macos")]
    {
        // -R reveals the file, selected, in Finder.
        std::process::Command::new("open")
            .arg("-R")
            .arg(&path)
            .spawn()
            .map_err(|e| e.to_string())?;
        Ok(())
    }
    #[cfg(all(unix, not(target_os = "macos")))]
    {
        // xdg-open can't select a specific file, so open the containing folder.
        let dir = std::path::Path::new(&path)
            .parent()
            .map(|p| p.to_path_buf())
            .unwrap_or_else(|| std::path::PathBuf::from(&path));
        std::process::Command::new("xdg-open")
            .arg(dir)
            .spawn()
            .map_err(|e| e.to_string())?;
        Ok(())
    }
    #[cfg(not(any(windows, unix)))]
    {
        let _ = &path;
        Err("Revealing files is not supported on this platform".to_string())
    }
}

/// Open a web URL in the user's default browser. Native decorations are off and
/// no opener/shell plugin is installed, so plain `target="_blank"` anchors do
/// nothing on macOS and spawn a chrome-less WebView2 popup on Windows — the app
/// routes external links through here instead.
#[tauri::command]
pub fn open_external(url: String) -> Result<(), String> {
    // Only ever hand the OS a web/mail URL, and reject quotes, so this can't be
    // coerced into launching an arbitrary command or file from the webview.
    let is_web = url.starts_with("https://")
        || url.starts_with("http://")
        || url.starts_with("mailto:");
    if !is_web || url.contains('"') {
        return Err(format!("Refusing to open non-web URL: {url}"));
    }

    #[cfg(windows)]
    {
        use std::os::windows::process::CommandExt;
        // `start "" "<url>"` opens the default browser; the quotes keep `&` in
        // query strings from being read as a cmd separator.
        std::process::Command::new("cmd")
            .raw_arg(format!("/C start \"\" \"{url}\""))
            .creation_flags(CREATE_NO_WINDOW)
            .spawn()
            .map_err(|e| e.to_string())?;
        Ok(())
    }
    #[cfg(target_os = "macos")]
    {
        std::process::Command::new("open")
            .arg(&url)
            .spawn()
            .map_err(|e| e.to_string())?;
        Ok(())
    }
    #[cfg(all(unix, not(target_os = "macos")))]
    {
        std::process::Command::new("xdg-open")
            .arg(&url)
            .spawn()
            .map_err(|e| e.to_string())?;
        Ok(())
    }
    #[cfg(not(any(windows, unix)))]
    {
        let _ = &url;
        Err("Opening links is not supported on this platform".to_string())
    }
}

/// Native open dialog. Returns the chosen absolute path, or `None` if cancelled.
#[tauri::command]
pub async fn pick_open_file(
    app: AppHandle,
    filter_name: String,
    extensions: Vec<String>,
) -> Option<String> {
    let exts: Vec<&str> = extensions.iter().map(String::as_str).collect();
    let (tx, rx) = oneshot::channel();
    app.dialog()
        .file()
        .add_filter(filter_name, &exts)
        .pick_file(move |picked| {
            let _ = tx.send(picked);
        });
    match rx.await {
        Ok(Some(path)) => path.into_path().ok().map(|p| p.to_string_lossy().into_owned()),
        _ => None,
    }
}

/// Native save dialog. Returns the chosen absolute path, or `None` if cancelled.
#[tauri::command]
pub async fn pick_save_file(app: AppHandle, default_name: String) -> Option<String> {
    let (tx, rx) = oneshot::channel();
    app.dialog()
        .file()
        .set_file_name(default_name)
        .save_file(move |picked| {
            let _ = tx.send(picked);
        });
    match rx.await {
        Ok(Some(path)) => path.into_path().ok().map(|p| p.to_string_lossy().into_owned()),
        _ => None,
    }
}
