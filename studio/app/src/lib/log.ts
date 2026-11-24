// Forwards webview errors to the Rust host's daily log file. Best-effort and
// never throws — logging must never itself break the app.

import { invoke } from "@tauri-apps/api/core";

type Level = "error" | "warn" | "info";

export function logMessage(level: Level, message: string): void {
  void invoke("log_message", { level, message }).catch(() => {
    // Tauri bridge unavailable (e.g. very early startup) — drop silently.
  });
}

/** Log an error with a short context label, unwrapping Error message + stack. */
export function logError(context: string, err: unknown): void {
  const detail =
    err instanceof Error
      ? `${err.message}${err.stack ? `\n${err.stack}` : ""}`
      : String(err);
  logMessage("error", `[${context}] ${detail}`);
}
