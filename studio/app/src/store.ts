import { create } from "zustand";
import type {
  DownloadProgress,
  DownloadResult,
  GenerateResult,
  MapLocation,
  MissingDataCountry,
  MissingDataPayload,
  PingResult,
  ProgressSnapshot,
  RegionCompleted,
  RemoteStatusResult,
  SidecarEvent,
  StartedEvent,
  LatLng,
  ValidateResult,
  ValiInfo,
  WarningEvent,
} from "./lib/types";
import { STARTER_DEFINITION } from "./lib/template";
import {
  newRequestId,
  request,
  sidecarRestart,
  SidecarRequestError,
} from "./lib/sidecar";
import {
  aiCancel,
  aiProviders,
  aiRun,
  workspaceRoot as getWorkspaceRoot,
  type AiProvider,
} from "./lib/commands";
import { buildEditPrompt, buildFixPrompt } from "./lib/aiPrompt";
import { logError } from "./lib/log";
import {
  createMap,
  definitionPath,
  deleteMap,
  listMaps,
  parseMapSummary,
  readDefinition,
  readLastRun,
  updateMeta,
  writeDefinition,
  writeLastRun,
  type MapEntry,
} from "./lib/workspace";
import type { DataStatus } from "./lib/types";

const LS_SLUG = "vali.currentSlug";
const LS_AI_OPEN = "vali.aiOpen";
const LS_DOCS_OPEN = "vali.docsOpen";
const LS_AI_PROVIDER = "vali.aiProvider";
const MAX_LOG_LINES = 500;
/** Generation and downloads can run far longer than the default 10 min. */
const LONG_TIMEOUT_MS = 2 * 60 * 60 * 1000;

// Guards against concurrent workspace initialization (React StrictMode double-
// invokes mount effects). Not a permanent latch: if a run doesn't finish (e.g.
// a transient error), a later trigger can re-run it.
let workspaceInitInFlight = false;
// Monotonic tokens so the latest async map-switch / validation wins over a
// slower earlier one that resolves out of order.
let selectSeq = 0;
let validateSeq = 0;

export interface DownloadState {
  id: string;
  code: string;
  running: boolean;
  phase: string;
  fileName: string;
  filesDone: number;
  filesTotal: number;
  bytesDone: number;
  bytesTotal: number;
  result?: DownloadResult;
  error?: string;
}

function invalidResult(message: string): ValidateResult {
  return { valid: false, errors: [message], effectiveDefinition: null };
}

function sortByName(maps: MapEntry[]): MapEntry[] {
  return [...maps].sort((a, b) => a.name.localeCompare(b.name));
}

interface StudioState {
  // ---- sidecar ----
  sidecarAlive: boolean;
  /** Latches true the first time the engine responds; gates the main UI. Never
   *  flips back, so a later transient drop doesn't kick the user to setup. */
  engineEverAlive: boolean;
  version: string | null;
  dataFolder: string | null;
  logs: string[];
  setSidecarAlive: (alive: boolean) => void;
  setPing: (version: string, dataFolder: string) => void;
  pushLog: (line: string) => void;
  restartEngine: () => Promise<void>;

  // ---- workspace ----
  workspaceRoot: string | null;
  maps: MapEntry[];
  currentSlug: string | null;
  initWorkspace: () => Promise<void>;
  selectMap: (slug: string) => Promise<void>;
  createNewMap: (name: string) => Promise<void>;
  importMap: (name: string, content: string) => Promise<void>;
  updateMapDetails: (
    slug: string,
    patch: { name?: string; description?: string },
  ) => Promise<void>;
  deleteMapBySlug: (slug: string) => Promise<void>;
  refreshCurrentMapSummary: () => void;

  // ---- editor document ----
  text: string;
  dirty: boolean;
  loadError: string | null;
  saveError: string | null;
  setText: (text: string) => void;
  saveCurrent: () => Promise<void>;
  retryLoad: () => Promise<void>;
  dismissSaveError: () => void;

  // ---- validation ----
  validation: ValidateResult | null;
  validating: boolean;
  validateCurrent: () => Promise<void>;

  // ---- Vali AI (persisted across map switches) ----
  aiOpen: boolean;
  aiInstruction: string;
  aiBusy: boolean;
  aiStartMs: number | null;
  /** Live phase text shown while the AI works (asking / checking / fixing). */
  aiPhase: string | null;
  /** Streamed text the local model is producing, shown live in the overlay. */
  aiStreamText: string;
  appendAiStream: (chunk: string) => void;
  aiError: string | null;
  aiProposal: string | null;
  aiProposalValidation: ValidateResult | null;
  setAiOpen: (open: boolean) => void;
  setAiInstruction: (text: string) => void;
  askAi: () => Promise<void>;
  cancelAi: () => Promise<void>;
  applyAiProposal: () => void;
  discardAiProposal: () => void;
  // ---- AI providers (which local CLI to use) ----
  aiProviders: AiProvider[];
  aiProvider: string;
  detectAiProviders: () => Promise<void>;
  setAiProvider: (id: string) => void;

  // ---- documentation panel ----
  docsOpen: boolean;
  setDocsOpen: (open: boolean) => void;

  // ---- generation run ----
  runningId: string | null;
  runningSlug: string | null;
  /** The map that the current run state (live or restored) belongs to. */
  resultSlug: string | null;
  started: StartedEvent | null;
  progress: ProgressSnapshot | null;
  warnings: string[];
  result: GenerateResult | null;
  runError: string | null;
  runStartMs: number | null;
  liveSamples: LatLng[];
  runGenerate: () => Promise<void>;
  beginRun: (id: string, slug: string) => void;
  finishRun: (result: GenerateResult) => void;
  failRun: (message: string) => void;
  cancelRun: () => Promise<void>;
  dismissRun: () => void;
  loadLastRun: (slug: string) => Promise<void>;

  // ---- missing-data prompt ----
  missingData: MissingDataCountry[] | null;
  dismissMissingData: () => void;
  downloadMissingAndRetry: () => Promise<void>;

  // ---- downloads ----
  downloads: Record<string, DownloadState>;
  startDownload: (
    code: string,
    opts: { full?: boolean; updates?: boolean },
  ) => Promise<DownloadResult>;
  cancelDownload: (code: string) => Promise<void>;

  // ---- vali info ----
  valiInfo: ValiInfo | null;
  setValiInfo: (info: ValiInfo) => void;

  // ---- data summary + sheet ----
  dataSummary: { countryCount: number; updateCount: number } | null;
  dataOpen: boolean;
  setDataOpen: (open: boolean) => void;
  refreshDataSummary: () => Promise<void>;
  setDataSummary: (summary: { countryCount: number; updateCount: number }) => void;

  // ---- events ----
  applyEvent: (evt: SidecarEvent) => void;

  // ---- map data ----
  mapLocations: MapLocation[];
  mapSourcePath: string | null;
  /** mtime of the loaded source file, so an in-place rewrite (regeneration
   *  overwrites the same filename) still triggers a reload. */
  mapSourceMtime: number | null;
  /** The definition text the displayed points were produced from; null when
   *  the map shows an externally loaded file not tied to this definition. */
  mapText: string | null;
  setMapLocations: (
    locations: MapLocation[],
    sourcePath: string | null,
    tiedToText: string | null,
    mtimeMs?: number | null,
  ) => void;
}

export const useStore = create<StudioState>((set, get) => ({
  sidecarAlive: false,
  engineEverAlive: false,
  version: null,
  dataFolder: null,
  logs: [],
  setSidecarAlive: (sidecarAlive) =>
    set((s) => ({
      sidecarAlive,
      engineEverAlive: s.engineEverAlive || sidecarAlive,
    })),
  setPing: (version, dataFolder) => set({ version, dataFolder }),
  pushLog: (line) =>
    set((s) => ({ logs: [...s.logs, line].slice(-MAX_LOG_LINES) })),
  restartEngine: async () => {
    await sidecarRestart();
    try {
      const ping = await request<PingResult>("ping", {});
      // Go through the latching setter so engineEverAlive flips and the setup
      // gate lifts immediately on a successful restart.
      get().setSidecarAlive(true);
      set({ version: ping.version, dataFolder: ping.dataFolder });
      // If the workspace didn't load earlier, do it now so Retry lands on the
      // user's maps instead of an empty list.
      void get().initWorkspace();
    } catch (e) {
      logError("restartEngine", e);
    }
  },

  workspaceRoot: null,
  maps: [],
  currentSlug: null,
  initWorkspace: async () => {
    // Already fully initialized (a map is selected), or a run is in flight.
    if (workspaceInitInFlight) return;
    if (get().currentSlug && get().workspaceRoot) return;
    workspaceInitInFlight = true;
    try {
      const root = await getWorkspaceRoot();
      set({ workspaceRoot: root });
      let maps = await listMaps(root);
      if (maps.length === 0) {
        const entry = await createMap(root, "My first map");
        maps = [entry];
      }
      set({ maps: sortByName(maps) });
      const stored = localStorage.getItem(LS_SLUG);
      const slug =
        stored && maps.some((m) => m.slug === stored) ? stored : maps[0].slug;
      await get().selectMap(slug);
    } catch (e) {
      // Leave it un-initialized (currentSlug stays null) so a later trigger —
      // the engine coming online, or the setup-page Retry — can re-run it,
      // instead of stranding the user on an empty workspace until a full reload.
      logError("initWorkspace", e);
    } finally {
      workspaceInitInFlight = false;
    }
  },
  selectMap: async (slug) => {
    const root = get().workspaceRoot;
    if (!root) return;
    // Re-selecting the open map (that loaded fine) is a no-op — don't reload
    // from disk and clobber edits made within the autosave-debounce window.
    if (slug === get().currentSlug && !get().loadError) return;

    const seq = ++selectSeq;
    // Flush unsaved edits on the outgoing map before swapping, so switching
    // never drops work. On flush failure, abort the switch (edits stay in text).
    const prevSlug = get().currentSlug;
    if (prevSlug && prevSlug !== slug && get().dirty) {
      try {
        await get().saveCurrent();
      } catch {
        return; // saveError is set by saveCurrent; keep the current map + edits
      }
    }
    if (seq !== selectSeq) return; // superseded by a newer switch during flush
    // A proposal computed for the previous map must never apply to this one.
    set({ aiProposal: null, aiProposalValidation: null });
    try {
      const text = await readDefinition(root, slug);
      if (seq !== selectSeq) return; // superseded during the read
      set({ currentSlug: slug, text, dirty: false, validation: null, loadError: null });
    } catch (e) {
      if (seq !== selectSeq) return;
      // Never overwrite an unreadable definition with starter content — surface
      // the error and let the user retry. Starter content is only ever written
      // by createMap / first-launch bootstrap.
      logError("selectMap", e);
      set({
        currentSlug: slug,
        text: "",
        dirty: false,
        validation: null,
        loadError: e instanceof Error ? e.message : String(e),
      });
      localStorage.setItem(LS_SLUG, slug);
      return;
    }
    localStorage.setItem(LS_SLUG, slug);
    void get().validateCurrent();
    // Restore this map's persisted generation summary (unless a run is live).
    if (!get().runningId) void get().loadLastRun(slug);
  },
  createNewMap: async (name) => {
    const root = get().workspaceRoot;
    if (!root) return;
    const entry = await createMap(root, name);
    set({ maps: sortByName([...get().maps, entry]) });
    await get().selectMap(entry.slug);
  },
  importMap: async (name, content) => {
    const root = get().workspaceRoot;
    if (!root) return;
    const entry = await createMap(root, name, content);
    set({ maps: sortByName([...get().maps, entry]) });
    await get().selectMap(entry.slug);
  },
  updateMapDetails: async (slug, patch) => {
    const root = get().workspaceRoot;
    if (!root) return;
    const merged = await updateMeta(root, slug, patch);
    set({
      maps: sortByName(
        get().maps.map((m) =>
          m.slug === slug
            ? { ...m, name: merged.name, description: merged.description ?? "" }
            : m,
        ),
      ),
    });
  },
  deleteMapBySlug: async (slug) => {
    const root = get().workspaceRoot;
    if (!root) return;
    const deletingCurrent = get().currentSlug === slug;
    await deleteMap(root, slug);
    const remaining = get().maps.filter((m) => m.slug !== slug);
    set({ maps: remaining });
    if (deletingCurrent) {
      // The deleted map's edits are moot; clear dirty so selectMap doesn't try
      // to flush (and recreate) the folder we just removed.
      set({ dirty: false });
      if (remaining.length === 0) {
        const entry = await createMap(root, "My first map");
        set({ maps: [entry] });
        await get().selectMap(entry.slug);
      } else {
        await get().selectMap(remaining[0].slug);
      }
    }
  },
  refreshCurrentMapSummary: () => {
    const slug = get().currentSlug;
    if (!slug) return;
    const { countries, goal } = parseMapSummary(get().text);
    set({
      maps: get().maps.map((m) =>
        m.slug === slug ? { ...m, countries, goal } : m,
      ),
    });
  },

  text: STARTER_DEFINITION,
  dirty: false,
  loadError: null,
  saveError: null,
  setText: (text) => set({ text, dirty: true }),
  saveCurrent: async () => {
    const root = get().workspaceRoot;
    const slug = get().currentSlug;
    if (!root || !slug) return;
    try {
      await writeDefinition(root, slug, get().text);
      set({ dirty: false, saveError: null });
    } catch (e) {
      logError("saveCurrent", e);
      set({ saveError: `Couldn't save the map: ${e instanceof Error ? e.message : String(e)}` });
      throw e;
    }
  },
  retryLoad: async () => {
    const slug = get().currentSlug;
    if (slug) await get().selectMap(slug);
  },
  dismissSaveError: () => set({ saveError: null }),

  validation: null,
  validating: false,
  validateCurrent: async () => {
    const text = get().text;
    const seq = ++validateSeq;
    set({ validating: true });
    get().refreshCurrentMapSummary();
    try {
      const result = await request<ValidateResult>("validate", { json: text });
      if (seq !== validateSeq) return; // a newer validation superseded us
      set({ validation: result, validating: false });
    } catch (e) {
      if (seq !== validateSeq) return;
      logError("validate", e);
      set({
        validation: invalidResult(
          `Sidecar unavailable: ${e instanceof Error ? e.message : String(e)}`,
        ),
        validating: false,
      });
    }
  },

  aiOpen: localStorage.getItem(LS_AI_OPEN) !== "false",
  aiInstruction: "",
  aiBusy: false,
  aiStartMs: null,
  aiPhase: null,
  aiStreamText: "",
  appendAiStream: (chunk) =>
    set((s) => ({ aiStreamText: (s.aiStreamText + chunk).slice(-12000) })),
  aiError: null,
  aiProposal: null,
  aiProposalValidation: null,
  setAiOpen: (open) => {
    localStorage.setItem(LS_AI_OPEN, String(open));
    set({ aiOpen: open });
  },
  setAiInstruction: (aiInstruction) => set({ aiInstruction }),
  askAi: async () => {
    const instruction = get().aiInstruction.trim();
    const provider = get().aiProvider;
    if (!instruction || get().aiBusy || !provider) return;
    // Bind the request to its map so a result that returns after the user has
    // switched maps can never propose (and then apply) edits to a different one.
    const slug = get().currentSlug;
    const baseText = get().text;
    // True while this request still owns the flow (same map, not cancelled).
    const live = () => get().currentSlug === slug && get().aiBusy;

    set({
      aiBusy: true,
      aiStartMs: Date.now(),
      aiPhase: "Asking your local AI…",
      aiStreamText: "",
      aiError: null,
      aiProposal: null,
      aiProposalValidation: null,
    });

    // Best-effort validation of a proposal via the engine.
    const validate = async (json: string): Promise<ValidateResult | null> => {
      try {
        return await request<ValidateResult>("validate", { json });
      } catch {
        return null;
      }
    };

    const MAX_FIX_ROUNDS = 2;
    try {
      let proposal = await aiRun(provider, buildEditPrompt(instruction, baseText));
      if (!live()) return;

      set({ aiPhase: "Checking against the schema…" });
      let validation = await validate(proposal);

      // Feed validation errors (which often name the correct property) back to
      // the model and let it self-correct before the user ever sees the diff.
      let round = 0;
      while (
        live() &&
        validation &&
        !validation.valid &&
        validation.errors.length > 0 &&
        round < MAX_FIX_ROUNDS
      ) {
        round++;
        const n = validation.errors.length;
        set({ aiPhase: `Fixing ${n} issue${n === 1 ? "" : "s"}…`, aiStreamText: "" });
        proposal = await aiRun(
          provider,
          buildFixPrompt(instruction, proposal, validation.errors),
        );
        if (!live()) return;
        set({ aiPhase: "Re-checking…" });
        validation = await validate(proposal);
      }

      if (!live()) return;
      set({ aiProposal: proposal, aiProposalValidation: validation });
    } catch (e) {
      if (String(e) === "cancelled") return; // user aborted — not an error
      if (get().currentSlug !== slug) return;
      logError("askAi", e);
      set({ aiError: e instanceof Error ? e.message : String(e) });
    } finally {
      set({ aiBusy: false, aiStartMs: null, aiPhase: null });
    }
  },
  cancelAi: async () => {
    if (!get().aiBusy) return;
    try {
      await aiCancel();
    } catch {
      // best-effort; the request rejects with "cancelled" which askAi ignores
    }
    // Clearing aiBusy also makes the askAi loop's live() guard bail out.
    set({ aiBusy: false, aiStartMs: null, aiPhase: null });
  },
  applyAiProposal: () => {
    const proposal = get().aiProposal;
    if (proposal === null) return;
    set({
      text: proposal,
      dirty: true,
      aiProposal: null,
      aiProposalValidation: null,
      aiInstruction: "",
    });
    void get().validateCurrent();
  },
  discardAiProposal: () => set({ aiProposal: null, aiProposalValidation: null }),

  aiProviders: [],
  aiProvider: localStorage.getItem(LS_AI_PROVIDER) ?? "",
  detectAiProviders: async () => {
    try {
      const list = await aiProviders();
      set({ aiProviders: list });
      // Keep the selection valid: fall back to the first available provider.
      const avail = list.filter((p) => p.available).map((p) => p.id);
      if (!avail.includes(get().aiProvider)) {
        const next = avail[0] ?? "";
        localStorage.setItem(LS_AI_PROVIDER, next);
        set({ aiProvider: next });
      }
    } catch (e) {
      logError("detectAiProviders", e);
    }
  },
  setAiProvider: (id) => {
    localStorage.setItem(LS_AI_PROVIDER, id);
    set({ aiProvider: id });
  },

  docsOpen: localStorage.getItem(LS_DOCS_OPEN) === "true",
  setDocsOpen: (open) => {
    localStorage.setItem(LS_DOCS_OPEN, String(open));
    set({ docsOpen: open });
  },

  runningId: null,
  runningSlug: null,
  resultSlug: null,
  started: null,
  progress: null,
  warnings: [],
  result: null,
  runError: null,
  runStartMs: null,
  liveSamples: [],
  runGenerate: async () => {
    const root = get().workspaceRoot;
    const slug = get().currentSlug;
    if (!root || !slug || get().runningId) return;
    const id = newRequestId();
    // Claim the run synchronously (before any await) so a double-click can't
    // race two generations past the runningId guard.
    get().beginRun(id, slug);
    set({ missingData: null });
    try {
      await get().saveCurrent();
    } catch (e) {
      get().failRun(
        `Couldn't save before generating: ${e instanceof Error ? e.message : String(e)}`,
      );
      return;
    }
    // The text being generated from. On success the map reflects exactly this,
    // so mark it in-sync immediately — otherwise the map flashes "Definition
    // changed" between finishing and the async output-load completing.
    const generatedText = get().text;
    const params: Record<string, unknown> = {
      definitionPath: definitionPath(root, slug),
      includeAdditionalInfo: false,
    };
    try {
      const result = await request<GenerateResult>(
        "generate",
        params,
        id,
        LONG_TIMEOUT_MS,
      );
      set({ mapText: generatedText });
      get().finishRun(result);
      // Persist the summary so the map's generation details survive restarts.
      const goal =
        get().started?.totalGoal ?? parseMapSummary(generatedText).goal ?? 0;
      void writeLastRun(root, slug, { atMs: Date.now(), goal, result }).catch(
        () => {},
      );
    } catch (e) {
      if (e instanceof SidecarRequestError && e.code === "missingData") {
        const payload = e.data as MissingDataPayload | undefined;
        let countries = payload?.countries ?? [];
        try {
          const status = await request<RemoteStatusResult>("remoteStatus", {
            countryCodes: countries.map((c) => c.code),
          });
          countries = countries.map((c) => ({
            ...c,
            estimatedBytes:
              status.countries.find((r) => r.code === c.code)?.bytesToDownload ?? null,
          }));
        } catch {
          // Sizes are cosmetic — show the modal without them.
        }
        set({
          missingData: countries,
          runningId: null,
          runError: null,
        });
      } else {
        logError("generate", e);
        get().failRun(e instanceof Error ? e.message : String(e));
      }
    }
  },
  beginRun: (id, slug) =>
    set({
      runningId: id,
      runningSlug: slug,
      resultSlug: slug,
      started: null,
      progress: null,
      warnings: [],
      result: null,
      runError: null,
      runStartMs: Date.now(),
      liveSamples: [],
    }),
  finishRun: (result) => set({ result, runningId: null }),
  failRun: (message) => set({ runError: message, runningId: null }),
  cancelRun: async () => {
    const id = get().runningId;
    if (!id) return;
    try {
      await request("cancel", { targetId: id }, newRequestId());
    } catch {
      // the generate response still arrives with stoppedEarly
    }
  },
  dismissRun: () =>
    set({
      started: null,
      progress: null,
      warnings: [],
      result: null,
      runError: null,
      liveSamples: [],
    }),
  loadLastRun: async (slug) => {
    const root = get().workspaceRoot;
    if (!root) return;
    const stored = await readLastRun(root, slug);
    // Guard against a fast map switch while the read was in flight.
    if (get().currentSlug !== slug || get().runningId) return;
    if (stored) {
      set({
        result: stored.result,
        started: { countries: [], totalGoal: stored.goal },
        progress: null,
        warnings: [],
        runError: null,
        runStartMs: null,
        resultSlug: slug,
      });
    } else {
      set({
        result: null,
        started: null,
        progress: null,
        warnings: [],
        runError: null,
        resultSlug: null,
      });
    }
  },

  missingData: null,
  dismissMissingData: () => set({ missingData: null }),
  downloadMissingAndRetry: async () => {
    const missing = get().missingData;
    if (!missing) return;
    const slug = get().currentSlug;
    set({ missingData: null });
    for (const c of missing) {
      try {
        await get().startDownload(c.code, {});
      } catch {
        // a failed download will surface again as missingData on retry
      }
    }
    // Only auto-retry generation if the user is still on the prompting map.
    if (get().currentSlug === slug) await get().runGenerate();
  },

  downloads: {},
  startDownload: async (code, opts) => {
    const id = newRequestId();
    set((s) => ({
      downloads: {
        ...s.downloads,
        [code]: {
          id,
          code,
          running: true,
          phase: "",
          fileName: "",
          filesDone: 0,
          filesTotal: 0,
          bytesDone: 0,
          bytesTotal: 0,
        },
      },
    }));
    try {
      const result = await request<DownloadResult>(
        "download",
        { countryCode: code, full: opts.full ?? false, updates: opts.updates ?? false },
        id,
        LONG_TIMEOUT_MS,
      );
      set((s) => ({
        downloads: { ...s.downloads, [code]: { ...s.downloads[code], running: false, result } },
      }));
      return result;
    } catch (e) {
      logError(`download:${code}`, e);
      const message = e instanceof Error ? e.message : String(e);
      set((s) => ({
        downloads: { ...s.downloads, [code]: { ...s.downloads[code], running: false, error: message } },
      }));
      throw e;
    }
  },
  cancelDownload: async (code) => {
    const d = get().downloads[code];
    if (!d) return;
    try {
      await request("cancel", { targetId: d.id }, newRequestId());
    } catch {
      // ignore — the download response arrives with cancelled:true
    }
  },

  valiInfo: null,
  setValiInfo: (valiInfo) => set({ valiInfo }),

  dataSummary: null,
  dataOpen: false,
  setDataOpen: (dataOpen) => set({ dataOpen }),
  setDataSummary: (dataSummary) => set({ dataSummary }),
  refreshDataSummary: async () => {
    try {
      const status = await request<DataStatus>("dataStatus", {});
      const codes = status.countries.map((c) => c.code);
      let updateCount = 0;
      if (codes.length > 0) {
        try {
          const rs = await request<RemoteStatusResult>("remoteStatus", {
            countryCodes: codes,
          });
          updateCount = rs.countries.filter(
            (c) => !c.error && c.local && c.remote && c.upToDate === false,
          ).length;
        } catch {
          // remote check needs network; the count stays 0 until Data is opened
        }
      }
      set({ dataSummary: { countryCount: codes.length, updateCount } });
    } catch {
      // engine offline or dataStatus failed — keep the last known summary
    }
  },

  applyEvent: (evt) => {
    if (evt.event === "downloadProgress") {
      const d = evt.data as DownloadProgress;
      set((s) => {
        const cur = s.downloads[d.countryCode];
        // Ignore stray/late progress: only update the live download for this
        // id, so a trailing event can't resurrect a finished download.
        if (!cur || !cur.running || cur.id !== evt.id) return {};
        return {
          downloads: {
            ...s.downloads,
            [d.countryCode]: {
              ...cur,
              phase: d.phase,
              fileName: d.fileName,
              filesDone: d.filesDone,
              filesTotal: d.filesTotal,
              bytesDone: d.bytesDone,
              bytesTotal: d.bytesTotal,
            },
          },
        };
      });
      return;
    }

    // Generation events are tied to the running request id. Once no run is
    // active (runningId null), drop them — a late event must not mutate the
    // restored/other run's displayed state.
    const runningId = get().runningId;
    if (evt.id !== runningId) return;
    switch (evt.event) {
      case "started":
        set({ started: evt.data as StartedEvent });
        break;
      case "progress":
        set({ progress: evt.data as ProgressSnapshot });
        break;
      case "regionCompleted": {
        const region = evt.data as RegionCompleted;
        const sample = region.sample ?? [];
        // Cap the live preview so a long run can't grow the array (and GPU
        // buffer) without bound.
        set((s) => ({ liveSamples: s.liveSamples.concat(sample).slice(-20000) }));
        break;
      }
      case "warning":
        set((s) => ({
          warnings: [...s.warnings, (evt.data as WarningEvent).message],
        }));
        break;
      default:
        break;
    }
  },

  mapLocations: [],
  mapSourcePath: null,
  mapSourceMtime: null,
  mapText: null,
  setMapLocations: (mapLocations, mapSourcePath, tiedToText, mtimeMs = null) =>
    set({ mapLocations, mapSourcePath, mapSourceMtime: mtimeMs, mapText: tiedToText }),
}));
