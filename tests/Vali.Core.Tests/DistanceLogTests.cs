using Shouldly;
using Vali.Core.Google;
using Xunit;

namespace Vali.Core.Tests;

[Collection("StaticState")]
public class DistanceLogTests
{
    [Fact]
    public void Lines_are_sorted_by_country_subdivision_stage_and_keep_call_order_within_a_key()
    {
        DistanceLog.Reset();
        try
        {
            DistanceLog.Record("NO", "NO-03", 1, 10, 500, 400, Locations(1));
            DistanceLog.Record("GB", "GB-ENG", 0, 20, 600, 600, Locations(2));
            DistanceLog.Record("NO", "NO-03", -1, 30, 700, 700, Locations(3));
            DistanceLog.Record("NO", "NO-03", 1, 40, 800, 800, Locations(4));

            var lines = DistanceLog.Lines();

            lines.Select(l => string.Join('\t', l.Split('\t').Take(4))).ShouldBe(
            [
                "GB\tGB-ENG\t0\t20",
                "NO\tNO-03\t-1\t30",
                "NO\tNO-03\t1\t10",
                "NO\tNO-03\t1\t40",
            ]);
        }
        finally
        {
            DistanceLog.Reset();
        }
    }

    [Fact]
    public void Line_holds_distances_count_and_hash()
    {
        DistanceLog.Reset();
        try
        {
            DistanceLog.Record("NO", "NO-03", 2, 99, 500, 450, Locations(1, 2));

            DistanceLog.Lines().Single().ShouldBe($"NO\tNO-03\t2\t99\t500\t450\t2\t{DistanceLog.PlacedHash(Locations(1, 2))}");
        }
        finally
        {
            DistanceLog.Reset();
        }
    }

    [Fact]
    public void PlacedHash_depends_on_order()
    {
        DistanceLog.PlacedHash(Locations(1, 2, 3)).ShouldNotBe(DistanceLog.PlacedHash(Locations(3, 2, 1)));
        DistanceLog.PlacedHash([]).ShouldBe("cbf29ce484222325");
        DistanceLog.PlacedHash(Locations(1)).Length.ShouldBe(16);
    }

    [Fact]
    public void WriteIfEnabled_writes_header_and_rows_to_the_path()
    {
        var path = Path.Combine(Path.GetTempPath(), $"distance-log-{Guid.NewGuid():N}.tsv");
        var before = DistanceLog.Path;
        DistanceLog.Reset();
        try
        {
            DistanceLog.Path = path;
            DistanceLog.Enabled.ShouldBeTrue();
            DistanceLog.Record("NO", "NO-03", -1, 5, 100, 100, Locations(7));

            DistanceLog.WriteIfEnabled();

            var lines = File.ReadAllLines(path);
            lines[0].ShouldBe("country\tsubdivision\tstage\tcandidates\tseedDistance\tchosenDistance\tplaced\tplacedHash");
            lines.Length.ShouldBe(2);
        }
        finally
        {
            DistanceLog.Path = before;
            DistanceLog.Reset();
            File.Delete(path);
        }
    }

    private static Location[] Locations(params long[] nodeIds) => nodeIds.Select(id => new Location
    {
        NodeId = id,
        Lat = 59,
        Lng = 10,
        Google = new GoogleData { PanoId = "", CountryCode = "NO" },
        Osm = new OsmData(),
        Nominatim = new NominatimData { CountryCode = "NO", SubdivisionCode = "NO-03" }
    }).ToArray();
}
