using System.Text.Json.Serialization;

namespace Vali.Core;

public static class LocationLakeMapGenerator
{
    public static async Task Generate(MapDefinition mapDefinition, string definitionPath)
    {
        var subdivisionGroups = new List<(IList<Location> locations, int regionGoalCount, int minDistance)>();
        foreach (var countryCode in mapDefinition.CountryCodes)
        {
            var allSubdivisions = SubdivisionWeights.AllSubdivisionFiles(countryCode);
            var availableSubdivisions = allSubdivisions
                .Select(x => x.subdivisionCode)
                .ToArray();
            var subDivisions = mapDefinition switch
            {
                 _ when mapDefinition.SubdivisionInclusions.TryGetValue(countryCode, out var inclusions) => inclusions,
                 _ when mapDefinition.SubdivisionExclusions.TryGetValue(countryCode, out var exclusions) => availableSubdivisions.Except(exclusions).ToArray(),
                 _ => availableSubdivisions
            };
            var subdivisionFiles = subDivisions.Select(s => allSubdivisions.First(f => f.subdivisionCode == s).file).ToArray();
            await DataDownloadService.EnsureFilesDownloaded(countryCode, subdivisionFiles);
            var locationCountGoal = CountryLocationCountGoal(mapDefinition, countryCode);
            var locationChunks = mapDefinition.DistributionStrategy.Key switch
            {
                DistributionStrategies.FixedCountByMaxMinDistance when mapDefinition.DistributionStrategy.TreatCountriesAsSingleSubdivision.Contains(countryCode) => DistributionStrategies.CountryByMaxMinDistance(
                    countryCode,
                    subdivisionFiles,
                    locationCountGoal,
                    mapDefinition),
                DistributionStrategies.FixedCountByMaxMinDistance => subdivisionFiles.AsParallel().Select(f =>
                    DistributionStrategies.SubdivisionByMaxMinDistance(
                        countryCode,
                        f,
                        locationCountGoal,
                        subDivisions,
                        mapDefinition)).ToArray(),
                DistributionStrategies.MaxCountByFixedMinDistance => DistributionStrategies.MaxLocationsInSubdivisionsByFixedMinDistance(
                    countryCode,
                    subdivisionFiles,
                    subDivisions,
                    mapDefinition),
                DistributionStrategies.EvenlyByDistanceWithinCountry => DistributionStrategies.EvenlyByDistanceInCountry(
                    countryCode,
                    subdivisionFiles,
                    subDivisions,
                    mapDefinition),
                _ => throw new ArgumentOutOfRangeException()
            };
            subdivisionGroups.AddRange(locationChunks);
        }

        if (subdivisionGroups.Any())
        {
            await StoreMap(mapDefinition, subdivisionGroups, definitionPath);
        }
        else
        {
            ConsoleLogger.Warn("No locations were generated for this map.");
        }
    }

    private static async Task StoreMap(MapDefinition mapDefinition,
        List<(IList<Location> locations, int regionGoalCount, int minDistance)> subdivisionGroups,
        string definitionPath)
    {
        var outFolder = Path.GetDirectoryName(definitionPath);
        if (outFolder is null)
        {
            ConsoleLogger.Error($"Can't get correct folder from path {definitionPath}");
            return;
        }

        var locations = subdivisionGroups.SelectMany(x => x.locations).ToArray();
        var definitionFilename = Path.GetFileNameWithoutExtension(definitionPath);
        var filename = definitionFilename + "-locations.json";
        var locationsPath = Path.Combine(outFolder, filename);

        var output = mapDefinition.Output;

        double HeadingSelector(Location l) => output switch
        {
            _ when output.CountryHeadingExpressions.TryGetValue(l.Nominatim.CountryCode, out var headingExpression) => LocationLakeFilterer.CompileIntLocationExpression(headingExpression)(l),
            _ when !string.IsNullOrEmpty(output.GlobalHeadingExpression) => LocationLakeFilterer.CompileIntLocationExpression(output.GlobalHeadingExpression)(l),
            _ => l.Google.Heading
        };

        var geoMapLocations = locations
            .Select(l => new GeoMapLocation
            {
                lat = l.Lat,
                lng = l.Lng,
                heading = HeadingSelector(l) % 360,
                zoom = output.GlobalZoom,
                pitch = output.GlobalPitch,
                extra = TagsGenerator.Tags(mapDefinition, l),
                panoId = output.PanoIdCountryCodes.Contains(l.Nominatim.CountryCode) || output.PanoIdCountryCodes.Contains("*") ? l.Google.PanoId : null
            }).ToArray();
        await File.WriteAllTextAsync(locationsPath, Serializer.Serialize(geoMapLocations));
        ConsoleLogger.Info($"{locations.Length, 6:N0} locations saved to {new FileInfo(locationsPath).FullName}");
        var countries = locations.GroupBy(x => x.Nominatim.CountryCode).ToArray();
        if (countries.Length > 1)
        {
            await File.WriteAllLinesAsync(Path.Combine(outFolder, definitionFilename + "-country-distribution.txt"),
                countries.OrderBy(x => x.Key).Select(x => $"{x.Key}\t{x.Count()}\t{CountryLocationCountGoal(mapDefinition, x.Key)}"));
        }

        var groups = subdivisionGroups.Where(s => s.locations.Any()).ToArray();
        await File.WriteAllLinesAsync(Path.Combine(outFolder, definitionFilename + "-subdivision-distribution.txt"),
            groups.OrderBy(x => x.locations.FirstOrDefault()?.Nominatim.SubdivisionCode)
                .Select(x => $"{x.locations.FirstOrDefault()?.Nominatim.SubdivisionCode}\t{x.locations.Count}\t{x.regionGoalCount}\t{x.minDistance}m."));
    }

    private static int CountryLocationCountGoal(MapDefinition mapDefinition, string countryCode)
    {
        var countryWeights = CountryWeights(mapDefinition);
        var countryDistributionTotalWeight = countryWeights.Sum(x => x.Value);
        var locationCountGoal = (decimal)mapDefinition.DistributionStrategy.LocationCountGoal * countryWeights[countryCode] /
                                (decimal)countryDistributionTotalWeight;
        return locationCountGoal.RoundToInt();
    }

    public static Dictionary<string, int> CountryWeights(MapDefinition mapDefinition)
    {
        return mapDefinition.CountryDistribution;
    }

    public record GeoMapLocation
    {
        public double lat { get; set; }
        public double lng { get; set; }
        public double heading { get; set; }
        public double? zoom { get; set; }
        public double? pitch { get; set; }
        public GeoMapLocationExtra? extra { get; set; }
        public string? panoId { get; set; }
    }

    public record GeoMapLocationExtra
    {
        public required string[] tags { get; set; }
    }
}
