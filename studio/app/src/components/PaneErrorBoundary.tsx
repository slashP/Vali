import { Component, type ErrorInfo, type ReactNode } from "react";
import { logMessage } from "../lib/log";

interface Props {
  label: string;
  children: ReactNode;
}
interface State {
  error: Error | null;
}

/**
 * Contains a render failure to a single pane (editor / map / docs) instead of
 * letting it tear down the whole app. The heavy third-party surfaces (Monaco,
 * maplibre-gl, deck.gl) are the likeliest to throw, and a whole-app reload would
 * drop unsaved in-memory state.
 */
export class PaneErrorBoundary extends Component<Props, State> {
  state: State = { error: null };

  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    logMessage(
      "error",
      `Pane "${this.props.label}" crashed: ${error.message}\n${error.stack ?? ""}\ncomponentStack:${info.componentStack ?? ""}`,
    );
  }

  render() {
    if (this.state.error) {
      return (
        <div className="pane-error">
          <div className="pane-error-title">
            The {this.props.label} hit a problem
          </div>
          <pre className="pane-error-detail">{this.state.error.message}</pre>
          <button
            className="primary"
            onClick={() => this.setState({ error: null })}
          >
            Try again
          </button>
        </div>
      );
    }
    return this.props.children;
  }
}
