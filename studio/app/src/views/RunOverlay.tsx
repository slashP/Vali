import { useEffect, useState } from "react";
import { useStore } from "../store";
import { formatDuration } from "../lib/format";
import { ChevronIcon, StopIcon } from "../components/Icons";
import type { CountryShortfall, ProgressCountry } from "../lib/types";

interface CountryRow {
  code: string;
  completed: number;
  total: number;
  produced: number;
  status: string;
}

export function RunOverlay() {
  const runningId = useStore((s) => s.runningId);
  const runningSlug = useStore((s) => s.runningSlug);
  const maps = useStore((s) => s.maps);
  const started = useStore((s) => s.started);
  const progress = useStore((s) => s.progress);
  const warnings = useStore((s) => s.warnings);
  const result = useStore((s) => s.result);
  const runError = useStore((s) => s.runError);
  const runStartMs = useStore((s) => s.runStartMs);

  const [now, setNow] = useState(Date.now());
  const [stopping, setStopping] = useState(false);
  const [minimized, setMinimized] = useState(true);
  const [expanded, setExpanded] = useState<Set<string>>(new Set());

  useEffect(() => {
    if (!runningId) return;
    const t = setInterval(() => setNow(Date.now()), 500);
    return () => clearInterval(t);
  }, [runningId]);

  useEffect(() => {
    if (runningId) setStopping(false);
  }, [runningId]);

  const visible = runningId || started || progress || result || runError;
  if (!visible) return null;

  const runningName = maps.find((m) => m.slug === runningSlug)?.name ?? runningSlug;
  const totalGoal = progress?.totalGoal ?? started?.totalGoal ?? 0;
  const totalLocations = result?.locationsWritten ?? progress?.totalLocations ?? 0;
  const pct = totalGoal > 0 ? Math.min(100, (totalLocations / totalGoal) * 100) : 0;
  const elapsedMs = result ? result.elapsedMs : runStartMs ? now - runStartMs : 0;
  const kind = "Generation";

  const done = Boolean(result) && !runningId;
  // Guard the deref: an older/partial run.json may have no `shortfall`, and a
  // render throw here would be a sticky, reload-proof crash (loadLastRun re-reads
  // the same file on every map select).
  const shortfallCountries = result?.shortfall?.countries ?? [];
  const short = Boolean(result?.outputPath) && shortfallCountries.length > 0;
  // A completed run fills the bar — finishing short of the goal is still "done".
  const barDone = done && !result?.stoppedEarly && Boolean(result?.outputPath);
  const barPct = runningId ? pct : barDone ? 100 : pct;
  // "short of goal" is conveyed by the actual/goal numbers themselves, so a
  // finished run just reads "complete".
  const status: { label: string; cls: string } = runningId
    ? { label: "running", cls: "working-badge" }
    : runError
      ? { label: "failed", cls: "stopped" }
      : result?.stoppedEarly
        ? { label: "stopped", cls: "stopped" }
        : { label: "complete", cls: "ok-badge" };

  const metaNumbers = (
    <div className="run-meta">
      <span>
        {totalLocations.toLocaleString()} / {totalGoal.toLocaleString()}
      </span>
      {(runningId || result) && <span>{formatDuration(elapsedMs)}</span>}
      {runningId && progress && (
        <span>
          {progress.countriesComplete}/{progress.countriesTotal} regions
        </span>
      )}
    </div>
  );

  const rows: CountryRow[] = [];
  const byCode = new Map<string, ProgressCountry>();
  for (const c of progress?.countries ?? []) byCode.set(c.code, c);
  const codes =
    started?.countries.map((c) => c.code) ??
    progress?.countries.map((c) => c.code) ??
    [];
  for (const code of codes) {
    const startedEntry = started?.countries.find((c) => c.code === code);
    const live = byCode.get(code);
    rows.push({
      code,
      completed: live?.completed ?? 0,
      total: live?.total ?? startedEntry?.workItems ?? 0,
      produced: live?.produced ?? 0,
      status: live?.status ?? "Queued",
    });
  }

  const stop = () => {
    setStopping(true);
    void useStore.getState().cancelRun();
  };

  const toggle = (code: string) =>
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(code)) next.delete(code);
      else next.add(code);
      return next;
    });

  return (
    <div className="run-overlay">
      <div className="run-overlay-head">
        <span className="run-overlay-title">
          {kind}
          {runningName ? ` · ${runningName}` : ""}
        </span>
        <span className={`badge ${status.cls}`}>{status.label}</span>
        <span className="grow" />
        {!minimized && runningId && (
          <button className="danger run-stop" onClick={stop} disabled={stopping}>
            <StopIcon size={13} /> {stopping ? "Stopping…" : "Stop"}
          </button>
        )}
        <button
          className={`icon-btn run-min${minimized ? "" : " up"}`}
          title={minimized ? "Expand run details" : "Minimize"}
          aria-label={minimized ? "Expand run details" : "Minimize"}
          onClick={() => setMinimized(!minimized)}
        >
          <ChevronIcon size={16} />
        </button>
      </div>

      <div className="progress-bar">
        <div className={barDone ? "done" : ""} style={{ width: `${barPct}%` }} />
      </div>

      {metaNumbers}

      <div className={`run-details${minimized ? "" : " open"}`}>
        <div className="run-details-inner">
          {runError && <div className="run-error-line">{runError}</div>}

          {runningId && rows.length > 0 && (
            <div className="run-country-list">
              {rows.map((r) => (
                <div
                  key={r.code}
                  className={`run-cc ${r.status.toLowerCase()}`}
                  title={`${r.status} · produced ${r.produced.toLocaleString()}`}
                >
                  <span className="run-cc-code">{r.code}</span>
                  <span className="run-cc-count">
                    {r.total > 0 ? `${r.completed}/${r.total}` : r.status}
                  </span>
                </div>
              ))}
            </div>
          )}

          {warnings.length > 0 && (
            <div className="run-warnings">
              {warnings.map((w, i) => (
                <div key={i}>⚠ {w}</div>
              ))}
            </div>
          )}

          {done && (
            <div className="run-result">
              {result?.outputPath ? (
                <>
                  <div className="hint run-result-note">
                    {short
                      ? "Finished below the goal — the filters left too few qualifying locations in some regions. That's fine; here's where it fell short:"
                      : "All goals met."}
                  </div>
                  {shortfallCountries.length > 0 && (
                    <div className="sf-list">
                      {shortfallCountries.map((c) => (
                        <ShortfallCountry
                          key={c.code}
                          country={c}
                          open={expanded.has(c.code)}
                          onToggle={() => toggle(c.code)}
                        />
                      ))}
                    </div>
                  )}
                </>
              ) : (
                <div className="hint">
                  {result?.stoppedEarly
                    ? "Stopped before any region completed — nothing was written."
                    : "The run completed but produced no locations."}
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function ShortfallCountry({
  country,
  open,
  onToggle,
}: {
  country: CountryShortfall;
  open: boolean;
  onToggle: () => void;
}) {
  const hasSubs = country.subdivisions.length > 0;
  return (
    <div className="sf-country">
      <button className="sf-country-head" onClick={onToggle} disabled={!hasSubs}>
        <span className={`sf-caret${open ? " open" : ""}`}>
          {hasSubs ? <ChevronIcon size={13} /> : null}
        </span>
        <strong>{country.code}</strong>
        <span className="grow" />
        <span className="sf-num">
          {country.produced.toLocaleString()} / {country.goal.toLocaleString()}
        </span>
        <span className="sf-pct">{country.pctBelow}%↓</span>
      </button>
      {hasSubs && (
        <div className={`sf-subs${open ? " open" : ""}`}>
          <div className="sf-subs-inner">
            {country.subdivisions.map((s) => (
              <div key={s.code} className="sf-sub">
                <span className="sf-sub-name">
                  {s.code} {s.name}
                </span>
                <span className="sf-num">
                  {s.produced.toLocaleString()} / {s.goal.toLocaleString()}
                </span>
                <span className="sf-pct">{s.pctBelow}%↓</span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
