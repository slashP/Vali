import { useEffect, useState } from "react";
import { useStore } from "../store";
import { request } from "../lib/sidecar";
import { loadLocationsFile } from "../lib/locations";
import type { CancelResult, ProgressCountry } from "../lib/types";

function formatDuration(ms: number): string {
  const total = Math.max(0, Math.floor(ms / 1000));
  const m = Math.floor(total / 60);
  const s = total % 60;
  return `${m}:${s.toString().padStart(2, "0")}`;
}

interface CountryRow {
  code: string;
  completed: number;
  total: number;
  produced: number;
  status: string;
}

export function RunView() {
  const runningId = useStore((s) => s.runningId);
  const isPreview = useStore((s) => s.isPreview);
  const started = useStore((s) => s.started);
  const progress = useStore((s) => s.progress);
  const warnings = useStore((s) => s.warnings);
  const result = useStore((s) => s.result);
  const runError = useStore((s) => s.runError);
  const runStartMs = useStore((s) => s.runStartMs);

  const [now, setNow] = useState(Date.now());
  const [stopping, setStopping] = useState(false);

  useEffect(() => {
    if (!runningId) return;
    const t = setInterval(() => setNow(Date.now()), 500);
    return () => clearInterval(t);
  }, [runningId]);

  if (!started && !progress && !result && !runError && !runningId) {
    return (
      <div className="run-view">
        <div className="empty">
          No run yet. Use Preview or Generate from the Editor.
        </div>
      </div>
    );
  }

  const totalGoal = progress?.totalGoal ?? started?.totalGoal ?? 0;
  const totalLocations = result?.locationsWritten ?? progress?.totalLocations ?? 0;
  const pct = totalGoal > 0 ? Math.min(100, (totalLocations / totalGoal) * 100) : 0;

  const elapsedMs = result
    ? result.elapsedMs
    : runStartMs
      ? now - runStartMs
      : 0;

  // Merge started (goal, total work) with progress (live status).
  const rows: CountryRow[] = [];
  const byCode = new Map<string, ProgressCountry>();
  for (const c of progress?.countries ?? []) byCode.set(c.code, c);
  const codes =
    started?.countries.map((c) => c.code) ??
    progress?.countries.map((c) => c.code) ??
    [];
  for (const code of codes) {
    const started_ = started?.countries.find((c) => c.code === code);
    const live = byCode.get(code);
    rows.push({
      code,
      completed: live?.completed ?? 0,
      total: live?.total ?? started_?.workItems ?? 0,
      produced: live?.produced ?? 0,
      status: live?.status ?? "Queued",
    });
  }

  const stop = async () => {
    if (!runningId) return;
    setStopping(true);
    try {
      await request<CancelResult>("cancel", { targetId: runningId });
    } catch {
      // ignore — the generate response will still arrive with stoppedEarly
    }
  };

  const viewOnMap = async () => {
    if (!result) return;
    const locs = await loadLocationsFile(result.outputPath);
    const { setMapLocations, setView } = useStore.getState();
    setMapLocations(locs, result.outputPath);
    setView("map");
  };

  return (
    <div className="run-view">
      <div className="toolbar" style={{ borderBottom: "none", padding: 0, marginBottom: 12 }}>
        <span className="title">
          {isPreview ? "Preview run" : "Generation"}
          {runningId && <span className="badge" style={{ marginLeft: 8 }}>running</span>}
          {result?.stoppedEarly && <span className="badge stopped" style={{ marginLeft: 8 }}>stopped early</span>}
        </span>
        <div className="grow" />
        {runningId && (
          <button className="danger" onClick={() => void stop()} disabled={stopping}>
            {stopping ? "Stopping…" : "Stop"}
          </button>
        )}
      </div>

      {runError && (
        <div className="hint" style={{ color: "var(--err)", marginBottom: 12 }}>
          {runError}
        </div>
      )}

      <div className="run-overall">
        <div className="progress-bar">
          <div style={{ width: `${pct}%` }} />
        </div>
        <div className="run-meta">
          <span>
            {totalLocations.toLocaleString()} / {totalGoal.toLocaleString()} locations
          </span>
          <span>elapsed {formatDuration(elapsedMs)}</span>
          {progress && (
            <span>
              {progress.countriesComplete} / {progress.countriesTotal} countries
            </span>
          )}
        </div>
      </div>

      {warnings.length > 0 && (
        <div style={{ marginBottom: 12 }}>
          {warnings.map((w, i) => (
            <div key={i} className="hint" style={{ color: "var(--warn)" }}>
              ⚠ {w}
            </div>
          ))}
        </div>
      )}

      <div className="country-grid">
        {rows.map((r) => (
          <div key={r.code} className={`country-card ${r.status.toLowerCase()}`}>
            <div className="cc">{r.code}</div>
            <div className="row">
              <span>work</span>
              <span>
                {r.completed} / {r.total}
              </span>
            </div>
            <div className="row">
              <span>produced</span>
              <span>{r.produced.toLocaleString()}</span>
            </div>
            <div className="row">
              <span className="badge">{r.status}</span>
            </div>
          </div>
        ))}
      </div>

      {result && (
        <div className="shortfall">
          <div className="section-title">
            Result — {result.locationsWritten.toLocaleString()} locations written
            <button
              className="primary"
              style={{ marginLeft: 12 }}
              onClick={() => void viewOnMap()}
            >
              View on map
            </button>
          </div>
          <div className="hint" style={{ marginBottom: 8 }}>
            {result.outputPath}
          </div>
          {result.shortfall.countries.length > 0 ? (
            <table>
              <thead>
                <tr>
                  <th>Region</th>
                  <th>Produced</th>
                  <th>Goal</th>
                  <th>Short</th>
                  <th>% below</th>
                </tr>
              </thead>
              <tbody>
                {result.shortfall.countries.map((c) => (
                  <ShortfallRows key={c.code} country={c} />
                ))}
              </tbody>
            </table>
          ) : (
            <div className="hint">No shortfall — all goals met.</div>
          )}
        </div>
      )}
    </div>
  );
}

function ShortfallRows({
  country,
}: {
  country: import("../lib/types").CountryShortfall;
}) {
  return (
    <>
      <tr>
        <td>
          <strong>{country.code}</strong>
        </td>
        <td>{country.produced.toLocaleString()}</td>
        <td>{country.goal.toLocaleString()}</td>
        <td>{country.shortfall.toLocaleString()}</td>
        <td>{country.pctBelow}%</td>
      </tr>
      {country.subdivisions.map((s) => (
        <tr key={s.code}>
          <td style={{ paddingLeft: 24, color: "var(--text-dim)" }}>
            {s.code} {s.name}
          </td>
          <td>{s.produced.toLocaleString()}</td>
          <td>{s.goal.toLocaleString()}</td>
          <td>{s.shortfall.toLocaleString()}</td>
          <td>{s.pctBelow}%</td>
        </tr>
      ))}
    </>
  );
}
