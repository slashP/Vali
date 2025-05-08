using System.Collections.Concurrent;
using Geohash;
using Vali.Core.Hash;

namespace Vali.Core;

public class Hasher
{
    private static readonly Geohasher GeoHasher = new();

    public static string Encode(double latitude, double longitude, HashPrecision precision) =>
        GeoHasher.Encode(latitude, longitude, (int)precision);

    public static string ParentOf(double latitude, double longitude, HashPrecision precision) =>
        GeoHasher.GetParent(GeoHasher.Encode(latitude, longitude, (int)precision));

    private static readonly ConcurrentDictionary<string, Dictionary<Direction, string>> NeighborCache = new();
    public static Dictionary<Direction, string> Neighbors(string hash)
    {
        if (NeighborCache.TryGetValue(hash, out var n))
        {
            return n;
        }

        var neighbors = GeoHasher.GetNeighbors(hash);
        NeighborCache.TryAdd(hash, neighbors);
        return neighbors;
    }

    public static BoundingBox GetBoundingBox(string geohash) => GeoHasher.GetBoundingBox(geohash);
}