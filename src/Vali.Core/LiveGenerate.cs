using System.Globalization;
using Geohash;
using Spectre.Console;
using Vali.Core.Data;
using Vali.Core.Google;
using Vali.Core.Hash;

namespace Vali.Core;

public class LiveGenerate
{
    public static async Task Generate(LiveGenerateMapDefinition map, string definitionPath)
    {
        var locations = new List<MapCheckrLocation>();
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                foreach (var country in map.CountryCodes)
                {
                    var locationCount = map.LocationCountGoal;
                    var task = ctx.AddTask($"[green]{CountryCodes.Name(country)} {locationCount} locations.[/]", maxValue: locationCount);
                    await Task.Delay(5);

                    locations.AddRange(await LocationsInCountry(country, locationCount, map, task));
                    task.StopTask();
                }
            });

        await StoreMap(definitionPath, locations, map);
    }

    private static async Task StoreMap(string? definitionPath, IReadOnlyCollection<MapCheckrLocation> locations, LiveGenerateMapDefinition mapDefinition)
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
        var geoMapLocations = locations
            .Select(l => new LocationLakeMapGenerator.GeoMapLocation
            {
                lat = l.Lat,
                lng = l.Lng,
                heading = Heading(l, mapDefinition),
                pitch = Pitch(mapDefinition),
                zoom = Zoom(mapDefinition),
                extra = Tags(mapDefinition, l),
                panoId = l.panoId
            }).ToArray();
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
        LiveGenerateMapDefinition map,
        ProgressTask task)
    {
        var roads = await Roads(countryCode);
        var candidateLocations = new List<MapCheckrLocation>();
        var counter = 0;
        var factor = 1;
        var minDistance = map.MinMinDistance;
        var fromDate = !string.IsNullOrEmpty(map.FromDate)
            ? DateTime.ParseExact(map.FromDate, "yyyy-MM", CultureInfo.InvariantCulture)
            : (DateTime?)null;
        var toDate = !string.IsNullOrEmpty(map.ToDate)
            ? DateTime.ParseExact(map.ToDate, "yyyy-MM", CultureInfo.InvariantCulture)
            : (DateTime?)null;
        while (candidateLocations.Count < goalCount * factor)
        {
            var exitRequested = Console.KeyAvailable && Console.ReadKey().KeyChar == 's';
            if (exitRequested)
            {
                break;
            }

            var precision = HashPrecision.Size_km_5x5;
            var sampleRoads = roads.TakeRandom(Math.Min(goalCount - candidateLocations.Count, 2500));
            var locations = sampleRoads.Select(x =>
            {
                var (lat, lng) = RandomPointInPoly(Hasher.GetBoundingBox(Hasher.Encode(x.lat, x.lng, HashPrecision.Size_m_153x153)));
                return new MapCheckrLocation
                {
                    lat = lat,
                    lng = lng,
                    locationId = (counter++).ToString()
                };
            })
            .Where(x => candidateLocations.All(c => Extensions.CalculateDistance(c.Lat, c.Lng, x.lat, x.lng) > minDistance))
            .ToArray();
            var googleLocations = await GoogleApi.GetLocations(locations, countryCode, 100_000, radius: 20, rejectLocationsWithoutDescription: true, silent: true, selectionStrategy: GoogleApi.PanoStrategy.Newest);
            var validForAdding = googleLocations
                .Where(x => x.result == GoogleApi.LocationLookupResult.Valid)
                .Select(x => x.location)
                .Where(x => x.countryCode == countryCode)
                .Where(x => fromDate == null || (x is { year: > 0, month: > 0 } && new DateTime(x.year, x.month, 1) >= fromDate))
                .Where(x => toDate == null || (x is { year: > 0, month: > 0 } && new DateTime(x.year, x.month, 1) <= toDate));
            foreach (var location in validForAdding)
            {
                var hash = Hasher.Encode(location.lat, location.lng, precision);
                var neighbours = Hasher.Neighbors(hash);
                if (candidateLocations.Where(x => x.hash == hash || neighbours.Any(n => n.Value == hash)).All(c => Extensions.CalculateDistance(c.Lat, c.Lng, location.lat, location.lng) > minDistance))
                {
                    candidateLocations.Add(location with
                    {
                        hash = hash
                    });
                }
            }
            task.Value(candidateLocations.Count / (double)factor);
        }

        var locationsInCountry = LocationDistributor.DistributeEvenly<MapCheckrLocation, string>(candidateLocations, minDistance, silent: true);
        return locationsInCountry;
    }

    private static async Task<IReadOnlyCollection<(double lat, double lng)>> Roads(string countryCode)
    {
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
}