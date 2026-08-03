import { useEffect, useRef } from "react";
import { useStore } from "./store";
import { Sidebar } from "./components/Sidebar";
import { StatusBar } from "./components/StatusBar";
import { TitleBar, ResizeHandles } from "./components/TitleBar";
import { Modal } from "./components/Modal";
import { DataSheet } from "./components/DataSheet";
import { UpdateBanner } from "./components/UpdateBanner";
import { Workspace } from "./views/Workspace";
import { SetupView } from "./views/SetupView";
import {
  listenSidecarEvents,
  listenSidecarExit,
  listenSidecarLog,
  request,
  sidecarStatus,
} from "./lib/sidecar";
import { installKeybindings } from "./lib/keybindings";
import { destroyWindow, onCloseRequested } from "./lib/window";
import { logError } from "./lib/log";
import { IS_MAC } from "./lib/platform";
import { formatBytes } from "./lib/format";
import type { PingResult } from "./lib/types";

function MissingDataModal() {
  const missing = useStore((s) => s.missingData);
  if (!missing) return null;
  const knownBytes = missing.filter(
    (c): c is typeof c & { estimatedBytes: number } =>
      typeof c.estimatedBytes === "number",
  );
  const totalBytes = knownBytes.reduce((a, c) => a + c.estimatedBytes, 0);
  const sizeKnown = knownBytes.length === missing.length;
  return (
    <Modal
      title="Location data required"
      width={480}
      onClose={() => useStore.getState().dismissMissingData()}
      footer={
        <>
          <button onClick={() => useStore.getState().dismissMissingData()}>
            Cancel
          </button>
          <button
            className="primary"
            onClick={() => void useStore.getState().downloadMissingAndRetry()}
          >
            Download &amp; generate
          </button>
        </>
      }
    >
      <p style={{ marginTop: 0 }}>
        This map needs location data that isn&apos;t downloaded yet:
      </p>
      <ul className="missing-list">
        {missing.map((c) => (
          <li key={c.code}>
            <strong>{c.code}</strong> — {c.missingFiles} file
            {c.missingFiles === 1 ? "" : "s"}
            {typeof c.estimatedBytes === "number"
              ? ` (~${formatBytes(c.estimatedBytes)})`
              : ""}
          </li>
        ))}
      </ul>
      <div className="hint">
        {sizeKnown ? `Total download ≈ ${formatBytes(totalBytes)}. ` : ""}
        Downloads run in the background, then generation retries automatically.
      </div>
    </Modal>
  );
}

export default function App() {
  const aliveRef = useRef(false);
  const engineEverAlive = useStore((s) => s.engineEverAlive);

  useEffect(() => {
    const { applyEvent, pushLog, setSidecarAlive, setPing } =
      useStore.getState();

    // Workspace is filesystem-only; bring it up regardless of the sidecar.
    void useStore.getState().initWorkspace();

    const unlisteners: Array<() => void> = [];
    let cancelled = false;
    const track = (p: Promise<() => void>) =>
      p.then((fn) => (cancelled ? fn() : unlisteners.push(fn)));

    void track(listenSidecarEvents(applyEvent));
    void track(listenSidecarLog(pushLog));
    void track(listenSidecarExit(() => setSidecarAlive(false)));

    const removeKeybindings = installKeybindings();
    unlisteners.push(removeKeybindings);

    // Flush the (debounced) autosave before the window is destroyed, so edits
    // made in the last ~600 ms aren't lost when the user closes the app.
    let closing = false;
    void track(
      onCloseRequested(async () => {
        if (closing) return;
        closing = true;
        try {
          if (useStore.getState().dirty) await useStore.getState().saveCurrent();
        } catch (e) {
          // Couldn't save — surface it but still close rather than trap the user.
          logError("saveOnClose", e);
        } finally {
          void destroyWindow();
        }
      }),
    );

    const poll = async () => {
      try {
        const alive = await sidecarStatus();
        setSidecarAlive(alive);
        if (alive && useStore.getState().version === null) {
          const ping = await request<PingResult>("ping", {});
          setPing(ping.version, ping.dataFolder);
        }
        // On the offline→online transition, (re)initialize the workspace if it
        // didn't finish earlier, then re-validate and refresh the badge.
        if (alive && !aliveRef.current) {
          void useStore.getState().initWorkspace();
          void useStore.getState().validateCurrent();
          void useStore.getState().refreshDataSummary();
        }
        aliveRef.current = alive;
      } catch {
        setSidecarAlive(false);
        aliveRef.current = false;
      }
    };
    void poll();
    const interval = setInterval(poll, 3000);

    return () => {
      cancelled = true;
      clearInterval(interval);
      for (const fn of unlisteners) fn();
    };
  }, []);

  return (
    <div className="app">
      <TitleBar />
      <div className="mid">
        {engineEverAlive ? (
          <>
            <UpdateBanner />
            <div className="body">
              <Sidebar />
              <Workspace />
            </div>
          </>
        ) : (
          <SetupView />
        )}
      </div>
      <StatusBar />
      {engineEverAlive && (
        <>
          <DataSheet />
          <MissingDataModal />
        </>
      )}
      {/* Windows/Linux run borderless (decorations off) and need custom edge
          grips; macOS uses the native window frame for resizing. */}
      {!IS_MAC && <ResizeHandles />}
    </div>
  );
}
