# Vali Studio — UI redesign (map-centric shell)

Design note for the shift from a 4-tab shell (Editor/Run/Map/Data) to a
map-library shell with a live editor+map workspace.

## Decisions

- **Storage**: unchanged — maps stay on disk as per-map folders
  (`<root>/<slug>/definition.json` + `meta.json`), outputs alongside. The
  engine generates from a real path, so files remain the natural fit. The new
  sidebar is a view over the same workspace, enriched with country codes parsed
  best-effort from each definition.
- **Run UI**: no Run tab. Generation progress + shortfall overlay the map pane
  (which already renders live sample dots). Dismissible.
- **AI**: same flow (instruction → inline diff → Apply/Discard), redocked
  *under* the editor with its own identity — "Vali AI", violet accent.
- **Titlebar**: custom, native decorations off. Wordmark + current map name +
  working window controls. Custom edge resize handles guarantee resizability.

## Shell

```
┌ titlebar (drag region · graticule motif) ──── Vali Studio · <map>  – ▢ ✕ ┐
├ maps sidebar ┬ workspace ───────────────────────────────────────────────┤
│  Maps    (n) │  ┌ editor toolbar ─────────────┐║ map (deck.gl)           │
│  ▸ Map A  US │  │ Definition · ● Valid   ▷ ⏵  │║  ┌ run overlay ┐        │
│  ▸ Map B  NO │  ├ monaco + errors ───────────┤║  │ progress…   │        │
│    …         │  ├ Vali AI (dock, collapsible)│║  └─────────────┘        │
│  + New map   │  └─────────────────────────────┘║  (draggable split)      │
├──────────────┴──────────────────────────────────────────────────────────┤
│ ● Engine v0.1.0        🗄 5 countries · 2 need update  [Data]        ⟳     │
└───────────────────────────────────────────────────────────────────────────┘
```

## Map honesty (always reflects the JSON)

The map pane only ever shows locations that match the current definition. The
store records `mapText` — the definition text the displayed points were produced
from. When the editor text diverges from `mapText`, the map is *stale*: it blurs
and shows "Definition changed — generate again to update the map" with
Preview/Generate actions. In sync after a generation (or when auto-loading a
map's newest output); not tied when an external file is loaded.

## Signature elements

1. Faint lat/long graticule in the titlebar + a small compass glyph by the
   wordmark — the cartographic thesis, kept subtle.
2. Country-code chips (mono) as each map's geographic fingerprint in the list.
3. Active map marked with an accent spine, like a tab in an atlas.

Motion stays minimal (hover transitions, gentle fades); `prefers-reduced-motion`
respected.
