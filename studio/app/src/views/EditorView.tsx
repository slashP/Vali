import { useCallback, useEffect, useRef, useState } from "react";
import Editor, { DiffEditor, type OnMount } from "@monaco-editor/react";
import { useStore } from "../store";
import { AiPanel } from "./AiPanel";
import { CheckIcon, SparkIcon } from "../components/Icons";

type EditorInstance = Parameters<OnMount>[0];

const EDITOR_OPTIONS = {
  minimap: { enabled: false },
  fontSize: 13,
  fontFamily: "Cascadia Code, Consolas, ui-monospace, monospace",
  scrollBeyondLastLine: false,
  tabSize: 2,
  automaticLayout: true,
  renderWhitespace: "none",
  padding: { top: 12 },
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
} as const;

const LS_AI_HEIGHT = "vali.aiHeight";
const AI_MIN = 130;

function initialAiHeight(): number {
  const stored = Number(localStorage.getItem(LS_AI_HEIGHT));
  if (Number.isFinite(stored) && stored >= AI_MIN) return stored;
  return 200;
}

export function EditorView() {
  const text = useStore((s) => s.text);
  const setText = useStore((s) => s.setText);
  const validation = useStore((s) => s.validation);
  const validating = useStore((s) => s.validating);
  const loadError = useStore((s) => s.loadError);
  const saveError = useStore((s) => s.saveError);
  const aiOpen = useStore((s) => s.aiOpen);
  const aiReady = useStore((s) => s.aiProviders.some((p) => p.available));
  const aiBusy = useStore((s) => s.aiBusy);
  const aiStartMs = useStore((s) => s.aiStartMs);
  const aiPhase = useStore((s) => s.aiPhase);
  const aiStreamText = useStore((s) => s.aiStreamText);
  const proposal = useStore((s) => s.aiProposal);
  const proposalValidation = useStore((s) => s.aiProposalValidation);

  const editorRef = useRef<EditorInstance | null>(null);
  const debounceRef = useRef<number | undefined>(undefined);
  const stackRef = useRef<HTMLDivElement | null>(null);
  const streamRef = useRef<HTMLPreElement | null>(null);

  const [aiHeight, setAiHeight] = useState(initialAiHeight);
  const [draggingAi, setDraggingAi] = useState(false);
  const [elapsed, setElapsed] = useState(0);

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

  // Keep the streamed output pinned to the latest line.
  useEffect(() => {
    const el = streamRef.current;
    if (el) el.scrollTop = el.scrollHeight;
  }, [aiStreamText]);

  useEffect(() => {
    if (!aiBusy || aiStartMs === null) {
      setElapsed(0);
      return;
    }
    const tick = () => setElapsed(Math.floor((Date.now() - aiStartMs) / 1000));
    tick();
    const t = window.setInterval(tick, 1000);
    return () => window.clearInterval(t);
  }, [aiBusy, aiStartMs]);

  const onAiDrag = useCallback((clientY: number) => {
    const el = stackRef.current;
    if (!el) return;
    const rect = el.getBoundingClientRect();
    const max = rect.height - 160;
    setAiHeight(Math.min(Math.max(AI_MIN, rect.bottom - clientY), Math.max(AI_MIN, max)));
  }, []);

  useEffect(() => {
    if (!draggingAi) return;
    const move = (e: MouseEvent) => onAiDrag(e.clientY);
    const up = () => setDraggingAi(false);
    window.addEventListener("mousemove", move);
    window.addEventListener("mouseup", up);
    return () => {
      window.removeEventListener("mousemove", move);
      window.removeEventListener("mouseup", up);
    };
  }, [draggingAi, onAiDrag]);

  useEffect(() => {
    localStorage.setItem(LS_AI_HEIGHT, String(aiHeight));
  }, [aiHeight]);

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
      <div className="editor-stack" ref={stackRef}>
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
                    <div className="ai-diff-actions">
                      <button
                        onClick={() => useStore.getState().discardAiProposal()}
                      >
                        Discard
                      </button>
                      <button
                        className="primary"
                        onClick={() => useStore.getState().applyAiProposal()}
                      >
                        <CheckIcon size={14} /> Apply change
                      </button>
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

                {aiBusy && (
                  <div className="ai-editor-busy" role="status" aria-live="polite">
                    <div className="ai-editor-busy-card">
                      <div className="ai-busy-head">
                        <span className="spinner" aria-hidden />
                        <div className="ai-editor-busy-text">
                          <div className="ai-busy-title">
                            {aiPhase ?? "Vali AI is editing your definition…"}
                          </div>
                          <div className="hint">
                            Running your local Claude · {elapsed}s
                          </div>
                        </div>
                        <button
                          className="ai-busy-cancel"
                          onClick={() => void useStore.getState().cancelAi()}
                        >
                          Cancel
                        </button>
                      </div>
                      {aiStreamText ? (
                        <pre className="ai-stream" ref={streamRef}>
                          {aiStreamText}
                        </pre>
                      ) : (
                        <div className="ai-indet" aria-hidden>
                          <span />
                        </div>
                      )}
                    </div>
                  </div>
                )}
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

        {aiOpen && aiReady && (
          <div
            className={`hsplit-divider${draggingAi ? " active" : ""}`}
            onMouseDown={(e) => {
              e.preventDefault();
              setDraggingAi(true);
            }}
            role="separator"
            aria-orientation="horizontal"
            title="Drag to resize Vali AI"
          >
            <span className="hsplit-grip" />
          </div>
        )}
        <AiPanel dockHeight={aiOpen && aiReady ? aiHeight : null} />
        {draggingAi && <div className="drag-shield row" />}
      </div>
    </div>
  );
}
