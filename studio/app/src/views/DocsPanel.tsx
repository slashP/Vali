import { useMemo, useState } from "react";
import { useStore } from "../store";
import { buildDocEntries, GROUP_ORDER, type DocEntry } from "../lib/schemaDocs";
import {
  BookIcon,
  CheckIcon,
  CopyIcon,
  SearchIcon,
  WinCloseIcon,
} from "../components/Icons";

const ENUM_CAP = 18;

function entryId(e: DocEntry): string {
  return `${e.group}.${e.key}`;
}

export function DocsPanel() {
  const [query, setQuery] = useState("");
  const [copied, setCopied] = useState<string | null>(null);

  const entries = useMemo(() => buildDocEntries(), []);
  const q = query.trim().toLowerCase();
  const filtered = useMemo(
    () => (q ? entries.filter((e) => e.haystack.includes(q)) : entries),
    [entries, q],
  );

  const groups = useMemo(() => {
    const byGroup = new Map<string, DocEntry[]>();
    for (const e of filtered) {
      const arr = byGroup.get(e.group);
      if (arr) arr.push(e);
      else byGroup.set(e.group, [e]);
    }
    const ordered: Array<[string, DocEntry[]]> = [];
    for (const g of GROUP_ORDER) {
      const arr = byGroup.get(g);
      if (arr) {
        ordered.push([g, arr]);
        byGroup.delete(g);
      }
    }
    for (const leftover of byGroup) ordered.push(leftover);
    return ordered;
  }, [filtered]);

  const copy = async (e: DocEntry) => {
    const id = entryId(e);
    try {
      await navigator.clipboard.writeText(e.snippet);
      setCopied(id);
      window.setTimeout(
        () => setCopied((c) => (c === id ? null : c)),
        1300,
      );
    } catch {
      // Clipboard unavailable — no-op; the snippet is still visible on the row.
    }
  };

  return (
    <div className="docs-panel">
      <div className="docs-head">
        <span className="docs-glyph">
          <BookIcon size={15} />
        </span>
        <span className="docs-title">Documentation</span>
        <span className="grow" />
        <button
          className="icon-btn"
          title="Close documentation"
          aria-label="Close documentation"
          onClick={() => useStore.getState().setDocsOpen(false)}
        >
          <WinCloseIcon />
        </button>
      </div>

      <div className="docs-search">
        <SearchIcon size={14} />
        <input
          autoFocus
          placeholder="Search settings…"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
        />
        {query && (
          <button
            className="docs-clear"
            aria-label="Clear search"
            onClick={() => setQuery("")}
          >
            ×
          </button>
        )}
      </div>

      <div className="docs-list">
        {filtered.length === 0 ? (
          <div className="docs-empty">
            No settings match “{query}”.
          </div>
        ) : (
          groups.map(([group, items]) => (
            <div className="docs-group" key={group}>
              <div className="docs-group-head">
                {group}
                <span className="docs-group-count">{items.length}</span>
              </div>
              {items.map((e) => (
                <DocRow
                  key={entryId(e)}
                  entry={e}
                  copied={copied === entryId(e)}
                  onCopy={() => void copy(e)}
                />
              ))}
            </div>
          ))
        )}
      </div>

      <div className="docs-foot hint">
        Click a setting to copy a starter snippet to your clipboard.
      </div>
    </div>
  );
}

function DocRow({
  entry,
  copied,
  onCopy,
}: {
  entry: DocEntry;
  copied: boolean;
  onCopy: () => void;
}) {
  const shownEnums = entry.enumValues.slice(0, ENUM_CAP);
  const extraEnums = entry.enumValues.length - shownEnums.length;

  return (
    <button className="docs-row" onClick={onCopy} title="Copy starter snippet">
      <div className="docs-row-top">
        <span className="docs-key">
          {entry.key}
          {entry.required && (
            <span className="docs-req" title="Required">
              *
            </span>
          )}
        </span>
        <span className="docs-type">{entry.type}</span>
        <span className="grow" />
        <span className={`docs-copy${copied ? " ok" : ""}`}>
          {copied ? <CheckIcon size={13} /> : <CopyIcon size={13} />}
        </span>
      </div>

      {entry.description && <div className="docs-desc">{entry.description}</div>}

      {shownEnums.length > 0 && (
        <div className="docs-enums">
          {shownEnums.map((v) => (
            <span className="docs-enum" key={v}>
              {v}
            </span>
          ))}
          {extraEnums > 0 && (
            <span className="docs-enum more">+{extraEnums} more</span>
          )}
        </div>
      )}

      {entry.examples.length > 0 && (
        <div className="docs-examples">
          {entry.examples.map((ex, i) => (
            <code className="docs-example" key={i}>
              {ex}
            </code>
          ))}
        </div>
      )}
    </button>
  );
}
