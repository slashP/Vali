import { invoke } from "@tauri-apps/api/core";
import type { DirEntryInfo } from "./types";

// ---- file system --------------------------------------------------------

export function readTextFile(path: string): Promise<string> {
  return invoke<string>("read_text_file", { path });
}

export function writeTextFile(path: string, content: string): Promise<void> {
  return invoke<void>("write_text_file", { path, content });
}

export function ensureDir(path: string): Promise<void> {
  return invoke<void>("ensure_dir", { path });
}

/** Remove a file, or a directory recursively. */
export function removePath(path: string): Promise<void> {
  return invoke<void>("remove_path", { path });
}

export function listDir(path: string): Promise<DirEntryInfo[]> {
  return invoke<DirEntryInfo[]>("list_dir", { path });
}

/** The managed workspace root (`<appDataDir>/maps`), created if missing. */
export function workspaceRoot(): Promise<string> {
  return invoke<string>("workspace_root");
}

/** Open the OS file manager with `path` selected. */
export function revealInExplorer(path: string): Promise<void> {
  return invoke<void>("reveal_in_explorer", { path });
}

/** Open a web URL in the user's default browser. */
export function openExternal(url: string): Promise<void> {
  return invoke<void>("open_external", { url });
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

export interface AiProvider {
  id: string;
  label: string;
  available: boolean;
}

/** The local AI CLIs Vali knows about, and whether each is installed. */
export function aiProviders(): Promise<AiProvider[]> {
  return invoke<AiProvider[]>("ai_providers");
}

/**
 * Run the chosen local AI CLI with a fully-built prompt. Returns the model's
 * result (expected to be a complete JSON document, not yet validated).
 */
export function aiRun(provider: string, prompt: string): Promise<string> {
  return invoke<string>("ai_run", { provider, prompt });
}

/** Abort the in-flight AI request (kills the local claude process). */
export function aiCancel(): Promise<void> {
  return invoke<void>("ai_cancel");
}
