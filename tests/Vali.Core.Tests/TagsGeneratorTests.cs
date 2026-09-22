using Vali.Core.Google;
using Shouldly;
using Xunit;

namespace Vali.Core.Tests;

public class TagsGeneratorTests
{
    private static string[] ElevationTags(int elevation, string tag)
    {
        var location = new Location
        {
            Google = new GoogleData(),
            Osm = new OsmData(),
            Nominatim = new NominatimData()
        };
        return TagsGenerator.Tags(location, new MapCheckrLocation { elevation = elevation }, [tag])!.tags;
    }

    [Theory]
    [InlineData("Elevation500")]
    [InlineData("Elevation-500")]
    public void Bucketed_elevation_accepts_bucket_with_or_without_dash(string tag)
    {
        ElevationTags(1380, tag).ShouldBe(["Elevation[1000-1499]m"]);
    }

    [Fact]
    public void Bare_elevation_emits_raw_value()
    {
        ElevationTags(1380, "Elevation").ShouldBe(["1380"]);
    }
}
