using Geohash;
using Vali.Core.Hash;

namespace Vali.Core;

public class Hasher
{
    private static readonly Geohasher GeoHasher = new();

    public static string Encode(double latitude, double longitude, HashPrecision precision) =>
        GeoHasher.Encode(latitude, longitude, (int)precision);

    public static Dictionary<Direction, string> Neighbors(string hash) => GeoHasher.GetNeighbors(hash);

    public static BoundingBox GetBoundingBox(string geohash) => GeoHasher.GetBoundingBox(geohash);
}