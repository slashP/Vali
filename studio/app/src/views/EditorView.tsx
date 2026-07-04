import { useCallback, useEffect, useRef, useState } from "react";
import Editor, { type OnMount } from "@monaco-editor/react";
import { useStore } from "../store";
import { request, newRequestId } from "../lib/sidecar";
import {
  pickOpenFile,
  pickSaveFile,
  readTextFile,
  writeTextFile,
} from "../lib/commands";
import type { GenerateResult, ValidateResult } from "../lib/types";
import { PREVIEW_GOAL } from "../lib/template";
import { AiPanel } from "./AiPanel";

type EditorInstance = Parameters<OnMount>[0];

const EDITOR_OPTIONS = {
  minimap: { enabled: false },
  fontSize: 13,
  fontFamily: "Cascadia Code, Consolas, ui-monospace, monospace",
  scrollBeyondLastLine: false,
  tabSize: 2,
  automaticLayout: true,
  renderWhitespace: "none",
} as const;

function synthErr(message: string): ValidateResult {
  return { valid: false, errors: [message], effectiveDefinition: null };
}

export function EditorView() {
  const text = useStore((s) => s.text);
  const setText = useStore((s) => s.setText);
  const validation = useStore((s) => s.validation);
  const validating = useStore((s) => s.validating);
  const sidecarAlive = useStore((s) => s.sidecarAlive);

  const [aiOpen, setAiOpen] = useState(false);
  const [busy, setBusy] = useState<string | null>(null);

  const editorRef = useRef<EditorInstance | null>(null);
  const debounceRef = useRef<number | undefined>(undefined);

  const onMount: OnMount = (editor) => {
    editorRef.current = editor;
  };

  const doValidate = useCallback(async (json: string) => {
    const { setValidating, setValidation } = useStore.getState();
    setValidating(true);
    try {
      const result = await request<ValidateResult>("validate", { json });
      setValidation(result);
    } catch (e) {
      setValidation(synthErr(`Sidecar unavailable: ${String(e)}`));
    } finally {
      setValidating(false);
    }
  }, []);

  // Debounced validation on text changes.
  useEffect(() => {
    window.clearTimeout(debounceRef.current);
    debounceRef.current = window.setTimeout(() => {
      void doValidate(text);
    }, 500);
    return () => window.clearTimeout(debounceRef.current);
  }, [text, doValidate]);

  const save = useCallback(async (): Promise<string | null> => {
    const { filePath: current, setFilePath, markSaved } = useStore.getState();
    let path = current;
    if (!path) {
      path = await pickSaveFile("map.json");
      if (!path) return null;
      setFilePath(path);
    }
    await writeTextFile(path, useStore.getState().text);
    markSaved();
    return path;
  }, []);

  const open = useCallback(async () => {
    const path = await pickOpenFile("Vali map definition", ["json", "jsonc"]);
    if (!path) return;
    const content = await readTextFile(path);
    useStore.getState().loadFile(path, content);
    void doValidate(content);
  }, [doValidate]);

  const runGenerate = useCallback(
    async (preview: boolean) => {
      setBusy(preview ? "Starting preview…" : "Starting generation…");
      try {
        const path = await save();
        if (!path) return;
        const id = newRequestId();
        const params: Record<string, unknown> = {
          definitionPath: path,
          includeAdditionalInfo: false,
        };
        if (preview) params.previewGoal = PREVIEW_GOAL;

        const { beginRun, finishRun, failRun, setView } = useStore.getState();
        beginRun(id, preview);
        setView("run");
        // Fire and forget — RunView shows progress; result completes the run.
        request<GenerateResult>("generate", params, id)
          .then(finishRun)
          .catch((e) => failRun(String(e)));
      } finally {
        setBusy(null);
      }
    },
    [save],
  );

  const errors = validation?.errors ?? [];
  const showErrors = validation !== null;

  return (
    <div className="editor-view">
      <div className="toolbar">
        <span className="title">Definition</span>
        <button onClick={open}>Open</button>
        <button onClick={() => void save()}>Save</button>
        <button onClick={() => useStore.getState().newFromTemplate()}>
          New from template
        </button>
        <div className="sep" />
        <button
          onClick={() => void doValidate(useStore.getState().text)}
          disabled={validating || !sidecarAlive}
        >
          {validating ? "Validating…" : "Validate now"}
        </button>
        <div className="grow" />
        <button
          onClick={() => void runGenerate(true)}
          disabled={!sidecarAlive || busy !== null}
        >
          Preview
        </button>
        <button
          className="primary"
          onClick={() => void runGenerate(false)}
          disabled={!sidecarAlive || busy !== null}
        >
          Generate
        </button>
        <div className="sep" />
        <button
          onClick={() => setAiOpen((v) => !v)}
          className={aiOpen ? "primary" : ""}
        >
          AI
        </button>
      </div>

      <div className="editor-body">
        <div className="editor-main">
          <div className="editor-monaco">
            <Editor
              language="json"
              theme="vs-dark"
              path="definition.json"
              value={text}
              onChange={(v) => setText(v ?? "")}
              onMount={onMount}
              options={EDITOR_OPTIONS}
            />
          </div>

          {showErrors && (
            <div className="errors-panel">
              <div className="errors-header">
                {validation?.valid
                  ? "Valid"
                  : `${errors.length} problem${errors.length === 1 ? "" : "s"}`}
              </div>
              {validation?.valid ? (
                <div className="ok-msg">Definition is valid.</div>
              ) : (
                <ul>
                  {errors.map((msg, i) => (
                    <li
                      key={i}
                      onClick={() => editorRef.current?.focus()}
                      title="Focus editor"
                    >
                      {msg}
                    </li>
                  ))}
                </ul>
              )}
            </div>
          )}
        </div>

        <AiPanel
          open={aiOpen}
          onClose={() => setAiOpen(false)}
          onApply={(newText) => setText(newText)}
        />
      </div>
    </div>
  );
}
