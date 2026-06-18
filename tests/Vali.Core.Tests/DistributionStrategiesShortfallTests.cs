using Shouldly;
using Xunit;

namespace Vali.Core.Tests;

public class DistributionStrategiesShortfallTests
{
    [Fact]
    public void SubdivisionByMaxMinDistance_carries_real_goal_when_filter_excludes_everything()
    {
        var location = new Location
        {
            NodeId = 1,
            Lat = 1,
            Lng = 1,
            Google = new GoogleData { Year = 2020 },
            Osm = new OsmData(),
            Nominatim = new NominatimData { CountryCode = "XX", SubdivisionCode = "XX-1" }
        };
        var file = Path.GetTempFileName();
        try
        {
            using (var stream = File.Create(file))
            {
                ProtoBuf.Serializer.Serialize(stream, new[] { location });
            }

            var mapDefinition = new MapDefinition
            {
                GlobalLocationFilter = "Year gte 9999", // matches nothing -> zero filtered locations
                SubdivisionDistribution = new() { ["XX"] = new() { ["XX-1"] = 1 } }
            };

            var result = DistributionStrategies.SubdivisionByMaxMinDistance(
                "XX", file, goalCount: 100, ["XX-1"], mapDefinition);

            result.locations.ShouldBeEmpty();
            result.regionGoalCount.ShouldBe(100); // the real goal, not 0
        }
        finally
        {
            File.Delete(file);
        }
    }
}
