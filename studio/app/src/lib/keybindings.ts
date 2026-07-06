// Global application keyboard shortcuts.
//
// Registered on the window in the capture phase so they run *before* Monaco's
// own handlers — for the combos we claim we stopPropagation, so the editor
// never sees them; everything else falls through to Monaco untouched.

import { useStore } from "../store";
import { IS_MAC } from "./platform";
import { zoomIn, zoomOut, zoomReset } from "./zoom";

/** Human label for the primary modifier, for tooltips. */
export const MOD = IS_MAC ? "⌘" : "Ctrl";

export interface Shortcut {
  keys: string;
  label: string;
}

/** Displayed in the keyboard-shortcuts modal. */
export const SHORTCUTS: Shortcut[] = [
  { keys: `${MOD}+Enter`, label: "Generate / apply AI change" },
  { keys: `${MOD}+Shift+D`, label: "Toggle documentation" },
  { keys: `${MOD}+Shift+A`, label: "Toggle Vali AI" },
  { keys: `${MOD}+Shift+L`, label: "Location data" },
  { keys: `${MOD}+=`, label: "Zoom in" },
  { keys: `${MOD}+-`, label: "Zoom out" },
  { keys: `${MOD}+0`, label: "Reset zoom" },
  { keys: "Esc", label: "Discard AI change / close panel" },
];

function focusAiInput() {
  window.setTimeout(() => {
    document.querySelector<HTMLTextAreaElement>(".ai-composer-input")?.focus();
  }, 0);
}

function handler(e: KeyboardEvent) {
  const store = useStore.getState();
  const target = e.target as HTMLElement | null;
  const tag = target?.tagName;
  // Monaco drives input through a hidden <textarea> inside .monaco-editor, so a
  // plain tag check would misread the code editor as a form field. Treat only
  // real inputs outside the editor (AI prompt, docs search) as fields.
  const inMonaco = !!target?.closest?.(".monaco-editor");
  const inField = (tag === "INPUT" || tag === "TEXTAREA") && !inMonaco;

  // Escape: back out of the current transient state, most-specific first.
  if (e.key === "Escape") {
    if (store.aiProposal !== null) {
      store.discardAiProposal();
      e.preventDefault();
    } else if (store.docsOpen && !store.dataOpen && !store.missingData) {
      store.setDocsOpen(false);
      e.preventDefault();
    }
    return;
  }

  // Plain Enter approves the AI proposal under review (Esc discards, above).
  // Fires from anywhere except a real input field (e.g. the docs search).
  if (
    e.key === "Enter" &&
    !e.shiftKey &&
    !e.metaKey &&
    !e.ctrlKey &&
    store.aiProposal !== null &&
    !inField
  ) {
    store.applyAiProposal();
    e.preventDefault();
    e.stopPropagation();
    return;
  }

  const mod = e.metaKey || e.ctrlKey;
  if (!mod) return;
  const key = e.key.toLowerCase();

  // Mod +/-/0: app-wide zoom (works everywhere, incl. in the editor).
  if (key === "=" || key === "+") {
    zoomIn();
    e.preventDefault();
    e.stopPropagation();
    return;
  }
  if (key === "-" || key === "_") {
    zoomOut();
    e.preventDefault();
    e.stopPropagation();
    return;
  }
  if (key === "0") {
    zoomReset();
    e.preventDefault();
    e.stopPropagation();
    return;
  }

  // Mod+Enter: apply the AI proposal under review, otherwise generate.
  if (key === "enter") {
    // Let field-specific Enter handlers win (e.g. the AI prompt box submits).
    if (inField) return;
    if (store.aiProposal !== null) {
      store.applyAiProposal();
    } else if (
      store.sidecarAlive &&
      !store.runningId &&
      store.validation?.valid === true
    ) {
      void store.runGenerate();
    } else {
      return;
    }
    e.preventDefault();
    e.stopPropagation();
    return;
  }

  if (!e.shiftKey) return;

  switch (key) {
    case "d":
      store.setDocsOpen(!store.docsOpen);
      break;
    case "a": {
      const wasOpen = store.aiOpen;
      store.setAiOpen(!wasOpen);
      if (!wasOpen) focusAiInput();
      break;
    }
    case "l":
      store.setDataOpen(!store.dataOpen);
      break;
    default:
      return;
  }
  e.preventDefault();
  e.stopPropagation();
}

/** Install the global shortcut handler. Returns a cleanup function. */
export function installKeybindings(): () => void {
  window.addEventListener("keydown", handler, true);
  return () => window.removeEventListener("keydown", handler, true);
}
