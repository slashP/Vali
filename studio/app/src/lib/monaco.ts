// Local, offline Monaco setup. No CDN — the app must work under CSP and offline.
//
// 1. Wire Monaco's web workers using Vite's `?worker` imports.
// 2. Point @monaco-editor/react's loader at this bundled monaco instance.
// 3. Register the Vali JSON schema for editor diagnostics.

import { loader } from "@monaco-editor/react";
import * as monaco from "monaco-editor";
import editorWorker from "monaco-editor/esm/vs/editor/editor.worker?worker";
import jsonWorker from "monaco-editor/esm/vs/language/json/json.worker?worker";
import valiSchema from "../schemas/vali.schema.json";

// Match any file the editor opens; we only ever open JSON models.
const SCHEMA_FILE_MATCH = ["**/*.json"];

let initialized = false;

export function setupMonaco(): void {
  if (initialized) return;
  initialized = true;

  self.MonacoEnvironment = {
    getWorker(_workerId, label) {
      if (label === "json") return new jsonWorker();
      return new editorWorker();
    },
  };

  loader.config({ monaco });

  monaco.json.jsonDefaults.setDiagnosticsOptions({
    validate: true,
    allowComments: true, // Vali accepts JSONC
    enableSchemaRequest: false, // no network fetches for schemas
    schemas: [
      {
        uri: "inmemory://vali/vali.schema.json",
        fileMatch: SCHEMA_FILE_MATCH,
        schema: valiSchema,
      },
    ],
  });
}

export { monaco };
