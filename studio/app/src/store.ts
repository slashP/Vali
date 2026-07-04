import { create } from "zustand";
import type {
  GenerateResult,
  MapLocation,
  ProgressSnapshot,
  SidecarEvent,
  StartedEvent,
  RegionCompleted,
  LatLng,
  ValidateResult,
  WarningEvent,
} from "./lib/types";
import { STARTER_DEFINITION } from "./lib/template";

export type View = "editor" | "run" | "map" | "data";

interface StudioState {
  // ---- navigation ----
  view: View;
  setView: (view: View) => void;

  // ---- sidecar ----
  sidecarAlive: boolean;
  version: string | null;
  dataFolder: string | null;
  logs: string[];
  setSidecarAlive: (alive: boolean) => void;
  setPing: (version: string, dataFolder: string) => void;
  pushLog: (line: string) => void;

  // ---- editor / definition document ----
  filePath: string | null;
  text: string;
  dirty: boolean;
  setText: (text: string) => void;
  setFilePath: (path: string | null) => void;
  loadFile: (path: string, text: string) => void;
  markSaved: () => void;
  newFromTemplate: () => void;

  // ---- validation ----
  validation: ValidateResult | null;
  validating: boolean;
  setValidation: (result: ValidateResult | null) => void;
  setValidating: (validating: boolean) => void;

  // ---- generation run ----
  runningId: string | null;
  isPreview: boolean;
  started: StartedEvent | null;
  progress: ProgressSnapshot | null;
  warnings: string[];
  result: GenerateResult | null;
  runError: string | null;
  runStartMs: number | null;
  liveSamples: LatLng[];
  beginRun: (id: string, isPreview: boolean) => void;
  finishRun: (result: GenerateResult) => void;
  failRun: (message: string) => void;
  applyEvent: (evt: SidecarEvent) => void;

  // ---- map data ----
  mapLocations: MapLocation[];
  mapSourcePath: string | null;
  setMapLocations: (locations: MapLocation[], sourcePath: string | null) => void;
}

const MAX_LOG_LINES = 500;

export const useStore = create<StudioState>((set, get) => ({
  view: "editor",
  setView: (view) => set({ view }),

  sidecarAlive: false,
  version: null,
  dataFolder: null,
  logs: [],
  setSidecarAlive: (sidecarAlive) => set({ sidecarAlive }),
  setPing: (version, dataFolder) => set({ version, dataFolder }),
  pushLog: (line) =>
    set((s) => ({ logs: [...s.logs, line].slice(-MAX_LOG_LINES) })),

  filePath: null,
  text: STARTER_DEFINITION,
  dirty: false,
  setText: (text) => set({ text, dirty: true }),
  setFilePath: (filePath) => set({ filePath }),
  loadFile: (filePath, text) => set({ filePath, text, dirty: false }),
  markSaved: () => set({ dirty: false }),
  newFromTemplate: () =>
    set({ text: STARTER_DEFINITION, filePath: null, dirty: true }),

  validation: null,
  validating: false,
  setValidation: (validation) => set({ validation }),
  setValidating: (validating) => set({ validating }),

  runningId: null,
  isPreview: false,
  started: null,
  progress: null,
  warnings: [],
  result: null,
  runError: null,
  runStartMs: null,
  liveSamples: [],
  beginRun: (id, isPreview) =>
    set({
      runningId: id,
      isPreview,
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
  applyEvent: (evt) => {
    const { runningId } = get();
    // Generation events are tied to the running request id.
    if (runningId && evt.id !== runningId) return;
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
        set((s) => ({ liveSamples: s.liveSamples.concat(sample) }));
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
  setMapLocations: (mapLocations, mapSourcePath) =>
    set({ mapLocations, mapSourcePath }),
}));
