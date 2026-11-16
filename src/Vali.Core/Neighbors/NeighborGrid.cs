namespace Vali.Core.Neighbors;

/// <summary>
/// The points of one neighbor filter's search set on a uniform grid whose cells are one radius tall and at least one
/// radius wide, in compact layout: points sorted by cell (row-major) into one contiguous array. Only the references are
/// stored (8 bytes a point, below the geohash bucket dictionary it replaces); coordinates are read through them. For a query point,
/// <see cref="CollectRanges"/> returns at most three index ranges (one per row) that together hold every stored point
/// <see cref="Extensions.PointsAreCloserThan(double,double,double,double,double)"/> accepts at this radius. The caller
/// makes the exact check.
/// </summary>
public sealed class NeighborGrid
{
    public const int MaxRanges = 3;

    // Cell indices are offset so every key is non-negative and a row's columns sort in numeric order.
    private const int Bias = 1 << 30;
    private const double MetresPerLatDegree = 110_000.0;

    private readonly double _cellLat;
    private readonly double _cellLng;
    private readonly double _referenceAbsLat;
    private readonly double _gridAbsLat;
    private readonly long[] _keys;
    private readonly int[] _starts;

    public Location[] Points { get; }

    public NeighborGrid(IReadOnlyList<Location> points, int radiusMeters, double referenceAbsLat)
    {
        _referenceAbsLat = referenceAbsLat;
        _gridAbsLat = Math.Min(referenceAbsLat, 89.9);
        if (radiusMeters <= 0 || points.Count == 0)
        {
            // Nothing is closer than a radius of 0 (the metric compares with <), so no point is ever in range.
            Points = [];
            _keys = [];
            _starts = [0];
            return;
        }

        // PointsAreCloserThan rejects dlat² > r²/110000² before anything else, so a match is at most one row away.
        _cellLat = radiusMeters / MetresPerLatDegree;
        // It compares with cos of the mean latitude, which is no more poleward than the reference for a query at or
        // below it, so a match is at most one column away (and 110,000 < 6371137·π/180 gives a further 1 % margin).
        _cellLng = radiusMeters / (MetresPerLatDegree * CosFloor(_gridAbsLat));

        var count = points.Count;
        var cellKeys = new long[count];
        var order = new int[count];
        for (var i = 0; i < count; i++)
        {
            cellKeys[i] = Key(Row(points[i].Lat), Column(points[i].Lng));
            order[i] = i;
        }

        Array.Sort(cellKeys, order);

        Points = new Location[count];
        var distinct = 0;
        for (var i = 0; i < count; i++)
        {
            Points[i] = points[order[i]];
            if (i == 0 || cellKeys[i] != cellKeys[i - 1])
            {
                distinct++;
            }
        }

        _keys = new long[distinct];
        _starts = new int[distinct + 1];
        var k = -1;
        for (var i = 0; i < count; i++)
        {
            if (i == 0 || cellKeys[i] != cellKeys[i - 1])
            {
                k++;
                _keys[k] = cellKeys[i];
                _starts[k] = i;
            }
        }

        _starts[distinct] = count;
    }

    /// <summary>
    /// Writes the half-open ranges of <see cref="Points"/> that can hold a match for the query and returns how many
    /// (at most <see cref="MaxRanges"/>; empty ranges are left out).
    /// </summary>
    public int CollectRanges(double lat, double lng, Span<(int Start, int End)> ranges)
    {
        if (_keys.Length == 0)
        {
            return 0;
        }

        var row = Row(lat);
        var column = Column(lng);
        var reach = 1L;
        var absLat = Math.Abs(lat);
        if (absLat > _gridAbsLat || _referenceAbsLat > _gridAbsLat)
        {
            // A match's mean latitude is at most one row more poleward than the query, and no more poleward than the
            // query or the most poleward stored point. Past the grid's own latitude, widen the column reach to match.
            var meanAbsLatBound = Math.Min(absLat + _cellLat, Math.Max(absLat, _referenceAbsLat));
            if (meanAbsLatBound > _gridAbsLat)
            {
                var needed = _cellLat * MetresPerLatDegree / (MetresPerLatDegree * CosFloor(meanAbsLatBound)) / _cellLng;
                reach = Math.Max(1L, (long)Math.Min(Math.Ceiling(needed), Bias));
            }
        }

        var count = 0;
        for (var dy = -1; dy <= 1; dy++)
        {
            var r = row + dy;
            var lo = LowerBound(Key(r, Math.Max(column - reach, 0)));
            var hi = LowerBound(Key(r, Math.Min(column + reach, 2L * Bias - 1)) + 1);
            if (hi > lo)
            {
                ranges[count++] = (_starts[lo], _starts[hi]);
            }
        }

        return count;
    }

    private long Row(double lat) => (long)Math.Floor(lat / _cellLat) + Bias;

    private long Column(double lng) => (long)Math.Floor(lng / _cellLng) + Bias;

    private static long Key(long row, long column) => (row << 32) | column;

    private static double CosFloor(double absLatDegrees) =>
        Math.Max(Math.Cos(Math.Min(absLatDegrees, 90.0) * Math.PI / 180.0), 1e-6);

    // First index in _keys whose key is >= key.
    private int LowerBound(long key)
    {
        int lo = 0, hi = _keys.Length;
        while (lo < hi)
        {
            var mid = (lo + hi) >>> 1;
            if (_keys[mid] < key) lo = mid + 1; else hi = mid;
        }

        return lo;
    }
}
