using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Vali.Core.Generation;
using Vali.Core.Google;

namespace Vali.Core;

public class LocationLakeMapGenerator
{
    public static async Task Generate(MapDefinition mapDefinition, string definitionPath, RunMode runMode, bool includeAdditionalLocationInfo = false)
    {
        var sw = Stopwatch.StartNew();
        if (GenPerf.Enabled)
        {
            GenPerf.Reset();
        }

        var logger = ValiLogger.Factory.CreateLogger<LocationLakeMapGenerator>();
        var progress = new GenerationProgress();

        // Phase A — prep: resolve files/goals per country and emit one flat work-list.
        var workItems = new List<GenerationWorkItem>();
        foreach (var countryCode in mapDefinition.CountryCodes)
        {
            var allSubdivisions = SubdivisionWeights.AllSubdivisionFiles(countryCode, runMode);
            var availableSubdivisions = allSubdivisions.Select(x => x.subdivisionCode).ToArray();
            var subDivisions = mapDefinition switch
            {
                _ when mapDefinition.SubdivisionInclusions.TryGetValue(countryCode, out var inclusions) => inclusions,
                _ when mapDefinition.SubdivisionExclusions.TryGetValue(countryCode, out var exclusions) => availableSubdivisions.Except(exclusions).ToArray(),
                _ => availableSubdivisions
            };
            var subdivisionFiles = subDivisions.Select(s => allSubdivisions.First(f => f.subdivisionCode == s).file).ToArray();
            await DataDownloadService.EnsureFilesDownloaded(countryCode, subdivisionFiles);
            var locationCountGoal = CountryLocationCountGoal(mapDefinition, countryCode);

            // Locals captured by the work-item closures (one set per country iteration).
            var cc = countryCode;
            var subs = subDivisions;
            var files = subdivisionFiles;
            var treatAsSingle = mapDefinition.DistributionStrategy.TreatCountriesAsSingleSubdivision.Contains(cc);

            var workItemsBefore = workItems.Count;
            switch (mapDefinition.DistributionStrategy.Key)
            {
                case DistributionStrategies.FixedCountByMaxMinDistance when treatAsSingle:
                    workItems.Add(new GenerationWorkItem(cc, EstimateCostBytes(files), null,
                        () => DistributionStrategies.CountryByMaxMinDistance(cc, files, locationCountGoal, mapDefinition)));
                    break;
                case DistributionStrategies.FixedCountByMaxMinDistance:
                    foreach (var (subdivisionCode, file) in subs.Zip(files))
                    {
                        var f = file;
                        workItems.Add(new GenerationWorkItem(cc, EstimateCostBytes([f]), subdivisionCode,
                            () => [DistributionStrategies.SubdivisionByMaxMinDistance(cc, f, locationCountGoal, subs, mapDefinition)]));
                    }
                    break;
                case DistributionStrategies.FixedCountByCoverageDensity when treatAsSingle:
                    workItems.Add(new GenerationWorkItem(cc, EstimateCostBytes(files), null,
                        () => DistributionStrategies.CountryByCoverageDensity(cc, files, locationCountGoal, mapDefinition)));
                    break;
                case DistributionStrategies.FixedCountByCoverageDensity:
                    foreach (var (subdivisionCode, file) in subs.Zip(files))
                    {
                        var f = file;
                        workItems.Add(new GenerationWorkItem(cc, EstimateCostBytes([f]), subdivisionCode,
                            () => [DistributionStrategies.SubdivisionByCoverageDensity(cc, f, locationCountGoal, subs, mapDefinition)]));
                    }
                    break;
                case DistributionStrategies.MaxCountByFixedMinDistance:
                    workItems.Add(new GenerationWorkItem(cc, EstimateCostBytes(files), null,
                        () => DistributionStrategies.MaxLocationsInSubdivisionsByFixedMinDistance(cc, files, subs, mapDefinition)));
                    break;
                case DistributionStrategies.EvenlyByDistanceWithinCountry:
                    workItems.Add(new GenerationWorkItem(cc, EstimateCostBytes(files), null,
                        () => DistributionStrategies.EvenlyByDistanceInCountry(cc, files, subs, mapDefinition)));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            progress.RegisterCountry(countryCode, workItems.Count - workItemsBefore, locationCountGoal);
        }

        // Phase B — one global memory-aware pass over all work items.
        var cpuCap = ApplicationSettingsService.ReadApplicationSettings().Parallelism ?? Environment.ProcessorCount;
        var budgetBytes = (long)(MemoryBudgetFraction * GC.GetGCMemoryInfo().TotalAvailableMemoryBytes);
        var view = SelectView(progress, sw);
        using var stopSignal = new CancellationTokenSource();
        using var generationDone = new CancellationTokenSource();
        var stopListener = ListenForStopKey(stopSignal, generationDone.Token);
        var schedulerTask = MemoryAwareScheduler.RunWithMemoryBudget(
            workItems,
            item => item.EstimatedCostBytes,
            item => Task.Run(() =>
            {
                var results = RunTimed(item);
                progress.ReportCompleted(item.CountryCode, item.SubdivisionCode, results);
                view.OnCompleted(item.CountryCode, item.SubdivisionCode, results);
                return results;
            }),
            cpuCap,
            budgetBytes,
            stopSignal.Token);
        await view.RunUntil(schedulerTask);
        var chunkArrays = await schedulerTask; // observe results / propagate exceptions
        generationDone.Cancel(); // the run is over — let the key listener exit
        await stopListener;
        var stoppedEarly = stopSignal.IsCancellationRequested;
        var subdivisionGroups = chunkArrays.SelectMany(x => x).ToList();

        static IGenerationProgressView SelectView(GenerationProgress progress, Stopwatch stopwatch)
        {
            var interactive = AnsiConsole.Profile.Capabilities.Interactive && AnsiConsole.Profile.Width > 0;
            return interactive && !ConsoleLogger.Silent
                ? new LiveGridView(progress, stopwatch)
                : new LineLogView(progress);
        }

        // Polls for the 's' key on a background task and asks the scheduler to stop dispatching new
        // work (in-flight work still finishes). Skipped when input is redirected (piped / CI).
        static async Task ListenForStopKey(CancellationTokenSource stopSignal, CancellationToken untilDone)
        {
            if (Console.IsInputRedirected)
            {
                return;
            }

            try
            {
                while (!untilDone.IsCancellationRequested && !stopSignal.IsCancellationRequested)
                {
                    if (Console.KeyAvailable && Console.ReadKey(intercept: true).Key == ConsoleKey.S)
                    {
                        stopSignal.Cancel();
                        return;
                    }

                    await Task.Delay(100, untilDone);
                }
            }
            catch (OperationCanceledException)
            {
                // The run finished before any key was pressed.
            }
            catch (InvalidOperationException)
            {
                // Console input turned out not to be readable; nothing to listen to.
            }
        }

        // Phase C — store.
        if (stoppedEarly)
        {
            ConsoleLogger.Warn($"Generation stopped — saving {subdivisionGroups.Sum(s => s.locations.Count):N0} locations from regions completed so far.");
        }

        logger.MapGenerated(subdivisionGroups.Sum(s => s.locations.Count), sw.Elapsed);

        if (subdivisionGroups.Any())
        {
            using (GenPerf.Measure(GenPerf.Phase.StoreProjection))
            {
                await StoreMap(mapDefinition, subdivisionGroups, definitionPath, includeAdditionalLocationInfo);
            }
        }
        else
        {
            ConsoleLogger.Warn("No locations were generated for this map.");
        }

        ShortfallSummary.Render(ShortfallSummary.Select(progress.Outcomes()));

        if (GenPerf.Enabled)
        {
            GenPerf.Report(sw.Elapsed);
        }

        static (IList<Location> locations, int regionGoalCount, int minDistance)[] RunTimed(GenerationWorkItem item)
        {
            using var _ = GenerationConcurrency.EnterWorkItem();
            var stopwatch = GenPerf.Enabled ? Stopwatch.StartNew() : null;
            var result = item.Run();
            if (stopwatch is not null)
            {
                GenPerf.AddCountryWork(item.CountryCode, stopwatch.Elapsed);
            }

            return result;
        }
    }

    private const double MemoryBudgetFraction = 0.6;
    private const double LiveBytesPerFileByte = 12.0;

    private static long EstimateCostBytes(string[] files)
    {
        long onDisk = 0;
        foreach (var file in files)
        {
            try
            {
                onDisk += new FileInfo(file).Length;
            }
            catch
            {
                // Missing/unreadable file contributes 0 to the estimate; the body will surface any real error.
            }
        }

        return (long)(onDisk * LiveBytesPerFileByte);
    }

    private sealed record GenerationWorkItem(
        string CountryCode,
        long EstimatedCostBytes,
        string? SubdivisionCode,
        Func<(IList<Location> locations, int regionGoalCount, int minDistance)[]> Run);

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

        var locations = subdivisionGroups.SelectMany(x => x.locations).DistinctBy(x => x.NodeId).Select(x => new
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
                descriptionLength = x.Google.DescriptionLength,
                isScout = x.Google.IsScout,
                resolutionHeight = x.Google.ResolutionHeight,
            }
        }).ToArray();
        if (GenerationDeterminism.Deterministic)
        {
            locations = locations.OrderBy(x => x.Loc.NodeId).ToArray();
        }
        else
        {
            Random.Shared.Shuffle(locations);
        }

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
                countryPanning: countryPanning,
                includeLinked: false,
                panoVerificationStart: mapDefinition.Output.PanoVerificationStart,
                panoVerificationEnd: mapDefinition.Output.PanoVerificationEnd,
                GoogleApi.BadCamStrategy.DisallowInCountriesWithDecentOtherCoverage);
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
            var retryUnknownErrors = await GoogleApi.GetLocations(unknownErrors, null, chunkSize: 20, radius: 50, rejectLocationsWithoutDescription: false, silent: true, selectionStrategy: GoogleApi.PanoStrategy.Newest, countryPanning: countryPanning, includeLinked: false, panoVerificationStart: mapDefinition.Output.PanoVerificationStart, mapDefinition.Output.PanoVerificationEnd, GoogleApi.BadCamStrategy.DisallowInCountriesWithDecentOtherCoverage);
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
                    CountryCode = loc.countryCode ?? "",
                    DefaultHeading = (double)loc.heading,
                    DrivingDirectionAngle = loc.drivingDirectionAngle,
                },
                Osm = new OsmData(),
                Nominatim = new NominatimData
                {
                    CountryCode = loc.countryCode ?? "",
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
                extra = TagsGenerator.Tags(l.Loc, l.MapCheckrLoc, mapDefinition.Output.LocationTags),
                panoId = output.PanoIdCountryCodes.Contains(l.Loc.Nominatim.CountryCode) || output.PanoIdCountryCodes.Contains("*") || isPanoSpecificCheckActive ? l.MapCheckrLoc.panoId : null,
                countryCode = includeAdditionalLocationInfo ? l.Loc.Nominatim.CountryCode : null,
                subdivisionCode = includeAdditionalLocationInfo ? l.Loc.Nominatim.SubdivisionCode : null,
                locationId = includeAdditionalLocationInfo ? l.Loc.LocationId.ToString() : null,
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
        public string? locationId { get; set; }
    }

    public record GeoMapLocationExtra
    {
        public string[] tags { get; set; } = [];
    }
}
