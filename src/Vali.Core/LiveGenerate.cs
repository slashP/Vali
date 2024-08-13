using System.Globalization;
using System.Text.Json;
using Geohash;
using NetTopologySuite.Geometries;
using Spectre.Console;
using Vali.Core.Data;
using Vali.Core.Google;
using Vali.Core.Hash;

namespace Vali.Core;

public class LiveGenerate
{
    private static readonly Dictionary<string, IReadOnlyCollection<(double lat, double lng)>> _roads = new();
    private static readonly Dictionary<string, IList<MapCheckrLocation>> _countries = new();
    private static readonly HashSet<string> _panoIds = new();

    public static async Task Generate(LiveGenerateMapDefinition map, string definitionPath)
    {
        try
        {
            var existingLocations = await ReadExistingLocations(definitionPath);
            foreach (var locsByCountry in existingLocations.GroupBy(c => c.countryCode))
            {
                _countries[locsByCountry.Key] = locsByCountry.ToList();
            }

            foreach (var mapCheckrLocation in _countries.SelectMany(x => x.Value))
            {
                _panoIds.Add(mapCheckrLocation.panoId);
            }

            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    ProgressTask defaultTask = null;

                    while (map.Countries.Any(c => !_countries.TryGetValue(c.Key, out var locs) || locs.Count < c.Value))
                    {
                        foreach (var (countryCode, locationCount) in map.Countries)
                        {
                            if (_countries.TryGetValue(countryCode, out var locs) && locs.Count >= locationCount)
                            {
                                continue;
                            }

                            var task = map.Countries switch
                            {
                                { Count: 1 } => defaultTask ??=
                                    ctx.AddTask(
                                        $"[green]{CountryCodes.Name(countryCode)} {locationCount} locations.[/]", maxValue: locationCount),
                                _ => ctx.AddTask(
                                    $"[green]{CountryCodes.Name(countryCode)} {locationCount} locations.[/]", maxValue: locationCount)
                            };
                            await Task.Delay(5);
                            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                            var locations = await LocationsInCountry(countryCode, locationCount, task, definitionPath, cts.Token, map.FromDate, map.ToDate);
                            await StoreMap(definitionPath, map);
                            task.StopTask();
                        }
                    }
                });
        }
        catch (PleaseStopException)
        {
            // User wanted to stop.
        }

        await StoreMap(definitionPath, map);
    }

    private static async Task StoreMap(string? definitionPath, LiveGenerateMapDefinition mapDefinition)
    {
        var outFolder = Path.GetDirectoryName(definitionPath);
        if (outFolder is null)
        {
            ConsoleLogger.Error($"Can't get correct folder from path {definitionPath}");
            return;
        }

        var definitionFilename = Path.GetFileNameWithoutExtension(definitionPath);
        var filename = definitionFilename + "-locations.json";
        var locationsPath = Path.Combine(outFolder, filename);
        var geoMapLocations = _countries
            .SelectMany(c => c.Value)
            .Select(l => new LocationLakeMapGenerator.GeoMapLocation
            {
                lat = l.Lat,
                lng = l.Lng,
                heading = Heading(l, mapDefinition),
                pitch = Pitch(mapDefinition),
                zoom = Zoom(mapDefinition),
                extra = Tags(mapDefinition, l),
                panoId = l.panoId,
                countryCode = l.countryCode
            })
            .ToArray();
        await File.WriteAllTextAsync(locationsPath, Serializer.Serialize(geoMapLocations));
    }

    private static double Heading(MapCheckrLocation l, LiveGenerateMapDefinition mapDefinition) =>
        mapDefinition.HeadingMode switch
        {
            "DrivingDirection" => l.drivingDirectionAngle + mapDefinition.HeadingDelta,
            _ => (double)(l.heading + mapDefinition.HeadingDelta)
        };

    private static int? Pitch(LiveGenerateMapDefinition mapDefinition) =>
        mapDefinition.PitchMode switch
        {
            "Random" => Random.Shared.Next(mapDefinition.RandomPitchMin ?? -89, mapDefinition.RandomPitchMax ?? 89),
            _ => mapDefinition.Pitch
        };

    private static double? Zoom(LiveGenerateMapDefinition mapDefinition) =>
        mapDefinition.ZoomMode switch
        {
            "Random" => Math.Round(Random.Shared.Next((int)((mapDefinition.RandomZoomMin ?? 0.435) * 100), (int)((mapDefinition.RandomZoomMax ?? 3.36) * 100)) / 1000d, 2),
            _ => mapDefinition.Zoom
        };

    private static async Task<IList<MapCheckrLocation>> LocationsInCountry(
        string countryCode,
        int goalCount,
        ProgressTask task,
        string definitionPath,
        CancellationToken ct,
        string? mapFromDate,
        string? mapToDate)
    {
        var roads = await Roads(countryCode);
        var candidateLocations = _countries.TryGetValue(countryCode, out var locs)
            ? locs
            : (await ReadExistingLocations(definitionPath)).Where(c => c.countryCode == countryCode).ToList();
        task.Value(candidateLocations.Count);
        var counter = 0;
        var fromDate = !string.IsNullOrEmpty(mapFromDate)
            ? DateTime.ParseExact(mapFromDate, "yyyy-MM", CultureInfo.InvariantCulture)
            : (DateTime?)null;
        var toDate = !string.IsNullOrEmpty(mapToDate)
            ? DateTime.ParseExact(mapToDate, "yyyy-MM", CultureInfo.InvariantCulture)
            : (DateTime?)null;
        while (candidateLocations.Count < goalCount)
        {
            _countries[countryCode] = candidateLocations.Where(c => c.countryCode == countryCode).ToList();
            if (ct.IsCancellationRequested)
            {
                break;
            }

            var exitRequested = Console.KeyAvailable && Console.ReadKey().KeyChar == 's';
            if (exitRequested)
            {
                throw new PleaseStopException();
            }

            var sampleRoads = roads.TakeRandom(2500);
            var locations = sampleRoads.Select(x =>
            {
                var (lat, lng) = RandomPointInPoly(Hasher.GetBoundingBox(Hasher.Encode(x.lat, x.lng, HashPrecision.Size_km_1x1)));
                return new MapCheckrLocation
                {
                    lat = lat,
                    lng = lng,
                    locationId = (counter++).ToString()
                };
            })
            .ToArray();
            var googleLocations = await GoogleApi.GetLocations(locations, countryCode, chunkSize: 100, radius: 300, rejectLocationsWithoutDescription: true, silent: true, selectionStrategy: GoogleApi.PanoStrategy.Newest);
            var validForAdding = googleLocations
                .Where(x => x.result == GoogleApi.LocationLookupResult.Valid)
                .Select(x => x.location)
                .Where(x => x.countryCode == countryCode)
                .Where(x => !_panoIds.Contains(x.panoId))
                .DistinctBy(x => x.panoId)
                .DistinctBy(x => (x.lat, x.lng))
                .Where(x => fromDate == null || (x is { year: > 0, month: > 0 } && new DateTime(x.year, x.month, 1) >= fromDate))
                .Where(x => toDate == null || (x is { year: > 0, month: > 0 } && new DateTime(x.year, x.month, 1) <= toDate));
            foreach (var location in validForAdding)
            {
                candidateLocations.Add(location);
                _panoIds.Add(location.panoId);
            }

            task.Value(candidateLocations.Count);
        }

        _countries[countryCode] = candidateLocations.Where(c => c.countryCode == countryCode).ToList();
        return candidateLocations;
    }

    private static async Task<IReadOnlyCollection<(double lat, double lng)>> Roads(string countryCode)
    {
        if (_roads.TryGetValue(countryCode, out var roads))
        {
            return roads;
        }

        var result = new List<(double lat, double lng)>();
        foreach (var file in Directory.GetFiles(Path.Combine(@"C:\dev\priv\map-data\roads", countryCode)))
        {
            var points = await File.ReadAllLinesAsync(file);
            result.AddRange(points.Select(p =>
            {
                var parts = p.Split(',');
                return (parts[0].ParseAsDouble(), parts[1].ParseAsDouble());
            }));
        }

        _roads[countryCode] = result;
        return result;
    }

    private static (double lat, double lng) RandomPointInPoly(BoundingBox boundingBox)
    {
        var xMin = boundingBox.MinLng;
        var xMax = boundingBox.MaxLng;
        var yMin = boundingBox.MinLat;
        var yMax = boundingBox.MaxLat;
        var lat = (Math.Asin(
                       Random.Shared.NextDouble() *
                       (Math.Sin((yMax * Math.PI) / 180) -
                        Math.Sin((yMin * Math.PI) / 180)) +
                       Math.Sin((yMin * Math.PI) / 180)
                   ) *
                   180) /
                  Math.PI;
        var lng = xMin + Random.Shared.NextDouble() * (xMax - xMin);
        return (lat, lng);
    }

    private static LocationLakeMapGenerator.GeoMapLocationExtra? Tags(LiveGenerateMapDefinition mapDefinition, MapCheckrLocation l) =>
        mapDefinition.LocationTags.Any()
            ? new LocationLakeMapGenerator.GeoMapLocationExtra
            {
                tags = mapDefinition.LocationTags.Select(e => e switch
                {
                    "Year" => TagsGenerator.Year(l.year),
                    "Month" => TagsGenerator.Month(l.month),
                    "YearMonth" => TagsGenerator.YearMonth(l.year, l.month),
                    "Season" => TagsGenerator.Season(l.countryCode, l.month),
                    _ => null
                }).Where(x => x != null).Select(x => x!).ToArray()
            }
            : null;

    private static async Task<List<MapCheckrLocation>> ReadExistingLocations(string definitionPath)
    {
        var outFolder = Path.GetDirectoryName(definitionPath);
        if (outFolder is null)
        {
            ConsoleLogger.Error($"Can't get correct folder from path {definitionPath}");
            return [];
        }

        var definitionFilename = Path.GetFileNameWithoutExtension(definitionPath);
        var filename = definitionFilename + "-locations.json";
        var locationsPath = Path.Combine(outFolder, filename);
        var existingLocations = File.Exists(locationsPath)
            ? JsonSerializer.Deserialize<List<MapCheckrLocation>>(await File.ReadAllTextAsync(locationsPath)) ?? []
            : [];
        return existingLocations.Select(x => x with
        {
            locationId = Guid.NewGuid().ToString("N")
        }).ToList();
    }
}

public class PleaseStopException : Exception
{
}