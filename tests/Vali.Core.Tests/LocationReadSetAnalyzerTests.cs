using System.Linq;
using Shouldly;
using Vali.Core.LeanDeserialization;
using Xunit;

namespace Vali.Core.Tests;

public class LocationReadSetAnalyzerTests
{
    private static readonly ProtoField WayIds = new(typeof(OsmData), 20, nameof(OsmData.WayIds));
    private static readonly ProtoField Surface = new(typeof(OsmData), 13, nameof(OsmData.Surface));
    private static readonly ProtoField County = new(typeof(NominatimData), 3, nameof(NominatimData.County));
    private static readonly ProtoField Buildings100 = new(typeof(OsmData), 3, nameof(OsmData.Buildings100));
    private static readonly ProtoField RoadType = new(typeof(OsmData), 16, nameof(OsmData.RoadType));

    [Fact]
    public void Baseline_only_map_strips_wayids_surface_county()
    {
        var set = LocationReadSetAnalyzer.Analyze(new MapDefinition { GlobalLocationFilter = "Year gte 2018" });
        set.ShouldNotContain(WayIds);
        set.ShouldNotContain(Surface);
        set.ShouldNotContain(County);
        set.ShouldContain(new ProtoField(typeof(GoogleData), 7, nameof(GoogleData.Year))); // baseline
    }

    [Fact]
    public void Global_filter_referencing_osm_includes_those_fields()
    {
        var set = LocationReadSetAnalyzer.Analyze(new MapDefinition { GlobalLocationFilter = "Buildings100 eq 0 and Roads50 eq 1" });
        set.ShouldContain(Buildings100);
        set.ShouldContain(new ProtoField(typeof(OsmData), 7, nameof(OsmData.Roads50)));
    }

    [Fact]
    public void Neighbor_wayid_expression_includes_wayids()
    {
        var def = new MapDefinition
        {
            NeighborFilters = [new NeighborFilter { Expression = "current:WayId eq WayId", Bound = "some", Radius = 2000 }],
        };
        LocationReadSetAnalyzer.Analyze(def).ShouldContain(WayIds);
    }

    [Fact]
    public void Highwaytype_tag_includes_roadtype()
    {
        var def = new MapDefinition { Output = new LocationOutput { LocationTags = ["HighwayType"] } };
        LocationReadSetAnalyzer.Analyze(def).ShouldContain(RoadType);
    }

    [Fact]
    public void County_tag_includes_county()
    {
        var def = new MapDefinition { Output = new LocationOutput { LocationTags = ["County"] } };
        LocationReadSetAnalyzer.Analyze(def).ShouldContain(County);
    }

    [Fact]
    public void Per_country_and_subdivision_filters_are_unioned()
    {
        var def = new MapDefinition
        {
            CountryLocationFilters = new() { ["FR"] = "Surface eq 'asphalt'" },
            SubdivisionLocationFilters = new() { ["US"] = new() { ["US-CA"] = "WayId eq 1" } },
        };
        var set = LocationReadSetAnalyzer.Analyze(def);
        set.ShouldContain(Surface);
        set.ShouldContain(WayIds);
    }

    [Fact]
    public void Heading_expression_fields_are_included()
    {
        var def = new MapDefinition { Output = new LocationOutput { GlobalHeadingExpression = "DrivingDirectionAngle" } };
        LocationReadSetAnalyzer.Analyze(def).ShouldContain(new ProtoField(typeof(GoogleData), 9, nameof(GoogleData.DrivingDirectionAngle)));
    }
}
