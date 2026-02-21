using System.Globalization;
using Vali.Core.Data;
using Vali.Core.Google;
using Vali.Core.Hash;

namespace Vali.Core;

public static class MapDefinitionDefaults
{
    private const string DefaultGeometryFilterCombinationMode = "intersection";
    public static readonly string[] HardcodedPanoIdCountries = ["ML", "JE", "IM", "CW", "BT"];

    public static MapDefinition ApplyDefaults(this MapDefinition definition)
    {
        var countryCodes = MapCountryCodes(definition.CountryCodes, definition.DistributionStrategy);
        var countryDistribution = CountryDistribution(definition);
        var namedExpressions = definition.NamedExpressions
            .ToDictionary(x => x.Key, x => ExpressionDefaults.Expand(definition.NamedExpressions, x.Value));
        var isYearMonthPanoVerificationStrategy = definition.Output.PanoVerificationStrategy?.StartsWith(GoogleApi.PanoStrategy.YearMonthPeriod.ToString()) == true;
        var yearMonthStrategy = ExtractYearMonthPanoStrategy(definition.Output.PanoVerificationStrategy);
        var definitionWithDefaults = definition with
        {
            CountryCodes = countryCodes,
            Output = definition.Output with
            {
                PanoIdCountryCodes = MapCountryCodes(definition.Output.PanoIdCountryCodes, definition.DistributionStrategy).Concat(HardcodedPanoIdCountries).Distinct().ToArray(),
                CountryHeadingExpressions = ExpandCountryDictionary(definition.Output.CountryHeadingExpressions),
                CountryPanoVerificationPanning = ExpandCountryDictionary(definition.Output.CountryPanoVerificationPanning),
                PanoVerificationStrategy = isYearMonthPanoVerificationStrategy
                    ? nameof(GoogleApi.PanoStrategy.YearMonthPeriod)
                    : definition.Output.PanoVerificationStrategy,
                PanoVerificationStart = isYearMonthPanoVerificationStrategy ? new DateOnly(yearMonthStrategy.yearStart, yearMonthStrategy.monthStart, 1) : null,
                PanoVerificationEnd = isYearMonthPanoVerificationStrategy ? new DateOnly(yearMonthStrategy.yearEnd, yearMonthStrategy.monthEnd, 1) : null,
            },
            SubdivisionInclusions = definition.SubdivisionInclusions,
            SubdivisionExclusions = definition.SubdivisionExclusions,
            CountryDistribution = countryDistribution,
            DistributionStrategy = definition.DistributionStrategy with
            {
                TreatCountriesAsSingleSubdivision = MapCountryCodes(definition.DistributionStrategy.TreatCountriesAsSingleSubdivision, definition.DistributionStrategy)
            },
            CountryLocationFilters = ExpandCountryDictionary(definition.CountryLocationFilters)
                .ToDictionary(x => x.Key, x => ExpressionDefaults.Expand(namedExpressions, x.Value)),
            CountryLocationPreferenceFilters = ExpandCountryDictionary(definition.CountryLocationPreferenceFilters)
                .ToDictionary(x => x.Key, x => x.Value.Select(y => y with
                {
                    Expression = ExpressionDefaults.Expand(namedExpressions, y.Expression),
                    NeighborFilters = y.NeighborFilters.Select(f => f with
                    {
                        Expression = ExpressionDefaults.Expand(namedExpressions, f.Expression)
                    }).ToArray(),
                    GeometryFilters = y.GeometryFilters.Select(g => g with
                    {
                        CombinationMode = y.GeometryFilters.FirstCombinationModeOrDefault()
                    }).ToArray()
                }).ToArray()),
            GlobalLocationFilter = definition.GlobalLocationFilter switch
            {
                {Length: > 0} => ExpressionDefaults.Expand(namedExpressions, definition.GlobalLocationFilter!),
                _ => "",
            },
            GlobalLocationPreferenceFilters = definition.GlobalLocationPreferenceFilters.Select(x => x with
            {
                Expression = ExpressionDefaults.Expand(namedExpressions, x.Expression),
                NeighborFilters = x.NeighborFilters.Select(f => f with
                {
                    Expression = ExpressionDefaults.Expand(namedExpressions, f.Expression)
                }).ToArray(),
                GeometryFilters = x.GeometryFilters.Select(g => g with
                {
                    CombinationMode = x.GeometryFilters.FirstCombinationModeOrDefault()
                }).ToArray()
            }).ToArray(),
            NeighborFilters = definition.NeighborFilters.Select(f => f with
            {
                Expression = ExpressionDefaults.Expand(namedExpressions, f.Expression)
            }).ToArray(),
            SubdivisionLocationFilters = definition.SubdivisionLocationFilters
                .ToDictionary(x => x.Key, x => x.Value
                    .ToDictionary(y => y.Key, y => ExpressionDefaults.Expand(namedExpressions, y.Value))),
            SubdivisionLocationPreferenceFilters = definition.SubdivisionLocationPreferenceFilters
                .ToDictionary(x => x.Key, x => x.Value
                    .ToDictionary(y => y.Key, y => y.Value.Select(z => z with
                    {
                        Expression = ExpressionDefaults.Expand(namedExpressions, z.Expression),
                        NeighborFilters = z.NeighborFilters.Select(f => f with
                        {
                            Expression = ExpressionDefaults.Expand(namedExpressions, f.Expression)
                        }).ToArray(),
                        GeometryFilters = z.GeometryFilters.Select(g => g with
                        {
                            CombinationMode = z.GeometryFilters.FirstCombinationModeOrDefault()
                        }).ToArray()
                    }).ToArray())),
            GeometryFilters = definition.GeometryFilters.Select(g => g with
            {
                CombinationMode = definition.GeometryFilters.FirstCombinationModeOrDefault(),
            }).ToArray(),
            CountryGeometryFilters = definition.CountryGeometryFilters.ToDictionary(cgf => cgf.Key, cgf => cgf.Value.Select(g => g with
            {
                CombinationMode = cgf.Value.FirstCombinationModeOrDefault()
            }).ToArray()).ApplyContinentBorderGeometryFilters(definition),
            SubdivisionGeometryFilters = definition.SubdivisionGeometryFilters.ToDictionary(sgf => sgf.Key, sgf => sgf.Value.ToDictionary(gf => gf.Key, gf => gf.Value.Select(g => g with
            {
                CombinationMode = gf.Value.FirstCombinationModeOrDefault()
            }).ToArray()))
        };

        return definitionWithDefaults;
    }

    public static Dictionary<string, T> ExpandCountryDictionary<T>(Dictionary<string, T> countryDictionary) =>
        countryDictionary == null
            ? new()
            : countryDictionary
                .SelectMany(x => MapCountryCodes([x.Key], new DistributionStrategy()).Select(y => (y, x.Value)))
                .GroupBy(x => x.y)
                .ToDictionary(x => x.Key, x => x.First().Value);

    private static Dictionary<string, GeometryFilter[]> ApplyContinentBorderGeometryFilters(
        this Dictionary<string, GeometryFilter[]> countryGeometryFilters,
        MapDefinition definition)
    {
        var continentFilters = definition.CountryCodes switch
        {
            ["europe"] => new Dictionary<string, GeometryFilter[]>
            {
                ["TR"] = [new GeometryFilter { InclusionMode = "include", PreloadedGeometries = ContinentBoundaries.EuropeanTurkey, CombinationMode = DefaultGeometryFilterCombinationMode }],
                ["RU"] = [new GeometryFilter { InclusionMode = "include", PreloadedGeometries = ContinentBoundaries.EuropeanRussia, CombinationMode = DefaultGeometryFilterCombinationMode }],
                ["KZ"] = [new GeometryFilter { InclusionMode = "include", PreloadedGeometries = ContinentBoundaries.EuropeanKazakhstan, CombinationMode = DefaultGeometryFilterCombinationMode }],
                ["ES"] = [new GeometryFilter { InclusionMode = "exclude", PreloadedGeometries = ContinentBoundaries.AfricanSpain, CombinationMode = DefaultGeometryFilterCombinationMode }],
            },
            ["asia"] => new Dictionary<string, GeometryFilter[]>
            {
                ["TR"] = [new GeometryFilter { InclusionMode = "exclude", PreloadedGeometries = ContinentBoundaries.EuropeanTurkey, CombinationMode = DefaultGeometryFilterCombinationMode }],
                ["RU"] = [new GeometryFilter { InclusionMode = "exclude", PreloadedGeometries = ContinentBoundaries.EuropeanRussia, CombinationMode = DefaultGeometryFilterCombinationMode }],
                ["KZ"] = [new GeometryFilter { InclusionMode = "exclude", PreloadedGeometries = ContinentBoundaries.EuropeanKazakhstan, CombinationMode = DefaultGeometryFilterCombinationMode }],
            },
            ["africa"] => new Dictionary<string, GeometryFilter[]>
            {
                ["ES"] = [new GeometryFilter { InclusionMode = "include", PreloadedGeometries = ContinentBoundaries.AfricanSpain, CombinationMode = DefaultGeometryFilterCombinationMode }],
            },
            ["oceania"] => new Dictionary<string, GeometryFilter[]>
            {
                ["US"] = [new GeometryFilter { InclusionMode = "include", PreloadedGeometries = ContinentBoundaries.Hawaii, CombinationMode = DefaultGeometryFilterCombinationMode }],
            },
            ["northamerica"] => new Dictionary<string, GeometryFilter[]>
            {
                ["US"] = [new GeometryFilter { InclusionMode = "exclude", PreloadedGeometries = ContinentBoundaries.Hawaii, CombinationMode = DefaultGeometryFilterCombinationMode }],
            },
            _ => []
        };

        return MergeGeometryFilters(countryGeometryFilters, continentFilters);
    }

    private static Dictionary<string, GeometryFilter[]> MergeGeometryFilters(
        Dictionary<string, GeometryFilter[]> existing,
        Dictionary<string, GeometryFilter[]> additional)
    {
        var result = new Dictionary<string, GeometryFilter[]>(existing);
        foreach (var (countryCode, filters) in additional)
        {
            result[countryCode] = result.TryGetValue(countryCode, out var existingFilters)
                ? existingFilters.Concat(filters).ToArray()
                : filters;
        }
        return result;
    }

    public static string[] MapCountryCodes(string[] countryCodes, DistributionStrategy distributionStrategy) =>
        countryCodes
            .SelectMany(c => c.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()))
            .SelectMany(s => ExpandCountryCode(s, distributionStrategy))
            .Distinct()
            .ToArray();

    public static string[] ExpandCountryCode(string countryCode, DistributionStrategy distributionStrategy)
    {
        var defaultDistribution = DefaultDistribution(distributionStrategy);
        return countryCode switch
        {
            "*" or "world" => CodesFromWeights(defaultDistribution),
            "europe" => CodesFromWeights(Weights.Europe),
            "asia" => CodesFromWeights(Weights.Asia),
            "africa" => CodesFromWeights(Weights.Africa),
            "southamerica" => CodesFromWeights(Weights.SouthAmerica),
            "northamerica" => CodesFromWeights(Weights.NorthAmerica),
            "oceania" => CodesFromWeights(Weights.Oceania),
            "lefthandtraffic" => CountryCodes.LeftHandTrafficCountries.Intersect(CodesFromWeights(defaultDistribution)).ToArray(),
            "righthandtraffic" => CountryCodes.Countries.Select(x => x.Key).Except(CountryCodes.LeftHandTrafficCountries).Intersect(CodesFromWeights(defaultDistribution)).ToArray(),
            _ => [countryCode.ToUpper()]
        };
    }

    private static string[] CodesFromWeights((string, int)[] weights) =>
        weights.Where(x => x.Item2 > 0).Select(x => x.Item1).ToArray();

    public static Dictionary<string, int> CountryDistribution(MapDefinition definition)
    {
        var countryCodes = MapCountryCodes(definition.CountryCodes, definition.DistributionStrategy);
        var defaultDistribution = DefaultDistribution(definition.DistributionStrategy);
        return definition.CountryCodes switch
        {
            _ when definition.CountryDistribution.Any() => definition.CountryDistribution,
            ["*"] => Distribution(defaultDistribution, definition),
            ["europe"] => Distribution(Weights.Europe, definition),
            ["asia"] => Distribution(Weights.Asia, definition),
            ["africa"] => Distribution(Weights.Africa, definition),
            ["southamerica"] => Distribution(Weights.SouthAmerica, definition),
            ["northamerica"] => Distribution(Weights.NorthAmerica, definition),
            ["oceania"] => Distribution(Weights.Oceania, definition),
            _ when definition.CountryDistribution.Count == 0 && countryCodes.Length == 1 => new() { { countryCodes.Single(), 10 } },
            _ when definition.CountryDistribution.Count == 0 => defaultDistribution
                .Where(x => countryCodes.Contains(x.Item1))
                .DistinctBy(x => x.Item1)
                .ToDictionary(x => x.Item1, x => x.Item2),
            _ => definition.CountryDistribution
        };
    }

    public static readonly string[] ValidMapShortNames = ["aarw", "aaw", "acw", "abw", "aiw", "proworld", "aow", "rainboltworld", "geotime", "lerg", "amw", "yellowbelly", "5kable"];

    public static (string, int)[] DefaultDistribution(DistributionStrategy distributionStrategy) =>
        distributionStrategy.CountryDistributionFromMap?.ToLowerInvariant() switch
        {
            "aarw" => Weights.ArbitraryRuralWorld,
            "aaw" => Weights.World,
            "acw" => Weights.CommunityWorld,
            "abw" => Weights.BalancedWorld,
            "aiw" => Weights.ImprovedWorld,
            "proworld" => Weights.ProWorld,
            "aow" => Weights.OfficialWorld,
            "rainboltworld" => Weights.RainboltWorld,
            "geotime" => Weights.GeoTime,
            "lerg" => Weights.LessExtremeRegionGuessing,
            "amw" => Weights.MovingWorld,
            "yellowbelly" => Weights.YellowBelly,
            "5kable" => Weights.A5kableWorld,
            { Length: > 0 } => [],
            _ => Weights.CommunityWorld
        };

    public static IEnumerable<NeighborFilter> AllNeighborFilters(this MapDefinition definition) =>
        definition.NeighborFilters
            .Concat(definition.GlobalLocationPreferenceFilters.SelectMany(f => f.NeighborFilters))
            .Concat(definition.CountryLocationPreferenceFilters.SelectMany(f => f.Value).SelectMany(f => f.NeighborFilters))
            .Concat(definition.SubdivisionLocationPreferenceFilters.SelectMany(f => f.Value.SelectMany(s => s.Value)).SelectMany(f => f.NeighborFilters));

    public static HashPrecision? HashPrecisionFromNeighborFiltersRadius(this MapDefinition definition) =>
        definition.AllNeighborFilters().Any()
            ? definition.AllNeighborFilters().Max(x => x.Radius) > 500
                ? HashPrecision.Size_km_5x5
                : HashPrecision.Size_km_1x1
            : null;

    public static HashPrecision? HashPrecisionFromProximityFilter(this ProximityFilter proximityFilter) =>
        proximityFilter.Radius > 0 && !string.IsNullOrEmpty(proximityFilter.LocationsPath)
            ? proximityFilter.Radius switch
            {
                < 500 => HashPrecision.Size_km_1x1,
                < 3000 => HashPrecision.Size_km_5x5,
                _ => HashPrecision.Size_km_39x20
            }
            : null;

    public static (GoogleApi.PanoStrategy? panoStrategy, int yearStart, int monthStart, int yearEnd, int monthEnd) ExtractYearMonthPanoStrategy(string? strategyString)
    {
        if (string.IsNullOrEmpty(strategyString) ||
            !strategyString.StartsWith(GoogleApi.PanoStrategy.YearMonthPeriod.ToString()))
        {
            return default;
        }

        var yearMonthPart = strategyString[GoogleApi.PanoStrategy.YearMonthPeriod.ToString().Length..];
        if (yearMonthPart.Length is not 12 || yearMonthPart.Any(c => !char.IsNumber(c)))
        {
            return default;
        }

        return yearMonthPart switch
        {
            { Length: 12 } => (YearMonth: GoogleApi.PanoStrategy.YearMonthPeriod, ParseNum(yearMonthPart[..4]), ParseNum(yearMonthPart.Substring(4, 2)), ParseNum(yearMonthPart.Substring(6, 4)), ParseNum(yearMonthPart.Substring(10, 2))),
            _ => (YearMonth: GoogleApi.PanoStrategy.YearMonthPeriod, 0, 0, 0, 0)
        };

        int ParseNum(string num) =>
            int.Parse(num, CultureInfo.InvariantCulture);
    }

    public static LiveGenerateMapDefinition ApplyDefaults(this LiveGenerateMapDefinition definition)
    {
        var isYearMonthPanoVerificationStrategy = definition.PanoSelectionStrategy?.StartsWith(GoogleApi.PanoStrategy.YearMonthPeriod.ToString()) == true;
        var yearMonthStrategy = ExtractYearMonthPanoStrategy(definition.PanoSelectionStrategy);

        return definition with
        {
            PanoSelectionStrategy = isYearMonthPanoVerificationStrategy
                ? GoogleApi.PanoStrategy.YearMonthPeriod.ToString()
                : definition.PanoSelectionStrategy,
            PanoVerificationStart = isYearMonthPanoVerificationStrategy ? new DateOnly(yearMonthStrategy.yearStart, yearMonthStrategy.monthStart, 1) : null,
            PanoVerificationEnd = isYearMonthPanoVerificationStrategy ? new DateOnly(yearMonthStrategy.yearEnd, yearMonthStrategy.monthEnd, 1) : null,
        };
    }

    private static Dictionary<string, int> Distribution((string, int)[] weights, MapDefinition mapDefinition) =>
        mapDefinition switch
        {
            { CountryDistribution.Count: > 0 } => mapDefinition.CountryDistribution,
            _ => weights.Where(x => x.Item2 > 0).ToDictionary(x => x.Item1, x => x.Item2)
        };

    private static string FirstCombinationModeOrDefault(this GeometryFilter[] filters) =>
        !string.IsNullOrEmpty(filters.FirstOrDefault()?.CombinationMode) ? filters.First().CombinationMode : DefaultGeometryFilterCombinationMode;
}