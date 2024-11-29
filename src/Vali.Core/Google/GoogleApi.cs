using System.Text.Json.Nodes;
using System.Text;
using System.Text.Json.Serialization;
using System;
using System.Net.Http.Json;
using System.Text.Json;

namespace Vali.Core.Google;

public class GoogleApi
{
    private static readonly HttpClient _client;
    private static readonly HttpClient _shortenerClient;

    static GoogleApi()
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri("https://maps.googleapis.com")
        };
        _shortenerClient = new HttpClient();
        _client.DefaultRequestHeaders.Add("x-user-agent", "grpc-web-javascript/0.1");
    }

    public static async Task<IReadOnlyCollection<MapCheckrLocation>> GetVerifiedLocations(
        IReadOnlyCollection<MapCheckrLocation> locations,
        string countryCode,
        int chunkSize,
        Dictionary<string, string?> countryPanning,
        bool silent = false,
        bool rejectLocationsWithoutDescription = true)
    {
        var result = new List<MapCheckrLocation>(locations.Count);
        var counter = 0;
        var startTime = DateTime.UtcNow;
        foreach (var chunk in locations.Chunk(chunkSize))
        {
            var locs = await Task.WhenAll(chunk.Select(x => GetVerifiedLocation(x, rejectLocationsWithoutDescription, knownCountryCode: countryCode, selectionStrategy: PanoStrategy.Newest, countryPanning: countryPanning)));
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
            PanoStrategy selectionStrategy,
            Dictionary<string, string?>? countryPanning) =>
        silent
            ? await locations.RunLimitedNumberAtATime(
                x => GetVerifiedLocation(x, rejectLocationsWithoutDescription, knownCountryCode: countryCode, selectionStrategy: selectionStrategy, countryPanning: countryPanning, radius: radius), chunkSize)
            : await locations.RunLimitedNumberAtATimeWithProgressBar(
                x => GetVerifiedLocation(x, rejectLocationsWithoutDescription, knownCountryCode: countryCode, selectionStrategy: selectionStrategy, countryPanning: countryPanning, radius: radius),
                chunkSize,
                "Verifying locations by calling Google APIs.");

    public static async Task<(MapCheckrLocation location, LocationLookupResult result)> GetVerifiedLocation(
        MapCheckrLocation location,
        bool rejectLocationsWithoutDescription,
        string knownCountryCode,
        PanoStrategy selectionStrategy,
        Dictionary<string, string?>? countryPanning,
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
            if (locationResponse.AsArray().Count == 2 && locationResponse[1] is JsonValue && locationResponse[1].GetValue<string>() is "Internal error encountered." or "The service is currently unavailable.")
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
                return (location, LocationLookupResult.UnknownError);
            }

            if (locationResponse.AsArray().Count == 2 && locationResponse[1] is JsonValue && locationResponse[1].GetValue<string>() == "Unrecoverable data loss or corruption.")
            {
                return (location, LocationLookupResult.NoImages);
            }

            if (locationResponse.AsArray().Count == 1 && locationResponse[0][2].GetValue<string>() == "Search returned no images.")
            {
                return (location, LocationLookupResult.NoImages);
            }

            var descriptionNode = locationResponse[1][3];
            var desc = descriptionNode.AsArray().Count >= 3 && descriptionNode[2] != null
                ?
                descriptionNode[2][0][0].GetValue<string>()
                : descriptionNode.AsArray().Count >= 3 && descriptionNode[0] != null
                    ? descriptionNode[0][0].GetValue<string>()
                    : null;
            var baseInfoArray = locationResponse[1][5][0].AsArray();
            var countryCode = baseInfoArray[1].AsArray().Count >= 5 ? baseInfoArray[1][4].GetValue<string>() : knownCountryCode;
            var year = locationResponse[1][6].AsArray().Count >= 8 ? locationResponse[1][6][7][0].GetValue<int>() : -1;
            var month = locationResponse[1][6].AsArray().Count >= 8 ? locationResponse[1][6][7][1].GetValue<int>() : -1;

            var pano = locationResponse[1][1][1].GetValue<string>();
            var defaultImage = new AlternativePano
            {
                Year = year,
                Month = month,
                PanoId = pano
            };
            var alternativeImages = (baseInfoArray.Count > 8 && baseInfoArray[8]?.AsArray() != null
                ? new[] { defaultImage }.Concat(baseInfoArray[8].AsArray().Select(x =>
                {
                    var index = x[0].GetValue<int>();
                    return new AlternativePano
                    {
                        Year = x[1].AsArray().Count >= 2 ? x[1][0].GetValue<int>() : -1,
                        Month = x[1].AsArray().Count >= 2 ? x[1][1].GetValue<int>() : -1,
                        PanoId = baseInfoArray[3][0][index][0][1].GetValue<string>()
                    };
                })).ToArray()
                : [defaultImage])
                .Where(x => x.PanoId.Length < 36)
                .Where(x => x.Year > 2000)
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

            var subdivision = (descriptionNode.AsArray().Count >= 3 && descriptionNode[2] != null &&
                              descriptionNode[2].AsArray().Count >= 2
                ? descriptionNode[2]?[1]?[0]?.GetValue<string>()
                : descriptionNode.AsArray().Count >= 3 && descriptionNode[2] != null &&
                  descriptionNode[2].AsArray().Count >= 1
                    ? descriptionNode[2]?[0]?[0]?.GetValue<string>()
                    : null)?.Split(',').Last().Trim();

            var lat = baseInfoArray[1][0][2].GetValue<double>();
            var lng = baseInfoArray[1][0][3].GetValue<double>();
            var elevation = baseInfoArray[1].AsArray().Count > 1 && baseInfoArray[1][1] is not null ? (baseInfoArray[1][1][0]?.GetValueKind() == JsonValueKind.String ? -1 : baseInfoArray[1][1][0]?.GetValue<double>() ?? -1) : 0;
            var arrows = baseInfoArray.Count > 6 ? baseInfoArray[6]?.AsArray().DistinctBy(x => x[1][3].GetValue<double>()).ToArray() ?? [] : [];
            if (rejectLocationsWithoutDescription && string.IsNullOrEmpty(desc))
            {
                return (location, LocationLookupResult.MissingDescription);
            }

            var defaultDrivingDirectionAngle = baseInfoArray[1].AsArray().Count >= 3 && baseInfoArray[1][2]?.AsArray().Count > 0 && baseInfoArray[1][2][0] != null ? (ushort)Math.Round(baseInfoArray[1][2][0].GetValue<decimal>(), 0) : (ushort)1000;

            string? panning = null;
            countryPanning?.TryGetValue(countryCode ?? "", out panning);
            var heading = (arrows, panning?.ToLower()) switch
            {
                ({ Length: > 1 }, "indrivingdirection") => arrows.Select(e => e[1][3].GetValue<decimal>()).MinBy(x => Math.Abs(x - defaultDrivingDirectionAngle)),
                ({ Length: > 1 }, "awayfromdrivingdirection") => arrows.Select(e => e[1][3].GetValue<decimal>()).MaxBy(x => Math.Abs(x - defaultDrivingDirectionAngle)),
                ({ Length: > 2 }, _) => arrows.Select(e => e[1][3].GetValue<decimal>()).GetPermutations(2).MinBy(x => Math.Abs(x.Max() - x.Min() - 180)).TakeRandom(1).Single(),
                ({ Length: > 0 }, _) => arrows[Random.Shared.Next(0, arrows.Length)][1][3].GetValue<decimal>(),
                _ => baseInfoArray[1].AsArray().Count >= 3 && baseInfoArray[1][2]?.AsArray().Count > 0 && baseInfoArray[1][2][0] != null ? Math.Round(baseInfoArray[1][2][0].GetValue<decimal>(), 0) : 1000
            };

            var defaultArrowCount = (ushort)arrows.Length;
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
                var (isGen1, drivingDirectionAngle, defaultHeading, arrowCount, metersAboveSeaLevel, _, _, _, _) = await DetailsFromPanoId(selected.PanoId);
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
                elevation = metersAboveSeaLevel;
            }

            if (pano.Length >= 36 || defaultDrivingDirectionAngle > 360 || year < 2005)
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
                arrowCount = defaultArrowCount,
                elevation = (int)Math.Round(elevation, 0),
                descriptionLength = desc?.Length ?? 0,
                alternativePanos = alternativeImages.Where(a => a.PanoId != pano).ToArray(),
                subdivision = subdivision
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

    public static bool IsGen1Possible(string countryCode, int year)
    {
        return countryCode switch
        {
            "AU" or "CA" or "FR" => year < 2009,
            "NZ" => year < 2010,
            "US" or "JP" or "MX" => year < 2011,
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
                metadataResponse[1].GetValue<string>() is "Internal error encountered." or "The service is currently unavailable.")
            {
                return null;
            }

            if (metadataResponse.AsArray().Count == 1 && metadataResponse.AsArray()[0]?.AsArray().Count == 1)
            {
                return null;
            }

            var imageSize = metadataResponse[1][0][2]?[2][0].GetValue<double>();
            var isGen1 = imageSize < 2000;
            return isGen1;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed verifying IsGen1. {content}");
            Console.WriteLine(e);
            return null;
        }
    }

    public static async Task<(bool? isGen1, ushort drivingDirectionAngle, decimal defaultHeading, int arrowCount, double elevation, int year, int month, double lat, double lng)> DetailsFromPanoId(string panoId)
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
            return (null, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        try
        {
            var metadataResponse = JsonNode.Parse(content);
            if (metadataResponse.AsArray().Count == 2 && metadataResponse[1] is JsonValue &&
                metadataResponse[1].GetValue<string>() is "Internal error encountered." or "The service is currently unavailable.")
            {
                return (null, 0, 0, 0, 0, 0, 0, 0, 0);
            }

            if (metadataResponse.AsArray().Count == 1)
            {
                return (null, 0, 0, 0, 0, 0, 0, 0, 0);
            }

            var imageSize = metadataResponse[1][0][2]?[2]?[0]?.GetValue<double>() ?? 0;
            var isGen1 = imageSize < 2000;
            var arrows = metadataResponse[1][0][5]?[0]?.AsArray().Count > 6 ? metadataResponse[1][0][5]?[0]?[6]?.AsArray() ?? []: [];
            var drivingDirectionAngle = metadataResponse[1][0][5]?[0][1].AsArray().Count > 2 ? (ushort)Math.Round(metadataResponse[1][0][5]?[0][1][2]?[0].GetValue<decimal>() ?? 0, 0) : (ushort)0;
            var elevation = metadataResponse[1][0][5]?[0][1][1]?[0]?.GetValue<double>() ?? -1;
            var heading = arrows switch
            {
                { Count: > 2 } => arrows.Select(e => e[1][3].GetValue<decimal>()).GetPermutations(2).MinBy(x => Math.Abs(x.Max() - x.Min() - 180)).TakeRandom(1).Single(),
                { Count: > 0 } => arrows[Random.Shared.Next(0, arrows.Count)][1][3].GetValue<decimal>(),
                _ => drivingDirectionAngle
            };
            var year = metadataResponse[1][0][6]?.AsArray().Count >= 8 ? metadataResponse[1][0][6]?[7][0].GetValue<int>() ?? 2000 : 2000;
            if (year < 2001)
            {
                return (null, 0, 0, 0, 0, 0, 0, 0, 0);
            }

            var month = metadataResponse[1][0][6].AsArray().Count >= 8 ? metadataResponse[1][0][6]?[7][1].GetValue<int>() ?? 1 : 1;
            var lat = metadataResponse[1][0][5]?[0][1][0][2].GetValue<double>() ?? 0;
            var lng = metadataResponse[1][0][5]?[0][1][0][3].GetValue<double>() ?? 0;
            return (isGen1, drivingDirectionAngle, heading, arrows.Count, elevation, year, month, lat, lng);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed verifying {nameof(DetailsFromPanoId)}. {content}");
            Console.WriteLine(e);
            return (null, 0, 0, 0, 0, 0, 0, 0, 0);
        }
    }

    public static async Task<(string panoId, double lat, double lng)[]> Neighbours(string panoId)
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
            return [];
        }

        try
        {
            var metadataResponse = JsonNode.Parse(content);
            if (metadataResponse.AsArray().Count == 2 && metadataResponse[1] is JsonValue &&
                metadataResponse[1].GetValue<string>() is "Internal error encountered." or "The service is currently unavailable.")
            {
                return [];
            }

            if (metadataResponse.AsArray().Count == 1)
            {
                return [];
            }

            var whatNow = metadataResponse[1][0][5]?[0]?[3]?[0]?.AsArray() ?? [];
            var whaaat = whatNow.Select(x => new
            {
                lat = x[2][0][2].GetValue<double>(),
                lng = x[2][0][3].GetValue<double>(),
                panoId = x[0][1].GetValue<string>()
            }).ToArray();
            return whaaat.Select(x => (x.panoId, x.lat, x.lng)).ToArray();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed verifying {nameof(Neighbours)}. {content}");
            Console.WriteLine(e);
            return [];
        }
    }

    public static async Task<string?> Shorten(double lat, double lng, string panoId)
    {
        var url = $"https://www.google.com/maps/rpc/shorturl?authuser=0&hl=no&gl=no&pb=!1shttps%3A%2F%2Fwww.google.com%2Fmaps%2F%40{lat.Format()}%2C{lng.Format()}%2C3a%2C90y%2C113.93h%2C90t%2Fdata%3D*213m7*211e1*213m5*211s{panoId}*212e0*216shttps%3A%252F%252Fstreetviewpixels-pa.googleapis.com%252Fv1%252Fthumbnail%253Fw%253D900%2526h%253D600%2526panoid%253D{panoId}%2526cb_client%253Dmaps_sv.share%2526yaw%253D113.928345%2526pitch%253D0%2526thumbfov%253D133*217i16384*218i8192%3Fentry%3Dtts%26g_ep%3DEgoyMDI0MDkxMS4wKgBIAVAD!2m2!1s3nDoZoWUOvbZwPAPmdvYqQY!7e81!6b1";
        var responseMessage = await _shortenerClient.GetAsync(url);
        var response = await responseMessage.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<string[]>(response.Split("\n")[1])?[0];
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
    public DateOnly? date { get; set; }
    [JsonIgnore]
    public double Lat => lat;
    [JsonIgnore]
    public double Lng => lng;
    [JsonIgnore]
    public string LocationId => locationId;
    public int? elevation { get; set; }
    public int? descriptionLength { get; set; }
    public AlternativePano[] alternativePanos { get; set; }
    public string? subdivision { get; set; }
}

public record AlternativePano
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string PanoId { get; set; }
}
