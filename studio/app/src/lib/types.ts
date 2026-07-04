// Types mirroring the NDJSON sidecar protocol (studio/docs/PROTOCOL.md).
// Keep these in sync with that document — it is the contract.

// ---- Envelopes ----------------------------------------------------------

export interface SidecarRequest {
  id: string;
  cmd: string;
  params: unknown;
}

export interface SidecarError {
  message: string;
  detail?: string;
}

export interface SidecarResponse<T = unknown> {
  id: string | null;
  ok: boolean;
  result?: T;
  error?: SidecarError;
}

/** Unsolicited event tied to the request `id` that started the work. */
export interface SidecarEvent<T = unknown> {
  event: string;
  id: string;
  data: T;
}

// ---- ping ---------------------------------------------------------------

export interface PingResult {
  version: string;
  dataFolder: string;
}

// ---- validate -----------------------------------------------------------

export interface ValidateResult {
  valid: boolean;
  errors: string[];
  effectiveDefinition: Record<string, unknown> | null;
}

// ---- expressionCheck ----------------------------------------------------

export type ExpressionContext = "location" | "mapCheckr";

export interface ExpressionError {
  message: string;
  position: number;
  length: number;
  detail?: string;
}

export type ExpressionCheckResult =
  | { valid: true }
  | { valid: false; error: ExpressionError };

// ---- effectiveDefinition ------------------------------------------------

export interface EffectiveDefinitionResult {
  definition: Record<string, unknown>;
}

// ---- countries / subdivisions ------------------------------------------

export interface CountryInfo {
  code: string;
  name: string;
  hasSubdivisionData: boolean;
}

export interface CountriesResult {
  countries: CountryInfo[];
}

export interface SubdivisionInfo {
  code: string;
  name: string;
  weight: number;
}

export interface SubdivisionsResult {
  subdivisions: SubdivisionInfo[];
}

// ---- dataStatus ---------------------------------------------------------

export interface DataCountry {
  code: string;
  files: number;
  bytes: number;
  lastWriteUtc: string;
}

export interface DataStatus {
  dataFolder: string;
  countries: DataCountry[];
}

// ---- generate: events ---------------------------------------------------

export type RegionStatus = "Queued" | "Working" | "Done" | "Short" | string;

/** Per-country entry inside the `started` event. */
export interface StartedCountry {
  code: string;
  workItems: number;
  goal: number;
}

export interface StartedEvent {
  countries: StartedCountry[];
  totalGoal: number;
}

/** Per-country entry inside a `progress` snapshot. */
export interface ProgressCountry {
  code: string;
  completed: number;
  total: number;
  status: RegionStatus;
  produced: number;
}

export interface ProgressSnapshot {
  countries: ProgressCountry[];
  countriesComplete: number;
  countriesTotal: number;
  totalLocations: number;
  totalGoal: number;
  elapsedMs: number;
}

/** A `[lat, lng]` pair as delivered in regionCompleted samples. */
export type LatLng = [number, number];

export interface RegionCompleted {
  countryCode: string;
  subdivisionCode: string;
  produced: number;
  goal: number;
  minDistance: number;
  sample: LatLng[];
}

export interface WarningEvent {
  message: string;
}

// ---- generate: result ---------------------------------------------------

export interface SubdivisionShortfall {
  code: string;
  name: string;
  produced: number;
  goal: number;
  shortfall: number;
  pctBelow: number;
}

export interface CountryShortfall {
  code: string;
  produced: number;
  goal: number;
  shortfall: number;
  pctBelow: number;
  subdivisions: SubdivisionShortfall[];
}

export interface Shortfall {
  countries: CountryShortfall[];
}

export interface GenerateResult {
  locationsWritten: number;
  outputPath: string;
  stoppedEarly: boolean;
  elapsedMs: number;
  shortfall: Shortfall;
}

export interface CancelResult {
  cancelled: boolean;
}

// ---- location file format ----------------------------------------------

/** GeoGuessr customCoordinates shape (Vali output). */
export interface MapLocation {
  lat: number;
  lng: number;
  heading?: number;
  pitch?: number;
  zoom?: number;
  panoId?: string;
  countryCode?: string;
  subdivisionCode?: string;
  locationId?: string | number;
  extra?: {
    tags?: string[];
    [key: string]: unknown;
  };
}
