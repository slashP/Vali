using Shouldly;
using Vali.Core.Google;
using Xunit;

namespace Vali.Core.Tests;

// Mutates the process-global GenerationDeterminism.Randomness; xUnit serializes tests within
// one class, and no other test reads this flag, so keep all flag-dependent tests here.
[Collection("StaticState")]
public class GenerationDeterminismTests
{
    private static Location Loc(long id) => new()
    {
        NodeId = id,
        Lat = 0,
        Lng = 0,
        Google = new GoogleData { PanoId = "", CountryCode = "NO" },
        Osm = new OsmData(),
        Nominatim = new NominatimData { CountryCode = "NO", SubdivisionCode = "NO-03" }
    };

    [Fact]
    public void Deterministic_reflects_randomness_mode()
    {
        try
        {
            GenerationDeterminism.Randomness = GenerationDeterminism.DistributionRandomness.None;
            GenerationDeterminism.Deterministic.ShouldBeTrue();
            GenerationDeterminism.Randomness = GenerationDeterminism.DistributionRandomness.Random;
            GenerationDeterminism.Deterministic.ShouldBeFalse();
        }
        finally { GenerationDeterminism.Randomness = GenerationDeterminism.DistributionRandomness.Random; }
    }

    [Fact]
    public void InCanonicalOrder_sorts_by_NodeId_when_deterministic()
    {
        try
        {
            GenerationDeterminism.Randomness = GenerationDeterminism.DistributionRandomness.None;
            var locs = new[] { Loc(3), Loc(1), Loc(2) };
            GenerationDeterminism.InCanonicalOrder(locs).Select(l => l.NodeId).ShouldBe(new long[] { 1, 2, 3 });
        }
        finally { GenerationDeterminism.Randomness = GenerationDeterminism.DistributionRandomness.Random; }
    }

    [Fact]
    public void InCanonicalOrder_preserves_input_order_when_random()
    {
        GenerationDeterminism.Randomness = GenerationDeterminism.DistributionRandomness.Random;
        var locs = new[] { Loc(3), Loc(1), Loc(2) };
        GenerationDeterminism.InCanonicalOrder(locs).Select(l => l.NodeId).ShouldBe(new long[] { 3, 1, 2 });
    }

    [Fact]
    public void Panning_rolls_are_seeded_per_pano_only_when_deterministic()
    {
        try
        {
            GenerationDeterminism.Randomness = GenerationDeterminism.DistributionRandomness.None;
            PanningResolver.RandomFor("abc").Next().ShouldBe(PanningResolver.SeededRandom("abc").Next());
            GenerationDeterminism.Randomness = GenerationDeterminism.DistributionRandomness.Random;
            PanningResolver.RandomFor("abc").ShouldBeSameAs(Random.Shared);
        }
        finally { GenerationDeterminism.Randomness = GenerationDeterminism.DistributionRandomness.Random; }
    }
}
