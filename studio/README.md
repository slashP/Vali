# Vali Studio

A desktop companion app for Vali: edit map definitions visually, run generation with live
progress and a working Stop button, preview the resulting map before uploading, and ask
Claude to edit your definition for you.

- Frontend: Tauri 2 + React 19 (`app/`)
- Sidecar: .NET 10 host referencing `Vali.Core` directly (`sidecar/Vali.Host/`)
- Contract between them: `docs/PROTOCOL.md` (NDJSON over stdio)
- Design & scope decisions: `docs/PLAN.md`

## Prerequisites

- .NET 10 SDK
- Node.js 20+
- Rust (stable) — for the Tauri shell
- Vali location data downloaded locally (`vali download --country "IE"` etc.)
- Optional, for the AI panel: [Claude Code](https://claude.com/claude-code) installed and
  logged in (`claude` on PATH). Uses your Claude subscription — no API key.

## Build & run (dev)

```powershell
# 1. Build the sidecar
dotnet build studio/sidecar/Vali.Host -c Release

# 2. Run the app
cd studio/app
npm install
npm run tauri dev
```

The Rust shell locates the sidecar exe via the `VALI_HOST_PATH` environment variable, falling
back to the Release then Debug build output paths.

## Verifying the sidecar

```powershell
# Protocol smoke test (ping/validate/expressions/countries/data)
./studio/sidecar/smoke-test.ps1

# Parity firewall: sidecar generation must be byte-identical to the CLI (VALI_DETERMINISTIC=1)
./studio/sidecar/parity-test.ps1 -DefinitionPath C:\maps\IE.json

# Mid-run cancellation (stop admitting, drain in-flight, save partials)
node studio/sidecar/cancel-test.mjs C:\maps\IE.json
```

All three were green at the time this was committed (IE, 497/500 locations, SHA256-identical
CLI/sidecar outputs).

## Current status / known limitations (v0.1)

- Preview runs require a count-goal strategy (`FixedCountByMaxMinDistance`,
  `FixedCountByCoverageDensity`).
- Pano verification is skipped (a warning event is emitted); use the Vali CLI for it.
- Downloads still go through `vali download` in a terminal; the Data view links to it.
- One generation at a time per sidecar.
- The AI panel is single-shot (no conversation memory yet) and requires user approval of a
  validated diff before anything is applied.
