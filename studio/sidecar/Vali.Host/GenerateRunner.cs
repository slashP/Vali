using System.Diagnostics;
using Vali.Core;
using Vali.Core.Generation;
using Vali.Core.Google;
using Vali.Core.Validation;
using static Vali.Core.LocationLakeMapGenerator;

namespace Vali.Host;

/// <summary>
/// Re-orchestrates LocationLakeMapGenerator.Generate against Vali.Core's public APIs, replacing
/// the console view with protocol events and the 's'-key stop with a cancel request. Cancellation
/// semantics match the CLI: stop admitting new regions, drain in-flight ones, save what completed.
/// </summary>
public sealed class GenerateRunner(ProtocolWriter writer, CaptureTextWriter capture)
{
    private const double MemoryBudgetFraction = 0.6;
    private const double LiveBytesPerFileByte = 12.0;

    private readonly object _lock = new();
    private (string Id, CancellationTokenSource Cts)? _current;

    public async Task Run(Request request)
    {
        var requestId = request.Id ?? Guid.NewGuid().ToString("N");
        var definitionPath = request.GetString("definitionPath");
        if (definitionPath is null || !File.Exists(definitionPath))
        {
            writer.RespondError(request.Id, $"Definition file not found: {definitionPath}");
            return;
        }

        using var cts = new CancellationTokenSource();
        lock (_lock)
        {
            if (_current is not null)
            {
                writer.RespondError(request.Id, "A generation is already running. Stop it first.");
                return;
            }

            _current = (requestId, cts);
        }

        try
        {
            await RunCore(requestId, definitionPath, request.GetInt("previewGoal"), request.GetBool("includeAdditionalInfo"), cts);
        }
        catch (Exception e)
        {
            writer.RespondError(requestId, $"Generation failed: {e.Message}", e.ToString());
        }
        finally
        {
            lock (_lock)
            {
                _current = null;
            }
        }
    }

    public void Cancel(Request request)
    {
        var targetId = request.GetString("targetId");
        lock (_lock)
        {
            if (_current is { } current && current.Id == targetId)
            {
                current.Cts.Cancel();
                writer.Respond(request.Id, new { cancelled = true });
                return;
            }
        }

        writer.RespondError(request.Id, "No running generation with that id.");
    }

    private async Task RunCore(string requestId, string definitionPath, int? previewGoal, bool includeAdditionalInfo, CancellationTokenSource cts)
    {
        var json = await File.ReadAllTextAsync(definitionPath);
        var humanReadableError = GenerateFileValidator.HumanReadableError(json);
        if (humanReadableError is not null)
        {
            writer.RespondError(requestId, humanReadableError);
            return;
        }

        MapDefinition definition;
        await ConsoleGate.Lock.WaitAsync();
        try
        {
            capture.DrainLines();
            var deserialized = GenerateFileValidator.TryDeserialize(json);
            if (deserialized is null)
            {
                writer.RespondError(requestId, "The JSON could not be read as a map definition.", string.Join(Environment.NewLine, capture.DrainLines()));
                return;
            }

            var validated = deserialized.ApplyDefaults().Validate();
            if (validated is null)
            {
                writer.RespondError(requestId, "Definition failed validation.", string.Join(Environment.NewLine, capture.DrainLines()));
                return;
            }

            definition = validated;
        }
        finally
        {
            ConsoleGate.Lock.Release();
        }

        if (previewGoal is { } goal)
        {
            if (definition.DistributionStrategy.Key is not (DistributionStrategies.FixedCountByMaxMinDistance or DistributionStrategies.FixedCountByCoverageDensity))
            {
                writer.RespondError(requestId, $"Preview requires a count-goal strategy (FixedCountByMaxMinDistance or FixedCountByCoverageDensity); '{definition.DistributionStrategy.Key}' is not supported yet.");
                return;
            }

            definition = definition with
            {
                DistributionStrategy = definition.DistributionStrategy with { LocationCountGoal = goal },
                Output = definition.Output with { PanoVerificationStrategy = null }
            };
        }
        else if (Enum.TryParse<GoogleApi.PanoStrategy>(definition.Output.PanoVerificationStrategy, out var panoStrategy) && panoStrategy != GoogleApi.PanoStrategy.None)
        {
            writer.Emit("warning", requestId, new
            {
                message = "Pano verification is not supported in Vali Studio yet — locations are written without Google verification. Use the Vali CLI when you need it."
            });
            definition = definition with { Output = definition.Output with { PanoVerificationStrategy = null } };
        }

        var progress = new GenerationProgress();
        var workItems = new List<WorkItem>();
        var countrySummaries = new List<object>();
        var totalGoal = 0;
        foreach (var countryCode in definition.CountryCodes)
        {
            var allSubdivisions = SubdivisionWeights.AllSubdivisionFiles(countryCode, RunMode.Default);
            var availableSubdivisions = allSubdivisions.Select(x => x.subdivisionCode).ToArray();
            var subDivisions = definition switch
            {
                _ when definition.SubdivisionInclusions.TryGetValue(countryCode, out var inclusions) => inclusions,
                _ when definition.SubdivisionExclusions.TryGetValue(countryCode, out var exclusions) => availableSubdivisions.Except(exclusions).ToArray(),
                _ => availableSubdivisions
            };
            var subdivisionFiles = subDivisions.Select(s => allSubdivisions.First(f => f.subdivisionCode == s).file).ToArray();
            var missing = subdivisionFiles.Where(f => !File.Exists(f)).ToArray();
            if (missing.Length > 0)
            {
                writer.RespondError(requestId, $"Location data for {countryCode} is missing ({missing.Length} file(s)). Run: vali download --country \"{countryCode}\"");
                return;
            }

            var locationCountGoal = CountryLocationCountGoal(definition, countryCode);
            var cc = countryCode;
            var subs = subDivisions;
            var files = subdivisionFiles;
            var def = definition;
            var treatAsSingle = definition.DistributionStrategy.TreatCountriesAsSingleSubdivision.Contains(cc);
            var workItemsBefore = workItems.Count;
            switch (definition.DistributionStrategy.Key)
            {
                case DistributionStrategies.FixedCountByMaxMinDistance when treatAsSingle:
                    workItems.Add(new WorkItem(cc, EstimateCostBytes(files), null,
                        () => DistributionStrategies.CountryByMaxMinDistance(cc, files, locationCountGoal, def)));
                    break;
                case DistributionStrategies.FixedCountByMaxMinDistance:
                    foreach (var (subdivisionCode, file) in subs.Zip(files))
                    {
                        var f = file;
                        workItems.Add(new WorkItem(cc, EstimateCostBytes([f]), subdivisionCode,
                            () => [DistributionStrategies.SubdivisionByMaxMinDistance(cc, f, locationCountGoal, subs, def)]));
                    }

                    break;
                case DistributionStrategies.FixedCountByCoverageDensity when treatAsSingle:
                    workItems.Add(new WorkItem(cc, EstimateCostBytes(files), null,
                        () => DistributionStrategies.CountryByCoverageDensity(cc, files, locationCountGoal, def)));
                    break;
                case DistributionStrategies.FixedCountByCoverageDensity:
                    foreach (var (subdivisionCode, file) in subs.Zip(files))
                    {
                        var f = file;
                        workItems.Add(new WorkItem(cc, EstimateCostBytes([f]), subdivisionCode,
                            () => [DistributionStrategies.SubdivisionByCoverageDensity(cc, f, locationCountGoal, subs, def)]));
                    }

                    break;
                case DistributionStrategies.MaxCountByFixedMinDistance:
                    workItems.Add(new WorkItem(cc, EstimateCostBytes(files), null,
                        () => DistributionStrategies.MaxLocationsInSubdivisionsByFixedMinDistance(cc, files, subs, def)));
                    break;
                case DistributionStrategies.EvenlyByDistanceWithinCountry:
                    workItems.Add(new WorkItem(cc, EstimateCostBytes(files), null,
                        () => DistributionStrategies.EvenlyByDistanceInCountry(cc, files, subs, def)));
                    break;
                default:
                    writer.RespondError(requestId, $"Unknown distribution strategy '{definition.DistributionStrategy.Key}'.");
                    return;
            }

            var added = workItems.Count - workItemsBefore;
            progress.RegisterCountry(countryCode, added, locationCountGoal);
            totalGoal += locationCountGoal;
            countrySummaries.Add(new { code = countryCode, workItems = added, goal = locationCountGoal });
        }

        writer.Emit("started", requestId, new { countries = countrySummaries, totalGoal });

        var stopwatch = Stopwatch.StartNew();
        var cpuCap = ApplicationSettingsService.ReadApplicationSettings().Parallelism ?? Environment.ProcessorCount;
        var budgetBytes = (long)(MemoryBudgetFraction * GC.GetGCMemoryInfo().TotalAvailableMemoryBytes);
        var schedulerTask = MemoryAwareScheduler.RunWithMemoryBudget(
            workItems,
            item => item.EstimatedCostBytes,
            item => Task.Run(() =>
            {
                (IList<Location> locations, int regionGoalCount, int minDistance)[] results;
                using (GenerationConcurrency.EnterWorkItem())
                {
                    results = item.Run();
                }

                progress.ReportCompleted(item.CountryCode, item.SubdivisionCode, results);
                EmitRegionCompleted(requestId, item, results);
                return results;
            }),
            cpuCap,
            budgetBytes,
            cts.Token);

        using var pumpDone = new CancellationTokenSource();
        var pump = PumpProgress(requestId, progress, stopwatch, pumpDone.Token);
        IReadOnlyList<(IList<Location> locations, int regionGoalCount, int minDistance)[]> chunkArrays;
        try
        {
            chunkArrays = await schedulerTask;
        }
        finally
        {
            pumpDone.Cancel();
            await pump;
        }

        EmitProgress(requestId, progress, stopwatch);
        var stoppedEarly = cts.IsCancellationRequested;
        var subdivisionGroups = chunkArrays.SelectMany(x => x).ToList();

        string? outputPath = null;
        var locationsWritten = 0;
        if (subdivisionGroups.Count > 0)
        {
            (outputPath, locationsWritten) = await Store(definition, subdivisionGroups, definitionPath, includeAdditionalInfo, isPreview: previewGoal.HasValue);
        }

        writer.Respond(requestId, new
        {
            locationsWritten,
            outputPath,
            stoppedEarly,
            elapsedMs = stopwatch.ElapsedMilliseconds,
            shortfall = ShortfallSummary.Select(progress.Outcomes())
        });
    }

    private async Task PumpProgress(string requestId, GenerationProgress progress, Stopwatch stopwatch, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                EmitProgress(requestId, progress, stopwatch);
                await Task.Delay(250, token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void EmitProgress(string requestId, GenerationProgress progress, Stopwatch stopwatch)
    {
        var snapshot = progress.Snapshot();
        writer.Emit("progress", requestId, new
        {
            countries = snapshot.Countries.Select(c => new
            {
                code = c.Code,
                completed = c.Completed,
                total = c.Total,
                status = c.Status.ToString(),
                produced = c.ProducedSoFar
            }),
            countriesComplete = snapshot.CountriesComplete,
            countriesTotal = snapshot.CountriesTotal,
            totalLocations = snapshot.TotalLocations,
            totalGoal = snapshot.TotalGoal,
            elapsedMs = stopwatch.ElapsedMilliseconds
        });
    }

    private void EmitRegionCompleted(string requestId, WorkItem item, (IList<Location> locations, int regionGoalCount, int minDistance)[] results)
    {
        foreach (var (locations, regionGoalCount, minDistance) in results)
        {
            var subdivisionCode = item.SubdivisionCode is { Length: > 0 }
                ? item.SubdivisionCode
                : locations.FirstOrDefault()?.Nominatim.SubdivisionCode ?? "";
            writer.Emit("regionCompleted", requestId, new
            {
                countryCode = item.CountryCode,
                subdivisionCode,
                produced = locations.Count,
                goal = regionGoalCount,
                minDistance,
                sample = Sample(locations)
            });
        }
    }

    private static double[][] Sample(IList<Location> locations)
    {
        const int max = 300;
        if (locations.Count == 0)
        {
            return [];
        }

        var step = Math.Max(1, locations.Count / max);
        var sample = new List<double[]>(Math.Min(max, locations.Count));
        for (var i = 0; i < locations.Count && sample.Count < max; i += step)
        {
            var location = locations[i];
            sample.Add([Math.Round(location.Google.Lat, 5), Math.Round(location.Google.Lng, 5)]);
        }

        return sample.ToArray();
    }

    private static async Task<(string path, int count)> Store(
        MapDefinition mapDefinition,
        List<(IList<Location> locations, int regionGoalCount, int minDistance)> subdivisionGroups,
        string definitionPath,
        bool includeAdditionalLocationInfo,
        bool isPreview)
    {
        var outFolder = Path.GetDirectoryName(definitionPath) ?? throw new InvalidOperationException($"Can't get correct folder from path {definitionPath}");
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

        var geoMapLocations = locations
            .Select(l => new GeoMapLocation
            {
                lat = l.MapCheckrLoc.Lat,
                lng = l.MapCheckrLoc.Lng,
                heading = HeadingSelector(l.MapCheckrLoc) % 360,
                zoom = output.GlobalZoom,
                pitch = output.GlobalPitch,
                extra = TagsGenerator.Tags(l.Loc, l.MapCheckrLoc, output.LocationTags),
                panoId = output.PanoIdCountryCodes.Contains(l.Loc.Nominatim.CountryCode) || output.PanoIdCountryCodes.Contains("*") ? l.MapCheckrLoc.panoId : null,
                countryCode = includeAdditionalLocationInfo ? l.Loc.Nominatim.CountryCode : null,
                subdivisionCode = includeAdditionalLocationInfo ? l.Loc.Nominatim.SubdivisionCode : null,
                locationId = includeAdditionalLocationInfo ? l.Loc.LocationId.ToString() : null,
            }).ToArray();
        var definitionFilename = Path.GetFileNameWithoutExtension(definitionPath);
        var filename = definitionFilename + (isPreview ? "-preview-locations.json" : "-locations.json");
        var locationsPath = Path.Combine(outFolder, filename);
        await File.WriteAllTextAsync(locationsPath, Serializer.Serialize(geoMapLocations));

        if (!isPreview)
        {
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
                        return $"{x.Key}\t{x.Count()}\t{goal.regionGoalCount}\t{goal.minDistance}m.";
                    }));
        }

        return (new FileInfo(locationsPath).FullName, geoMapLocations.Length);
    }

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
                // Missing/unreadable file contributes 0; the work item body surfaces any real error.
            }
        }

        return (long)(onDisk * LiveBytesPerFileByte);
    }

    private sealed record WorkItem(
        string CountryCode,
        long EstimatedCostBytes,
        string? SubdivisionCode,
        Func<(IList<Location> locations, int regionGoalCount, int minDistance)[]> Run);
}
