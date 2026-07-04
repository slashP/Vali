import { useStore, type View } from "../store";
import { DataIcon, EditorIcon, MapIcon, RunIcon } from "./Icons";
import type { JSX } from "react";

const NAV: {
  view: View;
  label: string;
  icon: (props: { size?: number }) => JSX.Element;
}[] = [
  { view: "editor", label: "Editor", icon: EditorIcon },
  { view: "run", label: "Run", icon: RunIcon },
  { view: "map", label: "Map", icon: MapIcon },
  { view: "data", label: "Data", icon: DataIcon },
];

export function Sidebar() {
  const view = useStore((s) => s.view);
  const setView = useStore((s) => s.setView);

  return (
    <nav className="sidebar">
      {NAV.map(({ view: v, label, icon: Icon }) => (
        <button
          key={v}
          className={`nav-btn${view === v ? " active" : ""}`}
          onClick={() => setView(v)}
          title={label}
        >
          <Icon />
          <span>{label}</span>
        </button>
      ))}
    </nav>
  );
}
