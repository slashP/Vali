import { useEffect, useRef } from "react";
import Editor, { DiffEditor, type OnMount } from "@monaco-editor/react";
import { useStore } from "../store";
import { AiPanel } from "./AiPanel";
import { BookIcon, SparkIcon } from "../components/Icons";
import { MOD } from "../lib/keybindings";

type EditorInstance = Parameters<OnMount>[0];

const EDITOR_OPTIONS = {
  minimap: { enabled: false },
  fontSize: 13,
  fontFamily: "Cascadia Code, Consolas, ui-monospace, monospace",
  scrollBeyondLastLine: false,
  tabSize: 2,
  automaticLayout: true,
  renderWhitespace: "none",
  padding: { top: 34 },
  // Hide the horizontal scrollbar but keep horizontal scrolling (wheel/cursor).
  scrollbar: { horizontalScrollbarSize: 0 },
} as const;

const DIFF_OPTIONS = {
  readOnly: true,
  renderSideBySide: false,
  minimap: { enabled: false },
  fontSize: 13,
  fontFamily: "Cascadia Code, Consolas, ui-monospace, monospace",
  scrollBeyondLastLine: false,
  automaticLayout: true,
  padding: { top: 12 },
  scrollbar: { horizontalScrollbarSize: 0 },
} as const;

export function EditorView() {
  const text = useStore((s) => s.text);
  const setText = useStore((s) => s.setText);
  const validation = useStore((s) => s.validation);
  const validating = useStore((s) => s.validating);
  const loadError = useStore((s) => s.loadError);
  const saveError = useStore((s) => s.saveError);
  const docsOpen = useStore((s) => s.docsOpen);
  const aiBusy = useStore((s) => s.aiBusy);
  const proposal = useStore((s) => s.aiProposal);
  const proposalValidation = useStore((s) => s.aiProposalValidation);

  const editorRef = useRef<EditorInstance | null>(null);
  const debounceRef = useRef<number | undefined>(undefined);

  const onMount: OnMount = (editor) => {
    editorRef.current = editor;
  };

  // Autosave + validate on change (debounced). Only when the doc is dirty, so
  // simply loading/switching a map doesn't rewrite the file. Never autosave
  // over a failed load (dirty stays false in that case).
  useEffect(() => {
    if (loadError) return;
    window.clearTimeout(debounceRef.current);
    debounceRef.current = window.setTimeout(() => {
      const store = useStore.getState();
      if (store.dirty) void store.saveCurrent().catch(() => {});
      void store.validateCurrent();
    }, 600);
    return () => window.clearTimeout(debounceRef.current);
  }, [text, loadError]);

  const errors = validation?.errors ?? [];
  const isValid = validation?.valid === true;
  const reviewing = proposal !== null;

  const chip = validating ? (
    <span className="valid-float checking">Checking…</span>
  ) : validation === null ? null : isValid ? (
    <span className="valid-float ok">
      <span className="valid-dot" /> Valid
    </span>
  ) : (
    <span className="valid-float bad">
      {errors.length} problem{errors.length === 1 ? "" : "s"}
    </span>
  );

  return (
    <div className="editor-pane">
      <div className="editor-stack">
        <div className="editor-main">
          {loadError ? (
            <div className="load-error">
              <div className="load-error-title">Couldn&apos;t open this map</div>
              <div className="hint">{loadError}</div>
              <button
                className="primary"
                onClick={() => void useStore.getState().retryLoad()}
              >
                Retry
              </button>
            </div>
          ) : (
            <>
              {saveError && (
                <div className="save-error">
                  <span>{saveError}</span>
                  <button onClick={() => useStore.getState().dismissSaveError()}>
                    Dismiss
                  </button>
                </div>
              )}
              <div className="editor-monaco">
                {!reviewing && chip}

                {reviewing ? (
                  <div className="editor-diff-mode">
                    <div className="ai-review-bar">
                      <span className="ai-glyph">
                        <SparkIcon size={13} />
                      </span>
                      <span className="ai-review-title">
                        Vali AI proposed a change
                      </span>
                      {proposalValidation && (
                        <span
                          className={`valid-chip ${proposalValidation.valid ? "ok" : "bad"}`}
                        >
                          {proposalValidation.valid
                            ? "validates cleanly"
                            : `${proposalValidation.errors.length} problem${proposalValidation.errors.length === 1 ? "" : "s"}`}
                        </span>
                      )}
                    </div>
                    <div className="editor-diff">
                      <DiffEditor
                        language="json"
                        theme="vali-dark"
                        original={text}
                        modified={proposal ?? ""}
                        options={DIFF_OPTIONS}
                      />
                    </div>
                  </div>
                ) : (
                  <Editor
                    language="json"
                    theme="vali-dark"
                    path="definition.json"
                    value={text}
                    onChange={(v) => setText(v ?? "")}
                    onMount={onMount}
                    options={{ ...EDITOR_OPTIONS, readOnly: aiBusy }}
                  />
                )}

                <button
                  className={`docs-fab docs-fab-editor${reviewing ? " lowered" : ""}${docsOpen ? " active" : ""}`}
                  onClick={() => useStore.getState().setDocsOpen(!docsOpen)}
                  title="Browse the map-definition settings"
                  aria-pressed={docsOpen}
                >
                  <BookIcon size={15} />
                  <span>Docs</span>
                  <span className="btn-kbd">
                    <kbd>{MOD}</kbd>
                    <kbd>⇧</kbd>
                    <kbd>D</kbd>
                  </span>
                </button>

                <AiPanel />
              </div>

              {!reviewing && validation !== null && !isValid && (
                <div className="errors-panel">
                  <div className="errors-header">
                    {errors.length} problem{errors.length === 1 ? "" : "s"}
                  </div>
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
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
}
