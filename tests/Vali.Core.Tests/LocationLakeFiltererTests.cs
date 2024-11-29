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
    [InlineData("Roads25 eq 1 and (HighwayType eq 'Residential' or HighwayType eq 'Service' or HighwayType eq 'Tertiary' or HighwayType eq 'Road' or HighwayType eq 'Track' or HighwayType eq 'Unclassified')")]
    public void Should_compile_expressions(string expression)
    {
        var locations = LocationArray();
        Should.NotThrow(() => LocationLakeFilterer.Filter(locations, expression, new(), new()));
    }

    [Fact]
    public void Should_translate_named_expressions()
    {
        var locations = LocationArray();
        var mapDefinition = new MapDefinition
        {
            NamedExpressions = new()
            {
                { "$$rural", "Buildings100 lt 2 and Roads100 lt 4" },
                { "$$urban", "Buildings100 gt 12" },
            }
        };

        var orExpression = ExpressionDefaults.Expand(mapDefinition.NamedExpressions, "$$rural or $$urban");
        var andExpression = ExpressionDefaults.Expand(mapDefinition.NamedExpressions, "$$rural and $$urban");
        Should.NotThrow(() => LocationLakeFilterer.Filter(locations, orExpression, mapDefinition, new()));
        Should.NotThrow(() => LocationLakeFilterer.Filter(locations, andExpression, mapDefinition, new()));
    }

    private static Location[] LocationArray() =>
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