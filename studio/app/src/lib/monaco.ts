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

  // Warm dark theme matched to the app palette (dev.plonk.no-inspired).
  monaco.editor.defineTheme("vali-dark", {
    base: "vs-dark",
    inherit: true,
    rules: [
      { token: "", foreground: "ece7df" },
      { token: "string.key.json", foreground: "e7c46a" },
      { token: "string.value.json", foreground: "8fcf9e" },
      { token: "string", foreground: "8fcf9e" },
      { token: "number", foreground: "d99a5b" },
      { token: "keyword.json", foreground: "c98fb0" },
      { token: "keyword", foreground: "c98fb0" },
      { token: "delimiter", foreground: "af9879" },
      { token: "comment", foreground: "7d6a50", fontStyle: "italic" },
    ],
    colors: {
      "editor.background": "#15110c",
      "editor.foreground": "#ece7df",
      "editorLineNumber.foreground": "#5c4f3c",
      "editorLineNumber.activeForeground": "#af9879",
      "editor.selectionBackground": "#3a3124",
      "editor.inactiveSelectionBackground": "#2a2318",
      "editor.lineHighlightBackground": "#1d1710",
      "editorCursor.foreground": "#dab24e",
      "editorWhitespace.foreground": "#2a2318",
      "editorIndentGuide.background1": "#241d15",
      "editorIndentGuide.activeBackground1": "#3a3124",
      "editorGutter.background": "#15110c",
      "editorError.foreground": "#ef4343",
      "editorWarning.foreground": "#e3a13c",
      "editorBracketMatch.background": "#3a312466",
      "editorBracketMatch.border": "#dab24e",
      "editorWidget.background": "#191410",
      "editorWidget.border": "#3a3124",
      "editorSuggestWidget.background": "#191410",
      "editorSuggestWidget.selectedBackground": "#2b241d",
      "input.background": "#15110c",
      "scrollbarSlider.background": "#3a312455",
      "scrollbarSlider.hoverBackground": "#3a312488",
      "scrollbarSlider.activeBackground": "#3a3124bb",
      "diffEditor.insertedTextBackground": "#46b99c22",
      "diffEditor.removedTextBackground": "#ef434322",
    },
  });

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
