// Builds the prompts for Vali AI. Crucially these embed the real JSON schema so
// the model uses actual property/operator/enum names instead of guessing (e.g.
// "ClosestCoast", not "DistanceToCoast").

import valiSchema from "../schemas/vali.schema.json";

// Minified once — keeps the prompt smaller without losing any of the schema.
const SCHEMA_JSON = JSON.stringify(valiSchema);

const RULES = `Rules:
- The result MUST validate against the JSON Schema below. Only use properties, enum values, distribution keys, and (in filter expressions) property names and operators that the schema defines. Never invent names.
- Filter expressions (e.g. globalLocationFilter, named expressions, per-country filters) may ONLY use the properties and operators listed in the schema's "globalLocationFilter" description. Distances are in meters. The distance-to-coast property is "ClosestCoast" (there is no "DistanceToCoast"); similarly use ClosestLake / ClosestRiver / ClosestRailway.
- Preserve every existing property and existing comment you were not asked to change.
- Comment ONLY what this edit adds or changes: add one short, plain-language // comment (this file is JSONC) on the line(s) you add or modify, so a non-developer understands the change (e.g. // only imagery from 2020 or newer). Do NOT comment properties you didn't touch, and never write more than a single brief line per change — no multi-line notes, no meta-commentary.
- Return ONLY the complete edited JSONC document — the JSON with its // comments and nothing else (no prose outside it, no markdown fences).`;

/** Prompt to apply a plain-language instruction to the current definition. */
export function buildEditPrompt(instruction: string, definitionJson: string): string {
  return `You are editing a Vali map-definition JSON file (Vali generates GeoGuessr maps).

${RULES}

JSON SCHEMA:
${SCHEMA_JSON}

CURRENT JSON:
${definitionJson}

INSTRUCTION:
${instruction}`;
}

/** Prompt to fix validation errors in a JSON the model just produced. The
 *  engine's error messages often name the correct property, so feeding them
 *  back lets the model self-correct precisely. */
export function buildFixPrompt(
  instruction: string,
  invalidJson: string,
  errors: string[],
): string {
  return `The Vali map-definition JSONC below has validation errors. Fix them and return ONLY the corrected, complete JSONC document (JSON with // comments) — no prose outside it, no fences.

${RULES}

VALIDATION ERRORS:
${errors.map((e) => `- ${e}`).join("\n")}

ORIGINAL INSTRUCTION (for context):
${instruction}

JSON TO FIX:
${invalidJson}

JSON SCHEMA:
${SCHEMA_JSON}`;
}
