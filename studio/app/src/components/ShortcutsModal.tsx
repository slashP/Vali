import { Modal } from "./Modal";
import { SHORTCUTS } from "../lib/keybindings";

export function ShortcutsModal({ onClose }: { onClose: () => void }) {
  return (
    <Modal
      title="Keyboard shortcuts"
      width={440}
      onClose={onClose}
      footer={
        <button className="primary" onClick={onClose}>
          Close
        </button>
      }
    >
      <ul className="sc-list">
        {SHORTCUTS.map((s) => (
          <li key={s.label}>
            <span className="sc-label">{s.label}</span>
            <span className="sc-keys">
              {s.keys.split("+").map((k) => (
                <kbd key={k}>{k}</kbd>
              ))}
            </span>
          </li>
        ))}
      </ul>
    </Modal>
  );
}
