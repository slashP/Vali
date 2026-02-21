using Shouldly;
using Vali.Core.Expressions;
using Vali.Core.Google;
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
        Should.NotThrow(() => LocationLakeFilterer.Filter(locations, [], [], expression, new(), [], [], new()));
    }

    [Theory]
    [InlineData("Month gte 6 and Month lte 9")]
    [InlineData("Elevation gt 500")]
    [InlineData("Elevation lte 100 or Elevation gte 3000")]
    [InlineData("DescriptionLength gt 0")]
    [InlineData("IsScout eq false")]
    [InlineData("ResolutionHeight gte 6656")]
    [InlineData("ClosestCoast lt 1000")]
    [InlineData("ClosestLake lt 500")]
    [InlineData("ClosestRiver lte 200")]
    [InlineData("ClosestRailway lt 300")]
    [InlineData("Tunnels200 eq 0 and Tunnels10 eq 0")]
    [InlineData("Roads0 gte 1")]
    [InlineData("Roads50 gt 0 and Roads200 lt 10")]
    [InlineData("Buildings25 gt 0 and Buildings200 lt 50")]
    [InlineData("IsResidential eq true")]
    [InlineData("HighwayTypeCount gte 2")]
    [InlineData("Lng gt 10.5 and Lng lt 25.0")]
    [InlineData("SubdivisionCode eq 'NO-03'")]
    [InlineData("CountryCode eq 'NO'")]
    [InlineData("Year eq 2023 and Month eq 6")]
    [InlineData("Year * 100 + Month gt 202306")]
    [InlineData("Elevation gt 100 and Elevation lt 500 and ClosestCoast gt 5000")]
    [InlineData("Buildings100 + Buildings200 gt 5")]
    [InlineData("Roads100 - Roads10 gte 0")]
    [InlineData("DrivingDirectionAngle modulo 90 eq 0")]
    [InlineData("ArrowCount gte 1 and ArrowCount lte 4")]
    [InlineData("(Year gt 2020 or (Year eq 2020 and Month gte 6)) and ArrowCount gte 2")]
    [InlineData("Surface neq 'asphalt'")]
    [InlineData("County neq 'SomeCounty'")]
    [InlineData("Heading gte 0 and Heading lt 360")]
    [InlineData("((Buildings100 gt 5 and Roads100 gt 3) or (Buildings200 gt 20 and Roads200 gt 5)) and Year gt 2018")]
    [InlineData("HighwayType eq 'Primary' or HighwayType eq 'Secondary' or HighwayType eq 'Tertiary'")]
    [InlineData("Surface eq 'asphalt' or Surface eq 'paved' or Surface eq 'concrete'")]
    public void Should_compile_additional_expressions(string expression)
    {
        var locations = LocationArray();
        Should.NotThrow(() => LocationLakeFilterer.Filter(locations, [], [], expression, new(), [], [], new()));
    }

    [Theory]
    [InlineData("ArrowCount eq 1", 1)]
    [InlineData("ArrowCount eq 2", 0)]
    [InlineData("Buildings100 eq 5", 1)]
    [InlineData("Buildings100 gt 10", 0)]
    [InlineData("Year gte 2020", 1)]
    [InlineData("Year lt 2020", 0)]
    [InlineData("Year lte 2023", 1)]
    [InlineData("Year neq 2023", 0)]
    [InlineData("CountryCode eq 'NO'", 1)]
    [InlineData("CountryCode neq 'NO'", 0)]
    [InlineData("SubdivisionCode eq 'NO-03'", 1)]
    [InlineData("SubdivisionCode eq 'SE-01'", 0)]
    [InlineData("ArrowCount eq 1 and Year eq 2023", 1)]
    [InlineData("ArrowCount eq 1 and Year eq 2020", 0)]
    [InlineData("ArrowCount eq 2 or Year eq 2023", 1)]
    [InlineData("ArrowCount eq 2 or Year eq 2020", 0)]
    [InlineData("Lat gt 59.0 and Lat lt 60.0", 1)]
    [InlineData("Lat lt 59.0", 0)]
    [InlineData("Lng gt 10.0 and Lng lt 11.5", 1)]
    [InlineData("Elevation gt 50 and Elevation lt 200", 1)]
    [InlineData("Elevation gt 200", 0)]
    [InlineData("Month gte 1 and Month lte 6", 1)]
    [InlineData("Month gt 6", 0)]
    [InlineData("DrivingDirectionAngle eq 180", 1)]
    [InlineData("DrivingDirectionAngle neq 180", 0)]
    [InlineData("IsResidential eq true", 1)]
    [InlineData("IsResidential eq false", 0)]
    [InlineData("Surface eq 'asphalt'", 1)]
    [InlineData("Surface eq 'gravel'", 0)]
    [InlineData("Heading eq 90", 1)]
    [InlineData("Heading neq 90", 0)]
    [InlineData("ClosestCoast lt 600", 1)]
    [InlineData("ClosestCoast gt 600", 0)]
    [InlineData("Roads100 eq 3 and Buildings100 eq 5", 1)]
    [InlineData("(ArrowCount eq 2 and Year eq 2020) or (ArrowCount eq 1 and Year eq 2023)", 1)]
    [InlineData("(ArrowCount eq 2 and Year eq 2023) or (ArrowCount eq 1 and Year eq 2020)", 0)]
    [InlineData("Year * 100 + Month eq 202306", 1)]
    [InlineData("DrivingDirectionAngle modulo 90 eq 0", 1)]
    [InlineData("DrivingDirectionAngle modulo 90 eq 1", 0)]
    [InlineData("Buildings100 + Roads100 eq 8", 1)]
    [InlineData("Buildings100 + Roads100 gt 10", 0)]
    public void Should_filter_locations_correctly(string expression, int expectedCount)
    {
        var locations = LocationArrayWithData();
        var result = LocationLakeFilterer.Filter(locations, [], [], expression, new(), [], [], new());
        result.Length.ShouldBe(expectedCount);
    }

    [Fact]
    public void Should_return_true_for_wildcard_bool_expression()
    {
        var expression = LocationLakeFilterer.CompileBoolLocationExpression("*");
        var location = LocationArrayWithData().Single();
        expression(location).ShouldBeTrue();
    }

    [Fact]
    public void Should_return_zero_for_wildcard_int_expression()
    {
        var expression = LocationLakeFilterer.CompileIntLocationExpression("*");
        var location = LocationArrayWithData().Single();
        expression(location).ShouldBe(0);
    }

    [Theory]
    [InlineData("Buildings100", 5)]
    [InlineData("Roads100", 3)]
    [InlineData("ArrowCount", 1)]
    [InlineData("Year", 2023)]
    [InlineData("Month", 6)]
    [InlineData("DrivingDirectionAngle", 180)]
    public void Should_compile_int_expressions(string expression, int expectedValue)
    {
        var compiled = LocationLakeFilterer.CompileIntLocationExpression(expression);
        var location = LocationArrayWithData().Single();
        compiled(location).ShouldBe(expectedValue);
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
        Should.NotThrow(() => LocationLakeFilterer.Filter(locations, [], [], orExpression, new(), [], [], mapDefinition));
        Should.NotThrow(() => LocationLakeFilterer.Filter(locations, [], [], andExpression, new(), [], [], mapDefinition));
    }

    [Fact]
    public void Should_expand_nested_named_expressions()
    {
        var namedExpressions = new Dictionary<string, string>
        {
            { "$$lowBuildings", "Buildings100 lt 2" },
            { "$$lowRoads", "Roads100 lt 4" },
            { "$$rural", "$$lowBuildings and $$lowRoads" },
        };

        var expanded = ExpressionDefaults.Expand(namedExpressions, "$$rural");
        expanded.ShouldContain("Buildings100 lt 2");
        expanded.ShouldContain("Roads100 lt 4");
        expanded.ShouldNotContain("$$");
    }

    [Fact]
    public void Should_expand_named_expression_and_filter_correctly()
    {
        var locations = LocationArrayWithData();
        var mapDefinition = new MapDefinition
        {
            NamedExpressions = new()
            {
                { "$$recent", "Year gte 2020" },
                { "$$urban", "Buildings100 gt 3" },
            }
        };

        var expression = ExpressionDefaults.Expand(mapDefinition.NamedExpressions, "$$recent and $$urban");
        var result = LocationLakeFilterer.Filter(locations, [], [], expression, new(), [], [], mapDefinition);
        result.Length.ShouldBe(1);
    }

    [Fact]
    public void Should_not_expand_unknown_named_expression()
    {
        var namedExpressions = new Dictionary<string, string>
        {
            { "$$known", "Buildings100 gt 0" }
        };

        var expanded = ExpressionDefaults.Expand(namedExpressions, "$$unknown");
        expanded.ShouldBe("$$unknown");
    }

    [Fact]
    public void Should_expand_named_expression_combined_with_literal()
    {
        var namedExpressions = new Dictionary<string, string>
        {
            { "$$recent", "Year gte 2020" },
        };

        var expanded = ExpressionDefaults.Expand(namedExpressions, "$$recent and ArrowCount gte 2");
        expanded.ShouldBe("(Year gte 2020) and ArrowCount gte 2");
    }

    [Theory]
    [InlineData("current:Buildings100 gt Buildings100")]
    [InlineData("(current:DrivingDirectionAngle + 360 - DrivingDirectionAngle) modulo 360 eq 0")]
    public void Should_compile_parent_expressions(string expression)
    {
        var parentLocations = LocationArray();
        var locations = LocationArray();
        locations.Single().Google.DrivingDirectionAngle = 200;
        var ex = LocationLakeFilterer.CompileExpressionWithParent<Location, bool>(expression, false);
        var j = parentLocations.Where(p => locations.Any(l => ex(l, p))).ToArray();
        NeighborFilter[] neighborFilters = [new NeighborFilter
        {
            Expression = expression,
            Bound = "some",
            Radius = 200
        }];
        var mapDefinition = new MapDefinition
        {
            NeighborFilters = neighborFilters
        };
        Should.NotThrow(() => LocationLakeFilterer.Filter(locations, [], [], "", new(), [], neighborFilters, mapDefinition));
    }

    [Theory]
    [InlineData("current:Year eq Year")]
    [InlineData("current:ArrowCount gte ArrowCount")]
    [InlineData("current:Roads100 gt Roads100 or current:Buildings100 gt Buildings100")]
    [InlineData("current:Lat gt Lat - 1 and current:Lat lt Lat + 1")]
    [InlineData("current:Surface eq Surface")]
    [InlineData("current:CountryCode eq CountryCode")]
    public void Should_compile_additional_parent_expressions(string expression)
    {
        var ex = LocationLakeFilterer.CompileExpressionWithParent<Location, bool>(expression, false);
        var location = LocationArrayWithData().Single();
        Should.NotThrow(() => ex(location, location));
    }

    [Fact]
    public void Should_evaluate_parent_expression_comparing_different_locations()
    {
        var ex = LocationLakeFilterer.CompileExpressionWithParent<Location, bool>("current:Year gt Year", false);
        var child = LocationArrayWithData().Single();
        var parent = LocationArrayWithData().Single();
        parent.Google.Year = 2025;

        ex(child, parent).ShouldBeTrue();
        ex(child, child).ShouldBeFalse();
    }

    [Fact]
    public void Should_return_fallback_for_wildcard_parent_expression()
    {
        var exTrue = LocationLakeFilterer.CompileExpressionWithParent<Location, bool>("*", true);
        var exFalse = LocationLakeFilterer.CompileExpressionWithParent<Location, bool>("*", false);
        var location = LocationArrayWithData().Single();

        exTrue(location, location).ShouldBeTrue();
        exFalse(location, location).ShouldBeFalse();
    }

    [Theory]
    [InlineData("external:HasPoles eq 'Yes'")]
    public void Should_compile_expression_with_external_data(string expression)
    {
        var locations = LocationArray();
        locations.First().ExternalData["HasPoles"] = "Yes";
        LocationLakeFilterer.Filter(locations, [], [], expression, new(), [], [], new()).Length.ShouldBe(1);
    }

    [Fact]
    public void Should_filter_by_external_data_neq()
    {
        var locations = LocationArrayWithData();
        locations.Single().ExternalData["RoadQuality"] = "Good";
        var result = LocationLakeFilterer.Filter(locations, [], [], "external:RoadQuality neq 'Bad'", new(), [], [], new());
        result.Length.ShouldBe(1);
    }

    [Fact]
    public void Should_filter_by_external_data_excluding_non_matching()
    {
        var locations = LocationArrayWithData();
        locations.Single().ExternalData["Coverage"] = "Full";
        var result = LocationLakeFilterer.Filter(locations, [], [], "external:Coverage eq 'Partial'", new(), [], [], new());
        result.Length.ShouldBe(0);
    }

    [Fact]
    public void Should_combine_external_data_with_regular_filter()
    {
        var locations = LocationArrayWithData();
        locations.Single().ExternalData["HasPoles"] = "Yes";
        var result = LocationLakeFilterer.Filter(locations, [], [], "Year eq 2023 and external:HasPoles eq 'Yes'", new(), [], [], new());
        result.Length.ShouldBe(1);
    }

    [Fact]
    public void Should_exclude_location_with_missing_external_key()
    {
        var locations = LocationArrayWithData();
        var result = LocationLakeFilterer.Filter(locations, [], [], "external:HasPoles eq 'Yes'", new(), [], [], new());
        result.Length.ShouldBe(0);
    }

    [Fact]
    public void Should_filter_with_empty_expression()
    {
        var locations = LocationArrayWithData();
        var result = LocationLakeFilterer.Filter(locations, [], [], "", new(), [], [], new());
        result.Length.ShouldBe(1);
    }

    [Fact]
    public void Should_filter_with_null_expression()
    {
        var locations = LocationArrayWithData();
        var result = LocationLakeFilterer.Filter(locations, [], [], null, new(), [], [], new());
        result.Length.ShouldBe(1);
    }

    [Fact]
    public void Should_filter_multiple_locations()
    {
        var locations = new[]
        {
            CreateLocation("NO", "NO-03", year: 2023, arrowCount: 2, buildings100: 10),
            CreateLocation("NO", "NO-03", year: 2019, arrowCount: 1, buildings100: 0),
            CreateLocation("NO", "NO-03", year: 2023, arrowCount: 3, buildings100: 5),
        };
        LocationLakeFilterer.Filter(locations, [], [], "Year eq 2023", new(), [], [], new()).Length.ShouldBe(2);
        LocationLakeFilterer.Filter(locations, [], [], "ArrowCount gte 2", new(), [], [], new()).Length.ShouldBe(2);
        LocationLakeFilterer.Filter(locations, [], [], "Year eq 2023 and ArrowCount gte 2", new(), [], [], new()).Length.ShouldBe(2);
        LocationLakeFilterer.Filter(locations, [], [], "Buildings100 gt 3", new(), [], [], new()).Length.ShouldBe(2);
        LocationLakeFilterer.Filter(locations, [], [], "Year eq 2019 and Buildings100 eq 0", new(), [], [], new()).Length.ShouldBe(1);
        LocationLakeFilterer.Filter(locations, [], [], "ArrowCount eq 4", new(), [], [], new()).Length.ShouldBe(0);
    }

    [Fact]
    public void Should_cache_compiled_bool_expressions()
    {
        var expression = "ArrowCount eq 1";
        var first = LocationLakeFilterer.CompileBoolLocationExpression(expression);
        var second = LocationLakeFilterer.CompileBoolLocationExpression(expression);
        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void Should_cache_compiled_int_expressions()
    {
        var expression = "ArrowCount";
        var first = LocationLakeFilterer.CompileIntLocationExpression(expression);
        var second = LocationLakeFilterer.CompileIntLocationExpression(expression);
        first.ShouldBeSameAs(second);
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

    private static Location[] LocationArrayWithData() =>
    [
        new Location
        {
            Lat = 59.9,
            Lng = 10.7,
            Osm = new OsmData
            {
                Buildings100 = 5,
                Buildings200 = 12,
                Buildings10 = 1,
                Buildings25 = 2,
                Roads100 = 3,
                Roads200 = 6,
                Roads10 = 1,
                Roads25 = 2,
                Roads50 = 2,
                Roads0 = 1,
                Surface = "asphalt",
                IsResidential = true,
                ClosestCoast = 500,
                ClosestLake = 1200,
                ClosestRiver = 300,
                ClosestRailway = 800,
            },
            Google = new GoogleData
            {
                PanoId = "abc123",
                CountryCode = "NO",
                Lat = 59.9,
                Lng = 10.7,
                ArrowCount = 1,
                Year = 2023,
                Month = 6,
                DefaultHeading = 90.0,
                DrivingDirectionAngle = 180,
                Elevation = 100,
                DescriptionLength = 15,
                ResolutionHeight = 6656,
            },
            Nominatim = new NominatimData
            {
                CountryCode = "NO",
                SubdivisionCode = "NO-03",
                County = "Oslo"
            }
        }
    ];

    [Theory]
    [InlineData("Buildings100 / 2 gt 1", 1)]
    [InlineData("Roads100 / 3 eq 1", 1)]
    [InlineData("Buildings200 / 4 eq 3", 1)]
    [InlineData("Buildings200 / 4 eq 2", 0)]
    public void Should_filter_with_division_operator(string expression, int expectedCount)
    {
        var locations = LocationArrayWithData();
        var result = LocationLakeFilterer.Filter(locations, [], [], expression, new(), [], [], new());
        result.Length.ShouldBe(expectedCount);
    }

    [Theory]
    [InlineData("year eq 2023")]
    [InlineData("arrowCount gte 1")]
    [InlineData("month eq 6")]
    [InlineData("elevation gt 50")]
    [InlineData("lat gt 50.0")]
    [InlineData("heading gt 0")]
    [InlineData("resolutionHeight gte 6656")]
    [InlineData("isScout eq false")]
    [InlineData("descriptionLength gt 0")]
    [InlineData("drivingDirectionAngle eq 180")]
    [InlineData("resolutionHeight lt 8192")]
    public void Should_compile_mapcheckr_expressions(string expression)
    {
        var location = CreateMapCheckrLocation();
        var compiled = LocationLakeFilterer.CompileBoolMapCheckrExpression(expression, true);
        Should.NotThrow(() => compiled(location));
    }

    [Fact]
    public void Should_evaluate_mapcheckr_expression_correctly()
    {
        var location = CreateMapCheckrLocation();
        var compiled = LocationLakeFilterer.CompileBoolMapCheckrExpression("year eq 2023", true);
        compiled(location).ShouldBeTrue();

        var compiledFalse = LocationLakeFilterer.CompileBoolMapCheckrExpression("year eq 2020", true);
        compiledFalse(location).ShouldBeFalse();
    }

    [Theory]
    [InlineData("DescriptionLength eq null", 0)]
    [InlineData("DescriptionLength neq null", 1)]
    public void Should_filter_with_null_comparison(string expression, int expectedCount)
    {
        var locations = LocationArrayWithData();
        var result = LocationLakeFilterer.Filter(locations, [], [], expression, new(), [], [], new());
        result.Length.ShouldBe(expectedCount);
    }

    [Theory]
    [InlineData("County eq 'Saint-Tropez'")]
    [InlineData("County eq 'New York'")]
    [InlineData("Surface eq 'semi-paved'")]
    public void Should_compile_expressions_with_special_chars_in_strings(string expression)
    {
        var locations = LocationArray();
        Should.NotThrow(() => LocationLakeFilterer.Filter(locations, [], [], expression, new(), [], [], new()));
    }

    [Fact]
    public void Should_handle_escaped_single_quotes_in_strings()
    {
        var locations = LocationArrayWithData();
        locations.Single().Nominatim.County = "O'Brien";
        var result = LocationLakeFilterer.Filter(locations, [], [], @"County eq 'O\'Brien'", new(), [], [], new());
        result.Length.ShouldBe(1);
    }

    [Fact]
    public void Should_cache_compiled_parent_expressions()
    {
        var expression = "current:Year eq Year";
        var first = LocationLakeFilterer.CompileParentBoolLocationExpression(expression);
        var second = LocationLakeFilterer.CompileParentBoolLocationExpression(expression);
        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void Should_throw_on_invalid_property_name()
    {
        var ex = Should.Throw<ExpressionCompilationException>(
            () => LocationLakeFilterer.CompileExpression<Location, bool>("Bildings100 gt 5", true));
        ex.Message.ShouldContain("Unknown property 'Bildings100'");
        ex.Message.ShouldContain("Did you mean 'Buildings100'");
        ex.Position.ShouldBe(0);
    }

    [Fact]
    public void Should_throw_on_malformed_expression()
    {
        Should.Throw<ExpressionCompilationException>(
            () => LocationLakeFilterer.CompileExpression<Location, bool>("Buildings100 gt and", true));
    }

    [Fact]
    public void Should_throw_on_unterminated_string()
    {
        var ex = Should.Throw<ExpressionCompilationException>(
            () => LocationLakeFilterer.CompileExpression<Location, bool>("Surface eq 'gravel", true));
        ex.Message.ShouldContain("Unterminated string");
    }

    [Fact]
    public void Should_throw_on_unmatched_paren()
    {
        var ex = Should.Throw<ExpressionCompilationException>(
            () => LocationLakeFilterer.CompileExpression<Location, bool>("(Year gt 2020", true));
        ex.Message.ShouldContain("Unmatched '('");
    }

    [Fact]
    public void Should_throw_with_positional_error_for_unknown_property()
    {
        var ex = Should.Throw<ExpressionCompilationException>(
            () => LocationLakeFilterer.CompileExpression<Location, bool>("Buildings100 gt Bildings200", true));
        ex.Position.ShouldBe(16);
        ex.Length.ShouldBe(11);
        ex.Message.ShouldContain("Unknown property 'Bildings200'");
    }

    [Theory]
    [InlineData("Elevation gt -100", 1)]
    [InlineData("Elevation lt -100", 0)]
    [InlineData("Elevation gt -200 and Elevation lt 200", 1)]
    public void Should_filter_with_negative_numbers(string expression, int expectedCount)
    {
        var locations = LocationArrayWithData();
        var result = LocationLakeFilterer.Filter(locations, [], [], expression, new(), [], [], new());
        result.Length.ShouldBe(expectedCount);
    }

    private static MapCheckrLocation CreateMapCheckrLocation() => new()
    {
        lat = 59.9,
        lng = 10.7,
        heading = 90,
        year = 2023,
        month = 6,
        arrowCount = 1,
        drivingDirectionAngle = 180,
        elevation = 100,
        descriptionLength = 15,
        resolutionHeight = 6656,
        panoId = "abc123",
    };

    private static long _nextNodeId = 1000;

    private static Location CreateLocation(
        string countryCode, string subdivisionCode,
        int year = 2023, int arrowCount = 1, int buildings100 = 0) =>
        new()
        {
            NodeId = Interlocked.Increment(ref _nextNodeId),
            Osm = new OsmData { Buildings100 = buildings100 },
            Google = new GoogleData
            {
                PanoId = "",
                CountryCode = countryCode,
                ArrowCount = arrowCount,
                Year = year,
                DescriptionLength = 10,
            },
            Nominatim = new NominatimData
            {
                CountryCode = countryCode,
                SubdivisionCode = subdivisionCode
            }
        };
}
