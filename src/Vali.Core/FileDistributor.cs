using System.Diagnostics;

namespace Vali.Core;

public class FileDistributor
{
    public static async Task DistributeFromFile(string path, int fixedMinDistance, bool overwrite)
    {
        var locationsFilename = Path.GetFileNameWithoutExtension(path);
        var filename = locationsFilename + "-locations.json";
        var outFolder = Path.GetDirectoryName(path)!;
        var locationsOutPath = Path.Combine(outFolder, filename);
        if (File.Exists(locationsOutPath) && !overwrite)
        {
            ConsoleLogger.Error($"File {filename} already exists in {outFolder}.");
            return;
        }

        var sw = Stopwatch.StartNew();
        var locations = await LocationReader.DeserializeLocationsFromFile(path);
        var distributionLocations = locations.Select((x, i) => new DistributionLocation
        {
            Lat = x.lat,
            Lng = x.lng,
            LocationId = i
        }).ToArray();
        var selection = LocationDistributor.DistributeEvenly(distributionLocations, fixedMinDistance);
        ConsoleLogger.Success($"Distributed {selection.Count} locations in {sw.Elapsed}. Saved to {locationsOutPath}");

        await File.WriteAllTextAsync(locationsOutPath, Serializer.Serialize(selection.Select(x => locations[(int)x.LocationId]).ToArray()));
    }

    public record DistributionLocation : IDistributionLocation
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
        public long LocationId { get; set; }
    }
}