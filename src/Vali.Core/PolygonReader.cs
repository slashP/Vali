using System.Collections.Concurrent;
using System.Text.Json;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace Vali.Core;

public class PolygonReader
{
    private static readonly ConcurrentDictionary<string, Polygon[]> Polygons = new();
    private static readonly object LockObject = new object();

    public static Polygon[] DeserializePolygonsFromFile(string path)
    {
        if (Polygons.TryGetValue(path, out var cachedPolygons))
        {
            return cachedPolygons;
        }

        lock (LockObject)
        {
            if (Polygons.TryGetValue(path, out cachedPolygons))
            {
                return cachedPolygons;
            }

            try
            {
                Geometry[] geometries = GeoJsonSerialization.DeserializeFromFile(path).Result;
                return geometries
                    .Where(g => g.OgcGeometryType == OgcGeometryType.Polygon)
                    .Select(g => new Polygon(
                        g.Coordinates
                        .Where(c => !Double.IsNaN(c.X) && !Double.IsNaN(c.Y))
                        .Select(c => new Polygon.PolyPoint(c.X, c.Y))
                        .ToArray()
                    )).ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Invalid GeoJson", ex);
            }
        }
    }


    private record GeoJsonGeometry
    {
        public string? type { get; set; }
        public double[][]? coordinates { get; set; }
    }

    private record GeoJsonFeature
    {
        public string? type { get; set; }
        public GeoJsonGeometry? geometry { get; set; }
    }

    private record GeoJsonFeatureCollection
    {
        public string? type { get; set; }
        public GeoJsonFeature[]? features { get; set; }
    }
}
