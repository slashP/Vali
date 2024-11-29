using System.Text.Json.Serialization;
using Vali.Core.Google;

namespace Vali.Core;

public static class LocationLakeMapGenerator
{
    public static async Task Generate(MapDefinition mapDefinition, string definitionPath, RunMode runMode, bool includeAdditionalLocationInfo = false)
    {
        var subdivisionGroups = new List<(IList<Location> locations, int regionGoalCount, int minDistance)>();
        foreach (var countryCode in mapDefinition.CountryCodes)
        {
            var allSubdivisions = SubdivisionWeights.AllSubdivisionFiles(countryCode, runMode);
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
            await StoreMap(mapDefinition, subdivisionGroups, definitionPath, includeAdditionalLocationInfo);
        }
        else
        {
            ConsoleLogger.Warn("No locations were generated for this map.");
        }
    }

    private static async Task StoreMap(
        MapDefinition mapDefinition,
        List<(IList<Location> locations, int regionGoalCount, int minDistance)> subdivisionGroups,
        string definitionPath,
        bool includeAdditionalLocationInfo)
    {
        var outFolder = Path.GetDirectoryName(definitionPath);
        if (outFolder is null)
        {
            ConsoleLogger.Error($"Can't get correct folder from path {definitionPath}");
            return;
        }

        var locations = subdivisionGroups.SelectMany(x => x.locations).Select(x => new
        {
            Loc = x,
            MapCheckrLoc = new MapCheckrLocation
            {
                locationId = x.NodeId.ToString(),
                lat = x.Google.Lat,
                lng = x.Google.Lng,
                countryCode = x.Nominatim.CountryCode,
                subdivisionCode = x.Nominatim.SubdivisionCode,
                panoId = x.Google.PanoId,
                heading = x.Google.Heading,
                arrowCount = (ushort)x.Google.ArrowCount,
                elevation = x.Google.Elevation,
                year = x.Google.Year,
                month = x.Google.Month,
                drivingDirectionAngle = (ushort)x.Google.DrivingDirectionAngle,
                descriptionLength = x.Google.DescriptionLength
            }
        }).ToArray();
        Random.Shared.Shuffle(locations);
        var locationsById = locations.ToDictionary(x => x.Loc.NodeId.ToString());
        if (Enum.TryParse<GoogleApi.PanoStrategy>(mapDefinition.Output.PanoVerificationStrategy, out var panoStrategy) && panoStrategy != GoogleApi.PanoStrategy.None)
        {
            var mapCheckrLocations = locations.Select(y => y.MapCheckrLoc).ToArray();
            var countryPanning = mapDefinition.Output.CountryPanoVerificationPanning;
            var verifiedLocations = await GoogleApi.GetLocations(
                mapCheckrLocations,
                countryCode: null,
                chunkSize: 100,
                radius: 50,
                rejectLocationsWithoutDescription: false,
                silent: false,
                selectionStrategy: panoStrategy,
                countryPanning: countryPanning);
            if (!string.IsNullOrEmpty(mapDefinition.Output.PanoVerificationExpression))
            {
                var userFilter = LocationLakeFilterer.CompileExpression<MapCheckrLocation, bool>(mapDefinition.Output.PanoVerificationExpression, true);
                verifiedLocations = verifiedLocations.Where(x => userFilter(x.location)).ToArray();
            }

            var percentUnsuccessful = verifiedLocations.Count != 0
                ? verifiedLocations.Count(x => x.result != GoogleApi.LocationLookupResult.Valid) /
                  (decimal)verifiedLocations.Count
                : 0;
            if (percentUnsuccessful > 0.05m)
            {
                ConsoleLogger.Warn($"{Math.Round(percentUnsuccessful * 100, 2)} % of locations removed during Google verification. Something may be wrong.");
                ConsoleLogger.Info(verifiedLocations.Where(x => x.result is not GoogleApi.LocationLookupResult.Valid).GroupBy(x => x.result).Select(x => $"{x.Key,20} | {x.Count(),6}").Merge(Environment.NewLine));
            }

            var unknownErrors = verifiedLocations.Where(x => x.result is GoogleApi.LocationLookupResult.UnknownError).Select(x => x.location).ToArray();
            var retryUnknownErrors = await GoogleApi.GetLocations(unknownErrors, null, chunkSize: 20, radius: 50, rejectLocationsWithoutDescription: false, silent: true, selectionStrategy: GoogleApi.PanoStrategy.Newest, countryPanning: countryPanning);
            locations = verifiedLocations.Where(x => x.result is GoogleApi.LocationLookupResult.Valid)
                .Concat(retryUnknownErrors.Where(x => x.result is GoogleApi.LocationLookupResult.Valid))
                .Select(x => new
                {
                    Loc = locationsById[x.location.locationId].Loc,
                    MapCheckrLoc = x.location
                })
                .ToArray();
        }

        var definitionFilename = Path.GetFileNameWithoutExtension(definitionPath);
        var filename = definitionFilename + "-locations.json";
        var locationsPath = Path.Combine(outFolder, filename);

        var output = mapDefinition.Output;

        double HeadingSelector(MapCheckrLocation loc)
        {
            var l = new Location
            {
                Google = new GoogleData
                {
                    PanoId = "",
                    CountryCode = loc.countryCode,
                    DefaultHeading = (double)loc.heading,
                    DrivingDirectionAngle = loc.drivingDirectionAngle,
                },
                Osm = new OsmData(),
                Nominatim = new NominatimData
                {
                    CountryCode = loc.countryCode,
                    SubdivisionCode = loc.subdivisionCode ?? ""
                }
            };

            return output switch
            {
                _ when output.CountryHeadingExpressions.TryGetValue(l.Google.CountryCode, out var headingExpression) =>
                    LocationLakeFilterer.CompileIntLocationExpression(headingExpression)(l),
                _ when !string.IsNullOrEmpty(output.GlobalHeadingExpression) => LocationLakeFilterer
                    .CompileIntLocationExpression(output.GlobalHeadingExpression)(l),
                _ => l.Google.Heading
            };
        }

        var isPanoSpecificCheckActive = panoStrategy != GoogleApi.PanoStrategy.None;
        var geoMapLocations = locations
            .Select(l => new GeoMapLocation
            {
                lat = l.MapCheckrLoc.Lat,
                lng = l.MapCheckrLoc.Lng,
                heading = HeadingSelector(l.MapCheckrLoc) % 360,
                zoom = output.GlobalZoom,
                pitch = output.GlobalPitch,
                extra = TagsGenerator.Tags(mapDefinition, l.Loc, l.MapCheckrLoc),
                panoId = output.PanoIdCountryCodes.Contains(l.Loc.Nominatim.CountryCode) || output.PanoIdCountryCodes.Contains("*") || isPanoSpecificCheckActive ? l.MapCheckrLoc.panoId : null,
                countryCode = includeAdditionalLocationInfo ? l.Loc.Nominatim.CountryCode : null,
                subdivisionCode = includeAdditionalLocationInfo ? l.Loc.Nominatim.SubdivisionCode : null,
            }).ToArray();
        await File.WriteAllTextAsync(locationsPath, Serializer.Serialize(geoMapLocations));
        ConsoleLogger.Info($"{locations.Length, 6:N0} locations saved to {new FileInfo(locationsPath).FullName}");
        var countries = locations.GroupBy(x => x.Loc.Nominatim.CountryCode).ToArray();
        if (countries.Length > 1)
        {
            await File.WriteAllLinesAsync(Path.Combine(outFolder, definitionFilename + "-country-distribution.txt"),
                countries.OrderBy(x => x.Key).Select(x => $"{x.Key}\t{x.Count()}\t{CountryLocationCountGoal(mapDefinition, x.Key)}"));
        }

        var regionalGoalCounts = subdivisionGroups
            .Select(x => (x.locations.FirstOrDefault()?.Nominatim.SubdivisionCode, x.regionGoalCount, x.minDistance))
            .Where(x => x.SubdivisionCode != null)
            .ToDictionary(x => x.SubdivisionCode!);
        await File.WriteAllLinesAsync(Path.Combine(outFolder, definitionFilename + "-subdivision-distribution.txt"),
            locations.GroupBy(x => x.Loc.Nominatim.SubdivisionCode)
                .Where(x => !string.IsNullOrEmpty(x.Key))
                .OrderBy(x => x.Key)
                .Select(x =>
                {
                    var goal = regionalGoalCounts.TryGetValue(x.Key, out var g) ? g : new();
                    return
                        $"{x.Key}\t{x.Count()}\t{goal.regionGoalCount}\t{goal.minDistance}m.";
                }));
    }

    public static int CountryLocationCountGoal(MapDefinition mapDefinition, string countryCode)
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

    public record GeoMapLocation : ILatLng
    {
        public double lat { get; set; }
        public double lng { get; set; }
        public double heading { get; set; }
        public double? zoom { get; set; }
        public double? pitch { get; set; }
        public GeoMapLocationExtra? extra { get; set; }
        public string? panoId { get; set; }
        public string? countryCode { get; set; }
        public string? subdivisionCode { get; set; }
        [JsonIgnore]
        public double Lat => lat;
        [JsonIgnore]
        public double Lng => lng;
    }

    public record GeoMapLocationExtra
    {
        public required string[] tags { get; set; }
    }
}
