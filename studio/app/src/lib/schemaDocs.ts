// Flattens the Vali JSON schema into a searchable list of documented settings
// for the Documentation panel. Purely derived from vali.schema.json, so it
// stays in sync with the engine's real definition on every build.

import rawSchema from "../schemas/vali.schema.json";

/* eslint-disable @typescript-eslint/no-explicit-any */
const schema = rawSchema as any;

export interface DocEntry {
  key: string;
  group: string;
  type: string;
  description: string;
  examples: string[];
  enumValues: string[];
  defaultValue: string | null;
  required: boolean;
  /** Ready-to-paste `"key": <value>` line. */
  snippet: string;
  /** Precomputed lowercase haystack for filtering. */
  haystack: string;
}

// Friendly labels + display order for each schema section. Anything not listed
// falls back to its raw definition name and sorts to the end.
const GROUP_META: Record<string, string> = {
  __root__: "Map definition",
  DistributionStrategy: "Distribution strategy",
  LocationOutput: "Output",
  LocationPreferenceFilter: "Preference filter",
  ProximityFilter: "Proximity filter",
  GeometryFilter: "Geometry filter",
  NeighborFilter: "Neighbor filter",
  LocationProbability: "Location probability",
  LocationWeightOverride: "Weight override",
  CountryPanning: "Country panning",
  PanningExpression: "Panning expression",
};

export const GROUP_ORDER: string[] = [
  "Map definition",
  "Distribution strategy",
  "Output",
  "Preference filter",
  "Proximity filter",
  "Geometry filter",
  "Neighbor filter",
  "Location probability",
  "Weight override",
  "Country panning",
  "Panning expression",
];

function friendly(defName: string): string {
  return GROUP_META[defName] ?? defName;
}

function refName(ref: string): string {
  return ref.split("/").pop() ?? ref;
}

function nonNullTypes(t: unknown): string[] {
  if (Array.isArray(t)) return t.filter((x) => x !== "null");
  if (typeof t === "string") return [t];
  return [];
}

function describeType(prop: any): string {
  if (!prop) return "any";
  if (prop.$ref) return friendly(refName(prop.$ref));

  const types = nonNullTypes(prop.type);

  if (types.includes("array")) {
    const items = prop.items;
    if (items?.$ref) return `${friendly(refName(items.$ref))}[]`;
    if (items?.oneOf) return "string[]";
    const itemTypes = nonNullTypes(items?.type);
    return `${itemTypes[0] ?? "any"}[]`;
  }

  if (types.includes("object")) {
    const ap = prop.additionalProperties;
    if (ap?.$ref) return `map → ${friendly(refName(ap.$ref))}`;
    if (ap && ap.additionalProperties) return "nested map";
    if (ap) return "map";
    return "object";
  }

  if (prop.enum) return types[0] ?? "enum";
  if (types.length > 0) return types.join(" | ");
  return "any";
}

function stringify(value: unknown): string {
  if (typeof value === "string") return value;
  try {
    return JSON.stringify(value);
  } catch {
    return String(value);
  }
}

function collectExamples(prop: any): string[] {
  const raw = prop?.examples;
  if (!Array.isArray(raw)) return [];
  return raw.map(stringify);
}

function collectEnum(prop: any): string[] {
  const values = prop?.items?.oneOf?.[0]?.enum ?? prop?.enum;
  if (!Array.isArray(values)) return [];
  return values.filter((v: unknown) => v !== null).map((v: unknown) => String(v));
}

function snippetValue(prop: any, enumValues: string[]): string {
  if (prop?.default !== undefined) return stringify(prop.default);
  if (enumValues.length > 0) {
    const first = enumValues[0];
    return nonNullTypes(prop.type).includes("string") || prop.$ref
      ? `"${first}"`
      : first;
  }
  if (prop?.$ref) return "{}";
  const types = nonNullTypes(prop?.type);
  if (types.includes("array")) return "[]";
  if (types.includes("object")) return "{}";
  if (types.includes("string")) return '""';
  if (types.includes("boolean")) return "false";
  if (types.includes("integer") || types.includes("number")) return "0";
  return "null";
}

function makeEntry(
  key: string,
  prop: any,
  group: string,
  requiredKeys: string[],
): DocEntry {
  const enumValues = collectEnum(prop);
  const type = describeType(prop);
  const description = prop?.description ?? "";
  const examples = collectExamples(prop);
  const defaultValue =
    prop?.default !== undefined ? stringify(prop.default) : null;
  const snippet = `"${key}": ${snippetValue(prop, enumValues)}`;
  const haystack = [key, group, description, type, enumValues.join(" ")]
    .join(" ")
    .toLowerCase();
  return {
    key,
    group,
    type,
    description,
    examples,
    enumValues,
    defaultValue,
    required: requiredKeys.includes(key),
    snippet,
    haystack,
  };
}

let cache: DocEntry[] | null = null;

/** Build (and memoize) the flat list of documented settings. */
export function buildDocEntries(): DocEntry[] {
  if (cache) return cache;
  const entries: DocEntry[] = [];

  const rootRequired: string[] = schema.required ?? [];
  for (const [key, prop] of Object.entries<any>(schema.properties ?? {})) {
    entries.push(makeEntry(key, prop, GROUP_META.__root__, rootRequired));
  }

  for (const [defName, def] of Object.entries<any>(schema.definitions ?? {})) {
    const props = def?.properties;
    if (!props) continue;
    const req: string[] = def.required ?? [];
    const group = friendly(defName);
    for (const [key, prop] of Object.entries<any>(props)) {
      entries.push(makeEntry(key, prop, group, req));
    }
  }

  cache = entries;
  return entries;
}
