using System.Diagnostics;
using Vali.Core.Expressions;

namespace Vali.Core.Neighbors;

/// <summary>
/// One or more neighbor filters compiled against a <see cref="NeighborIndex"/>: <see cref="Passes"/> tells whether a
/// candidate passes all of them, in one pass per filter over the grid cells around it, stopping as soon as the answer
/// is known. Thread-safe and allocation-free per candidate.
/// </summary>
public sealed class NeighborCheck
{
    private readonly Evaluator[] _evaluators;

    private NeighborCheck(Evaluator[] evaluators)
    {
        _evaluators = evaluators;
    }

    /// <summary>
    /// Null when there are no filters. Throws, as today, for a combination with no meaning (an unknown bound, or
    /// <c>lte</c> per direction with a null or negative limit), before any candidate is evaluated.
    /// </summary>
    public static NeighborCheck? For(IReadOnlyList<NeighborFilter> filters, NeighborIndex index)
    {
        if (filters.Count == 0)
        {
            return null;
        }

        var kinds = new (Kind kind, int limit)[filters.Count];
        for (var i = 0; i < filters.Count; i++)
        {
            kinds[i] = Classify(filters[i]);
        }

        var evaluators = new Evaluator[filters.Count];
        for (var i = 0; i < filters.Count; i++)
        {
            evaluators[i] = new Evaluator(filters[i], kinds[i].kind, kinds[i].limit, index);
        }

        return new NeighborCheck(evaluators);
    }

    public bool Passes(Location candidate)
    {
        var timed = GenPerf.Enabled;
        foreach (var evaluator in _evaluators)
        {
            bool passes;
            if (timed)
            {
                var start = Stopwatch.GetTimestamp();
                passes = evaluator.Passes(candidate, out var visits);
                GenPerf.AddNeighborEvaluation(visits, Stopwatch.GetTimestamp() - start);
            }
            else
            {
                passes = evaluator.Passes(candidate, out _);
            }

            if (!passes)
            {
                return false;
            }
        }

        return true;
    }

    // Same cases, in the same order, as the bound switch this replaces. `some`, `all` and `percentage-*` ignore
    // CheckEachCardinalDirectionSeparately, as before.
    private static (Kind kind, int limit) Classify(NeighborFilter f)
    {
        var separately = f.CheckEachCardinalDirectionSeparately;
        return f switch
        {
            { Bound: "gte", CheckEachCardinalDirectionSeparately: true } => f.Limit is { } n ? (Kind.AtLeastInSomeDirection, n) : (Kind.Never, 0),
            { Limit: 1, Bound: "gte", CheckEachCardinalDirectionSeparately: false } or { Bound: "some" } => (Kind.AtLeast, 1),
            { Bound: "gte", CheckEachCardinalDirectionSeparately: false } => f.Limit is { } n ? (Kind.AtLeast, n) : (Kind.Never, 0),
            { Limit: 0, Bound: "lte", CheckEachCardinalDirectionSeparately: true } or { CheckEachCardinalDirectionSeparately: true, Bound: "none" } => (Kind.NoneInSomeDirection, 0),
            { Limit: 0, Bound: "lte", CheckEachCardinalDirectionSeparately: false } or { Bound: "none" } => (Kind.AtMost, 0),
            { Limit: > 0, Bound: "lte", CheckEachCardinalDirectionSeparately: true } => (Kind.AtMostInSomeDirection, f.Limit.Value),
            { Bound: "lte", CheckEachCardinalDirectionSeparately: false } => f.Limit is { } n ? (Kind.AtMost, n) : (Kind.Never, 0),
            { Bound: "all" } => (Kind.All, 0),
            { Bound: "percentage-gte" } => f.Limit is { } n ? (Kind.PercentageAtLeast, n) : (Kind.Never, 0),
            { Bound: "percentage-lte" } => f.Limit is { } n ? (Kind.PercentageAtMost, n) : (Kind.Never, 0),
            _ => throw new InvalidOperationException($"Neighbor filter combination is not valid/implemented. Bound: {f.Bound}. Check separately: {separately}. Limit: {f.Limit}")
        };
    }

    private enum Kind
    {
        Never,
        AtLeast,
        AtMost,
        AtLeastInSomeDirection,
        NoneInSomeDirection,
        AtMostInSomeDirection,
        All,
        PercentageAtLeast,
        PercentageAtMost,
    }

    private sealed class Evaluator
    {
        private readonly Kind _kind;
        private readonly int _limit;
        private readonly double _radiusSquared;
        private readonly NeighborGrid? _grid;
        private readonly Func<Location, Location, bool>? _match;

        public Evaluator(NeighborFilter filter, Kind kind, int limit, NeighborIndex index)
        {
            _kind = kind;
            _limit = limit;
            _radiusSquared = (double)filter.Radius * filter.Radius;
            if (kind == Kind.Never)
            {
                return;
            }

            var expression = filter.Expression;
            var hasExpression = !string.IsNullOrEmpty(expression);
            // Counting bounds only look at matching neighbors, so a neighbor failing the neighbor-only part of the
            // expression can never count: drop it from the grid. `all` and percentages need every neighbor.
            var countsOnlyMatches = kind is not (Kind.All or Kind.PercentageAtLeast or Kind.PercentageAtMost);
            var parts = countsOnlyMatches && hasExpression && expression != "*"
                ? NeighborFilterDecomposer.Decompose(expression)
                : new NeighborExpressionParts(null, false);
            _grid = index.GridFor(filter.Radius, parts.NeighborOnly);
            var preFilteredWhole = parts.NeighborOnly != null && parts.NeighborOnlyIsWhole;
            _match = !hasExpression || preFilteredWhole ? null : LocationLakeFilterer.CompileParentBoolLocationExpression(expression);
        }

        public bool Passes(Location c, out int visits)
        {
            visits = 0;
            switch (_kind)
            {
                case Kind.Never:
                    return false;
                case Kind.AtLeast:
                    return _limit <= 0 || CountMatches(c, _limit, ref visits) >= _limit;
                case Kind.AtMost:
                    return _limit >= 0 && CountMatches(c, _limit + 1, ref visits) <= _limit;
                case Kind.AtLeastInSomeDirection:
                case Kind.NoneInSomeDirection:
                case Kind.AtMostInSomeDirection:
                    return Directions(c, ref visits);
                case Kind.All:
                    return All(c, ref visits);
                default:
                    return Percentage(c, ref visits);
            }
        }

        // Matching neighbors, counting stops at stopAt.
        private int CountMatches(Location c, int stopAt, ref int visits)
        {
            var grid = _grid!;
            Span<(int Start, int End)> ranges = stackalloc (int, int)[NeighborGrid.MaxRanges];
            var rangeCount = grid.CollectRanges(c.Lat, c.Lng, ranges);
            var points = grid.Points;
            var count = 0;
            for (var r = 0; r < rangeCount; r++)
            {
                for (var i = ranges[r].Start; i < ranges[r].End; i++)
                {
                    var n = points[i];
                    if (!Extensions.PointsAreCloserThan(c.Lat, c.Lng, n.Lat, n.Lng, _radiusSquared) || n.NodeId == c.NodeId)
                    {
                        continue;
                    }

                    visits++;
                    if (_match == null || _match(n, c))
                    {
                        if (++count >= stopAt)
                        {
                            return count;
                        }
                    }
                }
            }

            return count;
        }

        // Per direction (West: c.Lng < n.Lng, East: c.Lng > n.Lng, North: c.Lat > n.Lat, South: c.Lat < n.Lat), all four
        // counted in one pass. A neighbor can count in two directions, or in none.
        private bool Directions(Location c, ref int visits)
        {
            if (_kind == Kind.AtLeastInSomeDirection && _limit <= 0)
            {
                return true;
            }

            var grid = _grid!;
            Span<(int Start, int End)> ranges = stackalloc (int, int)[NeighborGrid.MaxRanges];
            var rangeCount = grid.CollectRanges(c.Lat, c.Lng, ranges);
            var points = grid.Points;
            int west = 0, east = 0, north = 0, south = 0;
            for (var r = 0; r < rangeCount; r++)
            {
                for (var i = ranges[r].Start; i < ranges[r].End; i++)
                {
                    var n = points[i];
                    if (!Extensions.PointsAreCloserThan(c.Lat, c.Lng, n.Lat, n.Lng, _radiusSquared) || n.NodeId == c.NodeId)
                    {
                        continue;
                    }

                    var isWest = c.Lng < n.Lng;
                    var isEast = c.Lng > n.Lng;
                    var isNorth = c.Lat > n.Lat;
                    var isSouth = c.Lat < n.Lat;
                    if (!(isWest || isEast || isNorth || isSouth))
                    {
                        continue;
                    }

                    visits++;
                    if (_match != null && !_match(n, c))
                    {
                        continue;
                    }

                    if (isWest) west++;
                    if (isEast) east++;
                    if (isNorth) north++;
                    if (isSouth) south++;
                    switch (_kind)
                    {
                        case Kind.AtLeastInSomeDirection when west >= _limit || east >= _limit || north >= _limit || south >= _limit:
                            return true;
                        case Kind.NoneInSomeDirection when west > 0 && east > 0 && north > 0 && south > 0:
                            return false;
                        case Kind.AtMostInSomeDirection when west > _limit && east > _limit && north > _limit && south > _limit:
                            return false;
                    }
                }
            }

            // AtLeast: no direction reached the limit. None / AtMost: some direction stayed at or under it.
            return _kind != Kind.AtLeastInSomeDirection;
        }

        // At least one neighbor, and every neighbor matches.
        private bool All(Location c, ref int visits)
        {
            var grid = _grid!;
            Span<(int Start, int End)> ranges = stackalloc (int, int)[NeighborGrid.MaxRanges];
            var rangeCount = grid.CollectRanges(c.Lat, c.Lng, ranges);
            var points = grid.Points;
            for (var r = 0; r < rangeCount; r++)
            {
                for (var i = ranges[r].Start; i < ranges[r].End; i++)
                {
                    var n = points[i];
                    if (!Extensions.PointsAreCloserThan(c.Lat, c.Lng, n.Lat, n.Lng, _radiusSquared) || n.NodeId == c.NodeId)
                    {
                        continue;
                    }

                    visits++;
                    if (_match != null && !_match(n, c))
                    {
                        return false;
                    }
                }
            }

            return visits > 0;
        }

        // match/all as a percentage against the limit, by cross-multiplication (exact, like today's decimal division).
        // With no neighbors the percentage is 0, as before.
        private bool Percentage(Location c, ref int visits)
        {
            var grid = _grid!;
            Span<(int Start, int End)> ranges = stackalloc (int, int)[NeighborGrid.MaxRanges];
            var rangeCount = grid.CollectRanges(c.Lat, c.Lng, ranges);
            var points = grid.Points;
            var matches = 0;
            for (var r = 0; r < rangeCount; r++)
            {
                for (var i = ranges[r].Start; i < ranges[r].End; i++)
                {
                    var n = points[i];
                    if (!Extensions.PointsAreCloserThan(c.Lat, c.Lng, n.Lat, n.Lng, _radiusSquared) || n.NodeId == c.NodeId)
                    {
                        continue;
                    }

                    visits++;
                    if (_match == null || _match(n, c))
                    {
                        matches++;
                    }
                }
            }

            var all = (long)visits;
            var scaled = matches * 100L;
            var threshold = (long)_limit * all;
            return _kind == Kind.PercentageAtLeast
                ? (all == 0 ? 0 >= _limit : scaled >= threshold)
                : (all == 0 ? 0 <= _limit : scaled <= threshold);
        }
    }
}
