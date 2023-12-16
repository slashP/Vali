using Shouldly;
using Xunit;

namespace Vali.Core.Tests;

public class LocationLakeFiltererTests
{
    [Theory]
    [InlineData("Buildings100 eq 1")]
    [InlineData("Buildings100 eq 1 and Buildings10 eq 0")]
    [InlineData("Buildings100 eq 1 and Buildings10 eq 0 or Roads10 lt 3")]
    [InlineData("Surface eq 'gravel' and Buildings200 eq 0")]
    [InlineData("County eq 'Buildings200 County' and Buildings200 gt 0")]
    [InlineData("ArrowCount gte 2")]
    [InlineData("Heading lt 10.2 or Lat gt 23 + 42")]
    [InlineData("ArrowCount eq 1 and Buildings100 eq 0")]
    [InlineData("(ArrowCount eq 1 and Buildings100 eq 0) or (DrivingDirectionAngle lt 10 and DrivingDirectionAngle gt 350)")]
    [InlineData("ArrowCount eq 2 and Year gt 2011 and Year lt 2019 and DrivingDirectionAngle neq 0")]
    [InlineData("(DrivingDirectionAngle gt 10 and Heading gt 10 and DrivingDirectionAngle lt 80 and Heading lt 80) or (DrivingDirectionAngle gt 100 and Heading gt 100 and DrivingDirectionAngle lt 170 and Heading lt 170) or (DrivingDirectionAngle gt 190 and Heading gt 190 and DrivingDirectionAngle lt 260 and Heading lt 260) or (DrivingDirectionAngle gt 280 and Heading gt 280 and DrivingDirectionAngle lt 350 and Heading lt 350)")]
    public void Should_compile_expressions(string expression)
    {
        var locations = LocationArray();
        Should.NotThrow(() => LocationLakeFilterer.Filter(locations, expression));
    }

    private static IReadOnlyCollection<Location> LocationArray() =>
        new[]
        {
            new Location
            {
                Osm = new OsmData(),
                Google = new GoogleData
                {
                    PanoId = "",
                    CountryCode = "NO",
                    ArrowCount = 1
                },
                Nominatim = new NominatimData
                {
                    CountryCode = "NO",
                    SubdivisionCode = "NO-03"
                }
            }
        };
}