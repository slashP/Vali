using NetTopologySuite.Geometries;
using Shouldly;
using Xunit;

namespace Vali.Core.Tests;

public class LocationDistributorTests
{
    [Theory]
    [InlineData(100, 500, 5000, 500)]
    [InlineData(100, 1000, 3900, 1000)]
    [InlineData(100, 2000, 2750, 2000)]
    [InlineData(100, 5000, 1500, 5000)]
    [InlineData(10000, 1000, 10000, 188)]
    [InlineData(75000, 1000, 75000, 5)]
    public void WithMaxMinDistance(int minDistance, int goalCount, int expectedMinDistance, int expectedCount)
    {
        var rng = new Random(42);
        var locations = Enumerable.Range(0, 50_000).Select(i => new Location
        {
            NodeId = i,
            Lat = 59.0 + rng.NextDouble() * 2,
            Lng = 10.0 + rng.NextDouble() * 2,
            Google = new GoogleData { PanoId = "", CountryCode = "NO" },
            Osm = new OsmData(),
            Nominatim = new NominatimData { CountryCode = "NO", SubdivisionCode = "NO-03" }
        }).ToArray();
        var distributed = LocationDistributor.WithMaxMinDistance<Location, long>(locations, goalCount, locationProbability: new(), [], avoidShuffle: true, minMinDistance: minDistance);
        distributed.locations.Count.ShouldBe(expectedCount);
        distributed.minDistance.ShouldBe(expectedMinDistance);
    }
}