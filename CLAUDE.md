# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is Vali?

Vali is a .NET CLI tool for generating GeoGuessr maps from a database of 100M+ Street View locations. Users define maps via JSON files (MapDefinition) specifying countries, distribution strategies, filters, and output settings. Vali reads pre-downloaded binary location data (Protocol Buffers), applies filters and distribution algorithms, and outputs location sets.

Distributed as a global .NET tool (`dotnet tool install -g vali`).

## Build & Test Commands

```bash
dotnet build Vali.sln
dotnet test tests/Vali.Core.Tests
dotnet test tests/Vali.Core.Tests --filter "FullyQualifiedName~ClassName.MethodName"
dotnet run --project src/Vali
```

The `Vali` project has a pre-build target that runs `SchemaGenerator` to regenerate JSON schemas in `src/Vali/Schemas/`. This runs automatically on every build.

`Vali.Core` has `TreatWarningsAsErrors` enabled.

## Project Structure

- **`src/Vali`** — CLI entry point (`Program.cs` is a top-level statements file using `System.CommandLine`). Defines commands: `generate`, `download`, `live-generate`, `subdivisions`, `countries`, `report`, `create-file`, `distribute-from-file`.
- **`src/Vali.Core`** — All business logic. No dependency on the CLI layer.
- **`tests/Vali.Core.Tests`** — xUnit tests with Shouldly assertions.
- **`tests/Vali.Benchmarks`** — BenchmarkDotNet performance benchmarks.
- **`tools/SchemaGenerator`** — Generates `vali.schema.json` and `vali-live-generate.schema.json` from C# types (MapDefinition, LiveGenerateMapDefinition).

## Architecture

### Core Data Flow

1. **MapDefinition** (JSON input) → parsed and validated by `GenerateFileValidator`
2. Defaults applied via `MapDefinitionDefaults.ApplyDefaults()`, then validated
3. **LocationLakeMapGenerator.Generate()** orchestrates:
   - Iterates countries/subdivisions
   - Reads binary `.bin` files via **LocationReader** (protobuf deserialization)
   - Applies distribution strategy via **DistributionStrategies**
   - Filters locations via **LocationLakeFilterer** (expression-based)
   - Applies neighbor/proximity/geometry filters
   - **LocationDistributor** selects final locations (binary search for optimal min-distance)
4. Output written as JSON location set

### Key Types

- **`Location`** (`Location.cs`) — Core record with `GoogleData`, `OsmData`, `NominatimData`. Protobuf-serialized for storage.
- **`MapDefinition`** (`MapDefinition.cs`) — Complete map specification: countries, distribution, filters (global/country/subdivision levels), output config.
- **`DistributionStrategies`** — Four strategies: `FixedCountByMaxMinDistance`, `MaxCountByFixedMinDistance`, `FixedCountByCoverageDensity`, `EvenlyByDistanceWithinCountry`.
- **`LocationLakeFilterer`** — Applies expression-based filters using `System.Linq.Dynamic.Core`. Supports named expressions (`$$variable` syntax).
- **`Hasher`** — Geohash encoding/decoding with neighbor caching for spatial queries.
- **`DataDownloadService`** — Downloads location data from Cloudflare R2 (`vali-download.slashp.workers.dev`). Supports full and incremental updates with BZip2 decompression.

### Filter Hierarchy

Filters apply at three levels: global, per-country, per-subdivision. Types:
- **Location filters** — Boolean expressions on Location properties (e.g., `Google.Year >= 2020`)
- **Preference filters** — Percentage-based allocation (e.g., 25% must match expression)
- **Proximity filters** — Include locations within radius of reference points
- **Geometry filters** — GeoJSON polygon inclusion/exclusion
- **Neighbor filters** — Require neighboring locations to meet criteria

### Validation

`Vali.Core/Validation/` contains validators that run before map generation: `GenerateFileValidator`, `FilterValidation`, `DistributionStrategyValidation`, `OutputValidation`, `InclusionValidation`, `DistributionValidation`.

## Technology

- .NET 8 (net8.0), C# 13, nullable reference types
- protobuf-net for binary location serialization
- System.CommandLine for CLI
- Spectre.Console for rich terminal output
- NetTopologySuite for GeoJSON/geometry
- OpenTelemetry for logging/telemetry
