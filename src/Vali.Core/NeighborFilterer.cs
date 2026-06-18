using Vali.Core.Expressions;
using Vali.Core.Hash;
using Loc = Vali.Core.Location;

namespace Vali.Core;

public class NeighborFilterer
{
    public static IEnumerable<Loc> FilterByNeighbors(
        IEnumerable<Loc> locations,
        Dictionary<ulong, List<Loc>> neighborLocationBuckets,
        NeighborFilter neighborFilter,
        MapDefinition mapDefinition)
    {
        var precision = mapDefinition.HashPrecisionFromNeighborFiltersRadius()!.Value;
        var filterExpression = string.IsNullOrEmpty(neighborFilter.Expression)
            ? (_, _) => true
            : LocationLakeFilterer.CompileParentBoolLocationExpression(neighborFilter.Expression);
        var directions = neighborFilter.CheckEachCardinalDirectionSeparately ? Enum.GetValues<CardinalDirection>().Cast<CardinalDirection?>().ToArray() : [null];
        // Inner PLINQ takes only a fair share of cores: the outer generation pass already runs many
        // work items concurrently, so a ProcessorCount-wide scan per item would starve the pool.
        var enumerable = locations.AsParallel().WithDegreeOfParallelism(GenerationConcurrency.InnerDegreeOfParallelism());
        var neighborFilterRadiusSquared = (double)neighborFilter.Radius * neighborFilter.Radius;

        var buckets = neighborLocationBuckets;
        if (IsPreFilterSafeBound(neighborFilter.Bound) && !string.IsNullOrEmpty(neighborFilter.Expression))
        {
            var pExpr = NeighborFilterDecomposer.NeighborOnlyExpression(neighborFilter.Expression);
            if (pExpr != null)
            {
                var p = LocationLakeFilterer.CompileBoolLocationExpression(pExpr);
                buckets = PreFilterBuckets(neighborLocationBuckets, p);
            }
        }

        return Scan(enumerable, buckets, neighborFilter, precision, filterExpression, directions, neighborFilterRadiusSquared);
    }

    private static bool IsPreFilterSafeBound(string? bound) =>
        bound is "some" or "gte" or "lte" or "none";

    // Keeps the same geohash keys (no re-hashing); drops locations that fail the neighbor-only
    // predicate. A dropped neighbor cannot satisfy the full expression for the safe bounds.
    private static Dictionary<ulong, List<Loc>> PreFilterBuckets(
        Dictionary<ulong, List<Loc>> buckets,
        Func<Loc, bool> predicate)
    {
        var result = new Dictionary<ulong, List<Loc>>(buckets.Count);
        foreach (var (key, list) in buckets)
        {
            List<Loc>? kept = null;
            foreach (var loc in list)
            {
                if (predicate(loc))
                {
                    (kept ??= new List<Loc>()).Add(loc);
                }
            }

            if (kept != null)
            {
                result[key] = kept;
            }
        }

        return result;
    }

    private static IEnumerable<Loc> Scan(
        ParallelQuery<Loc> enumerable,
        Dictionary<ulong, List<Loc>> buckets,
        NeighborFilter neighborFilter,
        HashPrecision precision,
        Func<Loc, Loc, bool> filterExpression,
        CardinalDirection?[] directions,
        double neighborFilterRadiusSquared)
    {
        return neighborFilter switch
        {
            { Bound: "gte", CheckEachCardinalDirectionSeparately: true } => enumerable
                .Where(l => directions.Any(d => LocationsFromDictionary(buckets, l, precision).Count(l2 =>
                    l.NodeId != l2.NodeId &&
                    IsInDirection(d, l, l2) &&
                    Extensions.PointsAreCloserThan(l.Lat, l.Lng, l2.Lat, l2.Lng, neighborFilterRadiusSquared) &&
                    filterExpression(l2, l)) >= neighborFilter.Limit)),
            { Limit: 1, Bound: "gte", CheckEachCardinalDirectionSeparately: false } or { Bound: "some" } => enumerable
                .Where(l => LocationsFromDictionary(buckets, l, precision).Any(l2 =>
                    l.NodeId != l2.NodeId &&
                    Extensions.PointsAreCloserThan(l.Lat, l.Lng, l2.Lat, l2.Lng, neighborFilterRadiusSquared) &&
                    filterExpression(l2, l))),
            { Bound: "gte", CheckEachCardinalDirectionSeparately: false } => enumerable
                .Where(l => LocationsFromDictionary(buckets, l, precision).Count(l2 =>
                    l.NodeId != l2.NodeId &&
                    Extensions.PointsAreCloserThan(l.Lat, l.Lng, l2.Lat, l2.Lng, neighborFilterRadiusSquared) &&
                    filterExpression(l2, l)) >= neighborFilter.Limit),
            { Limit: 0, Bound: "lte", CheckEachCardinalDirectionSeparately: true } or { CheckEachCardinalDirectionSeparately: true, Bound: "none" } => enumerable
                .Where(l => directions.Any(d => !LocationsFromDictionary(buckets, l, precision).Any(l2 =>
                    l.NodeId != l2.NodeId &&
                    IsInDirection(d, l, l2) &&
                    Extensions.PointsAreCloserThan(l.Lat, l.Lng, l2.Lat, l2.Lng, neighborFilterRadiusSquared) &&
                    filterExpression(l2, l)))),
            { Limit: 0, Bound: "lte", CheckEachCardinalDirectionSeparately: false } or { Bound: "none" } => enumerable
                .Where(l => !LocationsFromDictionary(buckets, l, precision).Any(l2 =>
                    l.NodeId != l2.NodeId &&
                    Extensions.PointsAreCloserThan(l.Lat, l.Lng, l2.Lat, l2.Lng, neighborFilterRadiusSquared) &&
                    filterExpression(l2, l))),
            { Limit: > 0, Bound: "lte", CheckEachCardinalDirectionSeparately: true } => enumerable
                .Where(l => directions.Any(d => LocationsFromDictionary(buckets, l, precision).Count(l2 =>
                    l.NodeId != l2.NodeId &&
                    IsInDirection(d, l, l2) &&
                    Extensions.PointsAreCloserThan(l.Lat, l.Lng, l2.Lat, l2.Lng, neighborFilterRadiusSquared) &&
                    filterExpression(l2, l)) <= neighborFilter.Limit)),
            { Bound: "lte", CheckEachCardinalDirectionSeparately: false } => enumerable
                .Where(l => LocationsFromDictionary(buckets, l, precision).Count(l2 =>
                    l.NodeId != l2.NodeId &&
                    Extensions.PointsAreCloserThan(l.Lat, l.Lng, l2.Lat, l2.Lng, neighborFilterRadiusSquared) &&
                    filterExpression(l2, l)) <= neighborFilter.Limit),
            { Bound: "all" } => enumerable
                .Where(l => LocationsFromDictionary(buckets, l, precision).Where(l2 =>
                        l.NodeId != l2.NodeId &&
                        Extensions.PointsAreCloserThan(l.Lat, l.Lng, l2.Lat, l2.Lng, neighborFilterRadiusSquared))
                    .AllAtLeastOne(l2 => filterExpression(l2, l))),
            { Bound: "percentage-gte" } => Percentage(enumerable, buckets, neighborFilter, precision, filterExpression, Bound.GreaterThan),
            { Bound: "percentage-lte" } => Percentage(enumerable, buckets, neighborFilter, precision, filterExpression, Bound.LessThan),
            _ => throw new InvalidOperationException($"Neighbor filter combination is not valid/implemented. Bound: {neighborFilter.Bound}. Check separately: {neighborFilter.CheckEachCardinalDirectionSeparately}. Limit: {neighborFilter.Limit}")
        };
    }

    private static ParallelQuery<Loc> Percentage(ParallelQuery<Loc> locations, Dictionary<ulong, List<Loc>> neighborLocationBuckets, NeighborFilter neighborFilter, HashPrecision precision, Func<Loc, Loc, bool> filterExpression, Bound bound)
    {
        var radiusSquared = (double)neighborFilter.Radius * neighborFilter.Radius;
        return locations
            .Where(l =>
            {
                var allCount = 0;
                var matchCount = 0;
                foreach (var l2 in LocationsFromDictionary(neighborLocationBuckets, l, precision))
                {
                    if (l.NodeId != l2.NodeId &&
                        Extensions.PointsAreCloserThan(l.Lat, l.Lng, l2.Lat, l2.Lng, radiusSquared))
                    {
                        allCount++;
                        if (filterExpression(l2, l))
                        {
                            matchCount++;
                        }
                    }
                }

                var percentage = allCount > 0 ? decimal.Divide(matchCount, allCount) * 100 : 0;
                return bound switch
                {
                    Bound.LessThan => percentage <= neighborFilter.Limit,
                    Bound.GreaterThan => percentage >= neighborFilter.Limit,
                    _ => throw new ArgumentOutOfRangeException(nameof(bound), bound, $"Bound {bound} not implemented.")
                };
            });
    }

    private static IEnumerable<Loc> LocationsFromDictionary(Dictionary<ulong, List<Loc>> neighborLocationBuckets, Loc l, HashPrecision precision) =>
        LocationLookupService.GetNearbyLocations(neighborLocationBuckets, Hasher.Encode(l.Lat, l.Lng, precision));

    private static bool IsInDirection(CardinalDirection? direction, Loc x1, Loc x2) =>
        direction switch
        {
            CardinalDirection.West => x1.Lng < x2.Lng,
            CardinalDirection.East => x1.Lng > x2.Lng,
            CardinalDirection.North => x1.Lat > x2.Lat,
            CardinalDirection.South => x1.Lat < x2.Lat,
            _ => false
        };

    private enum Bound
    {
        LessThan,
        GreaterThan
    }
}