using Shouldly;
using Xunit;

namespace Vali.Core.Tests;

public class DistributionStrategiesFixedMinDistanceTests
{
    [Fact]
    public void MaxLocationsInSubdivisionsByFixedMinDistance_sizes_custom_weights_from_largest_successful_goal()
    {
        var locations = Enumerable.Range(0, 300).Select(i => new Location
        {
            NodeId = i + 1,
            Lat = 1 + i / 20 * 0.01,
            Lng = 1 + i % 20 * 0.01,
            Google = new GoogleData { Year = 2020 },
            Osm = new OsmData(),
            Nominatim = new NominatimData { CountryCode = "XX", SubdivisionCode = "XX-1" }
        }).ToArray();
        var file = Path.GetTempFileName();
        try
        {
            using (var stream = File.Create(file))
            {
                ProtoBuf.Serializer.Serialize(stream, locations);
            }

            var mapDefinition = new MapDefinition
            {
                SubdivisionDistribution = new() { ["XX"] = new() { ["XX-1"] = 1 } },
                DistributionStrategy = new DistributionStrategy { FixedMinDistance = 100 }
            };

            var result = DistributionStrategies.MaxLocationsInSubdivisionsByFixedMinDistance(
                "XX", [file], ["XX-1"], mapDefinition).Single();

            result.regionGoalCount.ShouldBe(214); // 429 fails, 214 succeeds, 321 is never tried
            result.locations.Count.ShouldBe(214);
        }
        finally
        {
            File.Delete(file);
        }
    }
}
