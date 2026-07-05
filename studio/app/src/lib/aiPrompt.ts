// Builds the prompts for Vali AI. Crucially these embed the real JSON schema so
// the model uses actual property/operator/enum names instead of guessing (e.g.
// "ClosestCoast", not "DistanceToCoast").

import valiSchema from "../schemas/vali.schema.json";

// Minified once — keeps the prompt smaller without losing any of the schema.
const SCHEMA_JSON = JSON.stringify(valiSchema);

const RULES = `Rules:
- The result MUST validate against the JSON Schema below. Only use properties, enum values, distribution keys, and (in filter expressions) property names and operators that the schema defines. Never invent names.
- Filter expressions (e.g. globalLocationFilter, named expressions, per-country filters) may ONLY use the properties and operators listed in the schema's "globalLocationFilter" description. Distances are in meters. The distance-to-coast property is "ClosestCoast" (there is no "DistanceToCoast"); similarly use ClosestLake / ClosestRiver / ClosestRailway.
- Preserve every existing property you were not asked to change, and preserve JSON comments.
- Return ONLY the complete edited JSON document — no explanations, no markdown fences.`;

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
  return `The Vali map-definition JSON below has validation errors. Fix them and return ONLY the corrected, complete JSON document — no explanations, no fences.

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
