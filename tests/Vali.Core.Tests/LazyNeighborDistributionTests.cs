using Shouldly;
using Vali.Core.Neighbors;
using Xunit;

namespace Vali.Core.Tests;

[Collection("StaticState")]
public class LazyNeighborDistributionTests
{
    private const int Side = 20;
    private const double SpacingMetres = 200;
    private const double MetresPerDegree = 6371137.0 * Math.PI / 180.0;

    // A 20 x 20 grid 200 m apart around 60°N. With {radius 300, lte 5} an interior point has 8 neighbors
    // (4 at 200 m, 4 at 283 m) and drops out; an edge point has 5 and a corner 3, so the border ring stays.
    // The ring has the grid's bounding box, so the bisection's first probe is the same lazily and eagerly.
    private static Location[] Grid()
    {
        var dLat = SpacingMetres / MetresPerDegree;
        var dLng = SpacingMetres / (MetresPerDegree * Math.Cos(60.0 * Math.PI / 180.0));
        return Enumerable.Range(0, Side * Side).Select(i => Make(i + 1, 60.0 + i / Side * dLat, 10.0 + i % Side * dLng)).ToArray();
    }

    private static bool OnBorder(long nodeId)
    {
        var i = nodeId - 1;
        long row = i / Side, column = i % Side;
        return row == 0 || column == 0 || row == Side - 1 || column == Side - 1;
    }

    private static readonly NeighborFilter BorderFilter = new() { Radius = 300, Bound = "lte", Limit = 5 };

    [Fact]
    public void Lazy_map_neighbor_filter_places_the_same_locations_as_eager()
    {
        var definition = Definition() with { NeighborFilters = [BorderFilter] };

        var (eager, lazy) = RunBoth(Grid(), definition);

        eager.locations.Count.ShouldBe(60);
        eager.locations.ShouldAllBe(l => OnBorder(l.NodeId));
        lazy.locations.Select(l => l.NodeId).ShouldBe(eager.locations.Select(l => l.NodeId));
        lazy.regionGoalCount.ShouldBe(eager.regionGoalCount);
        lazy.minDistance.ShouldBe(eager.minDistance);
    }

    [Fact]
    public void Lazy_preference_neighbor_filter_places_the_same_locations_as_eager()
    {
        var definition = Definition() with
        {
            GlobalLocationPreferenceFilters =
            [
                new LocationPreferenceFilter { Expression = "Year gte 2000", Percentage = 50, LocationTag = "border", NeighborFilters = [BorderFilter] },
                new LocationPreferenceFilter { Expression = "Year gte 2000", Fill = true, LocationTag = "fill" },
            ]
        };

        var (eager, lazy) = RunBoth(Grid(), definition);

        eager.locations.Count(l => l.Tag == "border").ShouldBe(30);
        eager.locations.Where(l => l.Tag == "border").ShouldAllBe(l => OnBorder(l.NodeId));
        lazy.locations.Select(l => (l.NodeId, l.Tag)).ShouldBe(eager.locations.Select(l => (l.NodeId, l.Tag)));
        lazy.regionGoalCount.ShouldBe(eager.regionGoalCount);
        lazy.minDistance.ShouldBe(eager.minDistance);
    }

    [Fact]
    public void No_candidate_passing_a_lazy_map_filter_keeps_the_real_goal()
    {
        var definition = Definition() with { NeighborFilters = [new NeighborFilter { Radius = 1000, Bound = "none" }] };

        var (eager, lazy) = RunBoth(Grid(), definition);

        lazy.locations.ShouldBeEmpty();
        lazy.regionGoalCount.ShouldBe(60);
        lazy.minDistance.ShouldBe(0);
        (eager.locations.Count, eager.regionGoalCount, eager.minDistance).ShouldBe((0, 60, 0));
    }

    [Fact]
    public void Duplicate_NodeIds_fall_back_to_eager()
    {
        var grid = Grid();
        // A copy of border NodeId 2 in the dense interior, ahead of the border copy in the file. Eagerly the interior
        // copy fails the filter and the border copy survives DistinctBy; lazily DistinctBy would keep the interior one.
        var interior = grid[Side * 10 + 10];
        var duplicate = Make(2, interior.Lat + 1e-6, interior.Lng + 1e-6);
        var locations = new[] { duplicate }.Concat(grid).ToArray();
        var definition = Definition() with { NeighborFilters = [BorderFilter] };

        var (eager, lazy) = RunBoth(locations, definition);

        eager.locations.Single(l => l.NodeId == 2).Lat.ShouldBe(grid[1].Lat);
        lazy.locations.Select(l => (l.NodeId, l.Lat, l.Lng)).ShouldBe(eager.locations.Select(l => (l.NodeId, l.Lat, l.Lng)));
    }

    [Fact]
    public void Lazy_map_filter_evaluates_only_the_candidates_placement_reaches()
    {
        var definition = Definition() with { NeighborFilters = [BorderFilter] };
        var enabledBefore = GenPerf.Enabled;
        try
        {
            GenPerf.Enabled = true;
            GenPerf.Reset();
            RunOne(Grid(), definition, lazy: false, goalCount: 5);
            var eager = GenPerf.NeighborWork().evaluations;
            GenPerf.Reset();
            var result = RunOne(Grid(), definition, lazy: true, goalCount: 5);
            var lazy = GenPerf.NeighborWork().evaluations;

            eager.ShouldBe(Side * Side);
            result.locations.Count.ShouldBe(5);
            lazy.ShouldBeLessThan(eager / 2);
        }
        finally
        {
            GenPerf.Enabled = enabledBefore;
            GenPerf.Reset();
        }
    }

    [Fact]
    public void Lazy_preference_filter_evaluates_only_the_candidates_placement_reaches()
    {
        var definition = Definition() with
        {
            GlobalLocationPreferenceFilters =
            [
                new LocationPreferenceFilter { Expression = "Year gte 2000", Percentage = 50, LocationTag = "border", NeighborFilters = [BorderFilter] },
                new LocationPreferenceFilter { Expression = "Year gte 2000", Fill = true, LocationTag = "fill" },
            ]
        };
        var enabledBefore = GenPerf.Enabled;
        try
        {
            GenPerf.Enabled = true;
            GenPerf.Reset();
            RunOne(Grid(), definition, lazy: false, goalCount: 10);
            var eager = GenPerf.NeighborWork().evaluations;
            GenPerf.Reset();
            RunOne(Grid(), definition, lazy: true, goalCount: 10);
            var lazy = GenPerf.NeighborWork().evaluations;

            eager.ShouldBe(Side * Side);
            lazy.ShouldBeLessThan(eager / 2);
        }
        finally
        {
            GenPerf.Enabled = enabledBefore;
            GenPerf.Reset();
        }
    }

    private static MapDefinition Definition() => new()
    {
        SubdivisionDistribution = new() { ["XX"] = new() { ["XX-1"] = 1 } },
        DistributionStrategy = new DistributionStrategy { Key = DistributionStrategies.FixedCountByMaxMinDistance, LocationCountGoal = 60, MinMinDistance = 25 },
    };

    private static ((IList<Location> locations, int regionGoalCount, int minDistance) eager, (IList<Location> locations, int regionGoalCount, int minDistance) lazy)
        RunBoth(Location[] locations, MapDefinition definition)
    {
        var file = Path.GetTempFileName();
        var lazyBefore = NeighborEvaluation.Lazy;
        var randomnessBefore = GenerationDeterminism.Randomness;
        try
        {
            using (var stream = File.Create(file))
            {
                ProtoBuf.Serializer.Serialize(stream, locations);
            }

            GenerationDeterminism.Randomness = GenerationDeterminism.DistributionRandomness.None;
            NeighborEvaluation.Lazy = false;
            var eager = DistributionStrategies.SubdivisionByMaxMinDistance("XX", file, goalCount: 60, ["XX-1"], definition);
            NeighborEvaluation.Lazy = true;
            var lazy = DistributionStrategies.SubdivisionByMaxMinDistance("XX", file, goalCount: 60, ["XX-1"], definition);
            return (eager, lazy);
        }
        finally
        {
            NeighborEvaluation.Lazy = lazyBefore;
            GenerationDeterminism.Randomness = randomnessBefore;
            File.Delete(file);
        }
    }

    private static (IList<Location> locations, int regionGoalCount, int minDistance) RunOne(Location[] locations, MapDefinition definition, bool lazy, int goalCount)
    {
        var file = Path.GetTempFileName();
        var lazyBefore = NeighborEvaluation.Lazy;
        var randomnessBefore = GenerationDeterminism.Randomness;
        try
        {
            using (var stream = File.Create(file))
            {
                ProtoBuf.Serializer.Serialize(stream, locations);
            }

            GenerationDeterminism.Randomness = GenerationDeterminism.DistributionRandomness.None;
            NeighborEvaluation.Lazy = lazy;
            return DistributionStrategies.SubdivisionByMaxMinDistance("XX", file, goalCount, ["XX-1"], definition);
        }
        finally
        {
            NeighborEvaluation.Lazy = lazyBefore;
            GenerationDeterminism.Randomness = randomnessBefore;
            File.Delete(file);
        }
    }

    private static Location Make(long nodeId, double lat, double lng) => new()
    {
        NodeId = nodeId,
        Lat = lat,
        Lng = lng,
        Google = new GoogleData { PanoId = "", CountryCode = "XX", Lat = lat, Lng = lng, Year = 2020 },
        Osm = new OsmData(),
        Nominatim = new NominatimData { CountryCode = "XX", SubdivisionCode = "XX-1" }
    };
}
