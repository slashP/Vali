using Shouldly;
using Vali.Core;
using Xunit;

namespace Vali.Core.Tests;

public class PlaceSpacedTests
{
    private static Location[] UniformGrid(int count, int seed, double latSpan = 2.0, double lngSpan = 2.0)
    {
        var rng = new Random(seed);
        return Enumerable.Range(0, count).Select(i => new Location
        {
            NodeId = i,
            Lat = 59.0 + rng.NextDouble() * latSpan,
            Lng = 10.0 + rng.NextDouble() * lngSpan,
            Google = new GoogleData { PanoId = "", CountryCode = "NO" },
            Osm = new OsmData(),
            Nominatim = new NominatimData { CountryCode = "NO", SubdivisionCode = "NO-03" }
        }).ToArray();
    }

    private static double MinPairwiseMetres(IList<Location> locs)
    {
        var min = double.MaxValue;
        for (var i = 0; i < locs.Count; i++)
        for (var j = i + 1; j < locs.Count; j++)
            min = Math.Min(min, Extensions.ApproximateDistance(locs[i].Lat, locs[i].Lng, locs[j].Lat, locs[j].Lng));
        return min;
    }

    [Theory]
    [InlineData(2000, 500)]
    [InlineData(2000, 1000)]
    [InlineData(5000, 2000)]
    public void respects_min_distance(int d, int goal)
    {
        var locs = UniformGrid(20_000, 1);
        var result = LocationDistributor.PlaceSpaced<Location, long>(locs, goal, d);
        result.Count.ShouldBeLessThanOrEqualTo(goal);
        if (result.Count > 1)
            MinPairwiseMetres(result).ShouldBeGreaterThanOrEqualTo(d - 0.001);
    }

    [Fact]
    public void caps_at_goal_when_plentiful()
    {
        var locs = UniformGrid(20_000, 2);
        var result = LocationDistributor.PlaceSpaced<Location, long>(locs, 100, 200);
        result.Count.ShouldBe(100);
    }

    [Fact]
    public void count_is_monotone_non_increasing_in_distance()
    {
        var locs = UniformGrid(20_000, 3);
        var prev = int.MaxValue;
        foreach (var d in new[] { 200, 500, 1000, 2000, 5000, 10000 })
        {
            var count = LocationDistributor.PlaceSpaced<Location, long>(locs, 1_000_000, d).Count;
            count.ShouldBeLessThanOrEqualTo(prev);
            prev = count;
        }
    }

    [Fact]
    public void honors_locations_already_in_map()
    {
        var locs = UniformGrid(20_000, 4);
        // Block everything within 5km of the box centre.
        var alreadyInMap = new ILatLng[] { new SimplePoint(60.0, 11.0) };
        const int d = 5000;
        var result = LocationDistributor.PlaceSpaced<Location, long>(locs, 1_000_000, d, alreadyInMap);
        foreach (var loc in result)
            Extensions.ApproximateDistance(loc.Lat, loc.Lng, 60.0, 11.0).ShouldBeGreaterThanOrEqualTo(d - 0.001);
    }

    [Fact]
    public void deterministic_for_fixed_order()
    {
        var locs = UniformGrid(10_000, 5);
        var a = LocationDistributor.PlaceSpaced<Location, long>(locs, 500, 1000);
        var b = LocationDistributor.PlaceSpaced<Location, long>(locs, 500, 1000);
        a.Select(x => x.NodeId).ShouldBe(b.Select(x => x.NodeId));
    }

    private sealed record SimplePoint(double Lat, double Lng) : ILatLng;
}
