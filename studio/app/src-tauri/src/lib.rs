mod ai;
mod fs_cmds;
mod sidecar;

use tauri::Manager;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_dialog::init())
        .manage(sidecar::SidecarState::default())
        .invoke_handler(tauri::generate_handler![
            sidecar::sidecar_request,
            sidecar::sidecar_status,
            sidecar::sidecar_restart,
            ai::ai_available,
            ai::ai_edit,
            fs_cmds::read_text_file,
            fs_cmds::write_text_file,
            fs_cmds::pick_open_file,
            fs_cmds::pick_save_file,
        ])
        .setup(|app| {
            // TODO: set a real Content-Security-Policy before shipping — CSP is
            // null while the app is dev-stage (see tauri.conf.json app.security).
            let handle = app.handle().clone();
            if let Err(e) = sidecar::spawn_sidecar(&handle) {
                eprintln!("[vali-studio] sidecar spawn failed: {e}");
            }
            Ok(())
        })
        .on_window_event(|window, event| {
            if let tauri::WindowEvent::Destroyed = event {
                sidecar::kill_sidecar(window.app_handle());
            }
        })
        .build(tauri::generate_context!())
        .expect("error while building Vali Studio")
        .run(|app_handle, event| {
            if let tauri::RunEvent::Exit = event {
                sidecar::kill_sidecar(app_handle);
            }
        });
}
