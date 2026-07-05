import { useCallback, useEffect, useRef, useState } from "react";
import { useStore } from "../store";
import { EditorView } from "./EditorView";
import { DocsPanel } from "./DocsPanel";
import { MapView } from "./MapView";
import { BookIcon } from "../components/Icons";
import { PaneErrorBoundary } from "../components/PaneErrorBoundary";
import { MOD } from "../lib/keybindings";

const LS_SPLIT = "vali.splitFrac";
const LS_DOCS_WIDTH = "vali.docsWidth";
const MIN = 0.25;
const MAX = 0.75;
const DOCS_MIN = 260;
const DOCS_MAX = 620;

function initialFrac(): number {
  const stored = Number(localStorage.getItem(LS_SPLIT));
  if (Number.isFinite(stored) && stored >= MIN && stored <= MAX) return stored;
  return 0.5;
}

function initialDocsWidth(): number {
  const stored = Number(localStorage.getItem(LS_DOCS_WIDTH));
  if (Number.isFinite(stored) && stored >= DOCS_MIN && stored <= DOCS_MAX)
    return stored;
  return 340;
}

type DragTarget = "editor" | "docs" | null;

export function Workspace() {
  const docsOpen = useStore((s) => s.docsOpen);

  const containerRef = useRef<HTMLDivElement | null>(null);
  const docsRef = useRef<HTMLDivElement | null>(null);
  const [frac, setFrac] = useState(initialFrac);
  const [docsWidth, setDocsWidth] = useState(initialDocsWidth);
  const [drag, setDrag] = useState<DragTarget>(null);

  const onMove = useCallback(
    (clientX: number) => {
      const el = containerRef.current;
      if (!el) return;
      const rect = el.getBoundingClientRect();
      if (drag === "editor") {
        const next = (clientX - rect.left) / rect.width;
        setFrac(Math.min(MAX, Math.max(MIN, next)));
      } else if (drag === "docs") {
        const docsLeft = docsRef.current?.getBoundingClientRect().left ?? rect.left;
        const next = clientX - docsLeft;
        setDocsWidth(Math.min(DOCS_MAX, Math.max(DOCS_MIN, next)));
      }
    },
    [drag],
  );

  useEffect(() => {
    if (!drag) return;
    const move = (e: MouseEvent) => onMove(e.clientX);
    const up = () => setDrag(null);
    window.addEventListener("mousemove", move);
    window.addEventListener("mouseup", up);
    return () => {
      window.removeEventListener("mousemove", move);
      window.removeEventListener("mouseup", up);
    };
  }, [drag, onMove]);

  useEffect(() => {
    localStorage.setItem(LS_SPLIT, String(frac));
  }, [frac]);

  useEffect(() => {
    localStorage.setItem(LS_DOCS_WIDTH, String(docsWidth));
  }, [docsWidth]);

  return (
    <div className={`workspace${drag ? " dragging" : ""}`} ref={containerRef}>
      <div className="pane pane-editor" style={{ flexBasis: `${frac * 100}%` }}>
        <PaneErrorBoundary label="editor">
          <EditorView />
        </PaneErrorBoundary>
      </div>

      <div
        className="split-divider"
        onMouseDown={(e) => {
          e.preventDefault();
          setDrag("editor");
        }}
        onDoubleClick={() => setFrac(0.5)}
        role="separator"
        aria-orientation="vertical"
        title="Drag to resize · double-click to reset"
      >
        <span className="split-grip" />
      </div>

      {docsOpen && (
        <>
          <div
            className="pane pane-docs"
            ref={docsRef}
            style={{ flexBasis: docsWidth, width: docsWidth }}
          >
            <PaneErrorBoundary label="documentation">
              <DocsPanel />
            </PaneErrorBoundary>
          </div>
          <div
            className="split-divider"
            onMouseDown={(e) => {
              e.preventDefault();
              setDrag("docs");
            }}
            role="separator"
            aria-orientation="vertical"
            title="Drag to resize documentation"
          >
            <span className="split-grip" />
          </div>
        </>
      )}

      <div className="pane pane-map">
        <PaneErrorBoundary label="map">
          <MapView />
        </PaneErrorBoundary>
      </div>

      <button
        className={`docs-fab${docsOpen ? " active" : ""}`}
        onClick={() => useStore.getState().setDocsOpen(!docsOpen)}
        title="Browse the map-definition settings"
        aria-pressed={docsOpen}
      >
        <BookIcon size={15} />
        <span>Docs</span>
        <span className="btn-kbd">
          <kbd>{MOD}</kbd>
          <kbd>⇧</kbd>
          <kbd>D</kbd>
        </span>
      </button>

      {drag && <div className="drag-shield" />}
    </div>
  );
}
