using Shouldly;
using Vali.Core.Google;
using Vali.Core.Neighbors;
using Xunit;

namespace Vali.Core.Tests;

public class NeighborGridTests
{
    [Theory]
    [InlineData(59.9, 120)]
    [InlineData(59.9, 2000)]
    [InlineData(-54.8, 300)]
    [InlineData(78.2, 500)]
    [InlineData(89.5, 1000)]
    [InlineData(0.0, 200)]
    public void Ranges_cover_every_point_within_the_radius(double centerLat, int radius)
    {
        var points = RandomSquare(3_000, centerLat, 10.0, radius * 6, seed: radius);
        var grid = new NeighborGrid(points, radius, points.Max(p => Math.Abs(p.Lat)));
        var r2 = (double)radius * radius;

        foreach (var q in points)
        {
            var inRanges = IdsInRanges(grid, q.Lat, q.Lng, out var rangeCount);
            rangeCount.ShouldBeLessThanOrEqualTo(NeighborGrid.MaxRanges);
            foreach (var p in points)
            {
                if (Extensions.PointsAreCloserThan(q.Lat, q.Lng, p.Lat, p.Lng, r2))
                {
                    inRanges.ShouldContain(p.NodeId, $"query {q.NodeId} misses {p.NodeId}");
                }
            }
        }
    }

    // An index built from a different set than the candidates (Review Focus 2). At 70.0° -> 70.5° a match is under
    // one cell away either way; at 50° -> 75° it is about 2.2 cells away, so only the widened reach finds it.
    [Theory]
    [InlineData(70.0, 70.5)]
    [InlineData(50.0, 75.0)]
    public void Candidate_more_poleward_than_the_reference_still_finds_its_neighbors(double pointsLat, double queryLat)
    {
        const int radius = 500;
        var points = RandomSquare(500, pointsLat, 10.0, radius * 6, seed: 3).ToList();
        var query = (lat: queryLat, lng: 10.0);
        // 0.9 r east and west of the query, at the query's latitude (metres -> degrees there).
        var dLng = 0.9 * radius / (6371137.0 * Math.PI / 180.0 * Math.Cos(queryLat * Math.PI / 180.0));
        points.Add(MakeLocation(100_001, query.lat, query.lng + dLng));
        points.Add(MakeLocation(100_002, query.lat, query.lng - dLng));
        var reference = points.Where(p => p.NodeId < 100_000).Max(p => Math.Abs(p.Lat));

        var grid = new NeighborGrid(points, radius, reference);

        Extensions.PointsAreCloserThan(query.lat, query.lng, query.lat, query.lng + dLng, (double)radius * radius).ShouldBeTrue();
        var ids = IdsInRanges(grid, query.lat, query.lng, out var rangeCount);
        rangeCount.ShouldBeLessThanOrEqualTo(NeighborGrid.MaxRanges);
        ids.ShouldContain(100_001L);
        ids.ShouldContain(100_002L);
    }

    [Fact]
    public void Points_either_side_of_the_antimeridian_are_not_in_range()
    {
        const int radius = 500;
        var p = MakeLocation(1, 65.0, 179.9999);
        var q = MakeLocation(2, 65.0, -179.9999);
        var grid = new NeighborGrid([p, q], radius, 65.0);

        IdsInRanges(grid, q.Lat, q.Lng, out _).ShouldNotContain(1L);
        Extensions.PointsAreCloserThan(q.Lat, q.Lng, p.Lat, p.Lng, (double)radius * radius).ShouldBeFalse();
    }

    [Fact]
    public void Empty_index_gives_empty_grids()
    {
        var grid = new NeighborIndex().GridFor(500, null);

        grid.Points.Length.ShouldBe(0);
        IdsInRanges(grid, 59.9, 10.7, out _).ShouldBeEmpty();
    }

    [Fact]
    public void GridFor_caches_per_radius_and_expression()
    {
        var points = RandomSquare(200, 59.9, 10.7, 3000, seed: 9);
        var index = new NeighborIndex(points);

        var a = index.GridFor(500, "Year gte 2018");
        index.GridFor(500, "Year gte 2018").ShouldBeSameAs(a);
        index.GridFor(500, null).ShouldNotBeSameAs(a);
        index.GridFor(250, "Year gte 2018").ShouldNotBeSameAs(a);
        index.GridFor(500, null).ShouldBeSameAs(index.GridFor(500, null));

        a.Points.ShouldAllBe(p => p.Google.Year >= 2018);
        a.Points.Length.ShouldBe(points.Count(p => p.Google.Year >= 2018));
        index.GridFor(500, null).Points.Length.ShouldBe(points.Length);
    }

    [Fact]
    public void ReferenceAbsLat_is_taken_over_the_whole_source()
    {
        var points = new[] { MakeLocation(1, 59.0, 10.0, year: 2010), MakeLocation(2, -61.5, 10.0, year: 2020) };
        var index = new NeighborIndex(points);

        index.ReferenceAbsLat.ShouldBe(61.5);
        index.GridFor(500, "Year gte 2018").Points.Select(p => p.NodeId).ShouldBe([2L]);
    }

    [Fact]
    public void HasDuplicateNodeIds_detects_a_repeated_NodeId()
    {
        new NeighborIndex([MakeLocation(1, 59, 10), MakeLocation(2, 59, 10)]).HasDuplicateNodeIds.ShouldBeFalse();
        new NeighborIndex([MakeLocation(1, 59, 10), MakeLocation(2, 59, 10), MakeLocation(1, 60, 11)]).HasDuplicateNodeIds.ShouldBeTrue();
        new NeighborIndex().HasDuplicateNodeIds.ShouldBeFalse();
    }

    [Fact]
    public void Points_holds_every_source_point_once()
    {
        var points = RandomSquare(1_000, 59.9, 10.7, 5000, seed: 4);
        var grid = new NeighborGrid(points, 300, points.Max(p => Math.Abs(p.Lat)));

        grid.Points.Length.ShouldBe(points.Length);
        grid.Points.Select(p => p.NodeId).OrderBy(x => x).ShouldBe(points.Select(p => p.NodeId).OrderBy(x => x));
    }

    internal static HashSet<long> IdsInRanges(NeighborGrid grid, double lat, double lng, out int rangeCount)
    {
        Span<(int Start, int End)> ranges = stackalloc (int, int)[NeighborGrid.MaxRanges];
        rangeCount = grid.CollectRanges(lat, lng, ranges);
        var ids = new HashSet<long>();
        for (var r = 0; r < rangeCount; r++)
        {
            for (var i = ranges[r].Start; i < ranges[r].End; i++)
            {
                ids.Add(grid.Points[i].NodeId);
            }
        }

        return ids;
    }

    internal static Location[] RandomSquare(int count, double centerLat, double centerLng, double sideMetres, int seed)
    {
        var rng = new Random(seed);
        var metresPerDegree = 6371137.0 * Math.PI / 180.0;
        var dLat = sideMetres / metresPerDegree;
        var dLng = sideMetres / (metresPerDegree * Math.Max(Math.Cos(centerLat * Math.PI / 180.0), 1e-6));
        return Enumerable.Range(1, count)
            .Select(i => MakeLocation(
                i,
                Math.Clamp(centerLat + (rng.NextDouble() - 0.5) * dLat, -89.999, 89.999),
                centerLng + (rng.NextDouble() - 0.5) * dLng,
                year: 2008 + rng.Next(18)))
            .ToArray();
    }

    internal static Location MakeLocation(long nodeId, double lat, double lng, int year = 2020) => new()
    {
        NodeId = nodeId,
        Lat = lat,
        Lng = lng,
        Osm = new OsmData(),
        Google = new GoogleData { PanoId = "", CountryCode = "NO", Lat = lat, Lng = lng, Year = year, Month = 6 },
        Nominatim = new NominatimData { CountryCode = "NO", SubdivisionCode = "NO-03" }
    };
}
