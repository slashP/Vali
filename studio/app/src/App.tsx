import { useEffect } from "react";
import { useStore } from "./store";
import { Sidebar } from "./components/Sidebar";
import { StatusBar } from "./components/StatusBar";
import { EditorView } from "./views/EditorView";
import { RunView } from "./views/RunView";
import { MapView } from "./views/MapView";
import { DataView } from "./views/DataView";
import {
  listenSidecarEvents,
  listenSidecarExit,
  listenSidecarLog,
  request,
  sidecarStatus,
} from "./lib/sidecar";
import type { PingResult } from "./lib/types";

export default function App() {
  const view = useStore((s) => s.view);

  useEffect(() => {
    const {
      applyEvent,
      pushLog,
      setSidecarAlive,
      setPing,
    } = useStore.getState();

    const unlisteners: Array<() => void> = [];
    let cancelled = false;

    listenSidecarEvents(applyEvent).then((fn) => {
      if (cancelled) fn();
      else unlisteners.push(fn);
    });
    listenSidecarLog(pushLog).then((fn) => {
      if (cancelled) fn();
      else unlisteners.push(fn);
    });
    listenSidecarExit(() => setSidecarAlive(false)).then((fn) => {
      if (cancelled) fn();
      else unlisteners.push(fn);
    });

    // Poll liveness; ping once when the sidecar first comes up.
    const poll = async () => {
      try {
        const alive = await sidecarStatus();
        setSidecarAlive(alive);
        if (alive && useStore.getState().version === null) {
          const ping = await request<PingResult>("ping", {});
          setPing(ping.version, ping.dataFolder);
        }
      } catch {
        setSidecarAlive(false);
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
      <Sidebar />
      <main className="main">
        {view === "editor" && <EditorView />}
        {view === "run" && <RunView />}
        {view === "map" && <MapView />}
        {view === "data" && <DataView />}
      </main>
      <StatusBar />
    </div>
  );
}
