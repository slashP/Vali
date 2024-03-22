using static Vali.Core.LocationLakeMapGenerator;
using System.Globalization;
using Vali.Core.Data;

namespace Vali.Core;

public static class TagsGenerator
{
    public static GeoMapLocationExtra? Tags(MapDefinition mapDefinition, Location l) =>
        mapDefinition.Output.LocationTags.Any()
            ? new GeoMapLocationExtra
            {
                tags = mapDefinition.Output.LocationTags.Select(e => e switch
                {
                    "SubdivisionCode" => SubdivisionCode(l),
                    "County" => County(l),
                    "Surface" => Surface(l),
                    "Year" => Year(l),
                    "Month" => Month(l),
                    "YearMonth" => YearMonth(l),
                    "Season" => Season(l),
                    _ => null
                }).Concat(new[] { l.Tag }).Where(x => x != null).Select(x => x!).ToArray()
            }
            : null;

    public static string SubdivisionCode(Location l) => l.Nominatim.SubdivisionCode;

    public static string? County(Location l) => l.Nominatim.County;

    public static string? Surface(Location l) => l.Osm.Surface;

    public static string Year(Location l) => l.Google.Year.ToString();

    public static string Month(Location l) => new DateTime(DateTime.Now.Year, l.Google.Month, 1).ToString("MMM", CultureInfo.InvariantCulture);

    public static string YearMonth(Location l) => $"{l.Google.Year}-{l.Google.Month.ToString().PadLeft(2, '0')}";

    public static string Season(Location l) =>
        (CountryCodes.Hemisphere(l.Nominatim.CountryCode), l.Google.Month) switch
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