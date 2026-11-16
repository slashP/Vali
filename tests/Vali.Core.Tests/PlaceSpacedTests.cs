using Shouldly;
using Vali.Core;
using Xunit;

namespace Vali.Core.Tests;

public class PlaceSpacedTests
{
    private static Location[] UniformGrid(int count, int seed, double latSpan = 2.0, double lngSpan = 2.0)
    {
        var rng = new Random(seed);
        return Enumerable.Range(0, count).Select(i => new Location
        {
            NodeId = i,
            Lat = 59.0 + rng.NextDouble() * latSpan,
            Lng = 10.0 + rng.NextDouble() * lngSpan,
            Google = new GoogleData { PanoId = "", CountryCode = "NO" },
            Osm = new OsmData(),
            Nominatim = new NominatimData { CountryCode = "NO", SubdivisionCode = "NO-03" }
        }).ToArray();
    }

    private static double MinPairwiseMetres(IList<Location> locs)
    {
        var min = double.MaxValue;
        for (var i = 0; i < locs.Count; i++)
        for (var j = i + 1; j < locs.Count; j++)
            min = Math.Min(min, Extensions.ApproximateDistance(locs[i].Lat, locs[i].Lng, locs[j].Lat, locs[j].Lng));
        return min;
    }

    [Theory]
    [InlineData(2000, 500)]
    [InlineData(2000, 1000)]
    [InlineData(5000, 2000)]
    public void respects_min_distance(int d, int goal)
    {
        var locs = UniformGrid(20_000, 1);
        var result = LocationDistributor.PlaceSpaced<Location, long>(locs, goal, d);
        result.Count.ShouldBeLessThanOrEqualTo(goal);
        if (result.Count > 1)
            MinPairwiseMetres(result).ShouldBeGreaterThanOrEqualTo(d - 0.001);
    }

    [Fact]
    public void caps_at_goal_when_plentiful()
    {
        var locs = UniformGrid(20_000, 2);
        var result = LocationDistributor.PlaceSpaced<Location, long>(locs, 100, 200);
        result.Count.ShouldBe(100);
    }

    [Fact]
    public void count_is_monotone_non_increasing_in_distance()
    {
        var locs = UniformGrid(20_000, 3);
        var prev = int.MaxValue;
        foreach (var d in new[] { 200, 500, 1000, 2000, 5000, 10000 })
        {
            var count = LocationDistributor.PlaceSpaced<Location, long>(locs, 1_000_000, d).Count;
            count.ShouldBeLessThanOrEqualTo(prev);
            prev = count;
        }
    }

    [Fact]
    public void honors_locations_already_in_map()
    {
        var locs = UniformGrid(20_000, 4);
        // Block everything within 5km of the box centre.
        var alreadyInMap = new ILatLng[] { new SimplePoint(60.0, 11.0) };
        const int d = 5000;
        var result = LocationDistributor.PlaceSpaced<Location, long>(locs, 1_000_000, d, alreadyInMap);
        foreach (var loc in result)
            Extensions.ApproximateDistance(loc.Lat, loc.Lng, 60.0, 11.0).ShouldBeGreaterThanOrEqualTo(d - 0.001);
    }

    [Fact]
    public void deterministic_for_fixed_order()
    {
        var locs = UniformGrid(10_000, 5);
        var a = LocationDistributor.PlaceSpaced<Location, long>(locs, 500, 1000);
        var b = LocationDistributor.PlaceSpaced<Location, long>(locs, 500, 1000);
        a.Select(x => x.NodeId).ShouldBe(b.Select(x => x.NodeId));
    }

    [Fact]
    public void Lazy_accept_gives_the_same_sequence_as_filtering_first()
    {
        var ordered = UniformGrid(5000, seed: 7);
        Func<Location, bool> accept = l => l.NodeId % 3 != 0;
        var filtered = ordered.Where(accept).ToArray();
        var already = UniformGrid(50, seed: 8).Select(l => (ILatLng)new SimplePoint(l.Lat, l.Lng)).ToArray();

        foreach (var alreadyInMap in new IReadOnlyCollection<ILatLng>?[] { null, already })
        {
            var memo = new sbyte[ordered.Length];
            foreach (var d in new[] { 100, 400, 50, 1000, 400 })
            {
                var lazy = LocationDistributor.PlaceSpaced<Location, long>(ordered, 300, d, alreadyInMap, accept, memo);
                var eager = LocationDistributor.PlaceSpaced<Location, long>(filtered, 300, d, alreadyInMap);
                lazy.Select(x => x.NodeId).ShouldBe(eager.Select(x => x.NodeId), $"d={d}");
            }
        }
    }

    [Fact]
    public void Accept_runs_at_most_once_per_candidate_across_probes()
    {
        var ordered = UniformGrid(5000, seed: 9);
        var seen = new System.Collections.Concurrent.ConcurrentDictionary<long, byte>();
        var calls = 0;
        var twice = 0;
        bool Counting(Location l)
        {
            if (!seen.TryAdd(l.NodeId, 0)) Interlocked.Increment(ref twice);
            Interlocked.Increment(ref calls);
            return l.NodeId % 2 == 0;
        }

        var (locations, _) = LocationDistributor.WithMaxMinDistance<Location, long>(
            ordered, 500, locationProbability: new(), avoidShuffle: true, minMinDistance: 25, accept: Counting);

        locations.Count.ShouldBe(500);
        locations.ShouldAllBe(l => l.NodeId % 2 == 0);
        twice.ShouldBe(0);
        calls.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Accept_runs_only_for_candidates_that_pass_spacing()
    {
        var ordered = UniformGrid(5000, seed: 10);
        const int d = 20_000;
        var r2 = (double)d * d;
        var already = UniformGrid(20, seed: 12).Select(l => (ILatLng)new SimplePoint(l.Lat, l.Lng)).ToArray();
        var asked = new System.Collections.Concurrent.ConcurrentBag<Location>();
        bool Accept(Location l)
        {
            asked.Add(l);
            return l.NodeId % 2 == 0;
        }

        var result = LocationDistributor.PlaceSpaced<Location, long>(ordered, 10, d, already, Accept, null);

        asked.Count.ShouldBeLessThan(5000);
        // A point already in the map blocks a candidate before accept is asked about it.
        asked.ShouldAllBe(l => already.All(p => !Extensions.PointsAreCloserThan(p.Lat, p.Lng, l.Lat, l.Lng, r2)));
        result.Select(l => l.NodeId).ShouldBe(
            LocationDistributor.PlaceSpaced<Location, long>(ordered.Where(l => l.NodeId % 2 == 0).ToArray(), 10, d, already).Select(l => l.NodeId));
    }

    [Fact]
    public void Expensive_accept_runs_in_parallel_batches()
    {
        // Every candidate passes spacing at 25 m on this sparse set and only 1 in 20 is accepted, so placement asks
        // about hundreds of candidates in a row: those are evaluated several at a time.
        var ordered = UniformGrid(2000, seed: 13);
        var running = 0;
        var maxRunning = 0;
        bool Slow(Location l)
        {
            var now = Interlocked.Increment(ref running);
            int seen;
            while ((seen = Volatile.Read(ref maxRunning)) < now && Interlocked.CompareExchange(ref maxRunning, now, seen) != seen)
            {
            }

            Thread.Sleep(1);
            Interlocked.Decrement(ref running);
            return l.NodeId % 20 == 0;
        }

        var lazy = LocationDistributor.PlaceSpaced<Location, long>(ordered, 50, 25, null, Slow, null);

        lazy.Select(l => l.NodeId).ShouldBe(LocationDistributor.PlaceSpaced<Location, long>(ordered.Where(l => l.NodeId % 20 == 0).ToArray(), 50, 25).Select(l => l.NodeId));
        maxRunning.ShouldBeGreaterThan(1);
    }

    [Theory]
    [InlineData(25)]
    [InlineData(500)]
    [InlineData(3000)]
    public void Batched_accept_gives_the_same_sequence_as_filtering_first_at_a_low_pass_rate(int d)
    {
        var ordered = UniformGrid(8000, seed: 14);
        Func<Location, bool> accept = l => l.NodeId % 10 == 3;
        var already = UniformGrid(30, seed: 15).Select(l => (ILatLng)new SimplePoint(l.Lat, l.Lng)).ToArray();
        var memo = new sbyte[ordered.Length];

        foreach (var goal in new[] { 5, 60, 400 })
        {
            LocationDistributor.PlaceSpaced<Location, long>(ordered, goal, d, already, accept, memo).Select(l => l.NodeId)
                .ShouldBe(LocationDistributor.PlaceSpaced<Location, long>(ordered.Where(accept).ToArray(), goal, d, already).Select(l => l.NodeId), $"goal={goal}");
        }
    }

    [Fact]
    public void WithMaxMinDistance_lazy_equals_eager_when_the_bounding_box_is_the_same()
    {
        var locations = UniformGrid(5000, seed: 11);
        var extremes = new[]
        {
            locations.MinBy(l => l.Lat)!.NodeId, locations.MaxBy(l => l.Lat)!.NodeId,
            locations.MinBy(l => l.Lng)!.NodeId, locations.MaxBy(l => l.Lng)!.NodeId,
        }.ToHashSet();
        Func<Location, bool> accept = l => extremes.Contains(l.NodeId) || l.NodeId % 5 >= 2; // rejects 40 %

        foreach (var goal in new[] { 10, 100, 400, 1500 })
        {
            var lazy = LocationDistributor.WithMaxMinDistance<Location, long>(locations, goal, new(), avoidShuffle: true, minMinDistance: 25, accept: accept);
            var eager = LocationDistributor.WithMaxMinDistance<Location, long>(locations.Where(accept).ToArray(), goal, new(), avoidShuffle: true, minMinDistance: 25);
            lazy.locations.Select(l => l.NodeId).ShouldBe(eager.locations.Select(l => l.NodeId), $"goal={goal}");
            lazy.minDistance.ShouldBe(eager.minDistance, $"goal={goal}");
        }
    }

    private sealed record SimplePoint(double Lat, double Lng) : ILatLng;
}
