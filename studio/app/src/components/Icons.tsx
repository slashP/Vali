// Minimal stroke icons (no external icon dependency).

interface IconProps {
  size?: number;
}

const base = (size: number) => ({
  width: size,
  height: size,
  viewBox: "0 0 24 24",
  fill: "none",
  stroke: "currentColor",
  strokeWidth: 1.8,
  strokeLinecap: "round" as const,
  strokeLinejoin: "round" as const,
});

export function MapIcon({ size = 20 }: IconProps) {
  return (
    <svg {...base(size)}>
      <polygon points="3 6 9 3 15 6 21 3 21 18 15 21 9 18 3 21 3 6" />
      <line x1="9" y1="3" x2="9" y2="18" />
      <line x1="15" y1="6" x2="15" y2="21" />
    </svg>
  );
}

export function DataIcon({ size = 20 }: IconProps) {
  return (
    <svg {...base(size)}>
      <ellipse cx="12" cy="5" rx="8" ry="3" />
      <path d="M4 5v6c0 1.7 3.6 3 8 3s8-1.3 8-3V5" />
      <path d="M4 11v6c0 1.7 3.6 3 8 3s8-1.3 8-3v-6" />
    </svg>
  );
}

/** Compass rose — the app's cartographic mark. */
export function CompassIcon({ size = 18 }: IconProps) {
  return (
    <svg {...base(size)} strokeWidth={1.6}>
      <circle cx="12" cy="12" r="9" />
      <polygon points="12 5 14.2 12 12 19 9.8 12" fill="currentColor" stroke="none" />
      <circle cx="12" cy="12" r="1" fill="currentColor" stroke="none" />
    </svg>
  );
}

export function PlusIcon({ size = 16 }: IconProps) {
  return (
    <svg {...base(size)}>
      <line x1="12" y1="5" x2="12" y2="19" />
      <line x1="5" y1="12" x2="19" y2="12" />
    </svg>
  );
}

/** Four-point spark — the Vali AI mark. */
export function SparkIcon({ size = 16 }: IconProps) {
  return (
    <svg {...base(size)}>
      <path
        d="M12 2 L13.7 9.1 L21 11 L13.7 12.9 L12 20 L10.3 12.9 L3 11 L10.3 9.1 Z"
        fill="currentColor"
        stroke="none"
      />
    </svg>
  );
}

export function PencilIcon({ size = 15 }: IconProps) {
  return (
    <svg {...base(size)} strokeWidth={1.7}>
      <path d="M12 20h9" />
      <path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z" />
    </svg>
  );
}

export function TrashIcon({ size = 15 }: IconProps) {
  return (
    <svg {...base(size)} strokeWidth={1.7}>
      <polyline points="3 6 21 6" />
      <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
      <path d="M10 11v6M14 11v6" />
      <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
    </svg>
  );
}

export function ChevronIcon({ size = 16 }: IconProps) {
  return (
    <svg {...base(size)}>
      <polyline points="6 9 12 15 18 9" />
    </svg>
  );
}

/** Left-facing chevrons — collapse the sidebar to icons. */
export function CollapseIcon({ size = 15 }: IconProps) {
  return (
    <svg {...base(size)} strokeWidth={1.7}>
      <polyline points="13 7 8 12 13 17" />
      <polyline points="18 7 13 12 18 17" />
    </svg>
  );
}

/** Right-facing chevrons — expand the collapsed sidebar. */
export function ExpandIcon({ size = 15 }: IconProps) {
  return (
    <svg {...base(size)} strokeWidth={1.7}>
      <polyline points="11 7 16 12 11 17" />
      <polyline points="6 7 11 12 6 17" />
    </svg>
  );
}

export function ImportIcon({ size = 15 }: IconProps) {
  return (
    <svg {...base(size)} strokeWidth={1.7}>
      <path d="M12 3v12" />
      <polyline points="7 10 12 15 17 10" />
      <path d="M5 20h14" />
    </svg>
  );
}

export function ExportIcon({ size = 15 }: IconProps) {
  return (
    <svg {...base(size)} strokeWidth={1.7}>
      <path d="M12 21V9" />
      <polyline points="7 14 12 9 17 14" />
      <path d="M5 4h14" />
    </svg>
  );
}

export function StopIcon({ size = 15 }: IconProps) {
  return (
    <svg {...base(size)}>
      <rect x="6.5" y="6.5" width="11" height="11" rx="1.5" fill="currentColor" stroke="none" />
    </svg>
  );
}

export function RefreshIcon({ size = 14 }: IconProps) {
  return (
    <svg {...base(size)} strokeWidth={1.7}>
      <polyline points="21 4 21 9 16 9" />
      <path d="M20.5 13a8.5 8.5 0 1 1-2-7.9L21 9" />
    </svg>
  );
}

/** Open book — the documentation mark. */
export function BookIcon({ size = 15 }: IconProps) {
  return (
    <svg {...base(size)} strokeWidth={1.7}>
      <path d="M12 6.5C10.5 5.2 8.5 4.5 4 4.5v13c4.5 0 6.5 0.7 8 2 1.5-1.3 3.5-2 8-2v-13c-4.5 0-6.5 0.7-8 2Z" />
      <path d="M12 6.5v12.5" />
    </svg>
  );
}

export function SearchIcon({ size = 14 }: IconProps) {
  return (
    <svg {...base(size)} strokeWidth={1.7}>
      <circle cx="11" cy="11" r="7" />
      <line x1="21" y1="21" x2="16" y2="16" />
    </svg>
  );
}

export function CopyIcon({ size = 13 }: IconProps) {
  return (
    <svg {...base(size)} strokeWidth={1.7}>
      <rect x="9" y="9" width="11" height="11" rx="2" />
      <path d="M5 15V5a2 2 0 0 1 2-2h8" />
    </svg>
  );
}

export function CheckIcon({ size = 14 }: IconProps) {
  return (
    <svg {...base(size)} strokeWidth={2}>
      <polyline points="4 12 10 18 20 6" />
    </svg>
  );
}

export function KeyboardIcon({ size = 14 }: IconProps) {
  return (
    <svg {...base(size)} strokeWidth={1.6}>
      <rect x="2" y="6" width="20" height="12" rx="2" />
      <path d="M6 10h.01M10 10h.01M14 10h.01M18 10h.01M8 14h8" />
    </svg>
  );
}

// ---- window controls (10x10 grid, crisp thin strokes) -------------------

const win = {
  width: 10,
  height: 10,
  viewBox: "0 0 10 10",
  fill: "none",
  stroke: "currentColor",
  strokeWidth: 1,
} as const;

export function WinMinimizeIcon() {
  return (
    <svg {...win}>
      <line x1="1" y1="5.5" x2="9" y2="5.5" />
    </svg>
  );
}

export function WinMaximizeIcon() {
  return (
    <svg {...win}>
      <rect x="1.3" y="1.3" width="7.4" height="7.4" />
    </svg>
  );
}

export function WinRestoreIcon() {
  return (
    <svg {...win}>
      <rect x="1.2" y="2.8" width="6" height="6" />
      <polyline points="3.4 2.8 3.4 1.2 8.8 1.2 8.8 6.6 7.2 6.6" />
    </svg>
  );
}

export function WinCloseIcon() {
  return (
    <svg {...win}>
      <line x1="1.3" y1="1.3" x2="8.7" y2="8.7" />
      <line x1="8.7" y1="1.3" x2="1.3" y2="8.7" />
    </svg>
  );
}
