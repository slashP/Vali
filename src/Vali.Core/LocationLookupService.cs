using Vali.Core.Hash;

namespace Vali.Core;

public class LocationLookupService
{
    public static Dictionary<ulong, List<T>> Bucketize<T>(IEnumerable<T> allLocations, HashPrecision? precision) where T : ILatLng
    {
        if (!precision.HasValue)
        {
            return [];
        }

        var allLocationsDictionary = new Dictionary<ulong, List<T>>();
        foreach (var loc in allLocations)
        {
            var hash = Hasher.Encode(loc.Lat, loc.Lng, precision.Value);
            if (allLocationsDictionary.TryGetValue(hash, out var list))
            {
                list.Add(loc);
            }
            else
            {
                allLocationsDictionary[hash] = [loc];
            }
        }

        return allLocationsDictionary;
    }

    public static IEnumerable<T> GetNearbyLocations<T>(Dictionary<ulong, List<T>> buckets, ulong hash) =>
        NearbyLocations(buckets, hash, Hasher.Neighbors(hash));

    /// <summary>Every location in the buckets that can be closer than <paramref name="radiusMeters"/> to a point in the cell <paramref name="hash"/>.</summary>
    public static IEnumerable<T> GetNearbyLocations<T>(Dictionary<ulong, List<T>> buckets, ulong hash, double radiusMeters) =>
        NearbyLocations(buckets, hash, Hasher.NeighborsWithin(hash, radiusMeters));

    private static IEnumerable<T> NearbyLocations<T>(Dictionary<ulong, List<T>> buckets, ulong hash, ulong[] neighborHashes)
    {
        if (buckets.TryGetValue(hash, out var center))
        {
            foreach (var loc in center)
                yield return loc;
        }

        foreach (var neighborHash in neighborHashes)
        {
            if (buckets.TryGetValue(neighborHash, out var neighborList))
            {
                foreach (var loc in neighborList)
                    yield return loc;
            }
        }
    }
}
