// Lightweight "is there a newer release?" check against GitHub. We only surface
// a banner + link to the release page — no auto-download. The current version
// comes from the bundled app manifest (tauri.conf.json) via getVersion().
//
// The repo (slashP/Vali) is SHARED with the Vali CLI, which carries bare version
// tags like `3.0.1`. Those must never be read as Studio updates, so we only
// consider releases whose tag is prefixed `studio-v` (see release.yml). Querying
// releases/latest wouldn't work — it returns whichever release is newest across
// the whole repo, CLI included — so we list releases and filter.

import { getVersion } from "@tauri-apps/api/app";

const REPO = "slashP/Vali";
const LIST_API = `https://api.github.com/repos/${REPO}/releases?per_page=30`;
const RELEASES_PAGE = `https://github.com/${REPO}/releases`;
/** Studio releases are tagged `studio-v<semver>`; CLI tags are bare `x.y.z`. */
const STUDIO_TAG_PREFIX = "studio-v";

export interface UpdateInfo {
  current: string;
  latest: string;
  url: string;
}

interface GithubRelease {
  tag_name?: string;
  html_url?: string;
  draft?: boolean;
  prerelease?: boolean;
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

/** Returns update info when a newer Studio release exists, else null. Never
 *  throws — offline / no releases / rate-limited all resolve to null. */
export async function checkForUpdate(): Promise<UpdateInfo | null> {
  try {
    const current = await getVersion();
    // Bound the check so a captive portal / stalled socket can't leave it
    // pending forever.
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), 8000);
    let res: Response;
    try {
      res = await fetch(LIST_API, {
        headers: { Accept: "application/vnd.github+json" },
        signal: controller.signal,
      });
    } finally {
      clearTimeout(timer);
    }
    if (!res.ok) return null;
    const releases = (await res.json()) as GithubRelease[];
    if (!Array.isArray(releases)) return null;

    // Keep only published Studio releases, newest version first.
    const studio = releases
      .filter(
        (r) =>
          !r.draft &&
          !r.prerelease &&
          typeof r.tag_name === "string" &&
          r.tag_name.startsWith(STUDIO_TAG_PREFIX),
      )
      .map((r) => ({
        version: r.tag_name!.slice(STUDIO_TAG_PREFIX.length),
        url: r.html_url ?? RELEASES_PAGE,
      }))
      .sort((a, b) => (isNewer(a.version, b.version) ? -1 : 1));

    const newest = studio[0];
    if (!newest || !isNewer(newest.version, current)) return null;
    return { current, latest: newest.version, url: newest.url };
  } catch {
    return null;
  }
}
