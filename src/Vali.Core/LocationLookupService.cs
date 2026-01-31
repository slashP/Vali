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

    public static IEnumerable<T> GetNearbyLocations<T>(Dictionary<ulong, List<T>> buckets, ulong hash)
    {
        if (buckets.TryGetValue(hash, out var center))
        {
            foreach (var loc in center)
                yield return loc;
        }

        foreach (var neighborHash in Hasher.Neighbors(hash))
        {
            if (buckets.TryGetValue(neighborHash, out var neighborList))
            {
                foreach (var loc in neighborList)
                    yield return loc;
            }
        }
    }
}
