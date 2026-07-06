# Vali Studio — sidecar protocol (NDJSON over stdio)

The Tauri app spawns `Vali.Host` (a .NET console app referencing `Vali.Core`) and speaks
newline-delimited JSON over stdin/stdout. stdout carries ONLY protocol lines (UTF-8, no BOM,
one JSON object per line). All human/log noise goes to stderr.

## Envelopes

Request (app → sidecar, one line):

```json
{"id":"r1","cmd":"validate","params":{...}}
```

Response (sidecar → app, exactly one per request):

```json
{"id":"r1","ok":true,"result":{...}}
{"id":"r1","ok":false,"error":{"message":"...","detail":"..."}}
```

Event (sidecar → app, unsolicited, tied to the request that started the work):

```json
{"event":"progress","id":"r1","data":{...}}
```

`id` is an opaque string chosen by the app, unique per in-flight request.

## Commands

### `ping`
Params: none.
Result: `{"version":"0.1.0","dataFolder":"C:\\ProgramData\\Vali"}`

### `validate`
Params: `{"json":"<definition file content>","definitionPath":"<abs path to definition.json>"?}`
Result:
```json
{"valid":false,"errors":["<message>", ...],"effectiveDefinition":{...}|null}
```
Runs Vali's real chain: `HumanReadableError` → `TryDeserialize` → `ApplyDefaults` → `Validate`.
`errors` are the messages Vali's validators print (captured). `effectiveDefinition` is the
post-`ApplyDefaults` definition (camelCase JSON object) when deserialization succeeded, else null.
Note: file-existence checks inside validators (geometry `filePath` / proximity `locationsPath`)
resolve relative paths against `definitionPath`'s folder when it is supplied (matching how the CLI
resolves them from the definition's directory, and how `generate` resolves them). Without
`definitionPath`, relative paths resolve against the sidecar process CWD, so send it whenever the
definition lives on disk.

### `expressionCheck`
Params: `{"expression":"Year gte 2020","context":"location"|"mapCheckr","allowParent":false,"namedExpressions":{"$$x":"..."}?}`
Result: `{"valid":true}` or
```json
{"valid":false,"error":{"message":"...","position":5,"length":3,"detail":"..."}}
```
`position`/`length` index into the (expanded) expression string. Named expressions are expanded
before checking when provided.

### `effectiveDefinition`
Params: `{"json":"..."}`
Result: `{"definition":{...}}` — the definition after `ApplyDefaults` (country/keyword expansion,
`$$` substitution, auto geometry filters), pretty camelCase.

### `countries`
Params: none.
Result: `{"countries":[{"code":"NO","name":"Norway","hasSubdivisionData":true}]}`

### `subdivisions`
Params: `{"countryCode":"NO"}`
Result: `{"subdivisions":[{"code":"NO-03","name":"Oslo","weight":200}]}`

### `dataStatus`
Params: none.
Result:
```json
{"dataFolder":"C:\\ProgramData\\Vali",
 "countries":[{"code":"IE","files":26,"bytes":370000000,"lastWriteUtc":"2026-01-01T00:00:00Z"}]}
```
Scans `<dataFolder>\<CC>\*.bin`.

### `generate`
Params:
```json
{"definitionPath":"C:\\maps\\NO.json","previewGoal":3000,"includeAdditionalInfo":false}
```
`previewGoal` (optional): run as a preview — `locationCountGoal` is replaced, pano verification is
skipped, output goes to `<name>-preview-locations.json`, and no distribution `.txt` files are
written. Only valid for strategies `FixedCountByMaxMinDistance` / `FixedCountByCoverageDensity`;
other strategies return an error explaining why.

Events during the run:
- `started`: `{"countries":[{"code":"NO","workItems":11,"goal":10000}],"totalGoal":10000}`
- `progress` (throttled, ≤4/s): `{"countries":[{"code":"NO","completed":3,"total":11,"status":"Working","produced":2712}],"countriesComplete":0,"countriesTotal":1,"totalLocations":2712,"totalGoal":10000,"elapsedMs":5123}`
- `regionCompleted`: `{"countryCode":"NO","subdivisionCode":"NO-03","produced":251,"goal":250,"minDistance":800,"sample":[[59.91,10.75],...]}`
  (`sample` is ≤300 `[lat,lng]` pairs, for live map plotting.)
- `warning`: `{"message":"..."}` (e.g. pano verification requested but not supported by Studio yet)

Response (also the terminal event of the run):
```json
{"locationsWritten":9971,"outputPath":"C:\\maps\\NO-locations.json","stoppedEarly":false,
 "elapsedMs":81234,
 "shortfall":{"countries":[{"code":"NO","produced":9971,"goal":10000,"shortfall":29,"pctBelow":0,
   "subdivisions":[{"code":"NO-34","name":"Innlandet","produced":471,"goal":500,"shortfall":29,"pctBelow":6}]}]}}
```
Only one `generate` may run at a time; a second request fails immediately. A country's data
files are locked for the duration of a generate or download — starting one while the other
holds a needed country fails fast with "`<CC>` is busy (...)". `outputPath` is null/omitted
when zero regions completed (e.g. stopped immediately).

### `cancel`
Params: `{"targetId":"<id of a running generate or download request>"}`
Result: `{"cancelled":true}`.
For generate, semantics match Vali's `s` key: stop admitting new regions, drain in-flight
ones, save everything completed so far; the `generate` response then arrives with
`stoppedEarly:true`. For download: the in-flight file finishes, remaining files are skipped,
metadata is saved for what completed.

### `remoteStatus`
Params: `{"countryCodes":["NO","IE"]}`
Result (one entry per requested code; network failures per country, not fatal):
```json
{"countries":[{
  "code":"NO",
  "local":{"files":0,"bytes":0,"newestUtc":null},
  "remote":{"files":16,"bytes":123456789,"newestUploadedUtc":"2026-06-01T00:00:00Z"},
  "filesToDownload":16,"updateFilesToApply":0,"bytesToDownload":123456789,
  "upToDate":false,"error":null}]}
```
Compares Vali's per-country `downloads.json` manifest (created from disk state when absent,
same as the CLI) against the R2 listings (`list-countries/{cc}`, `list-country-updates/{cc}`).

### `download`
Params: `{"countryCode":"NO","full":false,"updates":false}`
Mirrors `vali download` for one country: stale files deleted, missing/outdated data files
downloaded (BZip2 → `.bin`), incremental update files applied by append, `downloads.json`
maintained — interoperable with the CLI. Concurrent downloads for different countries are
allowed; a second download for the same country is rejected.

Events (throttled): `downloadProgress`:
```json
{"countryCode":"NO","phase":"data","fileName":"NO+NO-03","filesDone":3,"filesTotal":16,
 "bytesDone":10485760,"bytesTotal":123456789}
```
Response:
```json
{"countryCode":"NO","downloadedFiles":16,"deletedFiles":0,"appliedUpdateFiles":2,
 "bytesDownloaded":123456789,"cancelled":false}
```

### `valiInfo`
Params: none.
Result: `{"studioEngineVersion":"0.1.0","latestValiVersion":"0.7.42","nugetUrl":"https://www.nuget.org/packages/vali"}`
(`latestValiVersion` is null when nuget.org is unreachable.)

## Error codes

The error envelope may carry a machine-readable `code` and `data`:
`{"message":"...","detail":"...","code":"missingData","data":{...}}`
- `missingData` (from `generate`): `data.countries = [{"code":"NO","missingFiles":16}]`
  — the UI should offer to download and retry (fetch sizes via `remoteStatus`).
- `folderNotWritable` (from `download`): the data folder can't be written; `data.folder` is the path.

## Output file format

Identical to Vali's CLI output: a bare JSON array of
`{lat,lng,heading,zoom?,pitch?,extra?{tags[]},panoId?,countryCode?,subdivisionCode?,locationId?}`
(GeoGuessr `customCoordinates` shape), written with Vali's own serializer settings.

## Sidecar lifetime

- The app owns the process: spawn on startup, kill on exit.
- Sidecar exits when stdin closes.
- Protocol errors (unparseable line) produce `{"id":null,"ok":false,"error":{...}}` on stdout
  and are otherwise ignored.
