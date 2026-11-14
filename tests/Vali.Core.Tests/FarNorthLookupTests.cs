using Shouldly;
using Vali.Core.Hash;
using Xunit;

namespace Vali.Core.Tests;

// A geohash cell's width in metres shrinks with the cosine of its latitude. Lookups that only search a cell's
// 8 neighbours miss points within the radius once cells get narrower than the radius: from ~66° N for a
// 500 m radius on 1x1 cells and a 2,000 m radius on 5x5 cells.
public class FarNorthLookupTests
{
    private const double MetersPerDegree = 111_199.0;

    [Theory]
    [InlineData(60.0, HashPrecision.Size_km_1x1, 500)]
    [InlineData(0.0, HashPrecision.Size_km_5x5, 2000)]
    [InlineData(-45.0, HashPrecision.Size_km_39x20, 15000)]
    public void NeighborsWithin_is_the_eight_neighbours_where_cells_are_wider_than_the_radius(double lat, HashPrecision precision, int radius)
    {
        var hash = Hasher.Encode(lat, 10.0, precision);

        Hasher.NeighborsWithin(hash, radius).ShouldBe(Hasher.Neighbors(hash));
    }

    [Fact]
    public void NeighborsWithin_reaches_two_cells_east_and_west_at_70_degrees_north()
    {
        var hash = Hasher.Encode(70.0, 10.0, HashPrecision.Size_km_1x1);
        var box = Hasher.GetBoundingBox(hash);
        var width = box.MaxLng - box.MinLng;
        var centreLng = (box.MinLng + box.MaxLng) / 2;

        var cells = Hasher.NeighborsWithin(hash, 500);

        cells.ShouldContain(Hasher.Encode(70.0, centreLng + 2 * width, HashPrecision.Size_km_1x1));
        cells.ShouldContain(Hasher.Encode(70.0, centreLng - 2 * width, HashPrecision.Size_km_1x1));
    }

    [Fact]
    public void NeighborsWithin_reaches_two_cells_north_and_south_when_the_radius_exceeds_the_cell_height()
    {
        var hash = Hasher.Encode(0.0, 10.0, HashPrecision.Size_km_5x5);
        var box = Hasher.GetBoundingBox(hash);
        var height = box.MaxLat - box.MinLat;
        var centreLat = (box.MinLat + box.MaxLat) / 2;

        var cells = Hasher.NeighborsWithin(hash, 6000);

        cells.ShouldContain(Hasher.Encode(centreLat + 2 * height, 10.0, HashPrecision.Size_km_5x5));
        cells.ShouldContain(Hasher.Encode(centreLat - 2 * height, 10.0, HashPrecision.Size_km_5x5));
    }

    [Theory]
    [InlineData(70.0, 500)]
    [InlineData(89.99, 2000)]
    public void NeighborsWithin_lists_each_cell_once_and_never_the_cell_itself(double lat, int radius)
    {
        var hash = Hasher.Encode(lat, 10.0, HashPrecision.Size_km_1x1);

        var cells = Hasher.NeighborsWithin(hash, radius);

        cells.ShouldBeUnique();
        cells.ShouldNotContain(hash);
    }

    [Theory]
    [InlineData(HashPrecision.Size_km_156x156)]
    [InlineData(HashPrecision.Size_km_39x20)]
    [InlineData(HashPrecision.Size_km_5x5)]
    [InlineData(HashPrecision.Size_km_1x1)]
    public void NeighborsWithin_covers_every_point_the_distance_check_accepts(HashPrecision precision)
    {
        var random = new Random(42);
        int[] radii = [120, 500, 2000, 6000, 15000];
        for (var i = 0; i < 20_000; i++)
        {
            var radius = radii[random.Next(radii.Length)];
            var lat = random.NextDouble() * 170 - 85;
            var lng = random.NextDouble() * 360 - 180;
            var reachLat = radius / MetersPerDegree;
            var otherLat = lat + (random.NextDouble() * 2 - 1) * reachLat;
            var otherLng = lng + (random.NextDouble() * 2 - 1) * reachLat / Math.Cos(Math.Abs(lat) * Math.PI / 180 + reachLat * Math.PI / 180);
            otherLng = otherLng > 180 ? otherLng - 360 : otherLng < -180 ? otherLng + 360 : otherLng;
            if (!Extensions.PointsAreCloserThan(lat, lng, otherLat, otherLng, (double)radius * radius))
            {
                continue;
            }

            var hash = Hasher.Encode(lat, lng, precision);
            var otherHash = Hasher.Encode(otherLat, otherLng, precision);
            if (otherHash != hash)
            {
                Hasher.NeighborsWithin(hash, radius).ShouldContain(otherHash, $"lat {lat}, lng {lng}, other {otherLat},{otherLng}, radius {radius}");
            }
        }
    }

    [Fact]
    public void Neighbor_filter_finds_a_neighbor_two_cells_east_at_70_degrees_north()
    {
        var (lat, lngA, lngB) = PairTwoCellsApart(70.0, HashPrecision.Size_km_1x1, meters: 450);
        Location[] locations = [MakeLocation(1, lat, lngA), MakeLocation(2, lat, lngB)];
        var filter = new NeighborFilter { Bound = "some", Radius = 500 };
        var mapDefinition = new MapDefinition { NeighborFilters = [filter] };
        var buckets = LocationLookupService.Bucketize(locations, mapDefinition.HashPrecisionFromNeighborFiltersRadius());

        var kept = NeighborFilterer.FilterByNeighbors(locations, buckets, filter, mapDefinition).Select(l => l.NodeId).OrderBy(id => id);

        kept.ShouldBe([1L, 2L]);
    }

    [Fact]
    public void Proximity_filter_finds_a_point_two_cells_east_at_70_degrees_north()
    {
        var (lat, lngA, lngB) = PairTwoCellsApart(70.0, HashPrecision.Size_km_5x5, meters: 1900);
        // The filter only runs when its locations file exists; the buckets carry the actual points.
        var locationsFile = Path.GetTempFileName();
        try
        {
            var proximityFilter = new ProximityFilter { LocationsPath = locationsFile, Radius = 2000 };
            var proximityBuckets = LocationLookupService.Bucketize<ILatLng>(
                [new LocationLakeMapGenerator.GeoMapLocation { lat = lat, lng = lngB }],
                proximityFilter.HashPrecisionFromProximityFilter());

            var kept = LocationLakeFilterer.Filter([MakeLocation(1, lat, lngA)], [], proximityBuckets, null, proximityFilter, [], [], new MapDefinition());

            kept.Select(l => l.NodeId).ShouldBe([1L]);
        }
        finally
        {
            File.Delete(locationsFile);
        }
    }

    [Fact]
    public void Distribution_keeps_the_min_distance_between_points_two_cells_apart_at_75_degrees_north()
    {
        var (lat, lngA, lngB) = PairTwoCellsApart(75.0, HashPrecision.Size_km_39x20, meters: 14_000);
        FileDistributor.DistributionLocation[] locations =
        [
            new() { Lat = lat, Lng = lngA, LocationId = 1 },
            new() { Lat = lat, Lng = lngB, LocationId = 2 },
        ];

        var picked = LocationDistributor.DistributeEvenly<FileDistributor.DistributionLocation, long>(locations, 15_000, new(), avoidShuffle: true, silent: true);

        picked.Count.ShouldBe(1);
    }

    // Two points on one parallel, `meters` apart: the first just inside its cell's east edge, the second two
    // cells further east, outside the 8 neighbours of the first one's cell.
    private static (double lat, double lngA, double lngB) PairTwoCellsApart(double lat, HashPrecision precision, double meters)
    {
        var box = Hasher.GetBoundingBox(Hasher.Encode(lat, 10.0, precision));
        var lngA = box.MaxLng - (box.MaxLng - box.MinLng) * 0.01;
        var lngB = lngA + meters / (MetersPerDegree * Math.Cos(lat * Math.PI / 180));
        var hashA = Hasher.Encode(lat, lngA, precision);
        var hashB = Hasher.Encode(lat, lngB, precision);
        hashB.ShouldNotBe(hashA);
        Hasher.Neighbors(hashA).ShouldNotContain(hashB);
        return (lat, lngA, lngB);
    }

    private static Location MakeLocation(long nodeId, double lat, double lng) => new()
    {
        NodeId = nodeId,
        Lat = lat,
        Lng = lng,
        Osm = new OsmData(),
        Google = new GoogleData { PanoId = "", CountryCode = "NO", Lat = lat, Lng = lng, Year = 2020 },
        Nominatim = new NominatimData { CountryCode = "NO", SubdivisionCode = "NO-54" },
    };
}
