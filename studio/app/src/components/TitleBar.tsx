import { useEffect, useState } from "react";
import { useStore } from "../store";
import {
  closeWindow,
  isWindowMaximized,
  minimizeWindow,
  onWindowResized,
  startResize,
  toggleMaximizeWindow,
  type ResizeDirection,
} from "../lib/window";
import {
  CompassIcon,
  WinCloseIcon,
  WinMaximizeIcon,
  WinMinimizeIcon,
  WinRestoreIcon,
} from "./Icons";
import { IS_MAC } from "../lib/platform";

export function TitleBar() {
  const maps = useStore((s) => s.maps);
  const currentSlug = useStore((s) => s.currentSlug);
  const dirty = useStore((s) => s.dirty);
  const [maximized, setMaximized] = useState(false);

  useEffect(() => {
    let unlisten: (() => void) | undefined;
    let alive = true;
    void isWindowMaximized().then((m) => alive && setMaximized(m));
    void onWindowResized(() => {
      void isWindowMaximized().then((m) => alive && setMaximized(m));
    }).then((fn) => {
      if (alive) unlisten = fn;
      else fn();
    });
    return () => {
      alive = false;
      unlisten?.();
    };
  }, []);

  const currentName = maps.find((m) => m.slug === currentSlug)?.name ?? null;

  return (
    <div className={`titlebar${IS_MAC ? " mac" : ""}`} data-tauri-drag-region>
      <div className="titlebar-brand" data-tauri-drag-region>
        <span className="brand-mark">
          <CompassIcon size={17} />
        </span>
        <span className="brand-word">
          Vali<span className="brand-word-dim">Studio</span>
        </span>
      </div>

      <div className="titlebar-center" data-tauri-drag-region>
        {currentName && (
          <span className="titlebar-doc">
            {currentName}
            {dirty && <span className="titlebar-dirty" title="Unsaved edits" />}
          </span>
        )}
      </div>

      {/* macOS draws native traffic-light controls; only Windows/Linux need
          our custom window buttons. */}
      {!IS_MAC && (
        <div className="titlebar-controls">
          <button
            className="win-btn"
            onClick={() => void minimizeWindow()}
            title="Minimize"
            aria-label="Minimize"
          >
            <WinMinimizeIcon />
          </button>
          <button
            className="win-btn"
            onClick={() => void toggleMaximizeWindow()}
            title={maximized ? "Restore" : "Maximize"}
            aria-label={maximized ? "Restore" : "Maximize"}
          >
            {maximized ? <WinRestoreIcon /> : <WinMaximizeIcon />}
          </button>
          <button
            className="win-btn win-close"
            onClick={() => void closeWindow()}
            title="Close"
            aria-label="Close"
          >
            <WinCloseIcon />
          </button>
        </div>
      )}
    </div>
  );
}

const HANDLES: { cls: string; dir: ResizeDirection }[] = [
  { cls: "rz-n", dir: "North" },
  { cls: "rz-s", dir: "South" },
  { cls: "rz-e", dir: "East" },
  { cls: "rz-w", dir: "West" },
  { cls: "rz-ne", dir: "NorthEast" },
  { cls: "rz-nw", dir: "NorthWest" },
  { cls: "rz-se", dir: "SouthEast" },
  { cls: "rz-sw", dir: "SouthWest" },
];

/** Invisible edge/corner grips — native decorations are off, so we drive
 *  window resizing ourselves for consistent behaviour across platforms. */
export function ResizeHandles() {
  return (
    <>
      {HANDLES.map(({ cls, dir }) => (
        <div
          key={cls}
          className={`resize-handle ${cls}`}
          onMouseDown={(e) => {
            if (e.button !== 0) return;
            void startResize(dir);
          }}
        />
      ))}
    </>
  );
}
