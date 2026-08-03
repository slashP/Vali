import { useState } from "react";
import { useStore } from "../store";
import { MOD } from "../lib/keybindings";
import { DataIcon, KeyboardIcon, RefreshIcon } from "./Icons";
import { ShortcutsModal } from "./ShortcutsModal";

export function StatusBar() {
  const alive = useStore((s) => s.sidecarAlive);
  const version = useStore((s) => s.version);
  const summary = useStore((s) => s.dataSummary);
  const downloads = useStore((s) => s.downloads);

  const [restarting, setRestarting] = useState(false);
  const [shortcutsOpen, setShortcutsOpen] = useState(false);

  const restart = async () => {
    setRestarting(true);
    try {
      await useStore.getState().restartEngine();
    } catch {
      // liveness poll reconciles
    } finally {
      setRestarting(false);
    }
  };

  const running = Object.values(downloads).filter((d) => d.running);
  const bytesDone = running.reduce((a, d) => a + d.bytesDone, 0);
  const bytesTotal = running.reduce((a, d) => a + d.bytesTotal, 0);
  const dlPct = bytesTotal > 0 ? Math.min(100, (bytesDone / bytesTotal) * 100) : 0;
  const dlLabel =
    running.length === 1
      ? `Downloading ${running[0].code}`
      : `Downloading ${running.length} countries`;

  return (
    <div className="statusbar">
      <span className="status-engine">
        <span className={`dot ${alive ? "ok" : "bad"}`} />
        {alive ? `Engine${version ? ` v${version}` : ""}` : "Engine offline"}
      </span>
      {!alive && (
        <button
          className="statusbar-btn"
          onClick={() => void restart()}
          disabled={restarting}
        >
          <RefreshIcon size={12} />
          {restarting ? "Restarting…" : "Restart"}
        </button>
      )}

      <button
        className="status-shortcuts"
        onClick={() => setShortcutsOpen(true)}
        title="Keyboard shortcuts"
      >
        <KeyboardIcon size={13} />
        <span>Shortcuts</span>
      </button>

      <span className="spacer" />

      {running.length > 0 && (
        <div className="dl-status" title={dlLabel}>
          <span className="dl-status-label">{dlLabel}</span>
          <span className={`dl-mini-bar${bytesTotal > 0 ? "" : " indet"}`}>
            <span style={bytesTotal > 0 ? { width: `${dlPct}%` } : undefined} />
          </span>
          {bytesTotal > 0 && (
            <span className="dl-status-pct">{Math.round(dlPct)}%</span>
          )}
        </div>
      )}

      <button
        className="data-pill"
        onClick={() => useStore.getState().setDataOpen(true)}
        title={`Manage location data (${MOD}+Shift+L)`}
      >
        <DataIcon size={14} />
        {summary ? (
          <>
            <span>
              {summary.countryCount}{" "}
              {summary.countryCount === 1 ? "country" : "countries"}
            </span>
            {summary.updateCount > 0 && (
              <span className="data-pill-warn">
                · {summary.updateCount} need update
              </span>
            )}
          </>
        ) : (
          <span>Location data</span>
        )}
      </button>

      {shortcutsOpen && (
        <ShortcutsModal onClose={() => setShortcutsOpen(false)} />
      )}
    </div>
  );
}
