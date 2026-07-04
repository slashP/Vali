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

export function EditorIcon({ size = 20 }: IconProps) {
  return (
    <svg {...base(size)}>
      <polyline points="9 8 5 12 9 16" />
      <polyline points="15 8 19 12 15 16" />
    </svg>
  );
}

export function RunIcon({ size = 20 }: IconProps) {
  return (
    <svg {...base(size)}>
      <polygon points="6 4 20 12 6 20 6 4" />
    </svg>
  );
}

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
