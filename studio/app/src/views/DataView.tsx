import { useEffect, useState } from "react";
import { request } from "../lib/sidecar";
import { useStore } from "../store";
import type { CountriesResult, DataStatus } from "../lib/types";

function formatBytes(bytes: number): string {
  if (bytes >= 1e9) return `${(bytes / 1e9).toFixed(2)} GB`;
  if (bytes >= 1e6) return `${(bytes / 1e6).toFixed(1)} MB`;
  if (bytes >= 1e3) return `${(bytes / 1e3).toFixed(0)} kB`;
  return `${bytes} B`;
}

function formatDate(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleDateString();
}

export function DataView() {
  const sidecarAlive = useStore((s) => s.sidecarAlive);
  const [status, setStatus] = useState<DataStatus | null>(null);
  const [names, setNames] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    if (!sidecarAlive) return;
    let alive = true;
    setError(null);
    request<DataStatus>("dataStatus", {})
      .then((s) => alive && setStatus(s))
      .catch((e) => alive && setError(String(e)));
    request<CountriesResult>("countries", {})
      .then((r) => {
        if (!alive) return;
        const map: Record<string, string> = {};
        for (const c of r.countries) map[c.code] = c.name;
        setNames(map);
      })
      .catch(() => {
        /* names are optional */
      });
    return () => {
      alive = false;
    };
  }, [sidecarAlive]);

  const copyCmd = async () => {
    try {
      await navigator.clipboard.writeText("vali download");
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    } catch {
      /* clipboard may be unavailable */
    }
  };

  const countries = status?.countries ?? [];

  return (
    <div className="data-view">
      <div className="toolbar" style={{ borderBottom: "none", padding: 0, marginBottom: 12 }}>
        <span className="title">Downloaded data</span>
        <div className="grow" />
        {status && <span className="hint">{status.dataFolder}</span>}
      </div>

      {!sidecarAlive && <div className="empty">Sidecar offline.</div>}
      {error && <div className="hint" style={{ color: "var(--err)" }}>{error}</div>}

      {sidecarAlive && !error && (
        <table className="data-table">
          <thead>
            <tr>
              <th>Code</th>
              <th>Country</th>
              <th>Files</th>
              <th>Size</th>
              <th>Last update</th>
            </tr>
          </thead>
          <tbody>
            {countries.length === 0 ? (
              <tr>
                <td colSpan={5} className="hint">
                  No downloaded data found.
                </td>
              </tr>
            ) : (
              countries.map((c) => (
                <tr key={c.code}>
                  <td>{c.code}</td>
                  <td>{names[c.code] ?? ""}</td>
                  <td>{c.files.toLocaleString()}</td>
                  <td>{formatBytes(c.bytes)}</td>
                  <td>{formatDate(c.lastWriteUtc)}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      )}

      <div className="note-box">
        <span>
          Downloads run through the Vali CLI for now — run <code>vali download</code>{" "}
          in a terminal.
        </span>
        <div className="grow" />
        <button onClick={() => void copyCmd()}>
          {copied ? "Copied" : "Copy"}
        </button>
      </div>
    </div>
  );
}
