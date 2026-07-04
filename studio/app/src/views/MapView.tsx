import { useEffect, useRef, useState } from "react";
import { Map as MapLibreMap, type IControl } from "maplibre-gl";
import { MapboxOverlay } from "@deck.gl/mapbox";
import { ScatterplotLayer } from "@deck.gl/layers";
import type { Color, Layer, PickingInfo } from "@deck.gl/core";
import { useStore } from "../store";
import { pickOpenFile } from "../lib/commands";
import { loadLocationsFile } from "../lib/locations";
import type { LatLng, MapLocation } from "../lib/types";

const MAP_STYLE = "https://tiles.openfreemap.org/styles/liberty";

type ColorMode = "solid" | "tag" | "year";

const SOLID: Color = [76, 139, 245];
const LIVE: Color = [55, 178, 107, 140];

// Categorical palette for the "by tag" mode.
const PALETTE: Color[] = [
  [76, 139, 245],
  [230, 126, 34],
  [46, 204, 113],
  [155, 89, 182],
  [241, 196, 15],
  [231, 76, 60],
  [26, 188, 156],
  [236, 64, 122],
  [149, 165, 166],
  [52, 152, 219],
];

function hashString(s: string): number {
  let h = 0;
  for (let i = 0; i < s.length; i++) h = (h * 31 + s.charCodeAt(i)) | 0;
  return Math.abs(h);
}

function firstTag(loc: MapLocation): string | null {
  const tags = loc.extra?.tags;
  return tags && tags.length > 0 ? tags[0] : null;
}

function yearFromTags(loc: MapLocation): number | null {
  for (const t of loc.extra?.tags ?? []) {
    const m = /\b(20\d{2}|19\d{2})\b/.exec(t);
    if (m) return Number(m[1]);
  }
  return null;
}

// Map a year to a blue→red ramp (older → newer). Assumes Street View era.
function yearColor(year: number): Color {
  const min = 2008;
  const max = 2025;
  const t = Math.max(0, Math.min(1, (year - min) / (max - min)));
  return [Math.round(60 + t * 195), Math.round(90 - t * 30), Math.round(220 - t * 180)];
}

function colorFor(loc: MapLocation, mode: ColorMode): Color {
  if (mode === "tag") {
    const tag = firstTag(loc);
    if (!tag) return SOLID;
    return PALETTE[hashString(tag) % PALETTE.length];
  }
  if (mode === "year") {
    const y = yearFromTags(loc);
    if (y === null) return [120, 120, 120];
    return yearColor(y);
  }
  return SOLID;
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
  const liveSamples = useStore((s) => s.liveSamples);
  const runningId = useStore((s) => s.runningId);

  const [colorMode, setColorMode] = useState<ColorMode>("solid");
  const [selected, setSelected] = useState<MapLocation | null>(null);

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
      attributionControl: { compact: true },
    });
    const overlay = new MapboxOverlay({ interleaved: false, layers: [] });
    map.addControl(overlay as unknown as IControl);
    mapRef.current = map;
    overlayRef.current = overlay;
    return () => {
      map.remove();
      mapRef.current = null;
      overlayRef.current = null;
    };
  }, []);

  // Rebuild layers when data / color mode / live samples change.
  useEffect(() => {
    const overlay = overlayRef.current;
    if (!overlay) return;

    const pointsLayer = new ScatterplotLayer<MapLocation>({
      id: "locations",
      data: mapLocations,
      getPosition: (d) => [d.lng, d.lat] as [number, number],
      getFillColor: (d) => colorFor(d, colorMode),
      getRadius: 30,
      radiusMinPixels: 2,
      radiusMaxPixels: 8,
      pickable: true,
      onClick: (info: PickingInfo<MapLocation>) => {
        setSelected(info.object ?? null);
      },
      updateTriggers: { getFillColor: colorMode },
    });

    const layers: Layer[] = [pointsLayer];

    // While a run is active, overlay the accumulating live samples.
    if (runningId && liveSamples.length > 0) {
      layers.push(
        new ScatterplotLayer<LatLng>({
          id: "live-samples",
          data: liveSamples,
          getPosition: (d) => [d[1], d[0]] as [number, number],
          getFillColor: LIVE,
          getRadius: 20,
          radiusMinPixels: 1.5,
          radiusMaxPixels: 5,
          pickable: false,
        }),
      );
    }

    overlay.setProps({ layers });
  }, [mapLocations, colorMode, liveSamples, runningId]);

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

  const loadFile = async () => {
    const path = await pickOpenFile("Locations", ["json"]);
    if (!path) return;
    const locs = await loadLocationsFile(path);
    useStore.getState().setMapLocations(locs, path);
    setSelected(null);
  };

  return (
    <div className="map-view">
      <div className="toolbar">
        <span className="title">Map</span>
        <button onClick={() => void loadFile()}>Load locations file</button>
        <div className="sep" />
        <label className="hint">Color by</label>
        <select
          value={colorMode}
          onChange={(e) => setColorMode(e.target.value as ColorMode)}
        >
          <option value="solid">Solid</option>
          <option value="tag">First tag</option>
          <option value="year">Year</option>
        </select>
        <div className="grow" />
        <span className="hint">
          {mapLocations.length.toLocaleString()} points
          {mapSourcePath ? ` · ${mapSourcePath}` : ""}
        </span>
      </div>

      <div className="map-container">
        <div className="map-canvas" ref={containerRef} />
        {selected && (
          <LocationPopup loc={selected} onClose={() => setSelected(null)} />
        )}
      </div>
    </div>
  );
}

function LocationPopup({
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
    <div className="map-popup">
      <button className="close" onClick={onClose}>
        ×
      </button>
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
  );
}
