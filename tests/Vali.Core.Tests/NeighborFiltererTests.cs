using Shouldly;
using Vali.Core.Google;
using Xunit;

namespace Vali.Core.Tests;

public class NeighborFiltererTests
{
    // Four mutually-neighbouring locations: all within ~150 m, well inside the 1 km filter radius,
    // so every location is a candidate-neighbour of every other.
    //   NodeId 1 (A): Year 2023, Quality "Good"
    //   NodeId 2 (B): Year 2020, Quality "Good"
    //   NodeId 3 (C): Year 2025, Quality "Bad"
    //   NodeId 4 (D): Year 2010, Quality missing
    private static Location[] Cluster() =>
    [
        MakeLocation(1, 59.9000, 10.7000, year: 2023, quality: "Good"),
        MakeLocation(2, 59.9010, 10.7000, year: 2020, quality: "Good"),
        MakeLocation(3, 59.9000, 10.7010, year: 2025, quality: "Bad"),
        MakeLocation(4, 59.9010, 10.7010, year: 2010, quality: null),
    ];

    [Fact]
    public void Should_keep_candidates_with_a_qualifying_neighbor_when_prefiltering()
    {
        // Safe bound "some" + a decomposable expression -> the pre-filter is active.
        // Neighbor-only part P = "external:Quality eq 'Good'"; the cross part is "current:Year gt Year"
        // (candidate.Year > neighbor.Year). A candidate is kept iff some other neighbour has
        // Quality 'Good' AND a strictly smaller Year.
        var kept = Filter("external:Quality eq 'Good' and current:Year gt Year");

        // A(2023) kept via Good neighbour B(2020); C(2025) kept via Good neighbour A(2023).
        // B(2020) has no older Good neighbour (only A is Good, and 2023 >= 2020); D(2010) has none older.
        // The pre-filter drops C and D (not Good) from the buckets without changing this result.
        kept.ShouldBe(new long[] { 1, 3 });
    }

    [Fact]
    public void Should_fall_back_to_full_scan_when_expression_has_no_neighbor_only_part()
    {
        // Pure current: expression -> NeighborOnlyExpression returns null -> no pre-filter,
        // the original bucket set is scanned. Candidate kept iff some neighbour has a smaller Year.
        var kept = Filter("current:Year gt Year");

        // A(2023)>B(2020), B(2020)>D(2010), C(2025)>anything -> kept. D(2010) has no older neighbour.
        kept.ShouldBe(new long[] { 1, 2, 3 });
    }

    private static long[] Filter(string expression)
    {
        var locations = Cluster();
        var filter = new NeighborFilter
        {
            Expression = expression,
            Bound = "some",
            Radius = 1000,
        };
        var mapDefinition = new MapDefinition { NeighborFilters = [filter] };
        var buckets = LocationLookupService.Bucketize(locations, mapDefinition.HashPrecisionFromNeighborFiltersRadius());

        return NeighborFilterer.FilterByNeighbors(locations, buckets, filter, mapDefinition)
            .Select(l => l.NodeId)
            .OrderBy(id => id)
            .ToArray();
    }

    private static Location MakeLocation(long nodeId, double lat, double lng, int year, string? quality)
    {
        var location = new Location
        {
            NodeId = nodeId,
            Lat = lat,
            Lng = lng,
            Osm = new OsmData(),
            Google = new GoogleData
            {
                PanoId = "",
                CountryCode = "NO",
                Lat = lat,
                Lng = lng,
                Year = year,
            },
            Nominatim = new NominatimData
            {
                CountryCode = "NO",
                SubdivisionCode = "NO-03",
            },
        };

        if (quality != null)
        {
            location.ExternalData["Quality"] = quality;
        }

        return location;
    }
}
