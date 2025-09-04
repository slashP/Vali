using System.Diagnostics;
using System.Globalization;
using Geohash;
using Spectre.Console;
using Vali.Core.Data;
using Vali.Core.Google;
using Vali.Core.Hash;
using Geometry = NetTopologySuite.Geometries.Geometry;
using Point = NetTopologySuite.Geometries.Point;

namespace Vali.Core;

public class LiveGenerate
{
    private static List<MapCheckrLocation> _locations = [];
    private static string _definitionPath = "";
    public const double MinZoom = 0.435;
    public const double MaxZoom = 3.36;
    private static HashSet<string> _panoIds = [];
    private static readonly Dictionary<string, int> Countries = [];
    private static readonly Location DefaultEmptyLocation = new()
    {
        Google = new GoogleData
        {
            PanoId = "",
            CountryCode = ""
        },
        Osm = new OsmData(),
        Nominatim = new NominatimData
        {
            CountryCode = "",
            SubdivisionCode = ""
        }
    };

    public static async Task Generate(LiveGenerateMapDefinition map, string definitionPath)
    {
        _definitionPath = definitionPath;
        await DataDownloadService.EnsureRoadFilesDownloaded();

        try
        {
            _locations = await ReadExistingLocations(definitionPath);
            foreach (var locsByCountry in _locations.GroupBy(c => c.countryCode))
            {
                if (locsByCountry.Key != null)
                {
                    Countries[locsByCountry.Key] = locsByCountry.Count();
                }
            }

            var locationFilterFunc = map.LocationFilter switch
            {
                { Length: > 0 } => LocationLakeFilterer.CompileExpression<MapCheckrLocation, bool>(map.LocationFilter, true),
                _ => _ => true
            };

            _panoIds = _locations.Select(x => x.panoId).ToHashSet();
            var geoJsons = new List<Geometry>();
            foreach (var geoJsonFile in map.GeoJsonFiles)
            {
                geoJsons.AddRange(GeoJsonSerialization.DeserializeFromFile(geoJsonFile));
            }

            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    ConsoleLogger.Info("Press 's' to stop.");
                    ProgressTask? defaultTask = null;

                    var overshootFactor = map.Distribution?.OvershootFactor ?? 1;
                    while (map.Countries.Any(c => !Countries.TryGetValue(c.Key, out var locCount) || locCount < c.Value * overshootFactor))
                    {
                        foreach (var (countryCode, count) in map.Countries)
                        {
                            var locationCount = count * overshootFactor;
                            if (Countries.TryGetValue(countryCode, out var locCount) && locCount >= locationCount)
                            {
                                continue;
                            }

                            var task = map.Countries switch
                            {
                                { Count: 1 } => defaultTask ??= ctx.AddTask(TaskDescription(countryCode, locationCount, 0), maxValue: locationCount),
                                _ => ctx.AddTask(TaskDescription(countryCode, locationCount, 0), maxValue: locationCount)
                            };
                            await Task.Delay(5);
                            var boxPrecision = (HashPrecision)(10 - map.BoxPrecision);
                            var radius = map.Radius;
                            var chunkSize = map.ParallelRequests;
                            var rejectLocationsWithoutDescription = map.RejectLocationsWithoutDescription && !LocationLakeFilterer.CountryCodesAcceptableWithoutDescription.Contains(countryCode);
                            var acceptedCoverage = map.AcceptedCoverage;
                            var batchSize = map.BatchSize;
                            var includeLinked = map.CheckLinkedPanoramas;
                            await LocationsInCountry(countryCode, locationCount, task, map.FromDate, map.ToDate, boxPrecision, radius, chunkSize, locationFilterFunc, map.PanoSelectionStrategy, map.PanoVerificationStart, map.PanoVerificationEnd, geoJsons, rejectLocationsWithoutDescription, acceptedCoverage, batchSize, includeLinked);
                            await StoreMap(map, false);
                            task.StopTask();
                        }
                    }
                });
        }
        catch (PleaseStopException)
        {
            // User wanted to stop.
        }

        var (path, mapLocationCount) = await StoreMap(map, true);
        if (path != null)
        {
            ConsoleLogger.Success($"{mapLocationCount} locations saved to {path}");
        }
    }

    private static async Task<(string? path, int locationCount)> StoreMap(
        LiveGenerateMapDefinition mapDefinition,
        bool finalIteration)
    {
        var locationsPath = await SaveLocations(mapDefinition, "-locations", _locations);
        if (mapDefinition.Distribution != null && finalIteration)
        {
            ConsoleLogger.Info("Distributing locations");
            var distributed = mapDefinition.Countries
                .Select(c => LocationDistributor.DistributeEvenly<MapCheckrLocation, string>(_locations.Where(l => l.countryCode == c.Key).ToArray(), minDistanceBetweenLocations: mapDefinition.Distribution.MinMinDistance, silent: true).TakeRandom(c.Value).ToList())
                .SelectMany(x => x)
                .ToArray();
            await SaveLocations(mapDefinition, "-locations-distributed", distributed);
        }

        return (locationsPath, _locations.Count);
    }

    private static double Heading(MapCheckrLocation l, LiveGenerateMapDefinition mapDefinition) =>
        mapDefinition.HeadingMode switch
        {
            "DrivingDirection" => l.drivingDirectionAngle + (mapDefinition.HeadingDelta ?? 0),
            "Random" => Random.Shared.Next(0, 359),
            _ => (double)(l.heading + (mapDefinition.HeadingDelta ?? 0))
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
            "Random" => Math.Round(Random.Shared.Next((int)((mapDefinition.RandomZoomMin ?? MinZoom) * 100), (int)((mapDefinition.RandomZoomMax ?? MaxZoom) * 100)) / 100d, 2),
            _ => mapDefinition.Zoom
        };

    private static async Task LocationsInCountry(
        string countryCode,
        int goalCount,
        ProgressTask task,
        string? mapFromDate,
        string? mapToDate,
        HashPrecision boxPrecision,
        int radius,
        int chunkSize,
        Func<MapCheckrLocation, bool> locationFilterFunc,
        string? selectionStrategy,
        DateOnly? panoVerificationStart,
        DateOnly? panoVerificationEnd,
        IReadOnlyCollection<Geometry> geoJsons,
        bool rejectLocationsWithoutDescription,
        string? acceptedCoverage,
        int batchSize,
        bool includeLinked)
    {
        var roads = await GetRoads(countryCode, geoJsons);
        var candidateLocationsCount = Countries.TryGetValue(countryCode, out var locCount)
            ? locCount
            : _locations.Count(l => l.countryCode == countryCode);
        task.Value(candidateLocationsCount);
        var fromDate = !string.IsNullOrEmpty(mapFromDate)
            ? DateTime.ParseExact(mapFromDate, "yyyy-MM", CultureInfo.InvariantCulture)
            : (DateTime?)null;
        var toDate = !string.IsNullOrEmpty(mapToDate)
            ? DateTime.ParseExact(mapToDate, "yyyy-MM", CultureInfo.InvariantCulture)
            : (DateTime?)null;
        var acceptedLookupResults = (acceptedCoverage switch
            {
                "Unofficial" => new[] { GoogleApi.LocationLookupResult.Ari },
                "Official" => [GoogleApi.LocationLookupResult.Valid],
                "All" => [GoogleApi.LocationLookupResult.Valid, GoogleApi.LocationLookupResult.Ari],
                _ => [GoogleApi.LocationLookupResult.Valid]
            }).Concat(rejectLocationsWithoutDescription
                ? []
                : new[] { GoogleApi.LocationLookupResult.MissingDescription })
            .ToArray();

        while (candidateLocationsCount < goalCount)
        {
            var stopwatchStartLocationCount = candidateLocationsCount;
            var stopwatch = Stopwatch.StartNew();
            var exitRequested = Console.KeyAvailable && Console.ReadKey().KeyChar == 's';
            if (exitRequested)
            {
                throw new PleaseStopException();
            }

            var locations = geoJsons.Count == 0
                ? roads.TakeRandom(batchSize).Select(x =>
                    {
                        var (lat, lng) =
                            RandomPointInPoly(Hasher.GetBoundingBox(Hasher.Encode(x.lat, x.lng, boxPrecision)));
                        return new MapCheckrLocation
                        {
                            lat = lat,
                            lng = lng,
                            locationId = Guid.NewGuid().ToString("N")
                        };
                    })
                    .ToArray()
                : RandomLocationsFromGeoJsons(geoJsons, batchSize);
            var panoStrategy = Enum.TryParse<GoogleApi.PanoStrategy>(selectionStrategy, true, out var s)
                ? s
                : GoogleApi.PanoStrategy.Newest;
            var googleLocations = await GoogleApi.GetLocations(locations, countryCode, chunkSize: chunkSize, radius: radius, rejectLocationsWithoutDescription: rejectLocationsWithoutDescription, silent: true, selectionStrategy: panoStrategy, countryPanning: null, includeLinked: includeLinked, panoVerificationStart, panoVerificationEnd, GoogleApi.BadCamStrategy.AllowForAll);
            var allLinked = includeLinked
                ? await GetLinked(googleLocations, 0, countryCode, panoStrategy, panoVerificationStart, panoVerificationEnd, chunkSize, rejectLocationsWithoutDescription, new())
                : [];
            var validForAdding = googleLocations
                .Concat(allLinked)
                .Where(x => acceptedLookupResults.Contains(x.result))
                .Select(x => x.location)
                .Where(x => x.countryCode == countryCode)
                .Where(x => !_panoIds.Contains(x.panoId))
                .DistinctBy(x => x.panoId)
                .DistinctBy(x => (x.lat, x.lng))
                .Where(locationFilterFunc)
                .Where(x => geoJsons.Count == 0 || geoJsons.Any(g => g.Covers(new Point(x.lng, x.lat))))
                .Where(x => fromDate == null || (x is { year: > 0, month: > 0 } && new DateTime(x.year, x.month, 1) >= fromDate))
                .Where(x => toDate == null || (x is { year: > 0, month: > 0 } && new DateTime(x.year, x.month, 1) <= toDate));
            foreach (var location in validForAdding)
            {
                _locations.Add(location);
                candidateLocationsCount++;
                Countries[countryCode] = candidateLocationsCount;
                _panoIds.Add(location.panoId);
            }

            task.Value(candidateLocationsCount);
            var speed = (candidateLocationsCount - stopwatchStartLocationCount) * 1000d / stopwatch.ElapsedMilliseconds;
            task.Description = TaskDescription(countryCode, goalCount, speed);
        }
    }

    private static async Task<IEnumerable<(MapCheckrLocation location, GoogleApi.LocationLookupResult result)>> GetLinked(
        IReadOnlyCollection<(MapCheckrLocation location, GoogleApi.LocationLookupResult result)> googleLocations,
        int depth,
        string countryCode,
        GoogleApi.PanoStrategy panoStrategy,
        DateOnly? panoVerificationStart,
        DateOnly? panoVerificationEnd,
        int chunkSize,
        bool rejectLocationsWithoutDescription,
        HashSet<string> tempPanoIdsChecked)
    {
        if (depth > 5)
        {
            return [];
        }

        var linked = googleLocations
            .Where(x => x.result == GoogleApi.LocationLookupResult.Valid)
            .SelectMany(x => x.location.linkedPanos)
            .DistinctBy(x => x.pano)
            .Where(x => !_panoIds.Contains(x.pano) && !tempPanoIdsChecked.Contains(x.pano))
            .Select(x => new MapCheckrLocation
            {
                lat = x.lat,
                lng = x.lng,
                locationId = x.pano
            })
            .ToArray();
        foreach (var linkedPano in linked)
        {
            tempPanoIdsChecked.Add(linkedPano.locationId);
        }

        var panoCheckedLocations = await GoogleApi.GetLocations(linked, countryCode, chunkSize: chunkSize, radius: 100, rejectLocationsWithoutDescription: rejectLocationsWithoutDescription, silent: true, selectionStrategy: panoStrategy, countryPanning: null, includeLinked: true, panoVerificationStart: panoVerificationStart, panoVerificationEnd: panoVerificationEnd, GoogleApi.BadCamStrategy.AllowForAll);
        return panoCheckedLocations.Concat(await GetLinked(panoCheckedLocations, depth + 1, countryCode, panoStrategy, panoVerificationStart, panoVerificationEnd, chunkSize, rejectLocationsWithoutDescription, tempPanoIdsChecked));
    }

    private static MapCheckrLocation[] RandomLocationsFromGeoJsons(IReadOnlyCollection<Geometry> geoJsons, int batchSize)
    {
        var totalArea = geoJsons.Sum(x => x.Area);
        var currentArea = 0d;
        var geometries = geoJsons.Select(g =>
        {
            var start = currentArea / totalArea;
            var end = (currentArea + g.Area) / totalArea;
            var cutoff = new
            {
                Envelope = g.Envelope,
                Start = start,
                End = end
            };
            currentArea += g.Area;
            return cutoff;
        }).ToArray();
        return Enumerable.Range(0, batchSize).Select(_ =>
            {
                var random = Random.Shared.NextDouble();
                var geometry = geometries.First(x => random >= x.Start && random < x.End).Envelope;
                var (lat, lng) =
                    RandomPointInPoly(new BoundingBox
                    {
                        MaxLat = geometry.Coordinates.Max(c => c.Y),
                        MinLat = geometry.Coordinates.Min(c => c.Y),
                        MaxLng = geometry.Coordinates.Max(c => c.X),
                        MinLng = geometry.Coordinates.Min(c => c.X),
                    });
                return new MapCheckrLocation
                {
                    lat = lat,
                    lng = lng,
                    locationId = Guid.NewGuid().ToString("N")
                };
            })
            .ToArray();
    }

    private static async Task<IReadOnlyCollection<(double lat, double lng)>> GetRoads(
        string countryCode,
        IReadOnlyCollection<Geometry> geoJsons)
    {
        if (geoJsons.Count > 0)
        {
            return [];
        }

        var result = new List<(double lat, double lng)>();
        foreach (var file in Directory.GetFiles(Path.Combine(DataDownloadService.RoadsFolder(), countryCode)))
        {
            var points = await File.ReadAllLinesAsync(file);
            var mapCheckrLocations = points.Select((p, i) =>
            {
                var parts = p.Split(',');
                return (parts[0].ParseAsDouble(), parts[1].ParseAsDouble());
            })
            .Where(x =>
            {
                var point = geoJsons.Count == 0 ? null : new Point(x.Item2, x.Item1);
                return geoJsons.Count == 0 || geoJsons.Any(g => g.Covers(point));
            });
            result.AddRange(mapCheckrLocations);
        }

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
            ? TagsGenerator.Tags(DefaultEmptyLocation, l, mapDefinition.LocationTags)
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
        var existingLocations = await Extensions.TryJsonDeserializeFromFileAsync<MapCheckrLocation[]>(locationsPath, []);
        return existingLocations.Select(x => x with
        {
            locationId = Guid.NewGuid().ToString("N")
        }).ToList();
    }

    private static async Task<string> SaveLocations(LiveGenerateMapDefinition definition, string filenameSuffix, IEnumerable<MapCheckrLocation> locations)
    {
        var outFolder = Path.GetDirectoryName(_definitionPath) ?? throw new Exception($"Can't get correct folder from path {_definitionPath}");
        var definitionFilename = Path.GetFileNameWithoutExtension(_definitionPath);
        var locationsPath = Path.Combine(outFolder, definitionFilename + filenameSuffix + ".json");
        await Extensions.JsonSerializeToFile(locationsPath, locations.Select(l => Map(l, definition)));
        return locationsPath;
    }

    private static LocationLakeMapGenerator.GeoMapLocation Map(MapCheckrLocation l, LiveGenerateMapDefinition mapDefinition)
    {
        return new LocationLakeMapGenerator.GeoMapLocation
        {
            lat = l.Lat,
            lng = l.Lng,
            heading = Heading(l, mapDefinition),
            pitch = Pitch(mapDefinition),
            zoom = Zoom(mapDefinition),
            extra = Tags(mapDefinition, l),
            panoId = l.panoId,
            countryCode = l.countryCode
        };
    }

    private static string TaskDescription(string countryCode, int locationCount, double? speed) =>
        $"[green]{CountryCodes.Name(countryCode)} {locationCount} locations. {(int)Math.Round(speed ?? 0, 0),5}/s.[/]";
}

public class PleaseStopException : Exception
{
}