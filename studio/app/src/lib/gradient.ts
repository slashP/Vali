// A stable two-stop gradient derived from a map's slug — every map gets its
// own identity color, always the same for the same map (like dev.plonk.no).

function hashSeed(s: string): number {
  let h = 2166136261;
  for (let i = 0; i < s.length; i++) {
    h ^= s.charCodeAt(i);
    h = Math.imul(h, 16777619);
  }
  return h >>> 0;
}

export interface MapGradient {
  from: string;
  to: string;
  css: string;
}

export function mapGradient(seed: string): MapGradient {
  const h = hashSeed(seed);
  const h1 = h % 360;
  const h2 = (h1 + 35 + ((h >> 9) % 90)) % 360;
  const from = `hsl(${h1} 70% 62%)`;
  const to = `hsl(${h2} 64% 48%)`;
  return { from, to, css: `linear-gradient(135deg, ${from}, ${to})` };
}
