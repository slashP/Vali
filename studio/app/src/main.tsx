import { Component, StrictMode, type ErrorInfo, type ReactNode } from "react";
import { createRoot } from "react-dom/client";
import { setupMonaco } from "./lib/monaco";
import { logMessage } from "./lib/log";
import { initZoom } from "./lib/zoom";
import App from "./App";
import "maplibre-gl/dist/maplibre-gl.css";
import "./styles.css";

interface BoundaryState {
  error: Error | null;
}

/**
 * Last-resort boundary so a render exception can never blank the whole app.
 * Shows the error and a Reload button.
 */
class ErrorBoundary extends Component<{ children: ReactNode }, BoundaryState> {
  state: BoundaryState = { error: null };

  static getDerivedStateFromError(error: Error): BoundaryState {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error("Vali Studio crashed:", error, info);
    logMessage(
      "error",
      `React crash: ${error.message}\n${error.stack ?? ""}\ncomponentStack:${info.componentStack ?? ""}`,
    );
  }

  render() {
    if (this.state.error) {
      return (
        <div className="crash-card">
          <div className="crash-title">Something went wrong</div>
          <pre className="crash-detail">{this.state.error.message}</pre>
          <button className="primary" onClick={() => window.location.reload()}>
            Reload
          </button>
        </div>
      );
    }
    return this.props.children;
  }
}

// Capture everything that escapes React so a field crash always leaves a trail.
window.addEventListener("error", (e) => {
  logMessage(
    "error",
    `uncaught: ${e.message} @ ${e.filename}:${e.lineno}:${e.colno}${
      e.error?.stack ? `\n${e.error.stack}` : ""
    }`,
  );
});
window.addEventListener("unhandledrejection", (e) => {
  const r = e.reason;
  const detail =
    r instanceof Error ? `${r.message}\n${r.stack ?? ""}` : String(r);
  logMessage("error", `unhandledrejection: ${detail}`);
});

setupMonaco();
initZoom();

const rootEl = document.getElementById("root");
if (!rootEl) throw new Error("Root element #root not found");

createRoot(rootEl).render(
  <StrictMode>
    <ErrorBoundary>
      <App />
    </ErrorBoundary>
  </StrictMode>,
);
