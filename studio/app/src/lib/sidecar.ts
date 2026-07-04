import { invoke } from "@tauri-apps/api/core";
import { listen, type UnlistenFn } from "@tauri-apps/api/event";
import type { SidecarEvent } from "./types";

/** Default request timeout; `generate` runs can last minutes. */
export const DEFAULT_TIMEOUT_MS = 600_000;

export function newRequestId(): string {
  return crypto.randomUUID();
}

/**
 * Send one NDJSON request to the sidecar and await its response.
 * The Rust side registers a pending oneshot under `id`, writes the line to
 * the child's stdin, and resolves when the matching response arrives.
 * Rejects (throws) on `ok:false` — the message is the sidecar's error.
 */
export async function request<T>(
  cmd: string,
  params: unknown,
  id: string = newRequestId(),
  timeoutMs: number = DEFAULT_TIMEOUT_MS,
): Promise<T> {
  return invoke<T>("sidecar_request", { id, cmd, params, timeoutMs });
}

export function sidecarStatus(): Promise<boolean> {
  return invoke<boolean>("sidecar_status");
}

export function sidecarRestart(): Promise<void> {
  return invoke<void>("sidecar_restart");
}

/** Subscribe to protocol events ({event,id,data}) forwarded from the sidecar. */
export function listenSidecarEvents(
  handler: (evt: SidecarEvent) => void,
): Promise<UnlistenFn> {
  return listen<SidecarEvent>("sidecar-event", (e) => handler(e.payload));
}

/** Subscribe to raw stderr log lines from the sidecar. */
export function listenSidecarLog(
  handler: (line: string) => void,
): Promise<UnlistenFn> {
  return listen<string>("sidecar-log", (e) => handler(e.payload));
}

/** Fires when the sidecar process exits. */
export function listenSidecarExit(
  handler: (info: unknown) => void,
): Promise<UnlistenFn> {
  return listen("sidecar-exit", (e) => handler(e.payload));
}
