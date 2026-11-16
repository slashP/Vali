using Shouldly;
using Vali.Core.Expressions;
using Vali.Core.Neighbors;
using Xunit;
using static Vali.Core.Tests.NeighborGridTests;

namespace Vali.Core.Tests;

public class NeighborCheckTests
{
    public static IEnumerable<object?[]> Matrix()
    {
        var bounds = new (string bound, int? limit)[]
        {
            ("some", null), ("gte", 0), ("gte", 1), ("gte", 3), ("gte", null), ("lte", 0), ("lte", 2), ("lte", null),
            ("none", null), ("all", null), ("percentage-gte", 50), ("percentage-gte", 75), ("percentage-lte", 25),
            ("percentage-gte", null),
        };
        var expressions = new[] { "", "*", "Year gte 2018", "current:Year gt Year", "Year gte 2015 and current:Year lte Year" };
        foreach (var (bound, limit) in bounds)
        foreach (var directions in new[] { false, true })
        {
            if (bound == "lte" && limit is null && directions)
            {
                continue; // throws; covered by Invalid_combinations_throw_when_the_check_is_created
            }

            foreach (var expression in expressions)
            foreach (var radius in new[] { 120, 500, 2000 })
            foreach (var lat in new[] { 59.9, 78.2 })
            {
                yield return [bound, limit, directions, expression, radius, lat];
            }
        }
    }

    [Theory]
    [MemberData(nameof(Matrix))]
    public void Matches_the_brute_force_reference(string bound, int? limit, bool directions, string expression, int radius, double lat)
    {
        var locations = RandomSquare(600, lat, 10.0, radius * 8, seed: radius + (int)lat);
        var filter = new NeighborFilter
        {
            Bound = bound,
            Limit = limit,
            CheckEachCardinalDirectionSeparately = directions,
            Expression = expression,
            Radius = radius,
        };

        var kept = NeighborFilterer.FilterByNeighbors(locations, new NeighborIndex(locations), filter)
            .Select(l => l.NodeId).OrderBy(x => x).ToArray();

        kept.ShouldBe(NeighborReference.Filter(locations, filter));
    }

    [Fact]
    public void Neighbor_just_inside_the_radius_counts_and_just_outside_does_not()
    {
        const int radius = 300;
        const double lat = 60.0;
        var r2 = (double)radius * radius;
        // Bisect the latitude offset where PointsAreCloserThan flips, straight north of the candidate.
        double inside = 0, outside = 0.01;
        for (var i = 0; i < 200; i++)
        {
            var mid = (inside + outside) / 2;
            if (Extensions.PointsAreCloserThan(lat, 10.0, lat + mid, 10.0, r2)) inside = mid; else outside = mid;
        }

        var some = new NeighborFilter { Bound = "some", Radius = radius };
        Kept([MakeLocation(1, lat, 10.0), MakeLocation(2, lat + inside - 1e-9, 10.0)], some).ShouldContain(1L);
        Kept([MakeLocation(1, lat, 10.0), MakeLocation(2, lat + outside + 1e-9, 10.0)], some).ShouldNotContain(1L);
    }

    [Fact]
    public void A_location_is_not_its_own_neighbor()
    {
        Location[] twins = [MakeLocation(1, 59.9, 10.7), MakeLocation(1, 59.9, 10.7)];

        Kept(twins, new NeighborFilter { Bound = "some", Radius = 500 }).ShouldBeEmpty();
        Kept(twins, new NeighborFilter { Bound = "none", Radius = 500 }).ShouldBe([1L, 1L]);
    }

    [Theory]
    [InlineData("all", null, false)]
    [InlineData("none", null, true)]
    [InlineData("percentage-lte", 0, true)]
    [InlineData("percentage-gte", 0, true)]
    [InlineData("percentage-gte", 1, false)]
    public void Empty_neighborhood_per_bound(string bound, int? limit, bool keeps)
    {
        Location[] isolated = [MakeLocation(1, 59.9, 10.7), MakeLocation(2, 61.0, 12.0)];

        Kept(isolated, new NeighborFilter { Bound = bound, Limit = limit, Radius = 500 }).Contains(1L).ShouldBe(keeps);
    }

    [Theory]
    [InlineData("gt", 1, false)]
    [InlineData("lte", null, true)]
    [InlineData("lte", -1, true)]
    public void Invalid_combinations_throw_when_the_check_is_created(string bound, int? limit, bool directions)
    {
        var filter = new NeighborFilter { Bound = bound, Limit = limit, CheckEachCardinalDirectionSeparately = directions, Radius = 500 };

        Should.Throw<InvalidOperationException>(() => NeighborCheck.For([filter], new NeighborIndex()))
            .Message.ShouldStartWith("Neighbor filter combination is not valid/implemented.");
        Should.Throw<InvalidOperationException>(() => NeighborFilterer.FilterByNeighbors([], new NeighborIndex(), filter));
    }

    [Fact]
    public void For_returns_null_without_filters()
    {
        NeighborCheck.For([], new NeighborIndex()).ShouldBeNull();
    }

    [Fact]
    public void Passes_is_the_and_of_every_filter()
    {
        var locations = RandomSquare(300, 59.9, 10.7, 4000, seed: 11);
        var index = new NeighborIndex(locations);
        NeighborFilter gte = new() { Bound = "gte", Limit = 3, Radius = 500 };
        NeighborFilter recent = new() { Bound = "percentage-gte", Limit = 50, Radius = 300, Expression = "Year gte 2016" };

        var check = NeighborCheck.For([gte, recent], index)!;

        var expected = NeighborReference.Filter(locations, gte).Intersect(NeighborReference.Filter(locations, recent)).OrderBy(x => x);
        locations.Where(check.Passes).Select(l => l.NodeId).OrderBy(x => x).ShouldBe(expected);
    }

    [Fact]
    public void Empty_index_with_filters_keeps_none_and_drops_some()
    {
        var locations = RandomSquare(50, 59.9, 10.7, 2000, seed: 5);

        LocationLakeFilterer.Filter(locations, new(), [], "", new(), [], [new NeighborFilter { Bound = "some", Radius = 500 }], new())
            .ShouldBeEmpty();
        LocationLakeFilterer.Filter(locations, new(), [], "", new(), [], [new NeighborFilter { Bound = "none", Radius = 500 }], new())
            .Length.ShouldBe(locations.Length);
    }

    [Theory]
    [InlineData("Year gte 2018", "Year gte 2018", true)]
    [InlineData("Year gte 2018 and current:Year gt Year", "Year gte 2018", false)]
    [InlineData("current:Year gt Year", null, false)]
    [InlineData("*", null, false)]
    [InlineData("", null, false)]
    public void Decompose_reports_whether_the_neighbor_only_part_is_whole(string expression, string? neighborOnly, bool whole)
    {
        NeighborFilterDecomposer.Decompose(expression).ShouldBe(new NeighborExpressionParts(neighborOnly, whole));
    }

    private static long[] Kept(Location[] locations, NeighborFilter filter) =>
        NeighborFilterer.FilterByNeighbors(locations, new NeighborIndex(locations), filter).Select(l => l.NodeId).OrderBy(x => x).ToArray();
}
