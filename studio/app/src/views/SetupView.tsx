import { useCallback, useEffect, useRef, useState } from "react";
import { useStore } from "../store";
import { IS_MAC } from "../lib/platform";
import { CompassIcon } from "../components/Icons";

const STARTUP_GRACE_MS = 8000;

/** Shown until the bundled engine first responds. The engine ships inside the
 *  app, so there's nothing to install — this only covers the rare case where it
 *  fails to launch (most often macOS Gatekeeper on an unsigned first run). */
export function SetupView() {
  const logs = useStore((s) => s.logs);
  const [timedOut, setTimedOut] = useState(false);
  const [restarting, setRestarting] = useState(false);
  const timerRef = useRef<number | undefined>(undefined);

  const armGraceTimer = useCallback(() => {
    window.clearTimeout(timerRef.current);
    timerRef.current = window.setTimeout(
      () => setTimedOut(true),
      STARTUP_GRACE_MS,
    );
  }, []);

  useEffect(() => {
    armGraceTimer();
    return () => window.clearTimeout(timerRef.current);
  }, [armGraceTimer]);

  const restart = async () => {
    setRestarting(true);
    setTimedOut(false);
    try {
      await useStore.getState().restartEngine();
    } catch {
      // the liveness poll reconciles; show troubleshooting again if it stays down
    } finally {
      setRestarting(false);
      armGraceTimer();
    }
  };

  const tail = logs.slice(-24);

  return (
    <div className="setup-view">
      <div className="setup-card">
        <div className="setup-mark">
          <CompassIcon size={30} />
        </div>
        <div className="setup-title">
          {timedOut
            ? "The Vali engine didn't start"
            : "Starting the Vali engine…"}
        </div>

        {!timedOut ? (
          <div className="setup-status">
            <span className="spinner" aria-hidden />
            <span className="hint">
              Getting things ready — this only takes a moment.
            </span>
          </div>
        ) : (
          <>
            <p className="setup-msg">
              Vali Studio bundles its own engine, so there&apos;s nothing to
              install — but it couldn&apos;t be started this time.
            </p>

            {IS_MAC && (
              <div className="setup-note">
                <strong>On macOS</strong>, the first launch can be blocked by
                Gatekeeper. Open <strong>Applications</strong>, right-click{" "}
                <strong>Vali Studio</strong>, choose <strong>Open</strong>, and
                confirm. After that it starts normally.
              </div>
            )}

            <div className="setup-actions">
              <button
                className="primary"
                onClick={() => void restart()}
                disabled={restarting}
              >
                {restarting ? "Restarting…" : "Retry"}
              </button>
            </div>

            {tail.length > 0 && (
              <details className="setup-logs">
                <summary>Engine log</summary>
                <pre>{tail.join("\n")}</pre>
              </details>
            )}
          </>
        )}
      </div>
    </div>
  );
}
