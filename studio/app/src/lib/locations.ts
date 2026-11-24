import { readTextFile } from "./commands";
import type { MapLocation } from "./types";

interface RawLocation {
  lat?: unknown;
  lng?: unknown;
  [key: string]: unknown;
}

function isFiniteNumber(v: unknown): v is number {
  return typeof v === "number" && Number.isFinite(v);
}

/**
 * Parse a Vali/GeoGuessr locations document defensively. Accepts a bare array
 * of `{lat,lng,...}` or a `{ customCoordinates: [...] }` wrapper. Non-numeric
 * or malformed entries are dropped.
 */
export function parseLocations(text: string): MapLocation[] {
  const doc = JSON.parse(text) as unknown;
  let arr: unknown;
  if (Array.isArray(doc)) {
    arr = doc;
  } else if (doc && typeof doc === "object" && "customCoordinates" in doc) {
    arr = (doc as { customCoordinates: unknown }).customCoordinates;
  } else {
    arr = [];
  }
  if (!Array.isArray(arr)) return [];

  const out: MapLocation[] = [];
  for (const item of arr) {
    if (!item || typeof item !== "object") continue;
    const r = item as RawLocation;
    if (isFiniteNumber(r.lat) && isFiniteNumber(r.lng)) {
      out.push(r as MapLocation);
    }
  }
  return out;
}

export async function loadLocationsFile(path: string): Promise<MapLocation[]> {
  const text = await readTextFile(path);
  return parseLocations(text);
}
