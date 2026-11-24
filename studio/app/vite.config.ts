import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// Tauri expects a fixed dev server on 5173 and hands the frontend TAURI_* env vars.
// https://v2.tauri.app/start/frontend/vite/
export default defineConfig({
  plugins: [react()],
  // Prevent Vite from obscuring Rust errors.
  clearScreen: false,
  server: {
    port: 5173,
    strictPort: true,
    host: "127.0.0.1",
    watch: {
      // Rust sources are watched by cargo, not Vite.
      ignored: ["**/src-tauri/**"],
    },
  },
  // Env vars starting with these prefixes are exposed to the frontend.
  envPrefix: ["VITE_", "TAURI_ENV_", "TAURI_PLATFORM"],
  build: {
    // Chromium/WebView2 target — modern evergreen.
    target: "es2022",
    sourcemap: true,
    chunkSizeWarningLimit: 4096,
  },
});
