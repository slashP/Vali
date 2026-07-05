// Lightweight "is there a newer release?" check against GitHub. We only surface
// a banner + link to the release page — no auto-download. The current version
// comes from the bundled app manifest (tauri.conf.json) via getVersion().

import { getVersion } from "@tauri-apps/api/app";

const REPO = "slashP/Vali";
const LATEST_API = `https://api.github.com/repos/${REPO}/releases/latest`;
const RELEASES_PAGE = `https://github.com/${REPO}/releases/latest`;

export interface UpdateInfo {
  current: string;
  latest: string;
  url: string;
}

/** Parse a version into numeric components; ignores a leading `v` and any
 *  pre-release/build suffix so `v1.2.3-beta` → [1,2,3]. */
function parts(v: string): number[] {
  return v
    .replace(/^v/i, "")
    .split(/[.\-+]/)
    .map((p) => Number.parseInt(p, 10))
    .filter((n) => !Number.isNaN(n));
}

function isNewer(latest: string, current: string): boolean {
  const a = parts(latest);
  const b = parts(current);
  const len = Math.max(a.length, b.length);
  for (let i = 0; i < len; i++) {
    const x = a[i] ?? 0;
    const y = b[i] ?? 0;
    if (x !== y) return x > y;
  }
  return false;
}

/** Returns update info when a newer release exists, else null. Never throws —
 *  offline / no releases / rate-limited all resolve to null. */
export async function checkForUpdate(): Promise<UpdateInfo | null> {
  try {
    const current = await getVersion();
    // Bound the check so a captive portal / stalled socket can't leave it
    // pending forever.
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), 8000);
    let res: Response;
    try {
      res = await fetch(LATEST_API, {
        headers: { Accept: "application/vnd.github+json" },
        signal: controller.signal,
      });
    } finally {
      clearTimeout(timer);
    }
    if (!res.ok) return null;
    const data = (await res.json()) as { tag_name?: string; html_url?: string };
    const latest = (data.tag_name ?? "").replace(/^v/i, "");
    if (!latest || !isNewer(latest, current)) return null;
    return { current, latest, url: data.html_url ?? RELEASES_PAGE };
  } catch {
    return null;
  }
}
