using System.Text.Json.Nodes;
using System.Text;
using System.Text.Json.Serialization;

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
            var locs = await Task.WhenAll(chunk.Select(x => GetVerifiedLocation(x, rejectLocationsWithoutDescription, knownCountryCode: countryCode)));
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

    public static async Task<IReadOnlyCollection<(MapCheckrLocation location, LocationLookupResult result)>> GetLocations(
        IEnumerable<MapCheckrLocation> locations,
        string countryCode,
        int chunkSize,
        int radius,
        bool rejectLocationsWithoutDescription,
        bool silent)
    {
        return await locations.RunLimitedNumberAtATime(x => GetVerifiedLocation(x, rejectLocationsWithoutDescription, knownCountryCode: countryCode, radius: radius), chunkSize);
    }

    public static async Task<(MapCheckrLocation location, LocationLookupResult result)> GetVerifiedLocation(
        MapCheckrLocation location,
        bool rejectLocationsWithoutDescription,
        string knownCountryCode,
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

            var publisher = locationResponse[1].AsArray().Count > 3 && locationResponse[1][4].AsArray().Count > 1 ? locationResponse[1][4]?[1]?[0]?[0]?[0]?.GetValue<string>() : "";
            var skipPublishers = new[]
            {
                "Map Your Town",
                "Chris du Plessis (Photos of Africa)",
                "Tawanda Kanhema",
                "Florian Habenicht",
                "Air360 kz",
                "Photo Almaty",
                "Service QR (sqr.kz)",
                "Aneesh Pradeep",
                "Denis Shevtsov (Den)",
                "Luis Larco",
                "Horacio Herbas",
                "Global Tech Educator",
                "hector Mercado",
                "Jose Antonio Santander Figueredo"
            };
            var copyright = locationResponse[1][4][0][0][0][0].GetValue<string>();
            var descriptionNode = locationResponse[1][3];
            var desc = descriptionNode.AsArray().Count >= 3 && descriptionNode[2] != null
                ?
                descriptionNode[2][0][0].GetValue<string>()
                : descriptionNode.AsArray().Count >= 3 && descriptionNode[0] != null
                    ? descriptionNode[0][0].GetValue<string>()
                    : null;

            if (!copyright.Contains(" Google"))
            {
                var result = !string.IsNullOrEmpty(publisher) && skipPublishers.Contains(publisher) ? LocationLookupResult.NoImages : LocationLookupResult.Ari;
                return (location, result);
            }

            var baseInfoArray = locationResponse[1][5][0].AsArray();
            var countryCode = baseInfoArray[1].AsArray().Count >= 5 ? baseInfoArray[1][4].GetValue<string>() : knownCountryCode;
            var year = locationResponse[1][6][7][0].GetValue<int>();
            var month = locationResponse[1][6][7][1].GetValue<int>();

            var pano = locationResponse[1][1][1].GetValue<string>();
            var isGen1Possible = countryCode switch
            {
                "US" or "NZ" or "AU" or "FR" or "JP" or "MX" => year < 2011,
                _ => false
            };
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

            return (location with
            {
                heading = Math.Round(heading, 2),
                countryCode = location.countryCode ?? countryCode,
                lat = lat,
                lng = lng,
                panoId = pano,
                year = year,
                month = month,
                drivingDirectionAngle = (ushort)Math.Round(baseInfoArray[1][2][0].GetValue<decimal>(), 0),
                arrowCount = (ushort)arrows.Count
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
            return imageSize < 2000;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed verifying gen1. {content}");
            Console.WriteLine(e);
            return null;
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
