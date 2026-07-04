use tauri::AppHandle;
use tauri_plugin_dialog::DialogExt;
use tokio::sync::oneshot;

#[tauri::command]
pub fn read_text_file(path: String) -> Result<String, String> {
    std::fs::read_to_string(&path).map_err(|e| format!("{path}: {e}"))
}

#[tauri::command]
pub fn write_text_file(path: String, content: String) -> Result<(), String> {
    std::fs::write(&path, content).map_err(|e| format!("{path}: {e}"))
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
