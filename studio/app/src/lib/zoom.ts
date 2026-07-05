// App-wide UI zoom (like browser zoom). Uniformly scales the whole webview so
// text/controls get bigger or smaller — handy on very high-DPI displays.

import { getCurrentWebview } from "@tauri-apps/api/webview";

const LS = "vali.zoom";
const MIN = 0.5;
const MAX = 2.0;
const STEP = 0.1;

function clamp(z: number): number {
  return Math.min(MAX, Math.max(MIN, Math.round(z * 100) / 100));
}

let current = clamp(Number(localStorage.getItem(LS)) || 1);

function apply(): void {
  void getCurrentWebview()
    .setZoom(current)
    .catch(() => {
      // setZoom needs the webview:allow-set-webview-zoom permission; ignore if
      // unavailable so zoom is a no-op rather than a crash.
    });
}

/** Apply any persisted zoom at startup. */
export function initZoom(): void {
  if (current !== 1) apply();
}

export function zoomBy(delta: number): void {
  current = clamp(current + delta);
  localStorage.setItem(LS, String(current));
  apply();
}

export function zoomIn(): void {
  zoomBy(STEP);
}
export function zoomOut(): void {
  zoomBy(-STEP);
}
export function zoomReset(): void {
  current = 1;
  localStorage.setItem(LS, "1");
  apply();
}
