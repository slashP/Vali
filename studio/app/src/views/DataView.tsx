import { useEffect, useRef, useState } from "react";
import { request } from "../lib/sidecar";
import { useStore } from "../store";
import { ConfirmModal } from "../components/Modal";
import { formatBytes, humanizeDate } from "../lib/format";
import type {
  CountriesResult,
  DataStatus,
  RemoteStatusCountry,
  RemoteStatusResult,
  ValiInfo,
} from "../lib/types";

const TWO_GB = 2 * 1024 * 1024 * 1024;

type Status = "uptodate" | "update" | "partial" | "none" | "unknown" | "error";

const STATUS_LABEL: Record<Status, string> = {
  uptodate: "Up to date",
  update: "Update available",
  partial: "Partial",
  none: "Not downloaded",
  unknown: "Downloaded",
  error: "Check failed",
};

interface Row {
  code: string;
  name: string;
  localFiles: number;
  localBytes: number;
  localNewest: string | null;
  remoteNewest: string | null;
  status: Status;
  bytesToDownload: number;
  remoteBytes: number;
  rsError: string | null;
}

function classify(rs: RemoteStatusCountry): Status {
  // A failed remoteStatus arrives as a bare { code, error } — no local/remote.
  if (rs.error || !rs.local || !rs.remote) return "error";
  if (rs.local.files === 0) return "none";
  if (rs.upToDate) return "uptodate";
  if ((rs.updateFilesToApply ?? 0) > 0) return "update";
  if ((rs.filesToDownload ?? 0) > 0) return "partial";
  return "uptodate";
}

interface PendingBig {
  code: string;
  bytes: number;
  full: boolean;
}

export function DataView() {
  const sidecarAlive = useStore((s) => s.sidecarAlive);
  const version = useStore((s) => s.version);
  const dataFolder = useStore((s) => s.dataFolder);
  const downloads = useStore((s) => s.downloads);
  const valiInfo = useStore((s) => s.valiInfo);

  const [dataStatus, setDataStatus] = useState<DataStatus | null>(null);
  const [names, setNames] = useState<Record<string, string>>({});
  const [countryList, setCountryList] = useState<CountriesResult["countries"]>([]);
  const [remote, setRemote] = useState<Record<string, RemoteStatusCountry>>({});
  const [error, setError] = useState<string | null>(null);
  const [addQuery, setAddQuery] = useState("");
  const [pendingBig, setPendingBig] = useState<PendingBig | null>(null);
  const [pendingUpdateAll, setPendingUpdateAll] = useState<{
    count: number;
    bytes: number;
  } | null>(null);

  const refreshedRef = useRef<Set<string>>(new Set());

  const fetchRemote = async (codes: string[]) => {
    if (codes.length === 0) return;
    try {
      const res = await request<RemoteStatusResult>("remoteStatus", {
        countryCodes: codes,
      });
      setRemote((prev) => {
        const next = { ...prev };
        for (const c of res.countries) next[c.code] = c;
        return next;
      });
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    }
  };

  const refreshData = async () => {
    try {
      const status = await request<DataStatus>("dataStatus", {});
      setDataStatus(status);
      return status.countries.map((c) => c.code);
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
      return [];
    }
  };

  // Initial load: data status, country names, remote status of local data, vali info.
  useEffect(() => {
    if (!sidecarAlive) return;
    let alive = true;
    setError(null);
    void (async () => {
      const localCodes = await refreshData();
      if (!alive) return;
      await fetchRemote(localCodes);
      try {
        const info = await request<ValiInfo>("valiInfo", {});
        if (alive) useStore.getState().setValiInfo(info);
      } catch {
        // valiInfo is best-effort (needs network)
      }
    })();
    request<CountriesResult>("countries", {})
      .then((r) => {
        if (!alive) return;
        const map: Record<string, string> = {};
        for (const c of r.countries) map[c.code] = c.name;
        setNames(map);
        setCountryList(r.countries);
      })
      .catch(() => {
        /* names optional */
      });
    return () => {
      alive = false;
    };
  }, [sidecarAlive]);

  // When a download completes, refresh that country's row once.
  useEffect(() => {
    for (const [code, d] of Object.entries(downloads)) {
      if (d.running || !d.result) continue;
      const sig = `${code}:${d.result.bytesDownloaded}:${d.result.downloadedFiles}`;
      if (refreshedRef.current.has(sig)) continue;
      refreshedRef.current.add(sig);
      void refreshData().then(() => fetchRemote([code]));
    }
  }, [downloads]);

  const beginDownload = (code: string, full: boolean, estBytes: number) => {
    if (estBytes > TWO_GB) {
      setPendingBig({ code, bytes: estBytes, full });
      return;
    }
    void useStore.getState().startDownload(code, { full });
  };

  const addCountry = () => {
    const q = addQuery.trim();
    if (!q) return;
    const byLabel = countryList.find((c) => `${c.code} — ${c.name}` === q);
    const byCode = countryList.find((c) => c.code === q.toUpperCase());
    const code = (byLabel ?? byCode)?.code;
    if (!code) return;
    setAddQuery("");
    void fetchRemote([code]);
  };

  // Build the row set from local data + any remote-fetched codes + downloads.
  const codes = new Set<string>();
  dataStatus?.countries.forEach((c) => codes.add(c.code));
  Object.keys(remote).forEach((c) => codes.add(c));
  Object.keys(downloads).forEach((c) => codes.add(c));

  const rows: Row[] = [...codes]
    .map((code) => {
      const local = dataStatus?.countries.find((c) => c.code === code);
      const rs = remote[code];
      let status: Status;
      if (rs) status = classify(rs);
      else if (local) status = "unknown";
      else status = "none";
      return {
        code,
        name: names[code] ?? "",
        // Guard every nested deref — a failed remoteStatus has no local/remote.
        localFiles: rs?.local?.files ?? local?.files ?? 0,
        localBytes: rs?.local?.bytes ?? local?.bytes ?? 0,
        localNewest: rs?.local?.newestUtc ?? local?.lastWriteUtc ?? null,
        remoteNewest: rs?.remote?.newestUploadedUtc ?? null,
        status,
        bytesToDownload: rs?.bytesToDownload ?? 0,
        remoteBytes: rs?.remote?.bytes ?? 0,
        rsError: rs?.error ?? null,
      };
    })
    .sort((a, b) => a.code.localeCompare(b.code));

  const isEmpty = rows.length === 0;

  const updatable = rows.filter(
    (r) => r.status === "update" || r.status === "partial",
  );
  const updatableBytes = updatable.reduce((a, r) => a + r.bytesToDownload, 0);
  const startUpdateAll = () => {
    for (const r of updatable) void useStore.getState().startDownload(r.code, {});
  };
  const updateAll = () => {
    // One confirmation for the whole batch when it's large, instead of
    // silently kicking off a multi-gigabyte transfer.
    if (updatableBytes > TWO_GB) {
      setPendingUpdateAll({ count: updatable.length, bytes: updatableBytes });
      return;
    }
    startUpdateAll();
  };

  // Keep the status-bar badge in sync with the detailed view while it's open.
  useEffect(() => {
    const countryCount = rows.filter((r) => r.localFiles > 0).length;
    const updateCount = rows.filter(
      (r) => r.status === "update" || r.status === "partial",
    ).length;
    useStore.getState().setDataSummary({ countryCount, updateCount });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [dataStatus, remote]);

  return (
    <div className="data-view">
      <div className="data-scroll">
        <div className="run-head">
          <span className="hint">Manage the location data downloaded on this computer.</span>
          <div className="grow" />
          {dataStatus && <span className="hint">{dataStatus.dataFolder}</span>}
        </div>

        {!sidecarAlive && <div className="empty">Engine offline.</div>}
        {error && (
          <div className="hint" style={{ color: "var(--err)" }}>
            {error}
          </div>
        )}

        {sidecarAlive && (
          <>
            <div className="add-country">
              <input
                list="country-options"
                placeholder="Add a country to download…"
                value={addQuery}
                onChange={(e) => setAddQuery(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter") addCountry();
                }}
              />
              <datalist id="country-options">
                {countryList.map((c) => (
                  <option key={c.code} value={`${c.code} — ${c.name}`} />
                ))}
              </datalist>
              <button onClick={addCountry}>Add</button>
              <span className="grow" />
              {updatable.length > 0 && (
                <button
                  className="primary update-all"
                  onClick={updateAll}
                  title="Download every available update in one go"
                >
                  Update all ({updatable.length}
                  {updatableBytes > 0 ? ` · ${formatBytes(updatableBytes)}` : ""})
                </button>
              )}
            </div>

            {isEmpty ? (
              <div className="empty">
                No location data yet — add a country above to download your first
                one.
              </div>
            ) : (
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Country</th>
                    <th>Files</th>
                    <th>Size</th>
                    <th>Downloaded</th>
                    <th>Remote</th>
                    <th>Status</th>
                    <th style={{ width: 220 }}>Action</th>
                  </tr>
                </thead>
                <tbody>
                  {rows.map((r) => (
                    <RowView key={r.code} row={r} onDownload={beginDownload} />
                  ))}
                </tbody>
              </table>
            )}
          </>
        )}
      </div>

      <EngineCard version={version} dataFolder={dataFolder} valiInfo={valiInfo} />

      {pendingBig && (
        <ConfirmModal
          title="Large download"
          message={
            <>
              Downloading <strong>{pendingBig.code}</strong> will transfer about{" "}
              {formatBytes(pendingBig.bytes)}. Continue?
            </>
          }
          confirmLabel="Download"
          onCancel={() => setPendingBig(null)}
          onConfirm={() => {
            const { code, full } = pendingBig;
            setPendingBig(null);
            void useStore.getState().startDownload(code, { full });
          }}
        />
      )}

      {pendingUpdateAll && (
        <ConfirmModal
          title="Update all data"
          message={
            <>
              Updating {pendingUpdateAll.count}{" "}
              {pendingUpdateAll.count === 1 ? "country" : "countries"} will
              transfer about {formatBytes(pendingUpdateAll.bytes)}. Continue?
            </>
          }
          confirmLabel="Update all"
          onCancel={() => setPendingUpdateAll(null)}
          onConfirm={() => {
            setPendingUpdateAll(null);
            startUpdateAll();
          }}
        />
      )}
    </div>
  );
}

function RowView({
  row,
  onDownload,
}: {
  row: Row;
  onDownload: (code: string, full: boolean, estBytes: number) => void;
}) {
  const download = useStore((s) => s.downloads[row.code]);
  const running = download?.running ?? false;

  return (
    <tr>
      <td>
        <strong>{row.code}</strong>
        {row.name ? ` ${row.name}` : ""}
      </td>
      <td>{row.localFiles.toLocaleString()}</td>
      <td>{row.localBytes > 0 ? formatBytes(row.localBytes) : "—"}</td>
      <td>{humanizeDate(row.localNewest)}</td>
      <td>{humanizeDate(row.remoteNewest)}</td>
      <td>
        <span className={`badge status-${row.status}`} title={row.rsError ?? undefined}>
          {STATUS_LABEL[row.status]}
        </span>
      </td>
      <td>
        {running ? (
          <DownloadProgress code={row.code} />
        ) : download?.error ? (
          <div className="dl-error">
            <span className="hint" title={download.error} style={{ color: "var(--err)" }}>
              failed
            </span>
            <button onClick={() => onDownload(row.code, false, row.bytesToDownload)}>
              Retry
            </button>
          </div>
        ) : (
          <RowAction row={row} onDownload={onDownload} />
        )}
      </td>
    </tr>
  );
}

function RowAction({
  row,
  onDownload,
}: {
  row: Row;
  onDownload: (code: string, full: boolean, estBytes: number) => void;
}) {
  switch (row.status) {
    case "none":
    case "partial":
      return (
        <button
          className="primary"
          onClick={() => onDownload(row.code, false, row.bytesToDownload)}
        >
          Download {row.bytesToDownload > 0 ? `(${formatBytes(row.bytesToDownload)})` : ""}
        </button>
      );
    case "update":
      return (
        <button
          className="primary"
          onClick={() => onDownload(row.code, false, row.bytesToDownload)}
        >
          Update {row.bytesToDownload > 0 ? `(${formatBytes(row.bytesToDownload)})` : ""}
        </button>
      );
    case "uptodate":
      return (
        <button onClick={() => onDownload(row.code, true, row.remoteBytes)}>
          Re-download
        </button>
      );
    case "unknown":
      return (
        <button onClick={() => onDownload(row.code, false, row.bytesToDownload)}>
          Check / update
        </button>
      );
    case "error":
      return (
        <button onClick={() => onDownload(row.code, false, row.bytesToDownload)}>
          Retry
        </button>
      );
    default:
      return null;
  }
}

function DownloadProgress({ code }: { code: string }) {
  const d = useStore((s) => s.downloads[code]);
  if (!d) return null;
  const pct = d.bytesTotal > 0 ? Math.min(100, (d.bytesDone / d.bytesTotal) * 100) : 0;
  return (
    <div className="dl-progress">
      <div className="progress-bar">
        <div style={{ width: `${pct}%` }} />
      </div>
      <div className="dl-row">
        <span className="hint">
          {d.filesDone}/{d.filesTotal} · {formatBytes(d.bytesDone)}
          {d.bytesTotal > 0 ? ` / ${formatBytes(d.bytesTotal)}` : ""}
        </span>
        <button onClick={() => void useStore.getState().cancelDownload(code)}>
          Cancel
        </button>
      </div>
    </div>
  );
}

function EngineCard({
  version,
  dataFolder,
  valiInfo,
}: {
  version: string | null;
  dataFolder: string | null;
  valiInfo: ValiInfo | null;
}) {
  return (
    <div className="engine-footer">
      <div className="engine-footer-main">
        <span className="engine-footer-title">Engine</span>
        <span className="engine-kv">
          <span className="k">Studio engine</span>
          <span className="v">
            {version ?? valiInfo?.studioEngineVersion ?? "—"}
          </span>
        </span>
        <span className="engine-kv">
          <span className="k">Latest release</span>
          <span className="v">
            {valiInfo?.latestValiVersion ? (
              <a href={valiInfo.nugetUrl} target="_blank" rel="noreferrer">
                {valiInfo.latestValiVersion}
              </a>
            ) : (
              "unknown"
            )}
          </span>
        </span>
        <span className="grow" />
        <span className="engine-kv folder">
          <span className="k">Data folder</span>
          <span className="v">{dataFolder ?? "—"}</span>
        </span>
      </div>
      <div className="hint engine-footer-note">
        Vali Studio bundles its own Vali engine — you don&apos;t need to install
        the CLI to generate maps.
      </div>
    </div>
  );
}
