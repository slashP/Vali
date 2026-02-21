using System.Collections.Concurrent;
using NetTopologySuite.Geometries;

namespace Vali.Core;

public static class GeometryReader
{
    private static readonly ConcurrentDictionary<string, Geometry[]> Geometries = new();
    private static readonly object LockObject = new object();

    public static Geometry[] DeserializeGeometriesFromFile(string path)
    {
        if (Geometries.TryGetValue(path, out var cachedGeometries))
        {
            return cachedGeometries;
        }

        lock (LockObject)
        {
            if (Geometries.TryGetValue(path, out cachedGeometries))
            {
                return cachedGeometries;
            }

            try
            {
                return GeoJsonSerialization.DeserializeFromFile(path);
            }
            catch (Exception)
            {
                ConsoleLogger.Error($"Invalid GeoJSON in file {path}, ignoring file. Try checking using https://geojsonlint.com/.");
                return [];
            }
        }
    }

    public static (GeometryFilter filter, Geometry[] geometries)[] GetApplicableGeometryFilters(this GeometryFilter[] filters) =>
        filters
            .Select(f => f.PreloadedGeometries is { Length: > 0 }
                ? (f, g: f.PreloadedGeometries)
                : File.Exists(f.FilePath)
                    ? (f, g: DeserializeGeometriesFromFile(f.FilePath))
                    : (f, g: []))
            .Where(r => r.g.Length > 0).Select(r => (r.f, r.g))
            .ToArray();
}
