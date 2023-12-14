using System.Globalization;
using System.Text.RegularExpressions;
using Vali.Core.Data;

namespace Vali.Core.Validation;

public static class FilterValidation
{
    public static MapDefinition? ValidateFilters(this MapDefinition definition)
    {
        foreach (var filter in definition.CountryLocationFilters)
        {
            if (!CountryCodes.Countries.ContainsKey(filter.Key))
            {
                ConsoleLogger.Error($"Country code {filter.Key} is not valid in {nameof(definition.CountryLocationFilters)}");
                return null;
            }
        }

        foreach (var filter in definition.SubdivisionLocationFilters)
        {
            var subdivisionWeights = SubdivisionWeights.CountryToSubdivision.TryGetValue(filter.Key, out var divisions)
                ? divisions
                : new();
            foreach (var (subdivisionCode, _) in filter.Value)
            {
                if (!subdivisionWeights.ContainsKey(subdivisionCode))
                {
                    ConsoleLogger.Error($"""
                                         Subdivision code {subdivisionCode} is not valid in {nameof(definition.SubdivisionLocationFilters)}
                                         Must be one of {subdivisionWeights.Select(x => x.Key).Merge(", ")}
                                         """);
                    return null;
                }
            }
        }

        return definition.GlobalLocationFilters
            .Concat(definition.CountryLocationFilters.SelectMany(x => x.Value))
            .Concat(definition.SubdivisionLocationFilters.SelectMany(x => x.Value.SelectMany(y => y.Value)))
            .Concat(definition.GlobalLocationPreferenceFilters.Select(x => x.Expression))
            .Concat(definition.CountryLocationPreferenceFilters.SelectMany(x => x.Value.Select(y => y.Expression)))
            .Concat(definition.SubdivisionLocationPreferenceFilters.SelectMany(x => x.Value.SelectMany(y => y.Value.Select(z => z.Expression))))
            .Select(definition.ValidateFilter)
            .Any(validated => validated == null)
            ? null
            : definition;
    }

    public static MapDefinition? ValidatePreferenceFilters(this MapDefinition definition)
    {
        var filters = definition.GlobalLocationPreferenceFilters
            .Concat(definition.CountryLocationPreferenceFilters.SelectMany(x => x.Value))
            .Concat(definition.SubdivisionLocationPreferenceFilters.SelectMany(x => x.Value.SelectMany(y => y.Value)));
        if (filters.Any(x => x is { Fill: false, Percentage: < 1 or > 100 }))
        {
            ConsoleLogger.Error($"Preference filter percentages must be between 1 and 100 or set {nameof(LocationPreferenceFilter.Fill)} to true");
            return null;
        }

        if (definition.GlobalLocationPreferenceFilters.Any() && definition.GlobalLocationPreferenceFilters.Sum(x => x.Percentage) > 100)
        {
            ConsoleLogger.Error($"{nameof(definition.GlobalLocationPreferenceFilters)} percentage must sum to less than 100. Use * to match anything.");
            return null;
        }

        if (definition.GlobalLocationPreferenceFilters.Count(x => x.Fill) > 1)
        {
            ConsoleLogger.Error($"{nameof(definition.GlobalLocationPreferenceFilters)} {nameof(LocationPreferenceFilter.Fill)} can only be true for one entry.");
            return null;
        }

        if (definition.CountryLocationPreferenceFilters.Any(c => c.Value.Any() && c.Value.Sum(x => x.Percentage) > 100))
        {
            ConsoleLogger.Error($"{nameof(definition.CountryLocationPreferenceFilters)} percentage must sum to less than 100. Use * to match anything.");
            return null;
        }

        if (definition.CountryLocationPreferenceFilters.Any(c => c.Value.Count(x => x.Fill) > 1))
        {
            ConsoleLogger.Error($"{nameof(definition.CountryLocationPreferenceFilters)} {nameof(LocationPreferenceFilter.Fill)} can only be true for one entry.");
            return null;
        }

        if (definition.SubdivisionLocationPreferenceFilters.Any(c => c.Value.Any() && c.Value.Any(s => s.Value.Any() && s.Value.Sum(x => x.Percentage) > 100)))
        {
            ConsoleLogger.Error($"{nameof(definition.SubdivisionLocationPreferenceFilters)} percentage must sum to 100. Use * to match anything.");
            return null;
        }

        if (definition.SubdivisionLocationPreferenceFilters.Any(c => c.Value.Any(s => s.Value.Count(x => x.Fill) > 1)))
        {
            ConsoleLogger.Error($"{nameof(definition.SubdivisionLocationPreferenceFilters)} {nameof(LocationPreferenceFilter.Fill)} can only be true for one entry.");
            return null;
        }

        return definition;
    }

    private static MapDefinition? ValidateFilter(this MapDefinition definition, string filter)
    {
        if (filter == "")
        {
            ConsoleLogger.Error("Filters cannot be empty.");
            return null;
        }

        if (filter == "*")
        {
            return definition;
        }

        foreach (var dotIndex in filter.AllIndexesOf("."))
        {
            if (dotIndex == 0 || dotIndex == filter.Length - 1 || !char.IsNumber(filter[dotIndex - 1]) || !char.IsNumber(filter[dotIndex + 1]))
            {
                ConsoleLogger.Error($"Only numbers can have the character '.': {filter}");
                return null;
            }
        }

        var removeStringsInSingleQuotes = filter.ReplaceValuesInSingleQuotesWithPlaceHolders().expressionWithPlaceholders;
        foreach (var expressionValue in removeStringsInSingleQuotes.ReplaceMultipleSpaces().Replace("(", "").Replace(")", "").Split(' '))
        {
            var operators = LocationLakeFilterer.ValidOperators().Select(x => x.Trim()).ToArray();
            var properties = LocationLakeFilterer.ValidProperties().Select(x => x.Trim()).ToArray();
            if (!operators.Contains(expressionValue.Trim(), StringComparer.InvariantCultureIgnoreCase) &&
                !properties.Contains(expressionValue.Trim()) &&
                !double.TryParse(expressionValue, NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
                !IsSingleQuoteWord(expressionValue))
            {
                if (expressionValue.Contains("'"))
                {
                    ConsoleLogger.Error("Using single quotes inside single quotes requires escaping by putting a backslash (\\) in front of it");
                    return null;
                }

                ConsoleLogger.Error($"""
                                     Expression value {expressionValue} is not valid. Must be
                                     * A number.
                                     * A value in single quotes like 'gravel'.
                                     * One of the operators [{operators.Merge(", ")}]
                                     * One of the properties [{properties.Merge(", ")}]
                                     """);
                return null;
            }
        }

        try
        {
            var expression = LocationLakeFilterer.CompileLocationExpression(filter);
            var locations = EmptyLocationArray().Where(expression).ToArray();
        }
        catch (Exception)
        {
            ConsoleLogger.Error($"""
                                 Invalid filter {filter}
                                 """);
            return null;
        }

        return definition;
    }

    private static string ReplaceMultipleSpaces(this string input) =>
        Regex.Replace(input, @"\s+", " ");


    private static bool IsSingleQuoteWord(string expression)
    {
        const char singleQuote = '\'';
        if (expression.Length < 3 ||
            expression[0] != singleQuote ||
            expression[^1] != singleQuote ||
            expression.Count(c => c == singleQuote) > 2)
        {
            return false;
        }

        return expression.TrimStart(singleQuote).TrimEnd(singleQuote).All(c => char.IsAsciiLetterOrDigit(c) || c == Extensions.PlaceholderValue);
    }

    private static IEnumerable<Location> EmptyLocationArray() =>
        new[]
        {
            new Location
            {
                Osm = new OsmData(),
                Google = new GoogleData
                {
                    PanoId = "",
                    CountryCode = "NO"
                },
                Nominatim = new NominatimData
                {
                    CountryCode = "NO",
                    SubdivisionCode = "NO-03"
                }
            }
        };
}