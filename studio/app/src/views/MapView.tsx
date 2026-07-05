import { useEffect, useRef, useState } from "react";
import { Map as MapLibreMap, type IControl } from "maplibre-gl";
import { MapboxOverlay } from "@deck.gl/mapbox";
import { ScatterplotLayer } from "@deck.gl/layers";
import type { Color, Layer, PickingInfo } from "@deck.gl/core";
import { useStore } from "../store";
import { pickOpenFile, revealInExplorer } from "../lib/commands";
import { MOD } from "../lib/keybindings";
import { logError } from "../lib/log";
import { loadLocationsFile } from "../lib/locations";
import { listOutputs } from "../lib/workspace";
import { RunOverlay } from "./RunOverlay";
import type { LatLng, MapLocation } from "../lib/types";

const MAP_STYLE = "https://basemaps.cartocdn.com/gl/dark-matter-gl-style/style.json";

const SOLID: Color = [227, 187, 92];
const LIVE: Color = [95, 199, 173, 170];

interface ScreenXY {
  x: number;
  y: number;
}

function boundsOf(locs: MapLocation[]): [[number, number], [number, number]] | null {
  if (locs.length === 0) return null;
  let minLat = Infinity,
    minLng = Infinity,
    maxLat = -Infinity,
    maxLng = -Infinity;
  for (const l of locs) {
    if (l.lat < minLat) minLat = l.lat;
    if (l.lat > maxLat) maxLat = l.lat;
    if (l.lng < minLng) minLng = l.lng;
    if (l.lng > maxLng) maxLng = l.lng;
  }
  return [
    [minLng, minLat],
    [maxLng, maxLat],
  ];
}

export function MapView() {
  const mapLocations = useStore((s) => s.mapLocations);
  const mapSourcePath = useStore((s) => s.mapSourcePath);
  const mapText = useStore((s) => s.mapText);
  const text = useStore((s) => s.text);
  const liveSamples = useStore((s) => s.liveSamples);
  const runningId = useStore((s) => s.runningId);
  const runningSlug = useStore((s) => s.runningSlug);
  const workspaceRoot = useStore((s) => s.workspaceRoot);
  const currentSlug = useStore((s) => s.currentSlug);
  const result = useStore((s) => s.result);
  const resultSlug = useStore((s) => s.resultSlug);
  const started = useStore((s) => s.started);
  const progress = useStore((s) => s.progress);
  const runError = useStore((s) => s.runError);
  const validation = useStore((s) => s.validation);
  const sidecarAlive = useStore((s) => s.sidecarAlive);

  const [selected, setSelected] = useState<MapLocation | null>(null);
  const [popupXY, setPopupXY] = useState<ScreenXY | null>(null);
  const [fileError, setFileError] = useState<string | null>(null);

  const containerRef = useRef<HTMLDivElement | null>(null);
  const mapRef = useRef<MapLibreMap | null>(null);
  const overlayRef = useRef<MapboxOverlay | null>(null);

  // Create the map + deck.gl overlay once.
  useEffect(() => {
    if (!containerRef.current) return;
    const map = new MapLibreMap({
      container: containerRef.current,
      style: MAP_STYLE,
      center: [10, 20],
      zoom: 1.4,
      attributionControl: false,
    });
    const overlay = new MapboxOverlay({
      interleaved: false,
      pickingRadius: 8,
      layers: [],
    });
    map.addControl(overlay as unknown as IControl);
    mapRef.current = map;
    overlayRef.current = overlay;
    return () => {
      map.remove();
      mapRef.current = null;
      overlayRef.current = null;
    };
  }, []);

  // Discover the current map's output files, and auto-load the newest.
  useEffect(() => {
    if (!workspaceRoot || !currentSlug) return;
    let alive = true;
    void listOutputs(workspaceRoot, currentSlug)
      .then((found) => {
        if (!alive) return;
        const newest = found[0];
        const st = useStore.getState();
        // Reload when the newest output is a different file OR the same file was
        // rewritten in place — a regeneration always overwrites
        // "definition-locations.json", so its path never changes, only its mtime.
        if (
          newest &&
          (newest.path !== st.mapSourcePath ||
            newest.mtimeMs !== st.mapSourceMtime)
        ) {
          loadLocationsFile(newest.path)
            .then((locs) => {
              if (alive)
                useStore
                  .getState()
                  .setMapLocations(
                    locs,
                    newest.path,
                    useStore.getState().text,
                    newest.mtimeMs,
                  );
            })
            .catch((e) => logError("loadOutput", e));
        }
      })
      .catch((e) => logError("listOutputs", e));
    return () => {
      alive = false;
    };
  }, [workspaceRoot, currentSlug, result]);

  // Rebuild layers when data / live samples change.
  useEffect(() => {
    const overlay = overlayRef.current;
    if (!overlay) return;

    const pointsLayer = new ScatterplotLayer<MapLocation>({
      id: "locations",
      data: mapLocations,
      getPosition: (d) => [d.lng, d.lat] as [number, number],
      getFillColor: SOLID,
      getRadius: 200,
      radiusMinPixels: 6,
      radiusMaxPixels: 44,
      stroked: true,
      lineWidthMinPixels: 1.2,
      getLineColor: [20, 16, 11, 220],
      pickable: true,
      autoHighlight: true,
      highlightColor: [255, 255, 255, 110],
      onClick: (info: PickingInfo<MapLocation>) => {
        if (info.object) {
          setSelected(info.object);
          setPopupXY({ x: info.x, y: info.y });
        } else {
          setSelected(null);
        }
      },
    });

    const layers: Layer[] = [pointsLayer];

    if (runningId && liveSamples.length > 0) {
      layers.push(
        new ScatterplotLayer<LatLng>({
          id: "live-samples",
          data: liveSamples,
          getPosition: (d) => [d[1], d[0]] as [number, number],
          getFillColor: LIVE,
          getRadius: 120,
          radiusMinPixels: 3,
          radiusMaxPixels: 12,
          pickable: false,
        }),
      );
    }

    overlay.setProps({ layers });
  }, [mapLocations, liveSamples, runningId]);

  // Keep the open popup pinned to its location as the map pans/zooms.
  useEffect(() => {
    const map = mapRef.current;
    if (!map || !selected) return;
    const reproject = () => {
      const p = map.project([selected.lng, selected.lat]);
      setPopupXY({ x: p.x, y: p.y });
    };
    reproject();
    map.on("move", reproject);
    return () => {
      map.off("move", reproject);
    };
  }, [selected]);

  // Fit bounds when a new dataset loads.
  useEffect(() => {
    const map = mapRef.current;
    if (!map) return;
    const b = boundsOf(mapLocations);
    if (!b) return;
    try {
      map.fitBounds(b, { padding: 48, duration: 0, maxZoom: 12 });
    } catch {
      // camera may not be ready on first paint; harmless
    }
  }, [mapLocations]);

  const loadExternal = async () => {
    try {
      const path = await pickOpenFile("Locations", ["json"]);
      if (!path) return;
      const locs = await loadLocationsFile(path);
      // An external file isn't tied to this definition — never mark it stale.
      useStore.getState().setMapLocations(locs, path, null);
      setSelected(null);
      setFileError(null);
    } catch (e) {
      logError("loadExternal", e);
      setFileError("Couldn't read that file — is it a valid locations JSON?");
    }
  };

  const stale = mapText !== null && mapText !== text;
  const runActive = Boolean(runningId) && runningSlug === currentSlug;
  // A changed definition invalidates the last run's result too — hide the run
  // overlay and blur the map until the user regenerates.
  const runOverlayVisible =
    resultSlug === currentSlug &&
    (runActive || (Boolean(started || progress || result || runError) && !stale));
  const showStale = !runActive && stale && mapLocations.length > 0;
  const showEmpty =
    !runActive && !stale && !runOverlayVisible && mapLocations.length === 0;

  const canRun = sidecarAlive && !runningId && validation?.valid === true;
  const runHint = !sidecarAlive
    ? "Engine offline"
    : validation?.valid !== true
      ? "Fix validation errors first"
      : undefined;

  const cw = containerRef.current?.clientWidth ?? 0;
  const popupLeft = popupXY
    ? Math.min(Math.max(popupXY.x, 150), (cw || 4000) - 150)
    : 0;
  const popupBelow = popupXY ? popupXY.y < 250 : false;

  return (
    <div className={`map-view${showStale ? " blurred" : ""}`}>
      <div className="map-canvas" ref={containerRef} />

      <div className="map-actions">
        <button
          className="primary"
          disabled={!canRun}
          title={runHint ?? "Generate locations to your goal"}
          onClick={() => void useStore.getState().runGenerate()}
        >
          Generate
          <span className="btn-kbd">
            <kbd>{MOD}</kbd>
            <kbd>↵</kbd>
          </span>
        </button>
      </div>

      <div className="map-source">
        <button className="ghost-sm" onClick={() => void loadExternal()}>
          Load file…
        </button>
        {mapSourcePath && (
          <button
            className="linkish"
            onClick={() => void revealInExplorer(mapSourcePath)}
            title="Show in folder"
          >
            Show in folder
          </button>
        )}
        {mapLocations.length > 0 && (
          <span className="map-points">
            {mapLocations.length.toLocaleString()} points
          </span>
        )}
        {fileError && <span className="map-file-error">{fileError}</span>}
      </div>

      {selected && popupXY && !showStale && (
        <div
          className={`map-popup${popupBelow ? " below" : ""}`}
          style={{ left: popupLeft, top: popupXY.y }}
        >
          <LocationPopupBody loc={selected} onClose={() => setSelected(null)} />
        </div>
      )}

      {showStale && (
        <div className="map-scrim">
          <div className="map-prompt">
            <div className="map-prompt-title">Definition changed</div>
            <div className="hint">
              The map still shows the previous result. Generate again to see your
              changes here.
            </div>
            <MapPromptActions canRun={canRun} runHint={runHint} />
          </div>
        </div>
      )}

      {showEmpty && (
        <div className="map-scrim subtle">
          <div className="map-prompt">
            <div className="map-prompt-title">No locations yet</div>
            <div className="hint">
              Run a preview or full generation to see this map&apos;s locations
              here.
            </div>
            <MapPromptActions canRun={canRun} runHint={runHint} />
          </div>
        </div>
      )}

      {runOverlayVisible && <RunOverlay />}
    </div>
  );
}

function MapPromptActions({
  canRun,
  runHint,
}: {
  canRun: boolean;
  runHint?: string;
}) {
  return (
    <div className="map-prompt-actions">
      <button
        className="primary"
        disabled={!canRun}
        title={runHint}
        onClick={() => void useStore.getState().runGenerate()}
      >
        Generate
        <span className="btn-kbd">
          <kbd>{MOD}</kbd>
          <kbd>↵</kbd>
        </span>
      </button>
    </div>
  );
}

function LocationPopupBody({
  loc,
  onClose,
}: {
  loc: MapLocation;
  onClose: () => void;
}) {
  const panoUrl = loc.panoId
    ? `https://www.google.com/maps/@?api=1&map_action=pano&pano=${encodeURIComponent(loc.panoId)}`
    : null;
  const tags = loc.extra?.tags ?? [];
  return (
    <>
      <div className="map-popup-head">
        <span className="map-popup-title">
          {loc.countryCode ? `Location · ${loc.countryCode}` : "Location"}
        </span>
        <button className="popup-close" onClick={onClose} aria-label="Close">
          ×
        </button>
      </div>
      <div className="map-popup-body">
      <div className="kv">
        <span className="k">lat</span>
        <span className="v">{loc.lat.toFixed(6)}</span>
      </div>
      <div className="kv">
        <span className="k">lng</span>
        <span className="v">{loc.lng.toFixed(6)}</span>
      </div>
      {loc.heading !== undefined && (
        <div className="kv">
          <span className="k">heading</span>
          <span className="v">{loc.heading}</span>
        </div>
      )}
      {loc.countryCode && (
        <div className="kv">
          <span className="k">country</span>
          <span className="v">{loc.countryCode}</span>
        </div>
      )}
      {loc.subdivisionCode && (
        <div className="kv">
          <span className="k">subdivision</span>
          <span className="v">{loc.subdivisionCode}</span>
        </div>
      )}
      {loc.panoId && (
        <div className="kv">
          <span className="k">panoId</span>
          <span className="v">{loc.panoId}</span>
        </div>
      )}
      {tags.length > 0 && (
        <div className="kv">
          <span className="k">tags</span>
          <span className="v">{tags.join(", ")}</span>
        </div>
      )}
      {panoUrl && (
        <div style={{ marginTop: 8 }}>
          <a href={panoUrl} target="_blank" rel="noreferrer">
            Open in Google Maps
          </a>
        </div>
      )}
      </div>
    </>
  );
}
