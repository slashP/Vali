// Thin wrapper over the Tauri window API for the custom titlebar. Native
// decorations are off (see tauri.conf.json), so minimize/maximize/close and
// edge resizing are driven from the UI.
import { getCurrentWindow } from "@tauri-apps/api/window";
import type { UnlistenFn } from "@tauri-apps/api/event";

// `ResizeDirection` isn't re-exported by @tauri-apps/api/window, so mirror its
// string-literal union here (structurally identical → assignable to the API).
export type ResizeDirection =
  | "East"
  | "North"
  | "NorthEast"
  | "NorthWest"
  | "South"
  | "SouthEast"
  | "SouthWest"
  | "West";

const appWindow = getCurrentWindow();

export function minimizeWindow(): Promise<void> {
  return appWindow.minimize();
}

export function toggleMaximizeWindow(): Promise<void> {
  return appWindow.toggleMaximize();
}

export function closeWindow(): Promise<void> {
  return appWindow.close();
}

/** Destroy the window without firing another close-requested event. */
export function destroyWindow(): Promise<void> {
  return appWindow.destroy();
}

/** Runs `handler` when a window close is requested (button, Cmd+Q, traffic
 *  light, OS). The default close is prevented so the handler can flush first;
 *  it is responsible for calling `destroyWindow()` when done. */
export function onCloseRequested(
  handler: () => void | Promise<void>,
): Promise<UnlistenFn> {
  return appWindow.onCloseRequested((event) => {
    event.preventDefault();
    void handler();
  });
}

export function isWindowMaximized(): Promise<boolean> {
  return appWindow.isMaximized();
}

/** Fires whenever the window is resized (maximize/restore/drag-resize). */
export function onWindowResized(handler: () => void): Promise<UnlistenFn> {
  return appWindow.onResized(() => handler());
}

/** Begin a native resize drag from an edge/corner handle. */
export function startResize(direction: ResizeDirection): Promise<void> {
  return appWindow.startResizeDragging(direction);
}
