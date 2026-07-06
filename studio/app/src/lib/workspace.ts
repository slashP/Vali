// Managed map workspace. Maps live under `<workspaceRoot>/<slug>/`:
//   definition.json   the Vali map definition (autosaved)
//   meta.json         { name, description, createdMs } — display metadata
//   run.json          last generation summary (persisted so it survives restarts)
// Outputs (`*-locations.json`, distribution `.txt`) land alongside them.

import {
  ensureDir,
  listDir,
  readTextFile,
  removePath,
  writeTextFile,
} from "./commands";
import { STARTER_DEFINITION } from "./template";
import type { GenerateResult } from "./types";

export interface MapEntry {
  slug: string;
  name: string;
  description: string;
  mtimeMs: number;
  /** Country codes parsed best-effort from the definition (sidebar chips). */
  countries: string[];
  /** Location goal parsed from the distribution strategy, when present. */
  goal: number | null;
}

/** A map's at-a-glance summary, parsed from its definition JSON. */
export interface MapSummary {
  countries: string[];
  goal: number | null;
}

/** Best-effort summary from a definition document. Never throws. */
export function parseMapSummary(text: string): MapSummary {
  try {
    const d = JSON.parse(text) as Record<string, unknown>;
    const codes = new Set<string>();
    const list = d.countryCodes;
    if (Array.isArray(list)) {
      for (const c of list) if (typeof c === "string") codes.add(c);
    }
    const dist = d.countryDistribution;
    if (dist && typeof dist === "object") {
      for (const c of Object.keys(dist)) codes.add(c);
    }
    const strat = d.distributionStrategy as Record<string, unknown> | undefined;
    const goal = strat?.locationCountGoal;
    return {
      countries: [...codes],
      goal: typeof goal === "number" ? goal : null,
    };
  } catch {
    return { countries: [], goal: null };
  }
}

export interface OutputFile {
  name: string;
  path: string;
  mtimeMs: number;
  bytes: number;
  isPreview: boolean;
}

interface MapMeta {
  name: string;
  description?: string;
  createdMs?: number;
}

/** A persisted generation summary (kept in the map's run.json). */
export interface StoredRun {
  atMs: number;
  goal: number;
  result: GenerateResult;
}

/** Path separator inferred from the (platform) workspace root. */
function sep(root: string): string {
  return root.includes("\\") ? "\\" : "/";
}

export function join(root: string, ...parts: string[]): string {
  const s = sep(root);
  return [root, ...parts].join(s);
}

export function slugify(name: string): string {
  const slug = name
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 60);
  return slug || "map";
}

export function mapDir(root: string, slug: string): string {
  return join(root, slug);
}

export function definitionPath(root: string, slug: string): string {
  return join(root, slug, "definition.json");
}

function metaPath(root: string, slug: string): string {
  return join(root, slug, "meta.json");
}

function runPath(root: string, slug: string): string {
  return join(root, slug, "run.json");
}

async function readMeta(root: string, slug: string): Promise<MapMeta | null> {
  try {
    const text = await readTextFile(metaPath(root, slug));
    const parsed = JSON.parse(text) as MapMeta;
    if (parsed && typeof parsed.name === "string") return parsed;
  } catch {
    // missing/corrupt meta — caller falls back to the slug
  }
  return null;
}

/** List all maps, newest folder first is not assumed — sorted by name. */
export async function listMaps(root: string): Promise<MapEntry[]> {
  const entries = await listDir(root);
  const dirs = entries.filter((e) => e.isDir);
  const maps: MapEntry[] = [];
  for (const dir of dirs) {
    const meta = await readMeta(root, dir.name);
    let summary: MapSummary = { countries: [], goal: null };
    try {
      summary = parseMapSummary(await readDefinition(root, dir.name));
    } catch {
      // a map folder without a readable definition still lists (no chips)
    }
    maps.push({
      slug: dir.name,
      name: meta?.name ?? dir.name,
      description: meta?.description ?? "",
      mtimeMs: dir.mtimeMs,
      countries: summary.countries,
      goal: summary.goal,
    });
  }
  maps.sort((a, b) => a.name.localeCompare(b.name));
  return maps;
}

async function uniqueSlug(root: string, base: string): Promise<string> {
  const existing = new Set((await listDir(root)).map((e) => e.name.toLowerCase()));
  if (!existing.has(base)) return base;
  for (let i = 2; i < 1000; i++) {
    const candidate = `${base}-${i}`;
    if (!existing.has(candidate)) return candidate;
  }
  return `${base}-${Date.now()}`;
}

/** Create a new map from the starter template. Returns its entry. */
export async function createMap(
  root: string,
  name: string,
  content: string = STARTER_DEFINITION,
  description = "",
): Promise<MapEntry> {
  const slug = await uniqueSlug(root, slugify(name));
  const dir = mapDir(root, slug);
  await ensureDir(dir);
  await writeTextFile(definitionPath(root, slug), content);
  const meta: MapMeta = { name, description, createdMs: Date.now() };
  await writeTextFile(metaPath(root, slug), JSON.stringify(meta, null, 2));
  const summary = parseMapSummary(content);
  return {
    slug,
    name,
    description,
    mtimeMs: Date.now(),
    countries: summary.countries,
    goal: summary.goal,
  };
}

/** Merge a metadata patch (name/description) into a map's meta.json. */
export async function updateMeta(
  root: string,
  slug: string,
  patch: Partial<MapMeta>,
): Promise<MapMeta> {
  const existing = (await readMeta(root, slug)) ?? { name: slug };
  const merged: MapMeta = { ...existing, ...patch };
  await writeTextFile(metaPath(root, slug), JSON.stringify(merged, null, 2));
  return merged;
}

export async function readLastRun(
  root: string,
  slug: string,
): Promise<StoredRun | null> {
  try {
    const parsed = JSON.parse(await readTextFile(runPath(root, slug))) as StoredRun;
    if (parsed && parsed.result) return parsed;
  } catch {
    // no persisted run yet, or unreadable — caller shows nothing
  }
  return null;
}

export async function writeLastRun(
  root: string,
  slug: string,
  run: StoredRun,
): Promise<void> {
  await writeTextFile(runPath(root, slug), JSON.stringify(run, null, 2));
}

export async function deleteMap(root: string, slug: string): Promise<void> {
  await removePath(mapDir(root, slug));
}

export async function readDefinition(
  root: string,
  slug: string,
): Promise<string> {
  return readTextFile(definitionPath(root, slug));
}

export async function writeDefinition(
  root: string,
  slug: string,
  text: string,
): Promise<void> {
  await writeTextFile(definitionPath(root, slug), text);
}

/** Discover generated location files in a map's folder, newest first. */
export async function listOutputs(
  root: string,
  slug: string,
): Promise<OutputFile[]> {
  let entries;
  try {
    entries = await listDir(mapDir(root, slug));
  } catch {
    return [];
  }
  const outputs = entries
    .filter((e) => !e.isDir && e.name.toLowerCase().endsWith("-locations.json"))
    .map((e) => ({
      name: e.name,
      path: join(root, slug, e.name),
      mtimeMs: e.mtimeMs,
      bytes: e.bytes,
      isPreview: e.name.toLowerCase().includes("-preview-"),
    }));
  outputs.sort((a, b) => b.mtimeMs - a.mtimeMs);
  return outputs;
}
