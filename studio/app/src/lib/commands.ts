import { invoke } from "@tauri-apps/api/core";

// ---- file system --------------------------------------------------------

export function readTextFile(path: string): Promise<string> {
  return invoke<string>("read_text_file", { path });
}

export function writeTextFile(path: string, content: string): Promise<void> {
  return invoke<void>("write_text_file", { path, content });
}

/** Native open dialog. Returns the chosen path, or null if cancelled. */
export function pickOpenFile(
  filterName: string,
  extensions: string[],
): Promise<string | null> {
  return invoke<string | null>("pick_open_file", { filterName, extensions });
}

/** Native save dialog. Returns the chosen path, or null if cancelled. */
export function pickSaveFile(defaultName: string): Promise<string | null> {
  return invoke<string | null>("pick_save_file", { defaultName });
}

// ---- AI copilot ---------------------------------------------------------

/** True if the `claude` CLI resolves on PATH. */
export function aiAvailable(): Promise<boolean> {
  return invoke<boolean>("ai_available");
}

/**
 * Ask the local Claude Code CLI to apply `instruction` to `definitionJson`.
 * Returns the edited JSON document (raw string, not yet validated).
 */
export function aiEdit(
  instruction: string,
  definitionJson: string,
): Promise<string> {
  return invoke<string>("ai_edit", { instruction, definitionJson });
}
