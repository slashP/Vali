﻿using Spectre.Console;
using Vali.Core.Hash;

namespace Vali.Core;

public static class LocationDistributor
{
    public enum LocationStatus
    {
        NotProcessed,
        Taken,
        Removed
    }

    public static readonly int[] Distances = [25, 50, 75, 100, 125, 150, 175, 200, 225, 250, 300, 350, 400, 450, 500, 600, 700, 800, 900, 1000, 1250, 1500, 1750, 2000, 2250, 2500, 2750, 3000, 3300, 3600, 3900, 4200, 4500, 5000, 6000, 7000, 8000, 9000, 10000, 12500, 15000, 20000, 25000, 30000, 35000, 40000, 45000, 50000, 55000, 60000, 65000, 70000, 75000];

    public static (IList<T> locations, int minDistance) WithMaxMinDistance<T, T2>(
        ICollection<T> locations,
        int goalCount,
        IReadOnlyCollection<T>? locationsAlreadyInMap = null,
        bool avoidShuffle = false,
        int? minMinDistance = null) where T : IDistributionLocation<T2>
    {
        if (goalCount == 0 || locations.Count == 0)
        {
            return (Array.Empty<T>(), 0);
        }

        var distances = Distances.OrderBy(x => x).SkipWhile(x => x < minMinDistance).ToArray();
        var initialMinDistance = distances.Skip(distances.Length / 2).First();
        var minDistanceIndex = Array.IndexOf(distances, initialMinDistance);
        var visited = distances.ToDictionary(x => x, _ => (IList<T>?)null);
        do
        {
            var minDistance = distances[minDistanceIndex];
            var distributedLocations = GetSome<T, T2>(locations, goalCount, minDistance, locationsAlreadyInMap, avoidShuffle);
            visited[minDistance] = distributedLocations;
            if (visited.First().Value != null && visited.First().Value?.Count < goalCount)
            {
                break;
            }

            minDistanceIndex = NextMinDistanceIndex(visited, goalCount, distances);
            var visitedOrdered = visited.OrderByDescending(x => x.Key).ToArray();
            var prev = visitedOrdered.First();
            if (prev.Value?.Count == goalCount)
            {
                return (prev.Value, prev.Key);
            }

            foreach (var pair in visitedOrdered.Skip(1))
            {
                if (prev.Value != null && prev.Value.Count < goalCount && pair.Value?.Count == goalCount)
                {
                    return (pair.Value, pair.Key);
                }

                prev = pair;
            }

            continue;

            static int NextMinDistanceIndex(Dictionary<int, IList<T>?> visitedLocations, int goalCount, int[] distances)
            {
                var visitedWithSatisfiedGoalCount = visitedLocations.Where(v => v.Value?.Count == goalCount).ToArray();
                var visitedWithoutSatisfiedGoalCount = visitedLocations.Where(v => v.Value?.Count != null && v.Value.Count < goalCount).ToArray();
                var lowestPossibleIndex = visitedWithSatisfiedGoalCount.Any()
                    ? Array.IndexOf(distances, visitedWithSatisfiedGoalCount.Max(x => x.Key))
                    : 0;
                var highestPossibleIndex = visitedWithoutSatisfiedGoalCount.Any()
                    ? Array.IndexOf(distances, visitedWithoutSatisfiedGoalCount.Min(x => x.Key))
                    : distances.Length;

                var distanceRange = distances[lowestPossibleIndex..highestPossibleIndex];
                var remainingDistances = distanceRange.Except(visitedLocations.Where(x => x.Value != null).Select(x => Array.IndexOf(distances, x.Key))).ToArray();
                return Array.IndexOf(distances, remainingDistances.Skip(remainingDistances.Length / 2).First());
            }
        } while (visited.Any(v => v.Value == null));

        var max = visited.MaxBy(x => x.Value?.Count);
        return (max.Value ?? [], max.Key);
    }

    public static IList<T> DistributeEvenly<T, T2>(
        ICollection<T> locations,
        int minDistanceBetweenLocations,
        bool avoidShuffle = false,
        bool ensureNoNeighborSpill = true,
        bool silent = false) where T : IDistributionLocation<T2> where T2 : notnull
    {
        var list = new List<(T loc, string hash)>();
        var precision = HashPrecision.Size_km_39x20;
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
            var groups = locations.GroupBy(x => Hasher.Encode(x.Lat, x.Lng, precision));
            var task = ctx?.AddTask($"[green]Distributing locations[/]", maxValue: groups.Count());
            foreach (var group in groups)
            {
                var neighbours = Hasher.Neighbors(group.Key).Select(x => x.Value).ToHashSet();
                var alreadyInMap = ensureNoNeighborSpill ? list.Where(x => neighbours.Contains(x.hash)).Select(x => x.loc).ToArray() : [];
                var selection = TakeSubSelection<T, T2>(group.ToArray(), 1_000_000, minDistanceBetweenLocations, locationsAlreadyInMap: alreadyInMap, avoidShuffle: avoidShuffle);
                list.AddRange(selection.Select(x => (x, ensureNoNeighborSpill ? Hasher.Encode(x.Lat, x.Lng, precision) : "")));
                task?.Increment(1);
            }
        }
    }

    public static IList<T> GetSome<T, T2>(
        ICollection<T> locations,
        int goalCount,
        int minDistanceBetweenLocations,
        IReadOnlyCollection<T>? locationsAlreadyInMap = null,
        bool avoidShuffle = false) where T : IDistributionLocation<T2> where T2 : notnull
    {
        if (goalCount == 0 || locations.Count == 0)
        {
            return Array.Empty<T>();
        }

        var resultLocations = new List<T>(goalCount);
        var locationsLookup = locations.ToDictionary(x => x.LocationId);
        var notProcessed = locations.Select(x => new MapLoc<T2>
            {
                Lat = x.Lat,
                Lng = x.Lng,
                LocationId = x.LocationId
            })
            .ToDictionary(p => p.LocationId);

        if (locationsAlreadyInMap != null && locationsAlreadyInMap.Any())
        {
            foreach (var point in notProcessed)
            {
                if (locationsAlreadyInMap.Any(p =>
                        Extensions.PointsAreCloserThan(
                            p.Lat,
                            p.Lng,
                            point.Value.Lat,
                            point.Value.Lng,
                            minDistanceBetweenLocations)))
                {
                    notProcessed.Remove(point.Key);
                }
            }
        }

        while (resultLocations.Count < goalCount && notProcessed.Any())
        {
            var randomPoint = notProcessed.ElementAt(avoidShuffle ? 0 : Random.Shared.Next(0, notProcessed.Count)).Value;
            notProcessed.Remove(randomPoint.LocationId);
            resultLocations.Add(locationsLookup[randomPoint.LocationId]);

            foreach (var pointOnMap in notProcessed.Values.Where(p =>
                         Extensions.PointsAreCloserThan(
                             p.Lat,
                             p.Lng,
                             randomPoint.Lat,
                             randomPoint.Lng,
                             minDistanceBetweenLocations)))
            {
                notProcessed.Remove(pointOnMap.LocationId);
            }
        }

        return resultLocations;
    }

    public static IList<T> TakeSubSelection<T, T2>(
        ICollection<T> locations,
        int goalCount,
        int minDistanceBetweenLocations,
        IReadOnlyCollection<T>? locationsAlreadyInMap = null,
        bool avoidShuffle = false) where T : IDistributionLocation<T2> where T2 : notnull
    {
        if (goalCount == 0 || locations.Count == 0)
        {
            return Array.Empty<T>();
        }

        var resultLocations = new List<T>(goalCount);
        var locationsLookup = locations.ToDictionary(x => x.LocationId);
        var notProcessed = locations.Select(x => new MapLoc<T2>
            {
                Lat = x.Lat,
                Lng = x.Lng,
                LocationId = x.LocationId
            })
            .ToDictionary(p => p.LocationId);
        if (locationsAlreadyInMap != null && locationsAlreadyInMap.Any())
        {
            foreach (var point in notProcessed)
            {
                if (locationsAlreadyInMap.Any(p =>
                        Extensions.PointsAreCloserThan(
                            p.Lat,
                            p.Lng,
                            point.Value.Lat,
                            point.Value.Lng,
                            minDistanceBetweenLocations)))
                {
                    notProcessed.Remove(point.Key);
                }
            }
        }

        while (resultLocations.Count < goalCount &&
               notProcessed.Any())
        {
            var randomPoint = notProcessed.ElementAt(avoidShuffle ? 0 : Random.Shared.Next(0, notProcessed.Count)).Value;
            notProcessed.Remove(randomPoint.LocationId);
            resultLocations.Add(locationsLookup[randomPoint.LocationId]);

            foreach (var pointOnMap in notProcessed.Values.Where(p =>
                         Extensions.PointsAreCloserThan(
                             p.Lat,
                             p.Lng,
                             randomPoint.Lat,
                             randomPoint.Lng,
                             minDistanceBetweenLocations)))
            {
                notProcessed.Remove(pointOnMap.LocationId);
            }
        }

        return resultLocations;
    }

    public struct MapLoc<T>
    {
        public double Lat;
        public double Lng;
        public T LocationId;
    }
}