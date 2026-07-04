import { useEffect, useState } from "react";
import { DiffEditor } from "@monaco-editor/react";
import { useStore } from "../store";
import { aiAvailable, aiEdit } from "../lib/commands";
import { request } from "../lib/sidecar";
import type { ValidateResult } from "../lib/types";

interface AiPanelProps {
  open: boolean;
  onClose: () => void;
  onApply: (newText: string) => void;
}

const DIFF_OPTIONS = {
  readOnly: true,
  renderSideBySide: false,
  minimap: { enabled: false },
  fontSize: 12,
  scrollBeyondLastLine: false,
  automaticLayout: true,
} as const;

export function AiPanel({ open, onClose, onApply }: AiPanelProps) {
  const currentText = useStore((s) => s.text);

  const [available, setAvailable] = useState<boolean | null>(null);
  const [instruction, setInstruction] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [proposal, setProposal] = useState<string | null>(null);
  const [proposalValidation, setProposalValidation] =
    useState<ValidateResult | null>(null);

  useEffect(() => {
    let alive = true;
    aiAvailable()
      .then((v) => alive && setAvailable(v))
      .catch(() => alive && setAvailable(false));
    return () => {
      alive = false;
    };
  }, []);

  const ask = async () => {
    setBusy(true);
    setError(null);
    setProposal(null);
    setProposalValidation(null);
    try {
      const edited = await aiEdit(instruction, currentText);
      setProposal(edited);
      try {
        const v = await request<ValidateResult>("validate", { json: edited });
        setProposalValidation(v);
      } catch {
        // Validation of the proposal is best-effort; ignore sidecar errors here.
      }
    } catch (e) {
      setError(String(e));
    } finally {
      setBusy(false);
    }
  };

  const apply = () => {
    if (proposal === null) return;
    onApply(proposal);
    setProposal(null);
    setProposalValidation(null);
  };

  const discard = () => {
    setProposal(null);
    setProposalValidation(null);
  };

  if (!open) return <aside className="ai-panel collapsed" />;

  const canAsk =
    available === true && !busy && instruction.trim().length > 0;

  return (
    <aside className="ai-panel">
      <div className="ai-header">
        <span>AI copilot</span>
        <button onClick={onClose}>×</button>
      </div>
      <div className="ai-content">
        {available === false && (
          <div className="hint">
            The <code>claude</code> CLI was not found on PATH. Install Claude
            Code to enable AI editing.
          </div>
        )}
        <textarea
          placeholder="Describe the change, e.g. 'only keep locations from 2020 or later and raise the goal to 10000'"
          value={instruction}
          onChange={(e) => setInstruction(e.target.value)}
          disabled={available !== true || busy}
        />
        <button className="primary" onClick={() => void ask()} disabled={!canAsk}>
          {busy ? "Asking Claude…" : "Ask Claude"}
        </button>

        {error && <div className="hint" style={{ color: "var(--err)" }}>{error}</div>}

        {proposal !== null && (
          <>
            <div className="hint">
              Proposed change (original → modified):
            </div>
            <div className="ai-diff">
              <DiffEditor
                language="json"
                theme="vs-dark"
                original={currentText}
                modified={proposal}
                options={DIFF_OPTIONS}
              />
            </div>
            {proposalValidation && (
              <div className="hint">
                {proposalValidation.valid
                  ? "Proposal validates cleanly."
                  : `Proposal has ${proposalValidation.errors.length} problem(s): ${proposalValidation.errors[0] ?? ""}`}
              </div>
            )}
            <div style={{ display: "flex", gap: 8 }}>
              <button className="primary" onClick={apply}>
                Apply
              </button>
              <button onClick={discard}>Discard</button>
            </div>
          </>
        )}
      </div>
    </aside>
  );
}
