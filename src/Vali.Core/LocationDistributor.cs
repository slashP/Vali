using System.Linq;
using System.Runtime.CompilerServices;
using Spectre.Console;
using Vali.Core.Hash;
using Weighted_Randomizer;

namespace Vali.Core;

public static class LocationDistributor
{
    public static readonly int[] Distances = [25, 50, 75, 100, 125, 150, 175, 200, 225, 250, 300, 350, 400, 450, 500, 600, 700, 800, 900, 1000, 1250, 1500, 1750, 2000, 2250, 2500, 2750, 3000, 3300, 3600, 3900, 4200, 4500, 5000, 6000, 7000, 8000, 9000, 10000, 12500, 15000, 20000, 25000, 30000, 35000, 40000, 45000, 50000, 55000, 60000, 65000, 70000, 75000];

    public static (IList<T> locations, int minDistance) WithMaxMinDistance<T, T2>(
        ICollection<T> locations,
        int goalCount,
        LocationProbability locationProbability,
        IReadOnlyCollection<ILatLng>? locationsAlreadyInMap = null,
        bool avoidShuffle = false,
        int? minMinDistance = null) where T : IDistributionLocation<T2> where T2 : IComparable<T2>
    {
        var distances = Distances.SkipWhile(x => x < minMinDistance).ToArray();
        if (distances.Length == 0 || goalCount <= 0)
        {
            return (Array.Empty<T>(), 0);
        }

        // Step 3: Cache geohash groupings across binary search iterations.
        var groupCache = new Dictionary<HashPrecision, IGrouping<ulong, T>[]>();

        int low = 0, high = distances.Length - 1;
        IList<T>? bestResult = null;
        int bestDistance = 0;
        IList<T>? fallbackResult = null;
        int fallbackDistance = 0;
        int fallbackCount = 0;

        while (low <= high)
        {
            var mid = low + (high - low) / 2;
            var minDistance = distances[mid];

            var precision = GetPrecision(minDistance);
            if (!groupCache.TryGetValue(precision, out var cachedGroups))
            {
                cachedGroups = locations.GroupBy(x => Hasher.Encode(x.Lat, x.Lng, precision)).ToArray();
                groupCache[precision] = cachedGroups;
            }

            var distributedLocations = DistributeEvenly<T, T2>(cachedGroups, minDistance, locationProbability, avoidShuffle: avoidShuffle, locationsAlreadyInMap: locationsAlreadyInMap, silent: true)
                .TakeRandom(goalCount)
                .ToArray();

            if (distributedLocations.Length >= goalCount)
            {
                bestResult = distributedLocations;
                bestDistance = minDistance;
                low = mid + 1;
            }
            else
            {
                if (distributedLocations.Length > fallbackCount)
                {
                    fallbackResult = distributedLocations;
                    fallbackDistance = minDistance;
                    fallbackCount = distributedLocations.Length;
                }

                high = mid - 1;
            }
        }

        if (bestResult != null)
        {
            return (bestResult, bestDistance);
        }

        return (fallbackResult ?? Array.Empty<T>(), fallbackDistance);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static HashPrecision GetPrecision(int minDistanceBetweenLocations) => minDistanceBetweenLocations switch
    {
        <= 200 => HashPrecision.Size_km_1x1,
        <= 1000 => HashPrecision.Size_km_5x5,
        <= 15000 => HashPrecision.Size_km_39x20,
        _ => HashPrecision.Size_km_156x156
    };

    public static IList<T> DistributeEvenly<T, T2>(
        ICollection<T> locations,
        int minDistanceBetweenLocations,
        LocationProbability locationProbability,
        bool avoidShuffle = false,
        bool ensureNoNeighborSpill = true,
        IReadOnlyCollection<ILatLng>? locationsAlreadyInMap = null,
        bool silent = false) where T : IDistributionLocation<T2>, ILatLng where T2 : IComparable<T2>
    {
        var precision = GetPrecision(minDistanceBetweenLocations);
        var groups = locations.GroupBy(x => Hasher.Encode(x.Lat, x.Lng, precision)).ToArray();
        return DistributeEvenly<T, T2>(groups, minDistanceBetweenLocations, locationProbability, avoidShuffle, ensureNoNeighborSpill, locationsAlreadyInMap, silent);
    }

    private static IList<T> DistributeEvenly<T, T2>(
        IGrouping<ulong, T>[] groups,
        int minDistanceBetweenLocations,
        LocationProbability locationProbability,
        bool avoidShuffle = false,
        bool ensureNoNeighborSpill = true,
        IReadOnlyCollection<ILatLng>? locationsAlreadyInMap = null,
        bool silent = false) where T : IDistributionLocation<T2>, ILatLng where T2 : IComparable<T2>
    {
        var list = new List<(T loc, ulong hash)>();
        var precision = GetPrecision(minDistanceBetweenLocations);
        if (silent)
        {
            Distribute(null);
        }
        else
        {
            AnsiConsole.Progress().Start(Distribute);
        }

        return list.Select(x => x.loc).ToArray();

        void Distribute(ProgressContext? ctx)
        {
            var task = ctx?.AddTask($"[green]Distributing locations[/]", maxValue: groups.Length);
            var placedByHash = ensureNoNeighborSpill ? new Dictionary<ulong, List<ILatLng>>() : null;
            foreach (var group in groups)
            {
                IReadOnlyCollection<ILatLng> alreadyInMap;
                if (placedByHash != null)
                {
                    var neighbors = Hasher.Neighbors(group.Key);
                    var neighborLocations = new List<ILatLng>();
                    foreach (var neighborHash in neighbors)
                    {
                        if (placedByHash.TryGetValue(neighborHash, out var bucket))
                            neighborLocations.AddRange(bucket);
                    }

                    if (locationsAlreadyInMap is { Count: > 0 })
                        neighborLocations.AddRange(locationsAlreadyInMap);
                    alreadyInMap = neighborLocations;
                }
                else
                {
                    alreadyInMap = locationsAlreadyInMap ?? Array.Empty<ILatLng>();
                }

                var distributionLocations = group.ToArray();
                var selection = GetSome<T, T2>(distributionLocations, 1_000_000, minDistanceBetweenLocations, locationProbability, locationsAlreadyInMap: alreadyInMap, avoidShuffle: avoidShuffle);
                foreach (var loc in selection)
                {
                    var hash = ensureNoNeighborSpill ? Hasher.Encode(loc.Lat, loc.Lng, precision) : 0UL;
                    list.Add((loc, hash));
                    if (placedByHash != null)
                    {
                        if (placedByHash.TryGetValue(hash, out var bucket))
                            bucket.Add(loc);
                        else
                            placedByHash[hash] = new List<ILatLng> { loc };
                    }
                }

                task?.Increment(1);
            }
        }
    }

    public static IList<T> GetSome<T, T2>(
        ICollection<T> locations,
        int goalCount,
        int minDistanceBetweenLocations,
        LocationProbability locationProbability,
        IReadOnlyCollection<ILatLng>? locationsAlreadyInMap = null,
        bool avoidShuffle = false) where T : IDistributionLocation<T2> where T2 : IComparable<T2>
    {
        if (goalCount <= 0 || locations.Count == 0)
        {
            return Array.Empty<T>();
        }

        var resultLocations = new List<T>(Math.Min(goalCount, locations.Count));
        var notProcessed = new Dictionary<T2, T>(locations.Count);
        foreach (var loc in locations)
        {
            notProcessed.Add(loc.LocationId, loc);
        }

        var distanceBetweenLocationsSquared = (double)minDistanceBetweenLocations * minDistanceBetweenLocations;
        var keysToRemove = new List<T2>();
        if (locationsAlreadyInMap != null && locationsAlreadyInMap.Count != 0)
        {
            foreach (var point in notProcessed)
            {
                foreach (var p in locationsAlreadyInMap)
                {
                    if (Extensions.PointsAreCloserThan(p.Lat, p.Lng, point.Value.Lat, point.Value.Lng, distanceBetweenLocationsSquared))
                    {
                        keysToRemove.Add(point.Key);
                        break;
                    }
                }
            }

            foreach (var key in keysToRemove)
            {
                notProcessed.Remove(key);
            }

            keysToRemove.Clear();
        }

        DynamicWeightedRandomizer<T2>? randomizer = null;
        if (typeof(T) == typeof(Location) && locationProbability.WeightOverrides.Length > 0)
        {
            var overrides = locationProbability.WeightOverrides;
            var compiledOverrides = new Func<Location, bool>[overrides.Length];
            for (var i = 0; i < overrides.Length; i++)
            {
                compiledOverrides[i] = LocationLakeFilterer.CompileBoolLocationExpression(overrides[i].Expression);
            }

            randomizer = [];
            foreach (var distributionLocation in notProcessed)
            {
                var loc = (distributionLocation.Value as Location)!;
                var weight = locationProbability.DefaultWeight;
                for (var i = 0; i < compiledOverrides.Length; i++)
                {
                    if (compiledOverrides[i](loc))
                    {
                        weight = overrides[i].Weight;
                        break;
                    }
                }

                randomizer[distributionLocation.Key] = weight;
            }
        }

        while (resultLocations.Count < goalCount && notProcessed.Count != 0)
        {
            T randomPoint;
            if (randomizer != null)
            {
                var key = randomizer.NextWithRemoval();
                randomPoint = notProcessed[key];
            }
            else
            {
                randomPoint = notProcessed.ElementAt(avoidShuffle ? 0 : Random.Shared.Next(0, notProcessed.Count)).Value;
            }

            notProcessed.Remove(randomPoint.LocationId);
            resultLocations.Add(randomPoint);

            foreach (var pointOnMap in notProcessed)
            {
                if (Extensions.PointsAreCloserThan(pointOnMap.Value.Lat, pointOnMap.Value.Lng, randomPoint.Lat, randomPoint.Lng, distanceBetweenLocationsSquared))
                {
                    keysToRemove.Add(pointOnMap.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                notProcessed.Remove(key);
                randomizer?.Remove(key);
            }

            keysToRemove.Clear();
        }

        return resultLocations;
    }
}
