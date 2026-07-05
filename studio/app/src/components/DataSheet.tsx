import { useEffect } from "react";
import { useStore } from "../store";
import { DataView } from "../views/DataView";
import { WinCloseIcon } from "./Icons";

export function DataSheet() {
  const open = useStore((s) => s.dataOpen);

  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") useStore.getState().setDataOpen(false);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open]);

  if (!open) return null;

  return (
    <div
      className="sheet-overlay"
      onClick={() => useStore.getState().setDataOpen(false)}
    >
      <div className="sheet" onClick={(e) => e.stopPropagation()}>
        <div className="sheet-head">
          <span className="sheet-title">Location data</span>
          <span className="grow" />
          <button
            className="icon-btn"
            aria-label="Close"
            title="Close"
            onClick={() => useStore.getState().setDataOpen(false)}
          >
            <WinCloseIcon />
          </button>
        </div>
        <div className="sheet-body">
          <DataView />
        </div>
      </div>
    </div>
  );
}
