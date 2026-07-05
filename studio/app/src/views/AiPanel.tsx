import { useEffect, useState } from "react";
import { useStore } from "../store";
import { ChevronIcon, SparkIcon } from "../components/Icons";

export function AiPanel({ dockHeight }: { dockHeight: number | null }) {
  const open = useStore((s) => s.aiOpen);
  const instruction = useStore((s) => s.aiInstruction);
  const busy = useStore((s) => s.aiBusy);
  const error = useStore((s) => s.aiError);
  const reviewing = useStore((s) => s.aiProposal !== null);
  const providers = useStore((s) => s.aiProviders);
  const provider = useStore((s) => s.aiProvider);

  const [detecting, setDetecting] = useState(false);
  const [checked, setChecked] = useState(false);

  useEffect(() => {
    void useStore
      .getState()
      .detectAiProviders()
      .finally(() => setChecked(true));
  }, []);

  const available = providers.filter((p) => p.available);
  const hasAi = available.length > 0;

  const recheck = async () => {
    setDetecting(true);
    try {
      await useStore.getState().detectAiProviders();
    } finally {
      setDetecting(false);
    }
  };

  // No local AI detected: a disabled, non-expandable bar with a recheck action.
  if (!hasAi) {
    return (
      <section className="ai-dock disabled">
        <div className="ai-dock-head static">
          <span className="ai-glyph">
            <SparkIcon size={15} />
          </span>
          <span className="ai-name">Vali AI</span>
          <span className="ai-sub">
            {checked ? "no local AI found" : "checking for local AI…"}
          </span>
          <span className="grow" />
          {checked && (
            <button
              className="ai-recheck"
              onClick={() => void recheck()}
              disabled={detecting}
            >
              {detecting ? "Checking…" : "Retry"}
            </button>
          )}
        </div>
      </section>
    );
  }

  const canAsk = !busy && !reviewing && instruction.trim().length > 0;

  return (
    <section
      className={`ai-dock${open ? " open" : " collapsed"}`}
      style={open && dockHeight != null ? { height: dockHeight } : undefined}
    >
      <button
        className="ai-dock-head"
        onClick={() => useStore.getState().setAiOpen(!open)}
        aria-expanded={open}
        title="Toggle Vali AI"
      >
        <span className="ai-glyph">
          <SparkIcon size={15} />
        </span>
        <span className="ai-name">Vali AI</span>
        <span className="ai-sub">edit the definition in plain language</span>
        <span className="grow" />
        {busy && <span className="ai-tag working">working…</span>}
        <span className={`ai-chevron${open ? " up" : ""}`}>
          <ChevronIcon size={16} />
        </span>
      </button>

      {open && (
        <div className="ai-dock-body">
          {reviewing && (
            <div className="hint">
              Review the proposed change in the editor above, then apply or
              discard it.
            </div>
          )}

          <textarea
            className="ai-input"
            placeholder="e.g. only keep locations from 2020 or later and raise the goal to 10,000"
            value={instruction}
            onChange={(e) =>
              useStore.getState().setAiInstruction(e.target.value)
            }
            disabled={busy || reviewing}
            onKeyDown={(e) => {
              // Enter sends; Shift+Enter inserts a newline.
              if (e.key === "Enter" && !e.shiftKey) {
                e.preventDefault();
                if (canAsk) void useStore.getState().askAi();
              }
            }}
          />

          <div className="ai-controls">
            <select
              className="ai-provider"
              value={provider}
              onChange={(e) => useStore.getState().setAiProvider(e.target.value)}
              disabled={busy}
              title="Choose which local AI to use"
            >
              {available.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.label}
                </option>
              ))}
            </select>
            <span className="grow" />
            <button
              className="primary ai-ask"
              onClick={() => void useStore.getState().askAi()}
              disabled={!canAsk}
            >
              {busy ? "Working…" : "Ask Vali AI"}
            </button>
          </div>

          {error && (
            <details className="ai-error" open>
              <summary>The request failed</summary>
              <pre>{error}</pre>
            </details>
          )}
        </div>
      )}
    </section>
  );
}
