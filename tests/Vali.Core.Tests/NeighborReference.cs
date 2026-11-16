namespace Vali.Core.Tests;

/// <summary>
/// Brute-force restatement of what a neighbor filter means, for every candidate in <c>all</c>: O(n²), no grid,
/// no pre-filter, today's decimal percentage. The semantics the index-based evaluator must reproduce.
/// </summary>
internal static class NeighborReference
{
    public static long[] Filter(Location[] all, NeighborFilter f)
    {
        var r2 = (double)f.Radius * f.Radius;
        var match = string.IsNullOrEmpty(f.Expression)
            ? (_, _) => true
            : LocationLakeFilterer.CompileParentBoolLocationExpression(f.Expression);
        var kept = new List<long>();
        foreach (var c in all)
        {
            var neighbors = all.Where(n => n.NodeId != c.NodeId && Extensions.PointsAreCloserThan(c.Lat, c.Lng, n.Lat, n.Lng, r2)).ToArray();
            if (Keeps(c, neighbors, f, match))
            {
                kept.Add(c.NodeId);
            }
        }

        return kept.OrderBy(x => x).ToArray();
    }

    private static bool Keeps(Location c, Location[] neighbors, NeighborFilter f, Func<Location, Location, bool> match)
    {
        int Count(Func<Location, bool> direction) => neighbors.Count(n => direction(n) && match(n, c));
        var directions = new Func<Location, bool>[]
        {
            n => c.Lng < n.Lng, // West
            n => c.Lng > n.Lng, // East
            n => c.Lat > n.Lat, // North
            n => c.Lat < n.Lat, // South
        };
        var all = Count(_ => true);
        var separately = f.CheckEachCardinalDirectionSeparately;
        switch (f.Bound)
        {
            case "some":
                return all > 0;
            case "gte" when separately:
                return directions.Any(d => Count(d) >= f.Limit);
            case "gte":
                return all >= f.Limit;
            case "none" when separately:
            case "lte" when separately && f.Limit == 0:
                return directions.Any(d => Count(d) == 0);
            case "none":
                return all == 0;
            case "lte" when separately && f.Limit > 0:
                return directions.Any(d => Count(d) <= f.Limit);
            case "lte" when !separately:
                return all <= f.Limit;
            case "all":
                return neighbors.Length > 0 && neighbors.All(n => match(n, c));
            case "percentage-gte":
            case "percentage-lte":
                var percentage = neighbors.Length > 0 ? decimal.Divide(all, neighbors.Length) * 100 : 0;
                return f.Bound == "percentage-gte" ? percentage >= f.Limit : percentage <= f.Limit;
            default:
                throw new InvalidOperationException($"Not a valid neighbor filter: {f.Bound}, {f.Limit}, {separately}");
        }
    }
}
