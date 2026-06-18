using System.Linq;
using Shouldly;
using Vali.Core.Expressions;
using Vali.Core.LeanDeserialization;
using Xunit;

namespace Vali.Core.Tests;

public class LocationFieldRegistryTests
{
    [Fact]
    public void Baseline_includes_output_and_default_filter_fields()
    {
        var b = LocationFieldRegistry.Baseline;
        b.ShouldContain(new ProtoField(typeof(Location), 1, nameof(Location.NodeId)));
        b.ShouldContain(new ProtoField(typeof(Location), 4, nameof(Location.Google)));
        b.ShouldContain(new ProtoField(typeof(Location), 5, nameof(Location.Osm)));
        b.ShouldContain(new ProtoField(typeof(GoogleData), 1, nameof(GoogleData.PanoId)));
        b.ShouldContain(new ProtoField(typeof(GoogleData), 14, nameof(GoogleData.ResolutionHeight)));
        b.ShouldContain(new ProtoField(typeof(OsmData), 10, nameof(OsmData.Tunnels10)));
        b.ShouldContain(new ProtoField(typeof(NominatimData), 1, nameof(NominatimData.CountryCode)));
    }

    [Fact]
    public void Baseline_excludes_always_strip_candidates()
    {
        var b = LocationFieldRegistry.Baseline;
        b.ShouldNotContain(new ProtoField(typeof(GoogleData), 6, nameof(GoogleData.CheckedAt)));
        b.ShouldNotContain(new ProtoField(typeof(GoogleData), 5, nameof(GoogleData.CountryCode)));
        b.ShouldNotContain(new ProtoField(typeof(NominatimData), 3, nameof(NominatimData.County)));
        b.ShouldNotContain(new ProtoField(typeof(OsmData), 20, nameof(OsmData.WayIds)));
    }

    [Fact]
    public void Computed_properties_map_to_backing_fields()
    {
        LocationFieldRegistry.PropertyToFields["WayId"].ShouldBe(new[] { new ProtoField(typeof(OsmData), 20, nameof(OsmData.WayIds)) });
        LocationFieldRegistry.PropertyToFields["HighwayType"].ShouldBe(new[] { new ProtoField(typeof(OsmData), 16, nameof(OsmData.RoadType)) });
        LocationFieldRegistry.PropertyToFields["HighwayTypeCount"].ShouldBe(new[] { new ProtoField(typeof(OsmData), 16, nameof(OsmData.RoadType)) });
        LocationFieldRegistry.PropertyToFields["Heading"].ShouldBe(new[] { new ProtoField(typeof(GoogleData), 4, nameof(GoogleData.DefaultHeading)) });
    }

    [Fact]
    public void Every_resolver_property_has_a_field_mapping()
    {
        foreach (var name in PropertyResolver.ForLocation().ValidPropertyNames)
        {
            LocationFieldRegistry.PropertyToFields.Keys.ShouldContain(name);
        }
    }
}
