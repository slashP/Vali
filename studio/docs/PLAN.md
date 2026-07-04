# Vali Studio — design & implementation plan

A desktop companion app for [Vali](https://github.com/slashP/Vali): visual editing of map
definitions, running generation with live progress and cancel, previewing the resulting map,
and AI-assisted editing. Tauri 2 (Rust) + React frontend + a .NET sidecar that references
`Vali.Core` directly.

Full background: the feasibility research and adversarial review live in the session that
produced this branch; the decisions below are its outcome.

## Architecture

```
┌────────────────────────────── Tauri app ──────────────────────────────┐
│  React 19 + Vite + TS                                                 │
│  Editor (Monaco + vali.schema.json + AI panel)                        │
│  Run view (progress grid, stop)   Map view (MapLibre + deck.gl)       │
│  Data view (downloaded countries)                                     │
├──────────────────────────── Rust (src-tauri) ─────────────────────────┤
│  sidecar.rs  spawn Vali.Host, NDJSON request/response correlation,    │
│              forward events to the webview                            │
│  ai.rs       spawn `claude -p --output-format json` (subscription     │
│              auth — never --bare), detect availability                │
│  fs commands read/write definition + locations files, file dialogs    │
└──────────────────────────────┬────────────────────────────────────────┘
                               │ NDJSON over stdin/stdout (docs/PROTOCOL.md)
                     ┌─────────┴─────────┐
                     │ Vali.Host (.NET)  │  references src/Vali.Core
                     │ validate/express/ │  ConsoleLogger.Silent stays false —
                     │ generate/cancel/  │  console output is captured and
                     │ countries/data    │  returned as validation messages
                     └───────────────────┘
```

Key decisions (from the review):

- **Monaco/JSON is the single source of truth.** No generic schema-driven form layer; structured
  panels come later and patch the JSON textually (comments must survive — Vali accepts JSONC).
- **The sidecar re-orchestrates `LocationLakeMapGenerator.Generate`** (~60 lines) against public
  APIs: `DistributionStrategies.*` for work items, `MemoryAwareScheduler.RunWithMemoryBudget`
  with our own `CancellationToken` (stop = drain in-flight, keep partials — same as the CLI's
  `s` key), `GenerationProgress` for snapshots, and a faithful replica of `StoreMap` for output.
- **Preview = a real scaled-down generation** (`previewGoal`), pano verification skipped, output
  to `<name>-preview-locations.json`. Count-goal strategies only in v1.
- **Never call `ValiLogger.Initialize()`** → no telemetry from the sidecar.
- **AI copilot** shells out to the user's installed Claude Code CLI (`claude -p`), which draws on
  their Claude subscription — no API key. Edits come back as full JSON, are validated through the
  sidecar, and are shown as a Monaco diff for approval before applying. Ollama backend later.

## Not in v1 (deliberate)

- Pano verification in Studio runs (a `warning` event tells the user to use the CLI for that).
- Downloads from Studio (Data view links to `vali download` in a terminal); sidecar download
  command comes later.
- Candidate-pool cache for fast repeated previews (design exists; add when preview loop feels slow).
- `MaxCountByFixedMinDistance`/`EvenlyByDistanceWithinCountry` preview (needs distance inflation).
- live-generate, Ollama backend, packaging/installer/signing, deterministic parity CI.

## Repo layout

Everything under `studio/` — zero changes to upstream Vali files.

```
studio/
  docs/PLAN.md, docs/PROTOCOL.md
  sidecar/Vali.Host/          .NET 10 console app (project-references ../../src/Vali.Core)
  app/                        Vite + React frontend
  app/src-tauri/              Rust shell
```

Dev workflow: `dotnet build studio/sidecar/Vali.Host -c Release`, then `npm run tauri dev` in
`studio/app`. The Rust shell finds the sidecar exe via `VALI_HOST_PATH` or the default
Release/Debug build output paths.

## Verification

- `dotnet build` clean (warnings as errors inherited from Vali.Core's settings stay green).
- Sidecar smoke test: pipe `ping`/`validate`/`expressionCheck` requests through stdin, assert
  responses (scripted, no Tauri needed).
- `npm run build` (tsc + vite) and `cargo check` clean.
- Deterministic parity test vs the CLI (`VALI_DETERMINISTIC=1`) — planned as CI, manual for now.
