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
    public const double MinZoom = 0.435;
    public const double MaxZoom = 3.36;
    private static readonly Dictionary<string, IReadOnlyCollection<MapCheckrLocation>> Roads = [];
    private static readonly Dictionary<string, IList<MapCheckrLocation>> Countries = [];
    private static HashSet<string> _panoIds = [];
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
        await DataDownloadService.EnsureRoadFilesDownloaded();

        try
        {
            var existingLocations = await ReadExistingLocations(definitionPath);
            foreach (var locsByCountry in existingLocations.GroupBy(c => c.countryCode))
            {
                if (locsByCountry.Key != null)
                {
                    Countries[locsByCountry.Key] = locsByCountry.ToList();
                }
            }

            var locationFilterFunc = map.LocationFilter switch
            {
                { Length: > 0 } => LocationLakeFilterer.CompileExpression<MapCheckrLocation, bool>(map.LocationFilter, true),
                _ => _ => true
            };

            _panoIds = Countries.SelectMany(x => x.Value.Select(y => y.panoId)).ToHashSet();
            var geoJsons = new List<Geometry>();
            foreach (var geoJsonFile in map.GeoJsonFiles)
            {
                geoJsons.AddRange(await GeoJsonSerialization.DeserializeFromFile(geoJsonFile));
            }

            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    ConsoleLogger.Info("Press 's' to stop.");
                    ProgressTask? defaultTask = null;

                    var overshootFactor = map.Distribution?.OvershootFactor ?? 1;
                    while (map.Countries.Any(c => !Countries.TryGetValue(c.Key, out var locs) || locs.Count < c.Value * overshootFactor))
                    {
                        foreach (var (countryCode, count) in map.Countries)
                        {
                            var locationCount = count * overshootFactor;
                            if (Countries.TryGetValue(countryCode, out var locs) && locs.Count >= locationCount)
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
                            var boxPrecision = (HashPrecision)(10 - map.BoxPrecision);
                            var radius = map.Radius;
                            var chunkSize = map.ParallelRequests;
                            var rejectLocationsWithoutDescription = map.RejectLocationsWithoutDescription;
                            var acceptedCoverage = map.AcceptedCoverage;
                            var locations = await LocationsInCountry(countryCode, locationCount, task, definitionPath, map.FromDate, map.ToDate, boxPrecision, radius, chunkSize, locationFilterFunc, map.PanoSelectionStrategy, geoJsons, rejectLocationsWithoutDescription, acceptedCoverage);
                            await StoreMap(definitionPath, map, false);
                            task.StopTask();
                        }
                    }
                });
        }
        catch (PleaseStopException)
        {
            // User wanted to stop.
        }

        var (path, count) = await StoreMap(definitionPath, map, true);
        if (path != null)
        {
            ConsoleLogger.Success($"{count} locations saved to {path}");
        }
    }

    private static async Task<(string? path, int locationCount)> StoreMap(string? definitionPath, LiveGenerateMapDefinition mapDefinition, bool finalIteration)
    {
        var outFolder = Path.GetDirectoryName(definitionPath);
        if (outFolder is null)
        {
            ConsoleLogger.Error($"Can't get correct folder from path {definitionPath}");
            return (null, 0);
        }

        var definitionFilename = Path.GetFileNameWithoutExtension(definitionPath);
        var distributedLocationsPath = Path.Combine(outFolder, definitionFilename + "-locations-distributed.json");
        var undistributedLocationsPath = Path.Combine(outFolder, definitionFilename + "-locations.json");
        await File.WriteAllTextAsync(undistributedLocationsPath, Serializer.Serialize(Countries.SelectMany(x => x.Value).Select(Map)));
        if (mapDefinition.Distribution != null && finalIteration)
        {
            var distributed = mapDefinition.Countries
                .Select(c => LocationDistributor.DistributeEvenly<MapCheckrLocation, string>(Countries.TryGetValue(c.Key, out var countryLocs) ? countryLocs : [], minDistanceBetweenLocations: mapDefinition.Distribution.MinMinDistance, silent: true).TakeRandom(c.Value).ToList())
                .SelectMany(x => x)
                .Select(Map);
            await File.WriteAllTextAsync(distributedLocationsPath, Serializer.Serialize(distributed));
        }

        return (undistributedLocationsPath, Countries.Sum(x => x.Value.Count));

        LocationLakeMapGenerator.GeoMapLocation Map(MapCheckrLocation l)
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

    private static async Task<IList<MapCheckrLocation>> LocationsInCountry(
        string countryCode,
        int goalCount,
        ProgressTask task,
        string definitionPath,
        string? mapFromDate,
        string? mapToDate,
        HashPrecision boxPrecision,
        int radius,
        int chunkSize,
        Func<MapCheckrLocation, bool> locationFilterFunc,
        string? selectionStrategy,
        IReadOnlyCollection<Geometry> geoJsons,
        bool rejectLocationsWithoutDescription,
        string? acceptedCoverage)
    {
        var roads = await GetRoads(countryCode, geoJsons);
        var candidateLocations = Countries.TryGetValue(countryCode, out var locs)
            ? locs
            : (await ReadExistingLocations(definitionPath)).Where(c => c.countryCode == countryCode).ToList();
        task.Value(candidateLocations.Count);
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

        while (candidateLocations.Count < goalCount)
        {
            Countries[countryCode] = candidateLocations.Where(c => c.countryCode == countryCode).ToList();

            var exitRequested = Console.KeyAvailable && Console.ReadKey().KeyChar == 's';
            if (exitRequested)
            {
                throw new PleaseStopException();
            }

            var locations = geoJsons.Count == 0
                ? roads.TakeRandom(500).Select(x =>
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
                : RandomLocationsFromGeoJsons(geoJsons);
            var panoStrategy = Enum.TryParse<GoogleApi.PanoStrategy>(selectionStrategy, true, out var s)
                ? s
                : GoogleApi.PanoStrategy.Newest;
            var googleLocations = await GoogleApi.GetLocations(locations, countryCode, chunkSize: chunkSize, radius: radius, rejectLocationsWithoutDescription: rejectLocationsWithoutDescription, silent: true, selectionStrategy: panoStrategy, countryPanning: null);
            var validForAdding = googleLocations
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
                candidateLocations.Add(location);
                _panoIds.Add(location.panoId);
            }

            task.Value(candidateLocations.Count);
        }

        var undistributedLocations = candidateLocations.Where(c => c.countryCode == countryCode).ToList();
        Countries[countryCode] = undistributedLocations;
        return candidateLocations;
    }

    private static MapCheckrLocation[] RandomLocationsFromGeoJsons(IReadOnlyCollection<Geometry> geoJsons)
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
        return Enumerable.Range(0, 5000).Select(_ =>
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

    private static async Task<IReadOnlyCollection<MapCheckrLocation>> GetRoads(
        string countryCode,
        IReadOnlyCollection<Geometry> geoJsons)
    {
        if (geoJsons.Count > 0)
        {
            return [];
        }

        if (Roads.TryGetValue(countryCode, out var roads))
        {
            return roads;
        }

        var result = new List<MapCheckrLocation>();
        foreach (var file in Directory.GetFiles(Path.Combine(DataDownloadService.RoadsFolder(), countryCode)))
        {
            var points = await File.ReadAllLinesAsync(file);
            var mapCheckrLocations = points.Select((p, i) =>
            {
                var parts = p.Split(',');
                return new MapCheckrLocation
                {
                    lat = parts[0].ParseAsDouble(),
                    lng = parts[1].ParseAsDouble(),
                    locationId = $"{countryCode}-{i}"
                };
            })
            .Where(x =>
            {
                var point = geoJsons.Count == 0 ? null : new Point(x.lng, x.lat);
                return geoJsons.Count == 0 || geoJsons.Any(g => g.Covers(point));
            });
            result.AddRange(mapCheckrLocations);
        }

        Roads[countryCode] = result;
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
}

public class PleaseStopException : Exception
{
}