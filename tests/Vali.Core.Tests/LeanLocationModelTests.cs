using System.IO;
using System.Linq;
using Shouldly;
using Vali.Core.LeanDeserialization;
using Xunit;

namespace Vali.Core.Tests;

public class LeanLocationModelTests
{
    private static Location FullLocation() => new()
    {
        NodeId = 42,
        Lat = 1.5,
        Lng = 2.5,
        Google = new GoogleData { PanoId = "p", Lat = 1.5, Lng = 2.5, Year = 2023, CountryCode = "NO" },
        Osm = new OsmData { Tunnels10 = 0, Buildings100 = 5, Surface = "asphalt", WayIds = [10L, 20L] },
        Nominatim = new NominatimData { CountryCode = "NO", SubdivisionCode = "NO-03", County = "Oslo" },
    };

    [Fact]
    public void Lean_model_decodes_registered_fields_and_skips_the_rest()
    {
        // Write with the full default model.
        using var ms = new MemoryStream();
        ProtoBuf.Serializer.Serialize(ms, new[] { FullLocation() });
        ms.Position = 0;

        // Lean model: enough to be valid output, deliberately omitting Osm.Buildings100/Surface/WayIds and Nominatim.County.
        ProtoField[] fields =
        [
            new(typeof(Location), 1, nameof(Location.NodeId)),
            new(typeof(Location), 2, nameof(Location.Lat)),
            new(typeof(Location), 3, nameof(Location.Lng)),
            new(typeof(Location), 4, nameof(Location.Google)),
            new(typeof(Location), 5, nameof(Location.Osm)),
            new(typeof(Location), 6, nameof(Location.Nominatim)),
            new(typeof(GoogleData), 1, nameof(GoogleData.PanoId)),
            new(typeof(GoogleData), 7, nameof(GoogleData.Year)),
            new(typeof(OsmData), 10, nameof(OsmData.Tunnels10)),
            new(typeof(NominatimData), 1, nameof(NominatimData.CountryCode)),
            new(typeof(NominatimData), 2, nameof(NominatimData.SubdivisionCode)),
        ];
        var model = LeanLocationModel.Build(fields);

        var result = (Location[])model.Deserialize(ms, null, typeof(Location[]));
        var loc = result.Single();

        // Kept:
        loc.NodeId.ShouldBe(42L);
        loc.Google.Year.ShouldBe(2023);
        loc.Google.PanoId.ShouldBe("p");
        loc.Nominatim.CountryCode.ShouldBe("NO");
        // Skipped → CLR defaults (no allocation/decode):
        loc.Osm.Buildings100.ShouldBe(0);
        loc.Osm.Surface.ShouldBeNull();
        (loc.Osm.WayIds?.Length ?? 0).ShouldBe(0); // stripped -> null or [] depending on protobuf-net construction
        loc.Nominatim.County.ShouldBeNull();
    }

    [Fact]
    public void For_returns_null_when_kill_switch_set()
    {
        var prior = Environment.GetEnvironmentVariable("VALI_LEAN_DESERIALIZE");
        try
        {
            Environment.SetEnvironmentVariable("VALI_LEAN_DESERIALIZE", "0");
            LeanLocationModel.For(new MapDefinition { GlobalLocationFilter = "Year gte 2018" }).ShouldBeNull();
        }
        finally { Environment.SetEnvironmentVariable("VALI_LEAN_DESERIALIZE", prior); }
    }

    [Fact]
    public void For_caches_models_by_read_set_signature()
    {
        var a = LeanLocationModel.For(new MapDefinition { GlobalLocationFilter = "Year gte 2018" });
        var b = LeanLocationModel.For(new MapDefinition { GlobalLocationFilter = "Year gte 2019" }); // same read-set
        a.ShouldNotBeNull();
        a.ShouldBeSameAs(b);
    }

    [Fact]
    public void For_round_trips_a_baseline_only_map()
    {
        using var ms = new MemoryStream();
        ProtoBuf.Serializer.Serialize(ms, new[] { FullLocation() });
        ms.Position = 0;
        var model = LeanLocationModel.For(new MapDefinition { GlobalLocationFilter = "Year gte 2018" })!;
        var loc = ((Location[])model.Deserialize(ms, null, typeof(Location[]))).Single();
        loc.Google.Year.ShouldBe(2023);   // kept (baseline)
        (loc.Osm.WayIds?.Length ?? 0).ShouldBe(0); // stripped (no WayId reference)
        loc.Nominatim.County.ShouldBeNull(); // stripped
    }
}
