// Synchronous platform detection for the webview. Used to branch UI chrome
// (native macOS traffic lights vs. custom window controls on Windows/Linux).

const ua =
  typeof navigator !== "undefined"
    ? navigator.platform || navigator.userAgent
    : "";

export const IS_MAC = /mac|iphone|ipad/i.test(ua);
export const IS_WINDOWS = /win/i.test(ua);
