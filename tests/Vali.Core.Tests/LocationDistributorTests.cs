using Shouldly;
using Xunit;

namespace Vali.Core.Tests;

public class LocationDistributorTests
{
    private static Location[] Synthetic(int count = 50_000)
    {
        var rng = new Random(42);
        return Enumerable.Range(0, count).Select(i => new Location
        {
            NodeId = i,
            Lat = 59.0 + rng.NextDouble() * 2,
            Lng = 10.0 + rng.NextDouble() * 2,
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
    [InlineData(100, 500)]
    [InlineData(100, 1000)]
    [InlineData(100, 2000)]
    [InlineData(100, 5000)]
    public void WithMaxMinDistance_reaches_goal_and_respects_spacing(int minMin, int goal)
    {
        var (locs, dist) = LocationDistributor.WithMaxMinDistance<Location, long>(
            Synthetic(), goal, locationProbability: new(), [], avoidShuffle: true, minMinDistance: minMin);

        locs.Count.ShouldBe(goal);
        LocationDistributor.Distances.ShouldContain(dist);
        dist.ShouldBeGreaterThanOrEqualTo(minMin);
        MinPairwiseMetres(locs).ShouldBeGreaterThanOrEqualTo(dist - 0.001);
    }

    [Theory]
    [InlineData(10000, 1000)] // can't fit 1000 at >=10km -> fallback to smallest distance
    [InlineData(75000, 1000)] // only the single largest bucket qualifies
    public void WithMaxMinDistance_fallback_returns_smallest_distance(int minMin, int goal)
    {
        var distances = LocationDistributor.Distances.SkipWhile(x => x < minMin).ToArray();
        var (locs, dist) = LocationDistributor.WithMaxMinDistance<Location, long>(
            Synthetic(), goal, locationProbability: new(), [], avoidShuffle: true, minMinDistance: minMin);

        locs.Count.ShouldBeLessThanOrEqualTo(goal);
        dist.ShouldBe(distances[0]);
        if (locs.Count > 1)
            MinPairwiseMetres(locs).ShouldBeGreaterThanOrEqualTo(dist - 0.001);
    }
}
