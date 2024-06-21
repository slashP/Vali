using System.Text.Json.Nodes;
using System.Text;
using System.Text.Json.Serialization;
using System;

namespace Vali.Core.Google;

public class GoogleApi
{
    private static readonly HttpClient _client;

    static GoogleApi()
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri("https://maps.googleapis.com")
        };
        _client.DefaultRequestHeaders.Add("x-user-agent", "grpc-web-javascript/0.1");
    }

    public static async Task<IReadOnlyCollection<MapCheckrLocation>> GetVerifiedLocations(
        IReadOnlyCollection<MapCheckrLocation> locations,
        string countryCode,
        int chunkSize,
        bool silent = false,
        bool rejectLocationsWithoutDescription = true)
    {
        var result = new List<MapCheckrLocation>(locations.Count);
        var counter = 0;
        var startTime = DateTime.UtcNow;
        foreach (var chunk in locations.Chunk(chunkSize))
        {
            var locs = await Task.WhenAll(chunk.Select(x => GetVerifiedLocation(x, rejectLocationsWithoutDescription, knownCountryCode: countryCode, selectionStrategy: PanoStrategy.Newest)));
            result.AddRange(locs.Where(x => x is { location: not null, result: LocationLookupResult.Valid }).Select(x => x.location));
            counter += chunkSize;
            var locationsCount = counter * 100 / (decimal)locations.Count;
            if (!silent)
            {
                Console.WriteLine($"{DateTime.UtcNow - startTime}. {result.Count,6} locations in {countryCode}. {counter,6} calls. {decimal.Round(locationsCount, 1),4} %");
            }
        }

        return result;
    }

    public static async Task<IReadOnlyCollection<(MapCheckrLocation location, LocationLookupResult result)>>
        GetLocations(
            IEnumerable<MapCheckrLocation> locations,
            string countryCode,
            int chunkSize,
            int radius,
            bool rejectLocationsWithoutDescription,
            bool silent,
            PanoStrategy selectionStrategy) =>
        silent
            ? await locations.RunLimitedNumberAtATime(
                x => GetVerifiedLocation(x, rejectLocationsWithoutDescription, knownCountryCode: countryCode, selectionStrategy: selectionStrategy, radius: radius), chunkSize)
            : await locations.RunLimitedNumberAtATimeWithProgressBar(
                x => GetVerifiedLocation(x, rejectLocationsWithoutDescription, knownCountryCode: countryCode, selectionStrategy: selectionStrategy, radius: radius),
                chunkSize,
                "Verifying locations by calling Google APIs.");

    public static async Task<(MapCheckrLocation location, LocationLookupResult result)> GetVerifiedLocation(
        MapCheckrLocation location,
        bool rejectLocationsWithoutDescription,
        string knownCountryCode,
        PanoStrategy selectionStrategy,
        int radius = 5)
    {
        var body = $@"
[[""apiv3"",null,null,null,""US"",null,null,null,null,null,[[false]]],[[null,null,{location.lat.Format()},{location.lng.Format()}],{radius}],[null,[""en"",""US""],null,null,null,null,null,null,[2],null,[[[2,true,2],[3,true,2],[10,true,2]]]],[[1,2,3,4,8,6]]]";
        string content;
        try
        {
            var responseMessage = await _client.PostAsync("$rpc/google.internal.maps.mapsjs.v1.MapsJsInternalService/SingleImageSearch", new StringContent(body, Encoding.UTF8, "application/json+protobuf"));
            content = await responseMessage.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await Task.Delay(TimeSpan.FromSeconds(5));
            return (location, LocationLookupResult.UnknownError);
        }

        try
        {
            var locationResponse = JsonNode.Parse(content);
            if (locationResponse.AsArray().Count == 2 && locationResponse[1] is JsonValue && locationResponse[1].GetValue<string>() == "Internal error encountered.")
            {
                Console.WriteLine("Got Internal error encountered.");
                await Task.Delay(TimeSpan.FromSeconds(3));
                return (location, LocationLookupResult.UnknownError);
            }

            if (locationResponse.AsArray().Count == 1 && locationResponse[0][2].GetValue<string>() == "Search returned no images.")
            {
                return (location, LocationLookupResult.NoImages);
            }

            var copyright = locationResponse[1][4][0][0][0][0].GetValue<string>();
            var descriptionNode = locationResponse[1][3];
            var desc = descriptionNode.AsArray().Count >= 3 && descriptionNode[2] != null
                ?
                descriptionNode[2][0][0].GetValue<string>()
                : descriptionNode.AsArray().Count >= 3 && descriptionNode[0] != null
                    ? descriptionNode[0][0].GetValue<string>()
                    : null;

            var baseInfoArray = locationResponse[1][5][0].AsArray();
            var countryCode = baseInfoArray[1].AsArray().Count >= 5 ? baseInfoArray[1][4].GetValue<string>() : knownCountryCode;
            var year = locationResponse[1][6][7][0].GetValue<int>();
            var month = locationResponse[1][6][7][1].GetValue<int>();

            var pano = locationResponse[1][1][1].GetValue<string>();
            var defaultImage = new
            {
                Year = year,
                Month = month,
                PanoId = pano
            };
            var alternativeImages = (baseInfoArray.Count > 8 && baseInfoArray[8]?.AsArray() != null
                ? new[] { defaultImage }.Concat(baseInfoArray[8].AsArray().Select(x =>
                {
                    var index = x[0].GetValue<int>();
                    return new
                    {
                        Year = x[1][0].GetValue<int>(),
                        Month = x[1][1].GetValue<int>(),
                        PanoId = baseInfoArray[3][0][index][0][1].GetValue<string>()
                    };
                })).ToArray()
                : [defaultImage])
                .Where(x => x.PanoId.Length < 36)
                .DistinctBy(x => x.PanoId)
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToArray();
            if (!alternativeImages.Any()) // all Ari
            {
                return (location, LocationLookupResult.Ari);
            }

            var isGen1Possible = IsGen1Possible(countryCode, year);
            if (isGen1Possible && await IsGen1(pano) != false)
            {
                return (location, LocationLookupResult.Gen1);
            }

            var lat = baseInfoArray[1][0][2].GetValue<double>();
            var lng = baseInfoArray[1][0][3].GetValue<double>();
            var arrows = baseInfoArray.Count > 6 ? baseInfoArray[6]?.AsArray() ?? [] : [];
            var heading = arrows switch
            {
                { Count: > 2 } => arrows.Select(e => e[1][3].GetValue<decimal>()).GetPermutations(2).MinBy(x => Math.Abs(x.Max() - x.Min() - 180)).TakeRandom(1).Single(),
                { Count: > 0 } => arrows[Random.Shared.Next(0, arrows.Count)][1][3].GetValue<decimal>(),
                _ => baseInfoArray[1][2][0].GetValue<decimal>()
            };
            var c = new[] { baseInfoArray[1][2][0].GetValue<decimal>(), heading };
            var d = new[] { 0, 180, 360 }.Select(x => Math.Abs(c.Max() - x - c.Min())).Min();
            if (rejectLocationsWithoutDescription && string.IsNullOrEmpty(desc))
            {
                return (location, LocationLookupResult.MissingDescription);
            }

            var defaultDrivingDirectionAngle = (ushort)Math.Round(baseInfoArray[1][2][0].GetValue<decimal>(), 0);
            var defaultArrowCount = (ushort)arrows.Count;
            var selected = selectionStrategy switch
            {
                PanoStrategy.Newest => alternativeImages.FirstOrDefault(),
                PanoStrategy.Random => alternativeImages.Where(x => !IsGen1Possible(countryCode, x.Year)).TakeRandom(1).SingleOrDefault(),
                PanoStrategy.RandomNotNewest => alternativeImages.Where(x => !IsGen1Possible(countryCode, x.Year)).Skip(1).TakeRandom(1).SingleOrDefault(),
                PanoStrategy.RandomAvoidNewest => alternativeImages.Length == 1 ? alternativeImages.Single() : alternativeImages.Where(x => !IsGen1Possible(countryCode, x.Year)).Skip(1).TakeRandom(1).SingleOrDefault(),
                PanoStrategy.RandomNotOldest => alternativeImages.Where(x => !IsGen1Possible(countryCode, x.Year)).Reverse().Skip(1).TakeRandom(1).SingleOrDefault(),
                PanoStrategy.RandomAvoidOldest => alternativeImages.Length == 1 ? alternativeImages.Single() : alternativeImages.Where(x => !IsGen1Possible(countryCode, x.Year)).Reverse().Skip(1).TakeRandom(1).SingleOrDefault(),
                PanoStrategy.SecondNewest => alternativeImages.Where(x => !IsGen1Possible(countryCode, x.Year)).Skip(1).FirstOrDefault(),
                PanoStrategy.Oldest => alternativeImages.Where(x => !IsGen1Possible(countryCode, x.Year)).Reverse().FirstOrDefault(),
                PanoStrategy.SecondOldest => alternativeImages.Where(x => !IsGen1Possible(countryCode, x.Year)).Reverse().Skip(1).FirstOrDefault(),
                _ => throw new ArgumentOutOfRangeException(nameof(selectionStrategy), selectionStrategy, null)
            };
            if (selected == null)
            {
                return (location, LocationLookupResult.NoImages);
            }

            if (selected.PanoId != pano)
            {
                var (isGen1, drivingDirectionAngle, defaultHeading, arrowCount) = await DetailsFromPanoId(selected.PanoId);
                if (isGen1 != false)
                {
                    return (location, LocationLookupResult.NoImages);
                }

                pano = selected.PanoId;
                year = selected.Year;
                month = selected.Month;
                defaultDrivingDirectionAngle = drivingDirectionAngle;
                defaultArrowCount = (ushort)arrowCount;
                heading = defaultHeading;
            }

            if (pano.Length >= 36)
            {
                return (location, LocationLookupResult.NoImages);
            }

            return (location with
            {
                heading = Math.Round(heading, 2),
                countryCode = location.countryCode ?? countryCode,
                lat = lat,
                lng = lng,
                panoId = pano,
                year = year,
                month = month,
                drivingDirectionAngle = defaultDrivingDirectionAngle,
                arrowCount = defaultArrowCount
            }, LocationLookupResult.Valid);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed handling info. {content}");
            Console.WriteLine(e);
            await Task.Delay(TimeSpan.FromSeconds(5));
            return (location, LocationLookupResult.UnknownError);
        }
    }

    private static bool IsGen1Possible(string countryCode, int year)
    {
        return countryCode switch
        {
            "US" or "NZ" or "AU" or "FR" or "JP" or "MX" => year < 2011,
            _ => false
        };
    }

    public static async Task<bool?> IsGen1(string panoId)
    {
        var body = $"""
                    [["apiv3",null,null,null,"US",null,null,null,null,null,[[0]]],["en","US"],[[[2,"{panoId}"]]],[[1,2,3,4,8,6]]]
                    """;
        string content;
        try
        {
            var responseMessage = await _client.PostAsync("$rpc/google.internal.maps.mapsjs.v1.MapsJsInternalService/GetMetadata", new StringContent(body, Encoding.UTF8, "application/json+protobuf"));
            content = await responseMessage.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await Task.Delay(TimeSpan.FromSeconds(5));
            return null;
        }

        try
        {
            var metadataResponse = JsonNode.Parse(content);
            if (metadataResponse.AsArray().Count == 2 && metadataResponse[1] is JsonValue &&
                metadataResponse[1].GetValue<string>() == "Internal error encountered.")
            {
                Console.WriteLine("Got Internal error encountered.");
                return null;
            }

            var imageSize = metadataResponse[1][0][2][2][0].GetValue<double>();
            var isGen1 = imageSize < 2000;
            return isGen1;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed verifying gen1. {content}");
            Console.WriteLine(e);
            return null;
        }
    }

    public static async Task<(bool? isGen1, ushort drivingDirectionAngle, decimal defaultHeading, int arrowCount)> DetailsFromPanoId(string panoId)
    {
        var body = $"""
                    [["apiv3",null,null,null,"US",null,null,null,null,null,[[0]]],["en","US"],[[[2,"{panoId}"]]],[[1,2,3,4,8,6]]]
                    """;
        string content;
        try
        {
            var responseMessage = await _client.PostAsync("$rpc/google.internal.maps.mapsjs.v1.MapsJsInternalService/GetMetadata", new StringContent(body, Encoding.UTF8, "application/json+protobuf"));
            content = await responseMessage.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await Task.Delay(TimeSpan.FromSeconds(5));
            return (null, 0, 0, 0);
        }

        try
        {
            var metadataResponse = JsonNode.Parse(content);
            if (metadataResponse.AsArray().Count == 2 && metadataResponse[1] is JsonValue &&
                metadataResponse[1].GetValue<string>() == "Internal error encountered.")
            {
                Console.WriteLine("Got Internal error encountered.");
                return (null, 0, 0, 0);
            }

            if (metadataResponse.AsArray().Count == 1)
            {
                return (null, 0, 0, 0);
            }

            var imageSize = metadataResponse[1][0][2]?[2]?[0]?.GetValue<double>() ?? 0;
            var isGen1 = imageSize < 2000;
            var arrows = metadataResponse[1][0][5]?[0]?[6]?.AsArray() ?? [];
            var drivingDirectionAngle = (ushort)Math.Round(metadataResponse[1][0][5][0][1][2][0].GetValue<decimal>(), 0);
            var a = new MapCheckrLocation
            {
                drivingDirectionAngle = drivingDirectionAngle,
            };
            var heading = arrows switch
            {
                { Count: > 2 } => arrows.Select(e => e[1][3].GetValue<decimal>()).GetPermutations(2).MinBy(x => Math.Abs(x.Max() - x.Min() - 180)).TakeRandom(1).Single(),
                { Count: > 0 } => arrows[Random.Shared.Next(0, arrows.Count)][1][3].GetValue<decimal>(),
                _ => drivingDirectionAngle
            };
            return (isGen1, drivingDirectionAngle, heading, arrows.Count);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed verifying gen1. {content}");
            Console.WriteLine(e);
            return (null, 0, 0, 0);
        }
    }

    public enum LocationLookupResult
    {
        DefinitelyInvalid,
        UnknownError,
        MissingDescription,
        Valid,
        NoCountryCode,
        Gen1,
        NoImages,
        Ari
    }

    public enum PanoStrategy
    {
        None,
        Newest,
        Random,
        RandomNotNewest,
        RandomAvoidNewest,
        RandomNotOldest,
        RandomAvoidOldest,
        SecondNewest,
        Oldest,
        SecondOldest
    }
}

public record MapCheckrLocation : IDistributionLocation<string>
{
    public double lat { get; set; }
    public double lng { get; set; }
    public decimal heading { get; set; }
    public decimal pitch { get; set; }
    public string locationId { get; set; }
    public string source { get; set; }
    public string hash { get; set; }
    public string replacesLocationId { get; set; }
    public string countryCode { get; set; }
    public string? subdivisionCode { get; set; }
    public int? minDistance { get; set; }
    public string panoId { get; set; }
    public int? region { get; set; }
    public int year { get; set; }
    public int month { get; set; }
    public ushort drivingDirectionAngle { get; set; }
    public ushort arrowCount { get; set; }
    [JsonIgnore]
    public double Lat => lat;
    [JsonIgnore]
    public double Lng => lng;
    [JsonIgnore]
    public string LocationId => locationId;
}
