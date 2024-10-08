﻿using static Vali.Core.LocationLakeMapGenerator;
using System.Globalization;
using Vali.Core.Data;
using Vali.Core.Google;

namespace Vali.Core;

public static class TagsGenerator
{
    public static GeoMapLocationExtra? Tags(MapDefinition mapDefinition, Location l, MapCheckrLocation mapCheckrLocation)
    {
        var tags = mapDefinition.Output.LocationTags.Select(e => e switch
        {
            "SubdivisionCode" => SubdivisionCode(mapCheckrLocation),
            "County" => County(l),
            "Surface" => Surface(l),
            "Year" => mapCheckrLocation.year.ToString(),
            "Month" => mapCheckrLocation.month.ToString(),
            "YearMonth" => YearMonth(mapCheckrLocation.year, mapCheckrLocation.month),
            "Elevation" => mapCheckrLocation.elevation?.ToString(),
            _ when e.StartsWith("Elevation") => mapCheckrLocation.elevation != null ? Range(mapCheckrLocation.elevation.Value, e.Replace("Elevation", "")) : null,
            "Season" => Season(l.Nominatim.CountryCode, mapCheckrLocation.month),
            "HighwayType" => Enum.GetValues<RoadType>().Where(r => r != RoadType.Unknown && l.Osm.RoadType.HasFlag(r)).Select(r => r.ToString()).Merge(" | "),
            _ => null
        }).Concat(new[] { l.Tag }).Where(x => x != null).Select(x => x!).ToArray();
        return tags.Any()
            ? new GeoMapLocationExtra
            {
                tags = tags
            }
            : null;
    }

    private static string? Range(int number, string bucketString)
    {
        if (!int.TryParse(bucketString, out var bucket))
        {
            return null;
        }

        var lower = ((number / bucket) * bucket);
        var upper = (((number / bucket) + 1) * bucket);
        return $"[{lower,4}-{upper,4}]m";
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