import { useStore } from "../store";

function fileName(path: string | null): string {
  if (!path) return "untitled";
  const parts = path.split(/[/\\]/);
  return parts[parts.length - 1] || path;
}

export function StatusBar() {
  const alive = useStore((s) => s.sidecarAlive);
  const version = useStore((s) => s.version);
  const dataFolder = useStore((s) => s.dataFolder);
  const filePath = useStore((s) => s.filePath);
  const dirty = useStore((s) => s.dirty);

  return (
    <div className="statusbar">
      <span>
        <span className={`dot ${alive ? "ok" : "bad"}`} />
        {alive ? `Sidecar${version ? ` v${version}` : ""}` : "Sidecar offline"}
      </span>
      <span title={dataFolder ?? undefined}>
        Data: {dataFolder ?? "unknown"}
      </span>
      <span className="spacer" />
      <span>
        {fileName(filePath)}
        {dirty ? " •" : ""}
      </span>
    </div>
  );
}
