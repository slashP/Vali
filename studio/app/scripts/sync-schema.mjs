// Keeps studio/app/src/schemas/vali.schema.json in sync with the CLI's
// generated schema (src/Vali/Schemas/vali.schema.json), which the SchemaGenerator
// pre-build target regenerates from the C# MapDefinition types on every build of
// src/Vali. Runs on predev/prebuild so the Monaco editor, docs panel and AI
// prompt validate against the current schema instead of a frozen snapshot.
import { readFile, writeFile, mkdir } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const source = resolve(here, "../../../src/Vali/Schemas/vali.schema.json");
const dest = resolve(here, "../src/schemas/vali.schema.json");

try {
  const src = await readFile(source, "utf8");
  const current = await readFile(dest, "utf8").catch(() => null);
  if (src === current) {
    console.log("[sync-schema] up to date");
  } else {
    await mkdir(dirname(dest), { recursive: true });
    await writeFile(dest, src);
    console.log(`[sync-schema] updated ${dest}`);
  }
} catch (err) {
  // Don't fail the build if the source isn't present (e.g. a partial checkout) —
  // the committed copy is used as-is. Warn loudly so drift stays visible.
  console.warn(`[sync-schema] skipped: ${err.message}`);
}
