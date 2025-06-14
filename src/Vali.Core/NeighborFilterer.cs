using Vali.Core.Hash;
using Loc = Vali.Core.Location;

namespace Vali.Core;

public class NeighborFilterer
{
    public static IEnumerable<Loc> FilterByNeighbors(
        IEnumerable<Loc> locations,
        Dictionary<string, List<Loc>> neighborLocationBuckets,
        NeighborFilter neighborFilter,
        MapDefinition mapDefinition)
    {
        var precision = mapDefinition.HashPrecisionFromNeighborFiltersRadius()!.Value;
        var filterExpression = string.IsNullOrEmpty(neighborFilter.Expression)
            ? (_, _) => true
            : LocationLakeFilterer.CompileParentBoolLocationExpression(neighborFilter.Expression);
        var directions = neighborFilter.CheckEachCardinalDirectionSeparately ? Enum.GetValues<CardinalDirection>().Cast<CardinalDirection?>().ToArray() : [null];
        return neighborFilter switch
        {
            { Bound: "gte", CheckEachCardinalDirectionSeparately: true } => locations
                .Where(l => directions.Any(d => LocationsFromDictionary(neighborLocationBuckets, l, precision).Count(l2 =>
                    l.NodeId != l2.NodeId &&
                    IsInDirection(d, l, l2) &&
                    Extensions.ApproximateDistance(l.Lat, l.Lng, l2.Lat, l2.Lng) <= neighborFilter.Radius &&
                    filterExpression(l2, l)) >= neighborFilter.Limit)),
            { Limit: 1, Bound: "gte", CheckEachCardinalDirectionSeparately: false } or { Bound: "some" } => locations
                .Where(l => LocationsFromDictionary(neighborLocationBuckets, l, precision).Any(l2 =>
                    l.NodeId != l2.NodeId &&
                    Extensions.ApproximateDistance(l.Lat, l.Lng, l2.Lat, l2.Lng) <= neighborFilter.Radius &&
                    filterExpression(l2, l))),
            { Bound: "gte", CheckEachCardinalDirectionSeparately: false } => locations
                .Where(l => LocationsFromDictionary(neighborLocationBuckets, l, precision).Count(l2 =>
                    l.NodeId != l2.NodeId &&
                    Extensions.ApproximateDistance(l.Lat, l.Lng, l2.Lat, l2.Lng) <= neighborFilter.Radius &&
                    filterExpression(l2, l)) >= neighborFilter.Limit),
            { Limit: 0, Bound: "lte", CheckEachCardinalDirectionSeparately: true } or { CheckEachCardinalDirectionSeparately: true, Bound: "none" } => locations
                .Where(l => directions.Any(d => !LocationsFromDictionary(neighborLocationBuckets, l, precision).Any(l2 =>
                    l.NodeId != l2.NodeId &&
                    IsInDirection(d, l, l2) &&
                    Extensions.ApproximateDistance(l.Lat, l.Lng, l2.Lat, l2.Lng) <= neighborFilter.Radius &&
                    filterExpression(l2, l)))),
            { Limit: 0, Bound: "lte", CheckEachCardinalDirectionSeparately: false } or { Bound: "none" } => locations
                .Where(l => !LocationsFromDictionary(neighborLocationBuckets, l, precision).Any(l2 =>
                    l.NodeId != l2.NodeId &&
                    Extensions.ApproximateDistance(l.Lat, l.Lng, l2.Lat, l2.Lng) <= neighborFilter.Radius &&
                    filterExpression(l2, l))),
            { Limit: > 0, Bound: "lte", CheckEachCardinalDirectionSeparately: true } => locations
                .Where(l => directions.Any(d => neighborLocationBuckets[Hasher.Encode(l.Lat, l.Lng, HashPrecision.Size_km_39x20)].Count(l2 =>
                    l.NodeId != l2.NodeId &&
                    IsInDirection(d, l, l2) &&
                    Extensions.ApproximateDistance(l.Lat, l.Lng, l2.Lat, l2.Lng) <= neighborFilter.Radius &&
                    filterExpression(l2, l)) <= neighborFilter.Limit)),
            { Bound: "lte", CheckEachCardinalDirectionSeparately: false } => locations
                .Where(l => LocationsFromDictionary(neighborLocationBuckets, l, precision).Count(l2 =>
                    l.NodeId != l2.NodeId &&
                    Extensions.ApproximateDistance(l.Lat, l.Lng, l2.Lat, l2.Lng) <= neighborFilter.Radius &&
                    filterExpression(l2, l)) <= neighborFilter.Limit),
            { Bound: "all" } => locations
                .Where(l => LocationsFromDictionary(neighborLocationBuckets, l, precision).Where(l2 =>
                        l.NodeId != l2.NodeId &&
                        Extensions.ApproximateDistance(l.Lat, l.Lng, l2.Lat, l2.Lng) <= neighborFilter.Radius)
                    .AllAtLeastOne(l2 => filterExpression(l2, l))),
            { Bound: "percentage-gte" } => Percentage(locations, neighborLocationBuckets, neighborFilter, precision, filterExpression, Bound.GreaterThan),
            { Bound: "percentage-lte" } => Percentage(locations, neighborLocationBuckets, neighborFilter, precision, filterExpression, Bound.LessThan),
            _ => throw new InvalidOperationException($"Neighbor filter combination is not valid/implemented. Bound: {neighborFilter.Bound}. Check separately: {neighborFilter.CheckEachCardinalDirectionSeparately}. Limit: {neighborFilter.Limit}")
        };
    }

    private static IEnumerable<Loc> Percentage(IEnumerable<Loc> locations, Dictionary<string, List<Loc>> neighborLocationBuckets, NeighborFilter neighborFilter, HashPrecision precision, Func<Loc, Loc, bool> filterExpression, Bound bound) =>
        locations
            .Where(l =>
            {
                var allCount = 0;
                var count = LocationsFromDictionary(neighborLocationBuckets, l, precision).Count(l2 =>
                    l.NodeId != l2.NodeId &&
                    Extensions.ApproximateDistance(l.Lat, l.Lng, l2.Lat, l2.Lng) <= neighborFilter.Radius &&
                    allCount++ < int.MaxValue &&
                    filterExpression(l2, l));
                var percentage = allCount > 0 ? decimal.Divide(count, allCount) * 100 : 0;
                return bound switch
                {
                    Bound.LessThan => percentage <= neighborFilter.Limit,
                    Bound.GreaterThan => percentage >= neighborFilter.Limit,
                    _ => throw new ArgumentOutOfRangeException(nameof(bound), bound, $"Bound {bound} not implemented.")
                };
            });

    private static IEnumerable<Loc> LocationsFromDictionary(Dictionary<string, List<Loc>> neighborLocationBuckets, Loc l, HashPrecision precision) =>
        neighborLocationBuckets.TryGetValue(Hasher.Encode(l.Lat, l.Lng, precision), out var locations)
            ? locations
            : [];

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