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

        // --- Fixed candidate order (built once) makes count(d) monotone across probes. ---
        var ordered = BuildOrderedCandidates<T, T2>(locations, locationProbability, avoidShuffle);

        var evalCache = new Dictionary<int, (IList<T> result, int count)>();

        (IList<T> result, int count) Eval(int idx)
        {
            if (evalCache.TryGetValue(idx, out var cached)) return cached;
            var placed = PlaceSpaced<T, T2>(ordered, goalCount, distances[idx], locationsAlreadyInMap);
            var r = (placed, placed.Count);
            evalCache[idx] = r;
            return r;
        }

        // --- Model-based seed: max packing in the bounding box ~ Area / d^2, solve for d at goalCount. ---
        var seed = SeedIndex<T, T2>(locations, distances, goalCount);

        // --- Seeded bisection for the largest index whose count reaches goalCount. ---
        int low = 0, high = distances.Length - 1;
        IList<T>? bestResult = null;
        int bestDistance = 0;
        int probe = Math.Clamp(seed, low, high);

        while (low <= high)
        {
            var (result, count) = Eval(probe);
            if (count >= goalCount)
            {
                bestResult = result;
                bestDistance = distances[probe];
                low = probe + 1;
            }
            else
            {
                high = probe - 1;
            }

            if (low > high) break;
            probe = low + (high - low) / 2;
        }

        if (bestResult != null)
        {
            return (bestResult, bestDistance);
        }

        // Fallback: smallest distance yields the most locations (monotone).
        var (fallback, _) = Eval(0);
        return (fallback, fallback.Count == 0 ? 0 : distances[0]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static HashPrecision GetPrecision(int minDistanceBetweenLocations) => minDistanceBetweenLocations switch
    {
        <= 200 => HashPrecision.Size_km_1x1,
        <= 1000 => HashPrecision.Size_km_5x5,
        <= 15000 => HashPrecision.Size_km_39x20,
        _ => HashPrecision.Size_km_156x156
    };

    /// <summary>Index of the value in <paramref name="sortedAscending"/> closest to <paramref name="target"/>.</summary>
    private static int NearestIndex(int[] sortedAscending, double target)
    {
        var best = 0;
        var bestDiff = double.MaxValue;
        for (var i = 0; i < sortedAscending.Length; i++)
        {
            var diff = Math.Abs(sortedAscending[i] - target);
            if (diff < bestDiff) { bestDiff = diff; best = i; }
        }
        return best;
    }

    /// <summary>
    /// Cheap first-probe guess: in a bounding box of area A, the max number of points spaced
    /// d apart is ~ A / d^2, so the d that yields goalCount is ~ sqrt(A / goalCount). Map that
    /// distance to the nearest candidate index. The bracketed search corrects any error.
    /// </summary>
    private static int SeedIndex<T, T2>(ICollection<T> locations, int[] distances, int goalCount)
        where T : IDistributionLocation<T2> where T2 : IComparable<T2>
    {
        double minLat = double.MaxValue, maxLat = double.MinValue, minLng = double.MaxValue, maxLng = double.MinValue;
        foreach (var loc in locations)
        {
            if (loc.Lat < minLat) minLat = loc.Lat;
            if (loc.Lat > maxLat) maxLat = loc.Lat;
            if (loc.Lng < minLng) minLng = loc.Lng;
            if (loc.Lng > maxLng) maxLng = loc.Lng;
        }

        const double metresPerDegree = 6371137.0 * Math.PI / 180.0;
        var midLatRad = (minLat + maxLat) * 0.5 * Math.PI / 180.0;
        var heightM = (maxLat - minLat) * metresPerDegree;
        var widthM = (maxLng - minLng) * metresPerDegree * Math.Cos(midLatRad);
        var area = Math.Max(heightM, 1.0) * Math.Max(widthM, 1.0);

        var guessDistance = Math.Sqrt(area / Math.Max(goalCount, 1));
        return NearestIndex(distances, guessDistance);
    }

    /// <summary>
    /// Produces the fixed candidate order reused across all probes: input order when <paramref name="avoidShuffle"/>;
    /// a weighted-random permutation (Efraimidis–Spirakis) when T is Location with weight overrides; otherwise a
    /// uniform shuffle. A single fixed order is what makes PlaceSpaced's count monotone across distances.
    /// </summary>
    private static T[] BuildOrderedCandidates<T, T2>(
        ICollection<T> locations, LocationProbability locationProbability, bool avoidShuffle)
        where T : IDistributionLocation<T2> where T2 : IComparable<T2>
    {
        var arr = locations as T[] ?? locations.ToArray();

        if (avoidShuffle)
        {
            return arr;
        }

        if (typeof(T) == typeof(Location) && locationProbability.WeightOverrides.Length > 0)
        {
            var overrides = locationProbability.WeightOverrides;
            var compiled = new Func<Location, bool>[overrides.Length];
            for (var i = 0; i < overrides.Length; i++)
            {
                compiled[i] = LocationLakeFilterer.CompileBoolLocationExpression(overrides[i].Expression);
            }

            var keyed = new (double key, T loc)[arr.Length];
            for (var i = 0; i < arr.Length; i++)
            {
                var loc = (arr[i] as Location)!;
                var weight = locationProbability.DefaultWeight;
                for (var j = 0; j < compiled.Length; j++)
                {
                    if (compiled[j](loc)) { weight = overrides[j].Weight; break; }
                }

                var w = weight <= 0 ? double.Epsilon : weight;
                // key = -ln(U)/w ; ascending key == sampling without replacement proportional to weight.
                keyed[i] = (-Math.Log(Random.Shared.NextDouble() + double.Epsilon) / w, arr[i]);
            }

            Array.Sort(keyed, static (a, b) => a.key.CompareTo(b.key));
            var ordered = new T[arr.Length];
            for (var i = 0; i < arr.Length; i++) ordered[i] = keyed[i].loc;
            return ordered;
        }

        var shuffled = (T[])arr.Clone();
        for (var n = shuffled.Length - 1; n > 0; n--)
        {
            var k = Random.Shared.Next(n + 1);
            (shuffled[n], shuffled[k]) = (shuffled[k], shuffled[n]);
        }
        return shuffled;
    }

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

    /// <summary>
    /// Greedily selects up to <paramref name="goalCount"/> of <paramref name="orderedCandidates"/> so every
    /// selected point is at least <paramref name="minDistanceBetweenLocations"/> metres from every other selected
    /// point (and from <paramref name="locationsAlreadyInMap"/>), iterating candidates in the given fixed order.
    ///
    /// A uniform background grid prunes neighbour checks to O(1) amortised, so this is O(n) per call regardless of
    /// distance, with an early exit once the goal is reached. Iterating a fixed order makes the result count a
    /// monotone non-increasing function of distance. Correctness of min-distance is always confirmed by the exact
    /// <see cref="Extensions.PointsAreCloserThan(double,double,double,double,double)"/> metric.
    /// </summary>
    public static IList<T> PlaceSpaced<T, T2>(
        IReadOnlyList<T> orderedCandidates,
        int goalCount,
        int minDistanceBetweenLocations,
        IReadOnlyCollection<ILatLng>? locationsAlreadyInMap = null)
        where T : IDistributionLocation<T2> where T2 : IComparable<T2>
    {
        if (goalCount <= 0 || orderedCandidates.Count == 0)
        {
            return Array.Empty<T>();
        }

        var d = minDistanceBetweenLocations;
        var dSquared = (double)d * d;

        // Reference cosine = smallest cos(lat) (most poleward) over candidates AND already-placed points, so the
        // projected longitude distance never under-estimates the metric distance. That guarantees any blocker within
        // d lands within a 3x3 cell neighbourhood, so the grid never misses one (no false accepts).
        var maxAbsLat = 0.0;
        for (var i = 0; i < orderedCandidates.Count; i++)
        {
            var a = Math.Abs(orderedCandidates[i].Lat);
            if (a > maxAbsLat) maxAbsLat = a;
        }
        if (locationsAlreadyInMap != null)
        {
            foreach (var p in locationsAlreadyInMap)
            {
                var a = Math.Abs(p.Lat);
                if (a > maxAbsLat) maxAbsLat = a;
            }
        }

        const double metresPerDegree = 6371137.0 * Math.PI / 180.0;
        var cosRef = Math.Cos(Math.Min(maxAbsLat, 89.0) * Math.PI / 180.0);
        if (cosRef < 1e-6) cosRef = 1e-6;
        var cellLat = d / metresPerDegree;            // degrees
        var cellLng = d / (metresPerDegree * cosRef); // degrees (>= cellLat)

        var grid = new Dictionary<long, List<(double lat, double lng)>>();

        if (locationsAlreadyInMap is { Count: > 0 })
        {
            foreach (var p in locationsAlreadyInMap)
            {
                var key = CellKey(p.Lng / cellLng, p.Lat / cellLat);
                if (!grid.TryGetValue(key, out var bucket)) { bucket = new List<(double, double)>(1); grid[key] = bucket; }
                bucket.Add((p.Lat, p.Lng));
            }
        }

        var result = new List<T>(Math.Min(goalCount, orderedCandidates.Count));
        for (var i = 0; i < orderedCandidates.Count; i++)
        {
            if (result.Count >= goalCount) break;

            var loc = orderedCandidates[i];
            var lat = loc.Lat;
            var lng = loc.Lng;
            var cx = (int)Math.Floor(lng / cellLng);
            var cy = (int)Math.Floor(lat / cellLat);

            var ok = true;
            for (var dx = -1; dx <= 1 && ok; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (grid.TryGetValue(Pack(cx + dx, cy + dy), out var bucket))
                    {
                        var blocked = false;
                        foreach (var (blat, blng) in bucket)
                        {
                            if (Extensions.PointsAreCloserThan(blat, blng, lat, lng, dSquared))
                            {
                                blocked = true;
                                break;
                            }
                        }
                        if (blocked) { ok = false; break; }
                    }
                }
            }

            if (ok)
            {
                result.Add(loc);
                var key = Pack(cx, cy);
                if (!grid.TryGetValue(key, out var b)) { b = new List<(double, double)>(1); grid[key] = b; }
                b.Add((lat, lng));
            }
        }

        return result;

        static long Pack(int cx, int cy) => ((long)cx << 32) | (uint)cy;
        static long CellKey(double fx, double fy) => Pack((int)Math.Floor(fx), (int)Math.Floor(fy));
    }
}
