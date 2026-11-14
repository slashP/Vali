using Shouldly;
using Xunit;

namespace Vali.Core.Tests;

// A long-running host (GMC's update-maps daemon) generates maps night after night in one process.
// Files that change between runs (vision exports, other maps' map-locations.json) must be read again.
public class StaleFileCacheTests
{
    [Fact]
    public void External_data_file_changed_since_last_generation_is_read_again()
    {
        var binFile = Path.GetTempFileName();
        var externalFile = Path.GetTempFileName();
        try
        {
            using (var stream = File.Create(binFile))
            {
                ProtoBuf.Serializer.Serialize(stream, new[]
                {
                    new Location
                    {
                        NodeId = 1,
                        Lat = 1,
                        Lng = 1,
                        Google = new GoogleData { Year = 2020 },
                        Osm = new OsmData(),
                        Nominatim = new NominatimData { CountryCode = "XX", SubdivisionCode = "XX-1" }
                    }
                });
            }

            var mapDefinition = new MapDefinition
            {
                SubdivisionDistribution = new() { ["XX"] = new() { ["XX-1"] = 1 } },
                CountryExternalDataFiles = new() { ["XX"] = [externalFile] }
            };

            File.WriteAllText(externalFile, """{"1":{"label":"A"}}""");
            var first = DistributionStrategies.SubdivisionByMaxMinDistance("XX", binFile, goalCount: 10, ["XX-1"], mapDefinition);
            first.locations.Single().ExternalData["label"].ShouldBe("A");

            File.WriteAllText(externalFile, """{"1":{"label":"BB"}}""");
            var second = DistributionStrategies.SubdivisionByMaxMinDistance("XX", binFile, goalCount: 10, ["XX-1"], mapDefinition);
            second.locations.Single().ExternalData["label"].ShouldBe("BB");
        }
        finally
        {
            File.Delete(binFile);
            File.Delete(externalFile);
        }
    }

    [Fact]
    public void Locations_file_changed_since_last_read_is_read_again()
    {
        var file = Path.GetTempFileName();
        try
        {
            File.WriteAllText(file, """[{"lat":1,"lng":1}]""");
            LocationReader.DeserializeLocationsFromFile(file).Length.ShouldBe(1);

            File.WriteAllText(file, """[{"lat":1,"lng":1},{"lat":2,"lng":2}]""");
            LocationReader.DeserializeLocationsFromFile(file).Length.ShouldBe(2);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void ClearCaches_makes_the_next_generation_read_input_files_again()
    {
        // Same length and same last write time: the change is invisible to the cache's stamp check.
        var file = Path.GetTempFileName();
        var stamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        try
        {
            File.WriteAllText(file, """[{"lat":1,"lng":1}]""");
            File.SetLastWriteTimeUtc(file, stamp);
            LocationReader.DeserializeLocationsFromFile(file).Single().lat.ShouldBe(1);

            File.WriteAllText(file, """[{"lat":2,"lng":1}]""");
            File.SetLastWriteTimeUtc(file, stamp);
            LocationLakeMapGenerator.ClearCaches();

            LocationReader.DeserializeLocationsFromFile(file).Single().lat.ShouldBe(2);
        }
        finally
        {
            File.Delete(file);
        }
    }
}
