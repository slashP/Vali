using Vali.Core.Hash;

namespace Vali.Core;

public class LocationLookupService
{
    public static Dictionary<string, List<T>> Bucketize<T>(IEnumerable<T> allLocations, HashPrecision? precision) where T : ILatLng
    {
        if (!precision.HasValue)
        {
            return [];
        }

        var allLocationsDictionary = new Dictionary<string, List<T>>();
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

            foreach (var (_, neighborHash) in Hasher.Neighbors(hash))
            {
                if (allLocationsDictionary.TryGetValue(neighborHash, out var neighborList))
                {
                    neighborList.Add(loc);
                }
                else
                {
                    allLocationsDictionary[neighborHash] = [loc];
                }
            }
        }

        return allLocationsDictionary;
    }
}