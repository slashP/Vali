using System.Globalization;
using Vali.Core.Data;

namespace Vali.Core.Validation;

public static class FilterValidation
{
    public static MapDefinition? ValidateFilters(this MapDefinition definition)
    {
        foreach (var namedExpression in definition.NamedExpressions)
        {
            if (!namedExpression.Key.StartsWith("$$"))
            {
                ConsoleLogger.Error($"All named expressions must start with $$. {namedExpression.Key} does not.");
                return null;
            }

            var overlapping = definition.NamedExpressions.FirstOrDefault(x => x.Key != namedExpression.Key && x.Key.Contains(namedExpression.Key));
            if (overlapping.Key != null)
            {
                ConsoleLogger.Error($"Named expression keys must not overlap. {overlapping.Key} vs. {namedExpression.Key}");
                return null;
            }
        }

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

        static void DryRun(string filter)
        {
            var expression = LocationLakeFilterer.CompileExpression<Location, bool>(filter, true);
            var locations = EmptyLocationArray().Where(expression).ToArray();
        }

        static void DryRunNeighborFilter(string filter)
        {
            var expression = LocationLakeFilterer.CompileParentBoolLocationExpression(filter);
            var locations = EmptyLocationArray().Where(l => EmptyLocationArray().Any(l2 => expression(l2, l))).ToArray();
        }

        var neighborFilterValidProperties = LocationLakeFilterer.ValidProperties()
            .SelectMany(x => new[]
            {
                x,
                $"current:{x}"
            })
            .ToArray();
        var def = new[] { definition.GlobalLocationFilter }
            .Concat(definition.CountryLocationFilters.Select(x => x.Value))
            .Concat(definition.SubdivisionLocationFilters.SelectMany(x => x.Value.Select(y => y.Value)))
            .Concat(definition.GlobalLocationPreferenceFilters.Select(x => x.Expression))
            .Concat(definition.CountryLocationPreferenceFilters.SelectMany(x => x.Value.Select(y => y.Expression)))
            .Concat(definition.SubdivisionLocationPreferenceFilters.SelectMany(x => x.Value.SelectMany(y => y.Value.Select(z => z.Expression))))
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(f => definition.ValidateExpression(f!, DryRun, $"Invalid filter {f}", LocationLakeFilterer.ValidProperties(), LocationLakeFilterer.ValidProperties()))
            .Any(validated => validated == null)
            ? null
            : definition;
        return def == null || definition.NeighborFilters
            .Concat(definition.GlobalLocationPreferenceFilters.SelectMany(x => x.NeighborFilters))
            .Concat(definition.CountryLocationPreferenceFilters.SelectMany(x => x.Value.SelectMany(y => y.NeighborFilters)))
            .Concat(definition.SubdivisionLocationPreferenceFilters.SelectMany(x => x.Value.SelectMany(y => y.Value.SelectMany(z => z.NeighborFilters))))
            .Select(n => n.Expression)
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(f => definition.ValidateExpression(f!, DryRunNeighborFilter, $"Invalid neighbor filter {f}", neighborFilterValidProperties, LocationLakeFilterer.ValidProperties()))
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

    public static MapDefinition? ValidateNeighborFilters(this MapDefinition definition)
    {
        foreach (var neighborFilter in definition.AllNeighborFilters())
        {
            if (neighborFilter.Radius <= 0)
            {
                ConsoleLogger.Error($"All {nameof(definition.NeighborFilters)} must have radius lager than 0.");
                return null;
            }

            const int maxRadius = 5000;
            if (neighborFilter.Radius > maxRadius)
            {
                ConsoleLogger.Error($"Using {nameof(definition.NeighborFilters)} with radius lager than {maxRadius} is not supported due to performance reasons.");
                return null;
            }

            if (neighborFilter.Bound is not ("gte" or "lte" or "all" or "none" or "some" or "percentage-gte" or "percentage-lte"))
            {
                ConsoleLogger.Error($"{nameof(neighborFilter)} {nameof(neighborFilter.Bound).ToLower()} (\"{neighborFilter.Bound}\") must be either 'gte' (greater than or equal) / 'lte' (less than or equal) / 'all' / 'none' / 'some' / 'percentage-gte' / 'percentage-lte'.");
                return null;
            }

            if (neighborFilter is { Limit: not null, Bound: "all" or "none" or "some" })
            {
                ConsoleLogger.Error("Do not set limit when using 'all' / 'none' / 'some' as bound.");
                return null;
            }

            if (neighborFilter is { Limit: < 0, Bound: "gte" or "lte" or "percentage-gte" or "percentage-lte" })
            {
                ConsoleLogger.Error($"Using {nameof(definition.NeighborFilters)} with limit less than 0 is not supported.");
                return null;
            }

            if (neighborFilter is { Limit: 0, Bound: "gte" or "percentage-gte" })
            {
                ConsoleLogger.Error($"Using {nameof(definition.NeighborFilters)} with limit 0 and bound 'gte'/'percentage-gte' does not have any effect since there are always 0 locations meeting the requirements. Did you mean bound 'lte'/'percentage-lte'?");
                return null;
            }

            if (neighborFilter is { Limit: > 100, Bound: "percentage-gte" or "percentage-lte" })
            {
                ConsoleLogger.Error($"Using {nameof(definition.NeighborFilters)} with limit bigger than 100 and bound 'percentage-gte'/'percentage-lte' is not supported.");
                return null;
            }
        }

        return definition;
    }

    public static MapDefinition? ValidateProximityFilters(this MapDefinition definition)
    {
        var proximityFilters = new[] { definition.ProximityFilter }
            .Concat(definition.CountryProximityFilters.Select(f => f.Value))
            .Concat(definition.SubdivisionProximityFilters.Select(s => s.Value).SelectMany(f => f.Values))
            .ToArray();
        foreach (var proximityFilter in proximityFilters)
        {
            if (proximityFilter.Radius <= 0 && !string.IsNullOrEmpty(proximityFilter.LocationsPath))
            {
                ConsoleLogger.Error($"Using {nameof(proximityFilter)} with radius less than 1 is not supported.");
                return null;
            }

            const int maxRadius = 30_000;
            if (proximityFilter.Radius > maxRadius)
            {
                ConsoleLogger.Error($"Using {nameof(definition.NeighborFilters)} with radius lager than {maxRadius} is not supported due to performance reasons.");
                return null;
            }

            if (!string.IsNullOrEmpty(proximityFilter.LocationsPath) && !File.Exists(proximityFilter.LocationsPath))
            {
                ConsoleLogger.Error($"File {proximityFilter.LocationsPath} used in a {nameof(proximityFilter)} does not exist.");
                return null;
            }
        }

        return definition;
    }

    public static MapDefinition? ValidatePolygonFilters(this MapDefinition definition)
    {
        var polygonFilters = definition.PolygonFilters
            .Concat(definition.CountryPolygonFilters.SelectMany(f => f.Value))
            .Concat(definition.SubdivisionPolygonFilters.Select(s => s.Value).SelectMany(f => f.Values).SelectMany(f => f))
            .ToArray();
        foreach (var polygonFilter in polygonFilters)
        {
            if (!string.IsNullOrEmpty(polygonFilter.PolygonsPath) && !File.Exists(polygonFilter.PolygonsPath))
            {
                ConsoleLogger.Error($"File {polygonFilter.PolygonsPath} used in a {nameof(polygonFilter)} does not exist.");
                return null;
            }
        }
        return definition;
    }

    public static T? ValidateExpression<T>(this T definition, string filter, Action<string> dryRun, string dryRunExceptionMessage, IReadOnlyCollection<string> validProperties, IReadOnlyCollection<string> outputVisibleValidProperties)
    {
        if (filter == "")
        {
            ConsoleLogger.Error("Expressions/filters cannot be empty.");
            return default;
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
                return default;
            }
        }

        var removeStringsInSingleQuotes = filter.ReplaceValuesInSingleQuotesWithPlaceHolders().expressionWithPlaceholders;
        foreach (var expressionValue in removeStringsInSingleQuotes.RemoveMultipleSpaces().RemoveParentheses().Split(' '))
        {
            var operators = LocationLakeFilterer.ValidOperators().Select(x => x.Trim()).ToArray();
            var properties = validProperties.Select(x => x.Trim()).ToArray();
            if (!operators.Contains(expressionValue.Trim(), StringComparer.InvariantCultureIgnoreCase) &&
                !properties.Contains(expressionValue.Trim()) &&
                !double.TryParse(expressionValue, NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
                !bool.TryParse(expressionValue, out _) &&
                !IsSingleQuoteWord(expressionValue) &&
                expressionValue != "null")
            {
                if (expressionValue.Contains("'"))
                {
                    ConsoleLogger.Error("Using single quotes inside single quotes requires escaping by putting a backslash (\\) in front of it");
                    return default;
                }

                ConsoleLogger.Error($"""
                                     Expression value {expressionValue} is not valid. Must be
                                     * A number.
                                     * A value in single quotes like 'gravel'.
                                     * One of the operators [{operators.Merge(", ")}]
                                     * One of the properties [{outputVisibleValidProperties.Select(x => x.Trim()).Merge(", ")}]
                                     * null
                                     """);
                return default;
            }
        }

        try
        {
            dryRun(filter);
        }
        catch (Exception e)
        {
            ConsoleLogger.Error(dryRunExceptionMessage);
            ConsoleLogger.Error(e.ToString());
            return default;
        }

        return definition;
    }

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

        return expression.TrimStart(singleQuote).TrimEnd(singleQuote).All(c => char.IsAsciiLetterOrDigit(c) || c == Extensions.PlaceholderValue || c == '_');
    }

    public static IEnumerable<Location> EmptyLocationArray() =>
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