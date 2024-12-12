using static Vali.Core.LocationLakeMapGenerator;
using System.Globalization;
using Vali.Core.Data;
using Vali.Core.Google;

namespace Vali.Core;

public static class TagsGenerator
{
    public static GeoMapLocationExtra? Tags(MapDefinition mapDefinition, Location l, MapCheckrLocation mapCheckrLocation)
    {
        var tags = mapDefinition.Output.LocationTags.SelectMany(e => e switch
        {
            "CountryCode" => [mapCheckrLocation.countryCode ?? ""],
            "SubdivisionCode" => [SubdivisionCode(mapCheckrLocation) ?? ""],
            "County" => [County(l) ?? ""],
            "Surface" => [Surface(l) ?? ""],
            "Year" => [mapCheckrLocation.year.ToString()],
            "Month" => [mapCheckrLocation.month.ToString()],
            "YearMonth" => [YearMonth(mapCheckrLocation.year, mapCheckrLocation.month)],
            "Elevation" => [mapCheckrLocation.elevation?.ToString() ?? ""],
            _ when e.StartsWith("Elevation") => [mapCheckrLocation.elevation != null ? Range("Elevation", mapCheckrLocation.elevation.Value, e.Replace("Elevation", ""), "m") ?? "" : ""],
            "ArrowCount" => [$"ArrowCount-{mapCheckrLocation.arrowCount}"],
            _ when e.StartsWith(nameof(Location.Google.DrivingDirectionAngle)) => IntTag(mapCheckrLocation, nameof(l.Google.DrivingDirectionAngle), x => x.drivingDirectionAngle, e),
            _ when e.StartsWith(nameof(Location.Google.Heading)) => IntTag(mapCheckrLocation, nameof(l.Google.Heading), x => (int)x.heading, e),
            "DescriptionLength" => [$"DescriptionLength-{(mapCheckrLocation.descriptionLength != null ? mapCheckrLocation.descriptionLength.Value : "null")}"],
            "IsScout" => [$"{nameof(Location.Google.IsScout)}-{(l.Google.IsScout ? "Yes" : "No")}"],
            "Season" => [Season(l.Nominatim.CountryCode, mapCheckrLocation.month)],
            "HighwayType" => Enum.GetValues<RoadType>().Where(r => r != RoadType.Unknown && l.Osm.RoadType.HasFlag(r)).Select(r => r.ToString()),
            nameof(Location.Osm.HighwayTypeCount) => [$"{nameof(Location.Osm.HighwayTypeCount)}-{l.Osm.HighwayTypeCount}"],
            _ when e.StartsWith(nameof(Location.Osm.Buildings200)) => IntTag(l, nameof(l.Osm.Buildings200), x => x.Osm.Buildings200, e),
            _ when e.StartsWith(nameof(Location.Osm.Buildings100)) => IntTag(l, nameof(l.Osm.Buildings100), x => x.Osm.Buildings100, e),
            _ when e.StartsWith(nameof(Location.Osm.Buildings25)) => IntTag(l, nameof(l.Osm.Buildings25), x => x.Osm.Buildings25, e),
            _ when e.StartsWith(nameof(Location.Osm.Buildings10)) => IntTag(l, nameof(l.Osm.Buildings10), x => x.Osm.Buildings10, e),
            _ when e.StartsWith(nameof(Location.Osm.Roads200)) => IntTag(l, nameof(l.Osm.Roads200), x => x.Osm.Roads200, e),
            _ when e.StartsWith(nameof(Location.Osm.Roads100)) => IntTag(l, nameof(l.Osm.Roads100), x => x.Osm.Roads100, e),
            _ when e.StartsWith(nameof(Location.Osm.Roads50)) => IntTag(l, nameof(l.Osm.Roads50), x => x.Osm.Roads50, e),
            _ when e.StartsWith(nameof(Location.Osm.Roads25)) => IntTag(l, nameof(l.Osm.Roads25), x => x.Osm.Roads25, e),
            _ when e.StartsWith(nameof(Location.Osm.Roads10)) => IntTag(l, nameof(l.Osm.Roads10), x => x.Osm.Roads10, e),
            _ when e.StartsWith(nameof(Location.Osm.Roads0)) => IntTag(l, nameof(l.Osm.Roads0), x => x.Osm.Roads0, e),
            _ when e.StartsWith(nameof(Location.Osm.Tunnels200)) => IntTag(l, nameof(l.Osm.Tunnels200), x => x.Osm.Tunnels200, e),
            _ when e.StartsWith(nameof(Location.Osm.Tunnels10)) => IntTag(l, nameof(l.Osm.Tunnels10), x => x.Osm.Tunnels10, e),
            _ when e.StartsWith(nameof(Location.Osm.ClosestCoast)) => l.Osm.ClosestCoast != null ? IntTag(l, nameof(l.Osm.ClosestCoast), x => x.Osm.ClosestCoast!.Value, e) : [],
            _ when e.StartsWith(nameof(Location.Osm.ClosestLake)) => l.Osm.ClosestLake != null ? IntTag(l, nameof(l.Osm.ClosestLake), x => x.Osm.ClosestLake!.Value, e) : [],
            _ when e.StartsWith(nameof(Location.Osm.ClosestRiver)) => l.Osm.ClosestRiver != null ? IntTag(l, nameof(l.Osm.ClosestRiver), x => x.Osm.ClosestRiver!.Value, e) : [],
            _ when e.StartsWith(nameof(Location.Osm.ClosestRailway)) => l.Osm.ClosestRailway != null ? IntTag(l, nameof(l.Osm.ClosestRailway), x => x.Osm.ClosestRailway!.Value, e) : [],
            nameof(Location.Osm.IsResidential) => [$"{nameof(Location.Osm.IsResidential)}-{(l.Osm.IsResidential ? "Yes" : "No")}"],
            _ => []
        }).Concat([l.Tag]).Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).ToArray();
        return tags.Any()
            ? new GeoMapLocationExtra
            {
                tags = tags
            }
            : null;
    }

    private static IEnumerable<string> IntTag(Location l, string name, Func<Location, int> func, string e)
    {
        return e.Contains("-") ? [Range(name, func(l), e.Replace($"{name}-", ""), "") ?? ""] : [$"{name}-{func(l)}"];
    }

    private static IEnumerable<string> IntTag(MapCheckrLocation l, string name, Func<MapCheckrLocation, int> func, string e)
    {
        return e.Contains("-") ? [Range(name, func(l), e.Replace($"{name}-", ""), "") ?? ""] : [$"{name}-{func(l)}"];
    }

    private static string? Range(string prefix, int number, string bucketString, string suffix)
    {
        if (!int.TryParse(bucketString, out var bucket))
        {
            return null;
        }

        var lower = ((number / bucket) * bucket);
        var upper = (((number / bucket) + 1) * bucket) - 1;
        return $"{prefix}[{lower,4}-{upper,4}]{suffix}";
    }

    public static string SubdivisionCode(Location l) => l.Nominatim.SubdivisionCode;
    public static string? SubdivisionCode(MapCheckrLocation l) => l.subdivisionCode;

    public static string? County(Location l) => l.Nominatim.County;

    public static string? Surface(Location l) => l.Osm.Surface;

    public static string Year(int year) => year.ToString();

    public static string Month(int month) => new DateTime(DateTime.Now.Year, month, 1).ToString("MMM", CultureInfo.InvariantCulture);

    public static string YearMonth(int year, int month) => $"{year}-{month.ToString().PadLeft(2, '0')}";

    public static string Season(string countryCode, int month) =>
        (CountryCodes.Hemisphere(countryCode), month) switch
        {
            (Hemisphere.Northern, 12) => "Winter",
            (Hemisphere.Northern, 1) => "Winter",
            (Hemisphere.Northern, 2) => "Winter",
            (Hemisphere.Northern, 3) => "Spring",
            (Hemisphere.Northern, 4) => "Spring",
            (Hemisphere.Northern, 5) => "Spring",
            (Hemisphere.Northern, 6) => "Summer",
            (Hemisphere.Northern, 7) => "Summer",
            (Hemisphere.Northern, 8) => "Summer",
            (Hemisphere.Northern, 9) => "Autumn",
            (Hemisphere.Northern, 10) => "Autumn",
            (Hemisphere.Northern, 11) => "Autumn",
            (Hemisphere.Southern, 12) => "Summer",
            (Hemisphere.Southern, 1) => "Summer",
            (Hemisphere.Southern, 2) => "Summer",
            (Hemisphere.Southern, 3) => "Autumn",
            (Hemisphere.Southern, 4) => "Autumn",
            (Hemisphere.Southern, 5) => "Autumn",
            (Hemisphere.Southern, 6) => "Winter",
            (Hemisphere.Southern, 7) => "Winter",
            (Hemisphere.Southern, 8) => "Winter",
            (Hemisphere.Southern, 9) => "Spring",
            (Hemisphere.Southern, 10) => "Spring",
            (Hemisphere.Southern, 11) => "Spring",
            _ => throw new ArgumentOutOfRangeException()
        };
}