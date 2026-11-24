mod ai;
mod fs_cmds;
mod logging;
mod sidecar;

use tauri::Manager;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_dialog::init())
        .manage(sidecar::SidecarState::default())
        .manage(ai::AiState::default())
        .invoke_handler(tauri::generate_handler![
            sidecar::sidecar_request,
            sidecar::sidecar_status,
            sidecar::sidecar_restart,
            ai::ai_providers,
            ai::ai_run,
            ai::ai_cancel,
            logging::log_message,
            fs_cmds::read_text_file,
            fs_cmds::write_text_file,
            fs_cmds::ensure_dir,
            fs_cmds::remove_path,
            fs_cmds::list_dir,
            fs_cmds::workspace_root,
            fs_cmds::reveal_in_explorer,
            fs_cmds::open_external,
            fs_cmds::pick_open_file,
            fs_cmds::pick_save_file,
        ])
        .setup(|app| {
            // Start file logging first so everything after it (incl. a failed
            // engine spawn or a later panic) lands in the daily log.
            if let Ok(dir) = app.path().app_log_dir() {
                logging::init(dir);
            }
            let handle = app.handle().clone();
            if let Err(e) = sidecar::spawn_sidecar(&handle) {
                tracing::error!(target: "sidecar", "spawn failed: {e}");
            }
            Ok(())
        })
        .on_window_event(|window, event| {
            if let tauri::WindowEvent::Destroyed = event {
                sidecar::shutdown_sidecar(window.app_handle());
            }
        })
        .build(tauri::generate_context!())
        .expect("error while building Vali Studio")
        .run(|app_handle, event| {
            if let tauri::RunEvent::Exit = event {
                sidecar::shutdown_sidecar(app_handle);
            }
        });
}
