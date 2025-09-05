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

    public static (GeometryFilter[] filters, Geometry[][] geometries) ProcessGeometryFilters(this GeometryFilter[] filters)
    {
        var filterFiles = filters
            .Where(f => File.Exists(f.GeometriesPath))
            .Select(f => (f, g: GeometryReader.DeserializeGeometriesFromFile(f.GeometriesPath)))
            .Where(r => r.g.Length > 0);
        return (filterFiles.Select(r => r.f).ToArray(), filterFiles.Select(r => r.g).ToArray());
    }
}
