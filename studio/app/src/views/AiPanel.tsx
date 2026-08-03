import { useEffect, useRef, useState } from "react";
import { useStore } from "../store";
import { CheckIcon, ChevronIcon, SendIcon, SparkIcon } from "../components/Icons";
import type { AiProvider } from "../lib/commands";

/**
 * The Vali AI composer — a floating message box docked over the bottom of the
 * JSON editor. It carries the whole flow so the input is always visible:
 *   idle       → textarea + model picker + send
 *   busy       → the question, a fixed-height live stream, and Cancel
 *   reviewing  → the question, validation status, and Discard / Apply
 * (Enter applies and Esc discards while reviewing — see keybindings.)
 */
export function AiPanel() {
  const open = useStore((s) => s.aiOpen);
  const instruction = useStore((s) => s.aiInstruction);
  const busy = useStore((s) => s.aiBusy);
  const phase = useStore((s) => s.aiPhase);
  const startMs = useStore((s) => s.aiStartMs);
  const error = useStore((s) => s.aiError);
  const proposal = useStore((s) => s.aiProposal);
  const proposalValidation = useStore((s) => s.aiProposalValidation);
  const providers = useStore((s) => s.aiProviders);
  const provider = useStore((s) => s.aiProvider);

  const reviewing = proposal !== null;

  const [elapsed, setElapsed] = useState(0);

  useEffect(() => {
    void useStore.getState().detectAiProviders();
  }, []);

  // Elapsed seconds while the model works.
  useEffect(() => {
    if (!busy || startMs === null) {
      setElapsed(0);
      return;
    }
    const tick = () => setElapsed(Math.floor((Date.now() - startMs) / 1000));
    tick();
    const t = window.setInterval(tick, 1000);
    return () => window.clearInterval(t);
  }, [busy, startMs]);

  const available = providers.filter((p) => p.available);
  const hasAi = available.length > 0;

  // Stay mounted through busy/review even if the panel was toggled shut.
  if (!hasAi || (!open && !busy && !reviewing)) return null;

  const question = instruction.trim();

  // ---- Reviewing: proposal summary + apply / discard ----
  if (reviewing) {
    return (
      <section className="ai-composer reviewing" aria-label="Vali AI">
        {question && <div className="ai-composer-q">{question}</div>}
        <div className="ai-review-line">
          <span className="ai-glyph">
            <SparkIcon size={14} />
          </span>
          <span className="ai-review-title">Proposed a change</span>
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
        <div className="ai-composer-bar">
          <span className="grow" />
          <button onClick={() => useStore.getState().discardAiProposal()}>
            Discard
            <span className="btn-kbd">
              <kbd>Esc</kbd>
            </span>
          </button>
          <button
            className="primary ai-apply"
            onClick={() => useStore.getState().applyAiProposal()}
          >
            <CheckIcon size={14} /> Apply change
            <span className="btn-kbd">
              <kbd>↵</kbd>
            </span>
          </button>
        </div>
      </section>
    );
  }

  // ---- Busy: question + clean working status (no response dump) ----
  if (busy) {
    return (
      <section className="ai-composer busy" aria-label="Vali AI" aria-live="polite">
        {question && <div className="ai-composer-q">{question}</div>}
        <div className="ai-composer-bar">
          <span className="ai-composer-id busy">
            <span className="spinner sm" aria-hidden />
            <span className="ai-phase">{phase ?? "Working…"}</span>
            <span className="ai-elapsed">{elapsed}s</span>
          </span>
          <span className="grow" />
          <button
            className="ai-cancel"
            onClick={() => void useStore.getState().cancelAi()}
          >
            Cancel
          </button>
        </div>
      </section>
    );
  }

  // ---- Idle: the composer ----
  const canAsk = question.length > 0;
  const send = () => {
    if (canAsk) void useStore.getState().askAi();
  };

  return (
    <section className="ai-composer" aria-label="Vali AI">
      <textarea
        className="ai-composer-input"
        placeholder="Ask your local AI to change your map."
        value={instruction}
        onChange={(e) => useStore.getState().setAiInstruction(e.target.value)}
        onKeyDown={(e) => {
          // Enter sends; Shift+Enter inserts a newline.
          if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            send();
          }
        }}
      />

      <div className="ai-composer-bar">
        <span className="ai-composer-id">
          <span className="ai-glyph">
            <SparkIcon size={14} />
          </span>
          <span className="ai-composer-name">Vali AI</span>
        </span>
        <span className="grow" />
        <ModelMenu
          providers={available}
          value={provider}
          onChange={(id) => useStore.getState().setAiProvider(id)}
        />
        <button
          className="ai-send"
          onClick={send}
          disabled={!canAsk}
          title="Send · Enter"
          aria-label="Send"
        >
          <SendIcon size={16} />
        </button>
      </div>

      {error && (
        <details className="ai-error" open>
          <summary>The request failed</summary>
          <pre>{error}</pre>
        </details>
      )}
    </section>
  );
}

/** Model picker styled as a popover menu that opens upward from the bar. */
function ModelMenu({
  providers,
  value,
  onChange,
}: {
  providers: AiProvider[];
  value: string;
  onChange: (id: string) => void;
}) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (!open) return;
    const onDown = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") setOpen(false);
    };
    document.addEventListener("mousedown", onDown);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onDown);
      document.removeEventListener("keydown", onKey);
    };
  }, [open]);

  const current = providers.find((p) => p.id === value) ?? providers[0];

  return (
    <div className="ai-model" ref={ref}>
      <button
        className="ai-model-btn"
        onClick={() => setOpen((o) => !o)}
        aria-haspopup="menu"
        aria-expanded={open}
        title="Choose which local AI to use"
      >
        <span className="ai-model-label">{current?.label ?? "AI"}</span>
        <ChevronIcon size={13} />
      </button>

      {open && (
        <div className="ai-model-menu" role="menu">
          {providers.map((p) => (
            <button
              key={p.id}
              className={`ai-model-item${p.id === value ? " sel" : ""}`}
              role="menuitemradio"
              aria-checked={p.id === value}
              onClick={() => {
                onChange(p.id);
                setOpen(false);
              }}
            >
              <span className="ai-model-item-label">{p.label}</span>
              {p.id === value && <CheckIcon size={14} />}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
