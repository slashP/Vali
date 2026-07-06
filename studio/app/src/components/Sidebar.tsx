import { useCallback, useEffect, useRef, useState } from "react";
import { useStore } from "../store";
import { ConfirmModal, Modal } from "./Modal";
import {
  CollapseIcon,
  ExpandIcon,
  PencilIcon,
  PlusIcon,
  ImportIcon,
  ExportIcon,
  TrashIcon,
} from "./Icons";
import {
  pickOpenFile,
  pickSaveFile,
  readTextFile,
  writeTextFile,
} from "../lib/commands";
import { readDefinition, type MapEntry } from "../lib/workspace";
import { mapGradient } from "../lib/gradient";
import { logError } from "../lib/log";

const LS_WIDTH = "vali.sidebarWidth";
const LS_COLLAPSED = "vali.sidebarCollapsed";
const MIN_W = 200;
const MAX_W = 420;
const COLLAPSED_W = 60;

function baseName(path: string): string {
  const parts = path.split(/[/\\]/);
  return parts[parts.length - 1] || path;
}

function initialWidth(): number {
  const stored = Number(localStorage.getItem(LS_WIDTH));
  if (Number.isFinite(stored) && stored >= MIN_W && stored <= MAX_W) return stored;
  return 264;
}

/** Short label for the collapsed-rail avatar: initials of the map name. */
function avatarLabel(m: MapEntry): string {
  const words = m.name.trim().split(/\s+/).filter(Boolean);
  const letters = words
    .slice(0, 2)
    .map((w) => w[0] ?? "")
    .join("");
  return (letters || m.name.slice(0, 2) || "?").toUpperCase();
}

export function Sidebar() {
  const maps = useStore((s) => s.maps);
  const currentSlug = useStore((s) => s.currentSlug);

  const [collapsed, setCollapsed] = useState(
    () => localStorage.getItem(LS_COLLAPSED) === "true",
  );
  const [width, setWidth] = useState(initialWidth);
  const [dragging, setDragging] = useState(false);

  const [prompt, setPrompt] = useState<{ kind: "new" } | null>(null);
  const [opError, setOpError] = useState<string | null>(null);
  const [details, setDetails] = useState<MapEntry | null>(null);
  const [confirmDelete, setConfirmDelete] = useState<MapEntry | null>(null);
  const [menuSlug, setMenuSlug] = useState<string | null>(null);

  const rootRef = useRef<HTMLDivElement | null>(null);

  const onMove = useCallback((clientX: number) => {
    const el = rootRef.current;
    if (!el) return;
    const left = el.getBoundingClientRect().left;
    setWidth(Math.min(MAX_W, Math.max(MIN_W, clientX - left)));
  }, []);

  useEffect(() => {
    if (!dragging) return;
    const move = (e: MouseEvent) => onMove(e.clientX);
    const up = () => setDragging(false);
    window.addEventListener("mousemove", move);
    window.addEventListener("mouseup", up);
    return () => {
      window.removeEventListener("mousemove", move);
      window.removeEventListener("mouseup", up);
    };
  }, [dragging, onMove]);

  useEffect(() => {
    localStorage.setItem(LS_WIDTH, String(width));
  }, [width]);
  useEffect(() => {
    localStorage.setItem(LS_COLLAPSED, String(collapsed));
  }, [collapsed]);

  const importMap = async () => {
    try {
      const path = await pickOpenFile("Vali map definition", ["json", "jsonc"]);
      if (!path) return;
      const content = await readTextFile(path);
      const name = baseName(path).replace(/\.jsonc?$/i, "");
      await useStore.getState().importMap(name || "Imported map", content);
      setOpError(null);
    } catch (e) {
      logError("importMap", e);
      setOpError("Couldn't import that file.");
    }
  };

  const exportMap = async (entry: MapEntry) => {
    try {
      const root = useStore.getState().workspaceRoot;
      if (!root) return;
      const content = await readDefinition(root, entry.slug);
      const path = await pickSaveFile(`${entry.name}.json`);
      if (!path) return;
      await writeTextFile(path, content);
      setOpError(null);
    } catch (e) {
      logError("exportMap", e);
      setOpError("Couldn't export the map.");
    }
  };

  const modals = (
    <>
      {prompt?.kind === "new" && (
        <MapFormModal
          title="New map"
          confirmLabel="Create"
          initialName="New map"
          initialDescription=""
          onCancel={() => setPrompt(null)}
          onSubmit={(name, description) => {
            setPrompt(null);
            void useStore.getState().createNewMap(name, description);
          }}
        />
      )}
      {details && (
        <MapFormModal
          title="Map details"
          confirmLabel="Save"
          initialName={details.name}
          initialDescription={details.description ?? ""}
          onCancel={() => setDetails(null)}
          onSubmit={(name, description) => {
            const slug = details.slug;
            setDetails(null);
            void useStore.getState().updateMapDetails(slug, { name, description });
          }}
        />
      )}
      {confirmDelete && (
        <ConfirmModal
          title="Delete map"
          message={
            <>
              Delete <strong>{confirmDelete.name}</strong> and all its generated
              output files? This cannot be undone.
            </>
          }
          confirmLabel="Delete"
          danger
          onCancel={() => setConfirmDelete(null)}
          onConfirm={() => {
            const slug = confirmDelete.slug;
            setConfirmDelete(null);
            void useStore.getState().deleteMapBySlug(slug);
          }}
        />
      )}
    </>
  );

  if (collapsed) {
    return (
      <nav ref={rootRef} className="sidebar collapsed" style={{ width: COLLAPSED_W }}>
        <div className="sidebar-rail-head">
          <button
            className="icon-btn"
            title="Expand sidebar"
            aria-label="Expand sidebar"
            onClick={() => setCollapsed(false)}
          >
            <ExpandIcon size={16} />
          </button>
          <button
            className="icon-btn new-map-btn"
            title="New map"
            aria-label="New map"
            onClick={() => setPrompt({ kind: "new" })}
          >
            <PlusIcon size={16} />
          </button>
        </div>
        <div className="map-rail">
          {maps.map((m) => (
            <button
              key={m.slug}
              className={`map-avatar${m.slug === currentSlug ? " active" : ""}`}
              title={m.name}
              style={{ background: mapGradient(m.slug).css }}
              onClick={() => void useStore.getState().selectMap(m.slug)}
            >
              {avatarLabel(m)}
            </button>
          ))}
        </div>
        <div className="sidebar-foot rail-foot">
          <button
            className="icon-btn"
            title="Import map"
            aria-label="Import map"
            onClick={() => void importMap()}
          >
            <ImportIcon size={15} />
          </button>
        </div>
        {modals}
      </nav>
    );
  }

  return (
    <nav
      ref={rootRef}
      className={`sidebar${dragging ? " resizing" : ""}`}
      style={{ width }}
    >
      <div className="sidebar-head">
        <span className="sidebar-title">Maps</span>
        <span className="sidebar-count">{maps.length}</span>
        <button
          className="icon-btn new-map-btn"
          onClick={() => setPrompt({ kind: "new" })}
          title="New map"
          aria-label="New map"
        >
          <PlusIcon size={16} />
        </button>
        <button
          className="icon-btn"
          onClick={() => setCollapsed(true)}
          title="Collapse sidebar"
          aria-label="Collapse sidebar"
        >
          <CollapseIcon size={15} />
        </button>
      </div>

      <div className="map-list">
        {maps.map((m) => {
          const active = m.slug === currentSlug;
          return (
            <div
              key={m.slug}
              className={`map-item${active ? " active" : ""}`}
              title={m.description || undefined}
              onClick={() => void useStore.getState().selectMap(m.slug)}
              role="button"
              tabIndex={0}
              onKeyDown={(e) => {
                if (e.key === "Enter" || e.key === " ") {
                  e.preventDefault();
                  void useStore.getState().selectMap(m.slug);
                }
              }}
            >
              <div className="map-item-top">
                <span
                  className="map-swatch"
                  style={{ background: mapGradient(m.slug).css }}
                />
                <span className="map-item-name">{m.name}</span>
                <button
                  className="icon-btn kebab"
                  title="Map actions"
                  aria-label="Map actions"
                  onClick={(e) => {
                    e.stopPropagation();
                    setMenuSlug((s) => (s === m.slug ? null : m.slug));
                  }}
                >
                  <span className="kebab-dots" />
                </button>
              </div>
              {m.description && (
                <div className="map-item-desc">{m.description}</div>
              )}
              {m.goal != null && (
                <div className="map-item-goal">
                  {m.goal.toLocaleString()} locations
                </div>
              )}

              {menuSlug === m.slug && (
                <>
                  <div
                    className="menu-backdrop"
                    onClick={(e) => {
                      e.stopPropagation();
                      setMenuSlug(null);
                    }}
                  />
                  <div className="row-menu" onClick={(e) => e.stopPropagation()}>
                    <button
                      onClick={() => {
                        setMenuSlug(null);
                        setDetails(m);
                      }}
                    >
                      <PencilIcon size={14} /> Edit details…
                    </button>
                    <button
                      onClick={() => {
                        setMenuSlug(null);
                        void exportMap(m);
                      }}
                    >
                      <ExportIcon size={14} /> Export…
                    </button>
                    <button
                      className="menu-danger"
                      onClick={() => {
                        setMenuSlug(null);
                        setConfirmDelete(m);
                      }}
                    >
                      <TrashIcon size={14} /> Delete
                    </button>
                  </div>
                </>
              )}
            </div>
          );
        })}
      </div>

      <div className="sidebar-foot">
        {opError && <div className="sidebar-op-error">{opError}</div>}
        <button className="ghost-wide" onClick={() => void importMap()}>
          <ImportIcon size={14} /> Import map
        </button>
      </div>

      <div
        className="sidebar-resize"
        onMouseDown={(e) => {
          e.preventDefault();
          setDragging(true);
        }}
        title="Drag to resize"
      >
        <span className="resize-grip" />
      </div>

      {modals}
    </nav>
  );
}

function MapFormModal({
  title,
  confirmLabel,
  initialName,
  initialDescription,
  onCancel,
  onSubmit,
}: {
  title: string;
  confirmLabel: string;
  initialName: string;
  initialDescription: string;
  onCancel: () => void;
  onSubmit: (name: string, description: string) => void;
}) {
  const [name, setName] = useState(initialName);
  const [description, setDescription] = useState(initialDescription);
  const trimmed = name.trim();
  return (
    <Modal
      title={title}
      width={460}
      onClose={onCancel}
      footer={
        <>
          <button onClick={onCancel}>Cancel</button>
          <button
            className="primary"
            disabled={!trimmed}
            onClick={() => onSubmit(trimmed, description.trim())}
          >
            {confirmLabel}
          </button>
        </>
      }
    >
      <label className="field-label">Name</label>
      {/* eslint-disable-next-line jsx-a11y/no-autofocus */}
      <input
        autoFocus
        value={name}
        onChange={(e) => setName(e.target.value)}
        style={{ width: "100%" }}
      />
      <label className="field-label" style={{ marginTop: 12 }}>
        Description
      </label>
      <textarea
        value={description}
        onChange={(e) => setDescription(e.target.value)}
        placeholder="What's this map for? (optional)"
        style={{ width: "100%", minHeight: 84 }}
      />
    </Modal>
  );
}
