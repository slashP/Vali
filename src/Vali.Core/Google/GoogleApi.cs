using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Vali.Core.Data;

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
        Dictionary<string, CountryPanning?> countryPanning,
        bool silent = false,
        bool rejectLocationsWithoutDescription = true)
    {
        var result = new List<MapCheckrLocation>(locations.Count);
        var counter = 0;
        var startTime = DateTime.UtcNow;
        foreach (var chunk in locations.Chunk(chunkSize))
        {
            var locs = await Task.WhenAll(chunk.Select(x => GetVerifiedLocation(x, rejectLocationsWithoutDescription, knownCountryCode: countryCode, selectionStrategy: PanoStrategy.Newest, countryPanning: countryPanning, includeLinked: false, panoVerificationStart: null, panoVerificationEnd: null, badCamStrategy: BadCamStrategy.DisallowInCountriesWithDecentOtherCoverage)));
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
            string? countryCode,
            int chunkSize,
            int radius,
            bool rejectLocationsWithoutDescription,
            bool silent,
            PanoStrategy selectionStrategy,
            Dictionary<string, CountryPanning?>? countryPanning,
            bool includeLinked,
            DateOnly? panoVerificationStart,
            DateOnly? panoVerificationEnd,
            BadCamStrategy badCamStrategy,
            Func<PanningContext, PanningDecision?>? panningOverride = null) =>
        silent
            ? await locations.RunLimitedNumberAtATime(
                x => GetVerifiedLocation(x, rejectLocationsWithoutDescription, knownCountryCode: countryCode, selectionStrategy: selectionStrategy, countryPanning: countryPanning, radius: radius, includeLinked: includeLinked, panoVerificationStart: panoVerificationStart, panoVerificationEnd: panoVerificationEnd, badCamStrategy: badCamStrategy, panningOverride: panningOverride), chunkSize, null)
            : await locations.RunLimitedNumberAtATimeWithProgressBar(
                x => GetVerifiedLocation(x, rejectLocationsWithoutDescription, knownCountryCode: countryCode, selectionStrategy: selectionStrategy, countryPanning: countryPanning, radius: radius, includeLinked: includeLinked, panoVerificationStart: panoVerificationStart, panoVerificationEnd: panoVerificationEnd, badCamStrategy: badCamStrategy, panningOverride: panningOverride),
                chunkSize,
                "Verifying locations by calling Google APIs.");

    public static string SingleImageSearchBody(double lat, double lng, int radius, bool officialOnly)
    {
        var imagery = officialOnly ? "[[[2,true,2]]]" : "[[[2,true,2],[3,true,2],[10,true,2]]]";
        return $@"
[[""apiv3"",null,null,null,""US"",null,null,null,null,null,[[false]]],[[null,null,{lat.Format()},{lng.Format()}],{radius}],[null,[""en"",""US""],null,null,null,null,null,null,[2],null,{imagery}],[[1,2,3,4,8,6]]]";
    }

    public static async Task<(MapCheckrLocation location, LocationLookupResult result)> GetVerifiedLocation(
        MapCheckrLocation location,
        bool rejectLocationsWithoutDescription,
        string? knownCountryCode,
        PanoStrategy selectionStrategy,
        Dictionary<string, CountryPanning?>? countryPanning,
        bool includeLinked,
        DateOnly? panoVerificationStart,
        DateOnly? panoVerificationEnd,
        BadCamStrategy badCamStrategy,
        int radius = 5,
        Func<PanningContext, PanningDecision?>? panningOverride = null,
        bool officialOnly = false)
    {
        var body = SingleImageSearchBody(location.lat, location.lng, radius, officialOnly);
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
            if (locationResponse == null)
            {
                return (location, LocationLookupResult.UnknownError);
            }

            if (locationResponse.AsArray().Count == 2 && locationResponse[1] is JsonValue && locationResponse[1]?.GetValue<string>() is "Internal error encountered." or "The service is currently unavailable.")
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
                return (location, LocationLookupResult.UnknownError);
            }

            if (locationResponse.AsArray().Count == 2 && locationResponse[1] is JsonValue && locationResponse[1]?.GetValue<string>() == "Unrecoverable data loss or corruption.")
            {
                return (location, LocationLookupResult.NoImages);
            }

            if (locationResponse.AsArray().Count == 1 && locationResponse[0]?[2]?.GetValue<string>() is "Search returned no images." or "Invalid location")
            {
                return (location, LocationLookupResult.NoImages);
            }

            var copyright = locationResponse[1]![4]![0]![0]![0]![0]!.GetValue<string>();

            var descriptionNode = locationResponse[1]![3];
            var desc = descriptionNode?.AsArray().Count >= 3 && descriptionNode[2] != null
                ?
                descriptionNode[2]![0]![0]!.GetValue<string>()
                : descriptionNode!.AsArray().Count >= 3 && descriptionNode[0] != null
                    ? descriptionNode[0]![0]!.GetValue<string>()
                    : null;
            var baseInfoArray = locationResponse[1]![5]![0]!.AsArray();
            var countryCode = baseInfoArray[1]!.AsArray().Count >= 5 ? baseInfoArray[1]![4]!.GetValue<string>() : knownCountryCode;
            var year = locationResponse[1]![6]!.AsArray().Count >= 8 ? locationResponse[1]![6]![7]![0]!.GetValue<int>() : -1;
            var month = locationResponse[1]![6]!.AsArray().Count >= 8 ? locationResponse[1]![6]![7]![1]!.GetValue<int>() : -1;
            var isScout = locationResponse[1]![6]!.AsArray().Count >= 6 &&
                                locationResponse[1]![6]![5]!.AsArray().Count >= 3 &&
                                locationResponse[1]![6]![5]![2]!.GetValue<string>() == "scout";
            var resolutionHeight = locationResponse[1]![2]![2]![0]!.GetValue<int>();

            var pano = locationResponse[1]![1]![1]!.GetValue<string>();
            var defaultImage = new AlternativePano
            {
                Year = year,
                Month = month,
                PanoId = pano
            };
            var alternativeImages = (baseInfoArray.Count > 8 && baseInfoArray[8]?.AsArray() != null
                ? new[] { defaultImage }.Concat(baseInfoArray[8]!.AsArray().Select(x =>
                {
                    var index = x![0]!.GetValue<int>();
                    return new AlternativePano
                    {
                        Year = x[1]!.AsArray().Count >= 2 ? x[1]![0]!.GetValue<int>() : -1,
                        Month = x[1]!.AsArray().Count >= 2 ? x[1]![1]!.GetValue<int>() : -1,
                        PanoId = baseInfoArray[3]![0]![index]![0]![1]!.GetValue<string>()
                    };
                })).ToArray()
                : [defaultImage])
                .Where(x => x.Year > 2000)
                .DistinctBy(x => x.PanoId)
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToArray();

            var linked = includeLinked && baseInfoArray.Count >= 4
                ? (baseInfoArray[3]!.AsArray().FirstOrDefault()?.AsArray() ?? [])
                    .Select(x => (x![0]![1]!.GetValue<string>(), x[2]![0]![2]!.GetValue<double>(), x[2]![0]![3]!.GetValue<double>()))
                    .Where(x => alternativeImages.All(a => a.PanoId != x.Item1))
                    .ToArray()
                : [];
            if (resolutionHeight <= Resolution.Gen1)
            {
                return (location, LocationLookupResult.Gen1);
            }

            var subdivision = (descriptionNode.AsArray().Count >= 3 && descriptionNode[2] != null &&
                              descriptionNode[2]!.AsArray().Count >= 2
                ? descriptionNode[2]?[1]?[0]?.GetValue<string>()
                : descriptionNode.AsArray().Count >= 3 && descriptionNode[2] != null &&
                  descriptionNode[2]!.AsArray().Count >= 1
                    ? descriptionNode[2]?[0]?[0]?.GetValue<string>()
                    : null)?.Split(',').Last().Trim();

            var lat = baseInfoArray[1]![0]![2]!.GetValue<double>();
            var lng = baseInfoArray[1]![0]![3]!.GetValue<double>();
            var elevation = baseInfoArray[1]!.AsArray().Count > 1 && baseInfoArray[1]![1] is not null ? (baseInfoArray[1]![1]![0]?.GetValueKind() == JsonValueKind.String ? -1 : baseInfoArray[1]![1]![0]?.GetValue<double>() ?? -1) : 0;
            var arrows = baseInfoArray.Count > 6 ? baseInfoArray[6]?.AsArray().DistinctBy(x => x![1]![3]!.GetValue<double>()).ToArray() ?? [] : [];
            if (rejectLocationsWithoutDescription && string.IsNullOrEmpty(desc))
            {
                return (location, LocationLookupResult.MissingDescription);
            }

            var defaultDrivingDirectionAngle = baseInfoArray[1]!.AsArray().Count >= 3 && baseInfoArray[1]![2]?.AsArray().Count > 0 && baseInfoArray[1]![2]![0] != null && baseInfoArray[1]![2]![0]!.GetValueKind() == JsonValueKind.Number ? (ushort)Math.Round(baseInfoArray[1]![2]![0]!.GetValue<decimal>(), 0) : (ushort)1000;
            var heading = Heading(countryPanning, panningOverride, new PanningContext(pano, countryCode, location.subdivisionCode, lat, lng, year, month, resolutionHeight), arrows, defaultDrivingDirectionAngle);

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
                PanoStrategy.YearMonthPeriod => alternativeImages
                    .Where(x => x.Year == panoVerificationStart!.Value.Year && x.Month >= panoVerificationStart.Value.Month || x.Year > panoVerificationStart.Value.Year)
                    .Where(x => x.Year == panoVerificationEnd!.Value.Year && x.Month <= panoVerificationEnd.Value.Month || x.Year < panoVerificationEnd.Value.Year)
                    .TakeRandom(1)
                    .FirstOrDefault(),
                _ => throw new ArgumentOutOfRangeException(nameof(selectionStrategy), selectionStrategy, null)
            };
            if (selected == null)
            {
                return (location, LocationLookupResult.NoImages);
            }

            bool? isGen1 = false;
            if (selected.PanoId != pano)
            {
                var (gen1, drivingDirectionAngle, defaultHeading, arrowCount, metersAboveSeaLevel, _, _, _, _, scout, resHeight, detailsCopyright) = await DetailsFromPanoId(selected.PanoId, countryPanning, countryCode, location.subdivisionCode, panningOverride);
                pano = selected.PanoId;
                year = selected.Year;
                month = selected.Month;
                defaultDrivingDirectionAngle = drivingDirectionAngle;
                defaultArrowCount = (ushort)arrowCount;
                heading = defaultHeading;
                elevation = metersAboveSeaLevel;
                isScout = scout;
                copyright = detailsCopyright;
                resolutionHeight = resHeight;
                isGen1 = gen1;
            }

            if (defaultDrivingDirectionAngle > 360 || year < 2005)
            {
                return (location, LocationLookupResult.NoImages);
            }

            var result = isGen1 == true ? LocationLookupResult.Gen1 : CopyrightToLookupResult(copyright);
            if (IsNotDesiredCoverage(result, resolutionHeight, countryCode, year))
            {
                var alternativeImageResults = new List<(string pano, int year, int month, ushort defaultDrivingDirectionAngle, ushort arrowCount, decimal heading, double elevation, bool isScout, bool? isGen1, string? copyright, int resolutionHeight, LocationLookupResult result)>();
                foreach (var alternativePano in alternativeImages.Where(i => i.PanoId != selected.PanoId))
                {
                    var (gen1, drivingDirectionAngle, defaultHeading, arrowCount, metersAboveSeaLevel, _, _, _, _, scout, resHeight, detailsCopyright) = await DetailsFromPanoId(alternativePano.PanoId, countryPanning, countryCode, location.subdivisionCode, panningOverride);
                    result = gen1 == true ? LocationLookupResult.Gen1 : CopyrightToLookupResult(detailsCopyright);
                    if (result == LocationLookupResult.Valid)
                    {
                        alternativeImageResults.Add((alternativePano.PanoId, alternativePano.Year, alternativePano.Month, drivingDirectionAngle, (ushort)arrowCount, defaultHeading, metersAboveSeaLevel, scout, gen1, detailsCopyright, resHeight, result));
                    }
                    if (alternativeImageResults.Any(i => !IsNotDesiredCoverage(i.result, i.resolutionHeight, countryCode, i.year)))
                    {
                        break;
                    }
                }

                var alternativeImageFound = alternativeImageResults
                    .Where(i => !IsNotDesiredCoverage(i.result, i.resolutionHeight, countryCode, i.year))
                    .OrderByDescending(i => i.resolutionHeight)
                    .ThenByDescending(i => i.year)
                    .ThenByDescending(i => i.month)
                    .FirstOrDefault();
                if (alternativeImageFound.pano != null)
                {
                    pano = alternativeImageFound.pano;
                    year = alternativeImageFound.year;
                    month = alternativeImageFound.month;
                    defaultDrivingDirectionAngle = alternativeImageFound.defaultDrivingDirectionAngle;
                    defaultArrowCount = alternativeImageFound.arrowCount;
                    heading = alternativeImageFound.heading;
                    elevation = alternativeImageFound.elevation;
                    isScout = alternativeImageFound.isScout;
                    isGen1 = alternativeImageFound.isGen1;
                    copyright = alternativeImageFound.copyright;
                    resolutionHeight = alternativeImageFound.resolutionHeight;
                }
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
                linkedPanos = linked,
                subdivision = subdivision,
                isScout = isScout,
                resolutionHeight = resolutionHeight
            }, result);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed handling info. {content}");
            Console.WriteLine(e);
            await Task.Delay(TimeSpan.FromSeconds(5));
            return (location, LocationLookupResult.UnknownError);
        }

        bool IsNotDesiredCoverage(LocationLookupResult result, int resolutionHeight, string? countryCode, int year)
        {
            return (result == LocationLookupResult.Ari && selectionStrategy == PanoStrategy.Newest) ||
                   (resolutionHeight == Resolution.Gen3BadCam && year > 2019 && badCamStrategy == BadCamStrategy.DisallowInCountriesWithDecentOtherCoverage && countryCode != null && !CountryCodes.BadCamAcceptableCountryCodes.Contains(countryCode));
        }
    }

    private static LocationLookupResult CopyrightToLookupResult(string copyright) =>
        Regex.IsMatch(copyright, @"© \d{4} Google")
            ? LocationLookupResult.Valid
            : LocationLookupResult.Ari;

    public static bool IsGen1Possible(string? countryCode, int year)
    {
        return countryCode switch
        {
            "AU" or "CA" or "FR" => year < 2009,
            "NZ" => year < 2010,
            "US" or "JP" or "MX" => year < 2011,
            _ => false
        };
    }

    public static async Task<(bool? isGen1, ushort drivingDirectionAngle, decimal defaultHeading, int arrowCount, double elevation, int year, int month, double lat, double lng, bool isScout, int resolutionHeight, string copyright)> DetailsFromPanoId(
        string panoId,
        Dictionary<string, CountryPanning?>? countryPanning,
        string? countryCode,
        string? subdivisionCode = null,
        Func<PanningContext, PanningDecision?>? panningOverride = null)
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
            return (null, 0, 0, 0, 0, 0, 0, 0, 0, false, 0, "");
        }

        try
        {
            var metadataResponse = JsonNode.Parse(content);
            if (metadataResponse == null)
            {
                return (null, 0, 0, 0, 0, 0, 0, 0, 0, false, 0, "");
            }

            if (metadataResponse.AsArray().Count == 2 && metadataResponse[1] is JsonValue &&
                metadataResponse[1]!.GetValue<string>() is "Internal error encountered." or "The service is currently unavailable.")
            {
                return (null, 0, 0, 0, 0, 0, 0, 0, 0, false, 0, "");
            }

            if (metadataResponse.AsArray().Count == 1)
            {
                return (null, 0, 0, 0, 0, 0, 0, 0, 0, false, 0, "");
            }

            var resolutionHeight = metadataResponse[1]![0]![2]?[2]?[0]?.GetValue<int>() ?? 0;
            var isGen1 = resolutionHeight <= Resolution.Gen1;
            var arrows = metadataResponse[1]![0]![5]?[0]?.AsArray().Count > 6 ? metadataResponse[1]![0]![5]?[0]?[6]?.AsArray().ToArray() ?? []: [];
            var drivingDirectionAngle = metadataResponse[1]![0]![5]?[0]![1]!.AsArray().Count > 2 ? (ushort)Math.Round(metadataResponse[1]![0]![5]?[0]![1]![2]?[0]!.GetValue<decimal>() ?? 0, 0) : (ushort)0;
            var elevation = metadataResponse[1]![0]![5]?[0]![1]![1]?[0]?.GetValue<double>() ?? -1;
            var year = metadataResponse[1]![0]![6]?.AsArray().Count >= 8 ? metadataResponse[1]![0]![6]?[7]![0]!.GetValue<int>() ?? 2000 : 2000;
            if (year < 2001)
            {
                return (null, 0, 0, 0, 0, 0, 0, 0, 0, false, 0, "");
            }

            var month = metadataResponse[1]![0]![6]!.AsArray().Count >= 8 ? metadataResponse[1]![0]![6]?[7]![1]!.GetValue<int>() ?? 1 : 1;
            var lat = metadataResponse[1]![0]![5]?[0]![1]![0]![2]!.GetValue<double>() ?? 0;
            var lng = metadataResponse[1]![0]![5]?[0]![1]![0]![3]!.GetValue<double>() ?? 0;
            var isScout = metadataResponse[1]![0]![6]![5]!.AsArray().Count >= 3 &&
                                metadataResponse[1]![0]![6]![5]![2]!.GetValue<string>() == "scout";
            var copyright = metadataResponse[1]![0]![4]![0]![0]![0]![0]!.GetValue<string>();
            var heading = Heading(countryPanning, panningOverride, new PanningContext(panoId, countryCode, subdivisionCode, lat, lng, year, month, resolutionHeight), arrows, drivingDirectionAngle);
            return (isGen1, drivingDirectionAngle, heading, arrows.Length, elevation, year, month, lat, lng, isScout, resolutionHeight, copyright);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed verifying {nameof(DetailsFromPanoId)}. {content}");
            Console.WriteLine(e);
            return (null, 0, 0, 0, 0, 0, 0, 0, 0, false, 0, "");
        }
    }

    public static async Task<(string panoId, double lat, double lng)[]> Neighbors(string panoId)
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
            if (metadataResponse == null)
            {
                return [];
            }

            if (metadataResponse.AsArray().Count == 2 && metadataResponse[1] is JsonValue &&
                metadataResponse[1]!.GetValue<string>() is "Internal error encountered." or "The service is currently unavailable.")
            {
                return [];
            }

            if (metadataResponse.AsArray().Count == 1)
            {
                return [];
            }

            var whatNow = metadataResponse[1]![0]![5]?[0]?[3]?[0]?.AsArray() ?? [];
            var whaaat = whatNow.Select(x => new
            {
                lat = x![2]![0]![2]!.GetValue<double>(),
                lng = x![2]![0]![3]!.GetValue<double>(),
                panoId = x![0]![1]!.GetValue<string>()
            }).ToArray();
            return whaaat.Select(x => (x.panoId, x.lat, x.lng)).ToArray();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed verifying {nameof(Neighbors)}. {content}");
            Console.WriteLine(e);
            return [];
        }
    }

    private static decimal Heading(
        Dictionary<string, CountryPanning?>? countryPannings,
        Func<PanningContext, PanningDecision?>? panningOverride,
        PanningContext context,
        JsonNode?[] arrows,
        ushort defaultDrivingDirectionAngle)
    {
        var rng = PanningResolver.RandomFor(context.PanoId);
        var panning = PanningResolver.ResolvePanning(countryPannings, panningOverride, context, rng);
        var arrowHeadings = arrows.Select(e => e![1]![3]!.GetValue<decimal>()).ToArray();
        return PanningResolver.PickHeading(arrowHeadings, defaultDrivingDirectionAngle, panning, rng);
    }

    public static async Task<string?> Shorten(double lat, double lng, string panoId)
    {
        var url = $"https://www.google.com/maps/rpc/shorturl?authuser=0&hl=no&gl=no&pb=!1shttps%3A%2F%2Fwww.google.com%2Fmaps%2F%40{lat.Format()}%2C{lng.Format()}%2C3a%2C90y%2C113.93h%2C90t%2Fdata%3D*213m7*211e1*213m5*211s{panoId}*212e0*216shttps%3A%252F%252Fstreetviewpixels-pa.googleapis.com%252Fv1%252Fthumbnail%253Fw%253D900%2526h%253D600%2526panoid%253D{panoId}%2526cb_client%253Dmaps_sv.share%2526yaw%253D113.928345%2526pitch%253D0%2526thumbfov%253D133*217i16384*218i8192%3Fentry%3Dtts%26g_ep%3DEgoyMDI0MDkxMS4wKgBIAVAD!2m2!1s3nDoZoWUOvbZwPAPmdvYqQY!7e81!6b1";
        var responseMessage = await _shortenerClient.GetAsync(url);
        var response = await responseMessage.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<string[]>(response.Split("\n")[1])?[0];
    }

    // Web-Mercator / slippy-map tile coordinate at the given zoom. asinh(x) = ln(x + sqrt(x^2 + 1)).
    public static (int x, int y) TileCoord(double lat, double lng, int zoom = 17)
    {
        var latRad = lat * Math.PI / 180.0;
        var n = 1 << zoom; // 2^zoom
        var x = (int)Math.Floor((lng + 180.0) / 360.0 * n);
        var y = (int)Math.Floor((1 - Math.Asinh(Math.Tan(latRad)) / Math.PI) / 2 * n);
        return (x, y);
    }

    public static async Task<string[]> PanoIdsInTile(int x, int y, int zoom = 17)
    {
        var url = $"https://www.google.com/maps/photometa/ac/v1?pb=!1m1!1smaps_sv.tactile!6m3!1i{x}!2i{y}!3i{zoom}!8b1";
        string content;
        try
        {
            var response = await _shortenerClient.GetAsync(url);
            content = await response.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return [];
        }

        try
        {
            // Strip the ")]}'" XSSI guard prefix by jumping to the first '['.
            var bracket = content.IndexOf('[');
            if (bracket < 0)
            {
                return [];
            }

            var node = JsonNode.Parse(content[bracket..]);
            // Pano-list shape: node[1][1] holds the panos. The endpoint also returns a "region" shape
            // (node[1] == [], sub-cell entries with cell ids — not panos — at node[2]) for some tiles.
            // JsonArray indexing throws on out-of-range (?. only guards null), so check the shape.
            if (node is not JsonArray root || root.Count < 2 ||
                root[1] is not JsonArray level1 || level1.Count < 2 ||
                level1[1] is not JsonArray panos)
            {
                return [];
            }

            return panos
                .Select(p => p?[0]?[0]?[1]?.GetValue<string>())
                .Where(id => id != null)
                .Select(id => id!)
                .ToArray();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed parsing tile pano ids. {content}");
            Console.WriteLine(e);
            return [];
        }
    }

    public static async Task<TilePanoData> GetTilePanos(int x, int y)
    {
        var url = $"https://www.google.com/maps/photometa/ac/v1?pb=!1m1!1smaps_sv.tactile!6m3!1i{x}!2i{y}!3i17!8b1";
        string content;
        try
        {
            var response = await _shortenerClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Tile panos {x},{y}: HTTP {(int)response.StatusCode}");
                return new TilePanoData(x, y, [], Failed: true);
            }

            content = await response.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return new TilePanoData(x, y, [], Failed: true);
        }

        try
        {
            var bracket = content.IndexOf('[');
            if (bracket < 0)
            {
                // Not the JSON payload (error/captcha page) — an error, not an empty tile.
                return new TilePanoData(x, y, [], Failed: true);
            }

            var node = JsonNode.Parse(content[bracket..]);
            // Same two shapes as PanoIdsInTile: pano list at node[1][1], or region/empty shapes.
            // The region/empty shapes are valid observations of a pano-free tile (Failed: false).
            if (node is not JsonArray root || root.Count < 2 ||
                root[1] is not JsonArray level1 || level1.Count < 2 ||
                level1[1] is not JsonArray panos)
            {
                return new TilePanoData(x, y, []);
            }

            var result = panos
                .Select(p => p?[0]?[0]?[1]?.GetValue<string>() is { } id &&
                             p?[0]?[2]?[0]?[2]?.GetValue<double>() is { } lat &&
                             p?[0]?[2]?[0]?[3]?.GetValue<double>() is { } lng
                    ? new TilePano(id, lat, lng)
                    : null)
                .Where(p => p != null)
                .Select(p => p!)
                .Where(p => TileCoord(p.Lat, p.Lng, 17) == (x, y))
                .ToArray();
            return new TilePanoData(x, y, result);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed parsing tile panos. {content}");
            Console.WriteLine(e);
            return new TilePanoData(x, y, [], Failed: true);
        }
    }

    public static async Task<(MapCheckrLocation location, LocationLookupResult result)> GetLocationFromPanoId(
        string panoId,
        string? knownCountryCode,
        Dictionary<string, CountryPanning?>? countryPanning,
        Func<PanningContext, PanningDecision?>? panningOverride = null)
    {
        var body = $"""
                    [["apiv3",null,null,null,"US",null,null,null,null,null,[[0]]],["en","US"],[[[2,"{panoId}"]]],[[1,2,3,4,8,6]]]
                    """;
        string content;
        try
        {
            var responseMessage = await _client.PostAsync(
                "$rpc/google.internal.maps.mapsjs.v1.MapsJsInternalService/GetMetadata",
                new StringContent(body, Encoding.UTF8, "application/json+protobuf"));
            if (!responseMessage.IsSuccessStatusCode)
            {
                // Never parse an error body: JSON-shaped error responses used to null-propagate
                // into a silent NoImages misclassification (throttling read as "pano gone").
                Console.WriteLine($"Metadata {panoId}: HTTP {(int)responseMessage.StatusCode}");
                await Task.Delay(TimeSpan.FromSeconds(5));
                return (new MapCheckrLocation { panoId = panoId }, LocationLookupResult.UnknownError);
            }

            content = await responseMessage.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await Task.Delay(TimeSpan.FromSeconds(5));
            return (new MapCheckrLocation { panoId = panoId }, LocationLookupResult.UnknownError);
        }

        try
        {
            // Element 0 is an RPC status, element 1 the payload (stub example below). A short array
            // (e.g. [[3]]) is a rejection with no payload — a definite "no answer", not an
            // observation about the pano, so it must not conclude NoImages.
            var root = JsonNode.Parse(content) as JsonArray;
            if (root == null || root.Count < 2)
            {
                Console.WriteLine($"Metadata {panoId}: rpc status {root?[0]?.ToJsonString() ?? content}");
                return (new MapCheckrLocation { panoId = panoId }, LocationLookupResult.UnknownError);
            }

            var b = root[1]?[0];
            if (b == null)
            {
                return (new MapCheckrLocation { panoId = panoId }, LocationLookupResult.NoImages);
            }

            // Stub / no-image responses (e.g. [[0],[[[2],[2,"pano"],null,null,null,null,null,[]]]]) leave
            // the core metadata nodes null. Real panos always populate all of them, so bail out here
            // rather than NRE on the hard dereferences below.
            if (b[2]?[2]?[0] == null || b[3] == null || b[4] == null || b[5]?[0] == null || b[6] == null)
            {
                return (new MapCheckrLocation { panoId = panoId }, LocationLookupResult.NoImages);
            }

            var baseInfoArray = b[5]![0]!.AsArray();
            var resolutionHeight = b[2]![2]![0]!.GetValue<int>();
            var year = b[6]!.AsArray().Count >= 8 ? b[6]![7]![0]!.GetValue<int>() : -1;
            var month = b[6]!.AsArray().Count >= 8 ? b[6]![7]![1]!.GetValue<int>() : -1;
            var isScout = b[6]!.AsArray().Count >= 6 &&
                          b[6]![5]!.AsArray().Count >= 3 &&
                          b[6]![5]![2]!.GetValue<string>() == "scout";
            var copyright = b[4]![0]![0]![0]![0]!.GetValue<string>();
            var lat = baseInfoArray[1]![0]![2]!.GetValue<double>();
            var lng = baseInfoArray[1]![0]![3]!.GetValue<double>();
            var countryCode = baseInfoArray[1]!.AsArray().Count >= 5
                ? baseInfoArray[1]![4]!.GetValue<string>()
                : knownCountryCode;

            var descriptionNode = b[3];
            var desc = descriptionNode?.AsArray().Count >= 3 && descriptionNode[2] != null
                ? descriptionNode[2]![0]![0]!.GetValue<string>()
                : descriptionNode!.AsArray().Count >= 3 && descriptionNode[0] != null
                    ? descriptionNode[0]![0]!.GetValue<string>()
                    : null;
            var subdivision = (descriptionNode.AsArray().Count >= 3 && descriptionNode[2] != null &&
                               descriptionNode[2]!.AsArray().Count >= 2
                ? descriptionNode[2]?[1]?[0]?.GetValue<string>()
                : descriptionNode.AsArray().Count >= 3 && descriptionNode[2] != null &&
                  descriptionNode[2]!.AsArray().Count >= 1
                    ? descriptionNode[2]?[0]?[0]?.GetValue<string>()
                    : null)?.Split(',').Last().Trim();

            var elevation = baseInfoArray[1]!.AsArray().Count > 1 && baseInfoArray[1]![1] is not null
                ? (baseInfoArray[1]![1]![0]?.GetValueKind() == JsonValueKind.String ? -1 : baseInfoArray[1]![1]![0]?.GetValue<double>() ?? -1)
                : 0;
            var arrows = baseInfoArray.Count > 6
                ? baseInfoArray[6]?.AsArray().DistinctBy(x => x![1]![3]!.GetValue<double>()).ToArray() ?? []
                : [];
            var defaultDrivingDirectionAngle = baseInfoArray[1]!.AsArray().Count >= 3 &&
                                               baseInfoArray[1]![2]?.AsArray().Count > 0 &&
                                               baseInfoArray[1]![2]![0] != null &&
                                               baseInfoArray[1]![2]![0]!.GetValueKind() == JsonValueKind.Number
                ? (ushort)Math.Round(baseInfoArray[1]![2]![0]!.GetValue<decimal>(), 0)
                : (ushort)1000;
            var heading = Heading(countryPanning, panningOverride, new PanningContext(panoId, countryCode, null, lat, lng, year, month, resolutionHeight), arrows, defaultDrivingDirectionAngle);

            var alternativeImages = (baseInfoArray.Count > 8 && baseInfoArray[8]?.AsArray() != null
                    ? baseInfoArray[8]!.AsArray().Select(x =>
                    {
                        var index = x![0]!.GetValue<int>();
                        return new AlternativePano
                        {
                            Year = x[1]!.AsArray().Count >= 2 ? x[1]![0]!.GetValue<int>() : -1,
                            Month = x[1]!.AsArray().Count >= 2 ? x[1]![1]!.GetValue<int>() : -1,
                            PanoId = baseInfoArray[3]![0]![index]![0]![1]!.GetValue<string>()
                        };
                    }).ToArray()
                    : [])
                .Concat([new AlternativePano { Year = year, Month = month, PanoId = panoId }])
                .Where(x => x.Year > 2000)
                .DistinctBy(x => x.PanoId)
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToArray();

            var linked = baseInfoArray.Count >= 4
                ? (baseInfoArray[3]?.AsArray().FirstOrDefault()?.AsArray() ?? [])
                    .Select(xx => xx != null && xx[0]?[1] != null && xx[2]?[0]?[2] != null && xx[2]?[0]?[3] != null
                        ? ((string pano, double lat, double lng)?)(xx[0]![1]!.GetValue<string>(), xx[2]![0]![2]!.GetValue<double>(), xx[2]![0]![3]!.GetValue<double>())
                        : null)
                    .Where(l => l != null)
                    .Select(l => l!.Value)
                    .Where(l => alternativeImages.All(a => a.PanoId != l.pano))
                    .ToArray()
                : [];

            if (year < 2005 || defaultDrivingDirectionAngle > 360)
            {
                return (new MapCheckrLocation { panoId = panoId }, LocationLookupResult.NoImages);
            }

            var result = resolutionHeight <= Resolution.Gen1
                ? LocationLookupResult.Gen1
                : CopyrightToLookupResult(copyright);

            var location = new MapCheckrLocation
            {
                locationId = panoId,
                lat = lat,
                lng = lng,
                heading = Math.Round(heading, 2),
                countryCode = countryCode,
                panoId = panoId,
                year = year,
                month = month,
                drivingDirectionAngle = defaultDrivingDirectionAngle,
                arrowCount = (ushort)arrows.Length,
                elevation = (int)Math.Round(elevation, 0),
                descriptionLength = desc?.Length ?? 0,
                alternativePanos = alternativeImages.Where(a => a.PanoId != panoId).ToArray(),
                linkedPanos = linked,
                subdivision = subdivision,
                isScout = isScout,
                resolutionHeight = resolutionHeight
            };
            return (location, result);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed handling pano metadata for {panoId}. {content}");
            Console.WriteLine(e);
            return (new MapCheckrLocation { panoId = panoId }, LocationLookupResult.UnknownError);
        }
    }

    public static async Task<IReadOnlyCollection<(MapCheckrLocation location, LocationLookupResult result)>> GetLocationsViaTiles(
        IEnumerable<MapCheckrLocation> points,
        string? countryCode,
        int chunkSize,
        Dictionary<string, CountryPanning?>? countryPanning,
        int zoom = 17,
        int picksPerTile = 1)
    {
        var tiles = points
            .Select(p => TileCoord(p.lat, p.lng, zoom))
            .Distinct()
            .ToArray();

        var panoIdsPerTile = await tiles.RunLimitedNumberAtATime(
            t => PanoIdsInTile(t.x, t.y, zoom), chunkSize, null);

        var pickedPanoIds = panoIdsPerTile
            .Where(ids => ids.Length > 0)
            .SelectMany(ids => ids.TakeRandom(Math.Min(picksPerTile, ids.Length)))
            .Distinct()
            .ToArray();

        var results = await pickedPanoIds.RunLimitedNumberAtATime(
            id => GetLocationFromPanoId(id, countryCode, countryPanning), chunkSize, null);

        return results.ToArray();
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
        SecondOldest,
        YearMonthPeriod
    }

    public enum BadCamStrategy
    {
        DisallowInCountriesWithDecentOtherCoverage,
        AllowForAll,
    }
}

public record TilePano(string PanoId, double Lat, double Lng);
// Failed distinguishes transport/parse errors from a genuinely pano-free tile: consumers must not
// treat a failed fetch as observed absence (e.g. by retiring baseline coverage).
public record TilePanoData(int TileX, int TileY, TilePano[] InBboxPanos, bool Failed = false);

    public record MapCheckrLocation : IDistributionLocation<string>
{
    public double lat { get; set; }
    public double lng { get; set; }
    public decimal heading { get; set; }
    public decimal pitch { get; set; }
    public string locationId { get; set; } = "";
    public string? source { get; set; }
    public string? hash { get; set; }
    public string? replacesLocationId { get; set; }
    public string? countryCode { get; set; }
    public string? subdivisionCode { get; set; }
    public int? minDistance { get; set; }
    public string panoId { get; set; } = "";
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
    public AlternativePano[] alternativePanos { get; set; } = [];
    public int panoramaCount => alternativePanos.Length + 1;
    public (string pano, double lat, double lng)[] linkedPanos { get; set; } = [];
    public string? subdivision { get; set; }
    public bool isScout { get; set; }
    public int resolutionHeight { get; set; }
}

public record AlternativePano
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string PanoId { get; set; } = "";
}

public static class Resolution
{
    public const int Gen4 = 8192;
    public const int Gen3BadCam = 6656;
    public const int Gen1 = 1664;
}
