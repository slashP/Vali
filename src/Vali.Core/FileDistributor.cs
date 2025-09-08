using System.Diagnostics;

namespace Vali.Core;

public class FileDistributor
{
    public static async Task DistributeFromFile(
        string path,
        int fixedMinDistance,
        bool overwrite,
        string? outputPath)
    {
        var locationsFilename = Path.GetFileNameWithoutExtension(path);
        var filename = locationsFilename + "-locations.json";
        var outFolder = Path.GetDirectoryName(path)!;
        var userSpecifiedOutputPath = outputPath switch
        {
            null => null,
            _ when outputPath.EndsWith(".json") => outputPath,
            _ => outputPath + ".json"
        };

        var userSpecifiedOutputPathWithFolder = userSpecifiedOutputPath switch
        {
            null => null,
            _ when Path.GetPathRoot(userSpecifiedOutputPath)?.Length == Path.GetDirectoryName(userSpecifiedOutputPath)?.Length => Path.Combine(outFolder, userSpecifiedOutputPath),
            _ => userSpecifiedOutputPath
        };
        if (userSpecifiedOutputPathWithFolder != null && !Directory.Exists(Path.GetDirectoryName(userSpecifiedOutputPathWithFolder)))
        {
            ConsoleLogger.Error($"Folder for {userSpecifiedOutputPathWithFolder} does not exist");
            return;
        }

        var locationsOutPath = userSpecifiedOutputPathWithFolder ?? Path.Combine(outFolder, filename);

        if (File.Exists(locationsOutPath) && !overwrite)
        {
            ConsoleLogger.Error($"File {filename} already exists in {outFolder}.");
            return;
        }

        var sw = Stopwatch.StartNew();
        var locations = LocationReader.DeserializeLocationsFromFile(path);
        var distributionLocations = locations.Select((x, i) => new DistributionLocation
        {
            Lat = x.lat,
            Lng = x.lng,
            LocationId = i
        }).ToArray();
        var selection = LocationDistributor.DistributeEvenly<DistributionLocation, long>(distributionLocations, fixedMinDistance, new());
        ConsoleLogger.Success($"Distributed {selection.Count} locations in {sw.Elapsed}. Saved to {locationsOutPath}");

        await File.WriteAllTextAsync(locationsOutPath, Serializer.Serialize(selection.Select(x => locations[(int)x.LocationId]).ToArray()));
    }

    public class DistributionLocation : IDistributionLocation<long>
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
        public long LocationId { get; set; }
    }
}