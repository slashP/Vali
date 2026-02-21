using System.Globalization;
using Spectre.Console;
using System.Text.Json;
using Vali.Core.Data;
using Vali.Core.Expressions;
using Vali.Core.Google;

namespace Vali.Core.Validation;

public static class LiveGenerateValidator
{
    public static LiveGenerateMapDefinition? TryDeserialize(string json)
    {
        try
        {
            return Serializer.Deserialize<LiveGenerateMapDefinition>(json);
        }
        catch (JsonException e)
        {
            ConsoleLogger.Error("The JSON file is not properly formatted. Try checking if it's valid JSON on https://jsonlint.com/");
            AnsiConsole.WriteLine(e.Message);
            return null;
        }
    }

    public static LiveGenerateMapDefinition? Validate(this LiveGenerateMapDefinition definition) =>
        definition
            .ValidateDefinition()
            ?.ValidateLocationFilter();

    private static LiveGenerateMapDefinition? ValidateDefinition(this LiveGenerateMapDefinition definition)
    {
        var invalidCountryCodes = definition.Countries.Keys.Where(x => !CountryCodes.Countries.ContainsKey(x)).ToArray();
        if (invalidCountryCodes.Length != 0)
        {
            ConsoleLogger.Error($"Invalid country code(s): {invalidCountryCodes.Merge(", ")}");
            ConsoleLogger.Warn($"Must be one of: {CountryCodes.Countries.Keys.Merge(", ")}");
            return null;
        }

        var roadsFolder = DataDownloadService.RoadsFolder();
        var countriesWithRoadData = Directory.Exists(roadsFolder)
            ? Directory.GetDirectories(roadsFolder).Select(x => new DirectoryInfo(x).Name).ToArray()
            : [];
        var unsupportedCountryCodes = definition.Countries.Keys.Where(x => !countriesWithRoadData.Contains(x)).ToArray();
        if (countriesWithRoadData.Length > 0 && unsupportedCountryCodes.Length != 0)
        {
            ConsoleLogger.Error($"Country code(s) do not have road data available and are not supported: {unsupportedCountryCodes.Merge(", ")}");
            ConsoleLogger.Warn($"Must be one of: {countriesWithRoadData.Merge(", ")}");
            return null;
        }

        if (definition.Countries.Count == 0)
        {
            ConsoleLogger.Error("No country specified. Specify at least one country in 'countries'.");
            return null;
        }

        if (definition.Distribution != null && definition.Distribution.MinMinDistance <= 0)
        {
            ConsoleLogger.Error("distribution.minMinDistance must be a positive number.");
            return null;
        }

        var mapCheckrLocation = ExampleMapCheckrLocation();
        var invalidTags = definition.LocationTags
            .Where(x => TagsGenerator
                .Tags(FilterValidation.EmptyLocationArray().First(), mapCheckrLocation, [x])?.tags
                .Length is null or 0)
            .ToArray();
        if (invalidTags.Length != 0)
        {
            ConsoleLogger.Error($"Invalid location tag(s): {invalidTags.Merge(", ")}");
            return null;
        }

        if (InvalidDate(definition.FromDate, "yyyy-MM"))
        {
            ConsoleLogger.Error("fromDate must be in the format yyyy-mm");
            return null;
        }

        if (InvalidDate(definition.ToDate, "yyyy-MM"))
        {
            ConsoleLogger.Error("toDate must be in the format yyyy-mm");
            return null;
        }

        if (!string.IsNullOrEmpty(definition.HeadingMode) && definition.HeadingMode != "DrivingDirection" && definition.HeadingMode != "Random")
        {
            ConsoleLogger.Error("headingMode must be empty, 'DrivingDirection' or 'Random'");
            return null;
        }

        if (definition is { HeadingMode: "Random", HeadingDelta: not null })
        {
            ConsoleLogger.Error("Specifying 'headingDelta' when headingMode is Random does not work.");
            return null;
        }

        if (definition.HeadingDelta is < 0 or > 359)
        {
            ConsoleLogger.Error("headingDelta must be between 0 and 359");
            return null;
        }

        if (!string.IsNullOrEmpty(definition.PitchMode) && definition.PitchMode != "Random")
        {
            ConsoleLogger.Error("pitchMode must be empty or 'Random'");
            return null;
        }

        if (definition is { PitchMode: "Random", Pitch: not null })
        {
            ConsoleLogger.Error("Specifying 'pitch' when pitchMode is Random does not work.");
            return null;
        }

        if (definition.PitchMode != "Random" && (definition.RandomPitchMin.HasValue || definition.RandomPitchMax.HasValue))
        {
            ConsoleLogger.Error("Specifying 'randomPitchMin' or 'randomPitchMax' when pitchMode is not Random does not work.");
            return null;
        }

        if (definition.RandomPitchMin is < -89 or > 89)
        {
            ConsoleLogger.Error("randomPitchMin must be between -89 and 89");
            return null;
        }

        if (definition.RandomPitchMax is < -89 or > 89)
        {
            ConsoleLogger.Error("randomPitchMax must be between -89 and 89");
            return null;
        }

        if (!string.IsNullOrEmpty(definition.ZoomMode) && definition.ZoomMode != "Random")
        {
            ConsoleLogger.Error("zoomMode must be empty or 'Random'");
            return null;
        }

        if (definition is { ZoomMode: "Random", Zoom: not null })
        {
            ConsoleLogger.Error("Specifying 'zoom' when zoomMode is Random does not work.");
            return null;
        }

        if (definition.ZoomMode != "Random" && (definition.RandomZoomMin.HasValue || definition.RandomZoomMax.HasValue))
        {
            ConsoleLogger.Error("Specifying 'randomPitchMin' or 'randomPitchMax' when pitchMode is not Random does not work.");
            return null;
        }

        if (definition.RandomZoomMin is < LiveGenerate.MinZoom or > LiveGenerate.MaxZoom)
        {
            ConsoleLogger.Error("randomZoomMin must be between -89 and 89.");
            return null;
        }

        if (definition.RandomZoomMax is < LiveGenerate.MinZoom or > LiveGenerate.MaxZoom)
        {
            ConsoleLogger.Error("randomZoomMax must be between -89 and 89.");
            return null;
        }

        if (definition.BoxPrecision is < 0 or > 9)
        {
            ConsoleLogger.Error("boxPrecision must be between 0 and 9.");
            return null;
        }

        if (definition.ParallelRequests < 1)
        {
            ConsoleLogger.Error("chunkSize must be larger than 0.");
            return null;
        }

        if (definition.Radius < 5)
        {
            ConsoleLogger.Error("radius must be at least 5.");
            return null;
        }

        if (!string.IsNullOrEmpty(definition.PanoSelectionStrategy) && !definition.PanoSelectionStrategy.StartsWith(GoogleApi.PanoStrategy.YearMonthPeriod.ToString()))
        {
            if (!Enum.TryParse<GoogleApi.PanoStrategy>(definition.PanoSelectionStrategy, out var strategy))
            {
                ConsoleLogger.Error($"panoSelectionStrategy must be empty or one of {Enum.GetValues<GoogleApi.PanoStrategy>().Select(x => x.ToString()).Merge(", ")}");
                return null;
            }
        }

        if (!string.IsNullOrEmpty(definition.PanoSelectionStrategy) && definition.PanoSelectionStrategy.StartsWith(GoogleApi.PanoStrategy.YearMonthPeriod.ToString()))
        {
            var strategy = MapDefinitionDefaults.ExtractYearMonthPanoStrategy(definition.PanoSelectionStrategy);
            if (strategy.panoStrategy == null)
            {
                ConsoleLogger.Error("panoSelectionStrategy 'YearMonth' must be followed by exactly 6 or 12 numbers, correctly assigning year and month. yyyymmyyyymm");
                return null;
            }

            if (strategy.yearStart > DateTime.Now.Year || strategy.yearStart < 2000 ||
                strategy.yearEnd > DateTime.Now.Year || strategy.yearEnd < 2000 ||
                strategy.monthStart < 1 || strategy.monthStart > 12 || strategy.monthEnd < 1 || strategy.monthEnd > 12)
            {
                ConsoleLogger.Error("You must use reasonable values for year and month in panoSelectionStrategy 'YearMonth'");
                return null;
            }
        }

        var acceptedCoverageTypes = new[] { "Official", "Unofficial", "All" };
        if (!string.IsNullOrEmpty(definition.AcceptedCoverage) && !acceptedCoverageTypes.Contains(definition.AcceptedCoverage))
        {
            ConsoleLogger.Error($"acceptedCoverage must be one of {acceptedCoverageTypes.Merge(", ")}.");
            return null;
        }

        foreach (var geoJsonFile in definition.GeoJsonFiles)
        {
            if (!File.Exists(geoJsonFile))
            {
                ConsoleLogger.Error($"File {geoJsonFile} does not exist.");
                return null;
            }

            if (definition.Countries.Count > 1)
            {
                ConsoleLogger.Warn("Combining the use of geo json file(s) with multiple countries is probably not a good idea. Use with caution.");
            }
        }

        return definition;
    }

    private static LiveGenerateMapDefinition? ValidateLocationFilter(this LiveGenerateMapDefinition definition)
    {
        if (string.IsNullOrEmpty(definition.LocationFilter))
        {
            return definition;
        }

        if (definition.ValidateExpression(
                definition.LocationFilter,
                DryRun,
                $"Invalid filter {definition.LocationFilter}",
                LocationLakeFilterer.ValidMapCheckrLocationProperties(),
                LocationLakeFilterer.ValidMapCheckrLocationProperties(),
                PropertyResolver.ForMapCheckrLocation()) == null)
        {
            return null;
        }

        try
        {
            DryRun(definition.LocationFilter, definition);
        }
        catch (Exception)
        {
            ConsoleLogger.Error($"Filter {definition.LocationFilter} is invalid.");
            return null;
        }

        return definition;

        static void DryRun<T>(string filter, T definition)
        {
            var expression = LocationLakeFilterer.CompileExpression<MapCheckrLocation, bool>(filter, true);
            var locations = new[] { ExampleMapCheckrLocation() }.Where(expression).ToArray();
        }
    }

    private static MapCheckrLocation ExampleMapCheckrLocation()
    {
        return new MapCheckrLocation
        {
            elevation = 200,
            countryCode = "NO",
            subdivision = "Oslo",
        };
    }

    private static bool InvalidDate(string? dateString, string? formatString) =>
        dateString != null && !DateTime.TryParseExact(dateString, formatString,
            CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out _);
}