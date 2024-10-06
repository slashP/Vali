using Vali.Core.Data;

namespace Vali.Core;

public static class MapDefinitionDefaults
{
    public static readonly string[] HardcodedPanoIdCountries = ["ML", "JE", "IM", "CW", "BT"];

    public static MapDefinition ApplyDefaults(this MapDefinition definition)
    {
        var countryCodes = MapCountryCodes(definition.CountryCodes, definition.DistributionStrategy);
        var countryDistribution = CountryDistribution(definition);
        var definitionWithDefaults = definition with
        {
            CountryCodes = countryCodes,
            Output = definition.Output with
            {
                PanoIdCountryCodes = MapCountryCodes(definition.Output.PanoIdCountryCodes, definition.DistributionStrategy).Concat(HardcodedPanoIdCountries).Distinct().ToArray(),
                CountryHeadingExpressions = ExpandCountryDictionary(definition.Output.CountryHeadingExpressions)
            },
            SubdivisionInclusions = Inclusions(definition),
            SubdivisionExclusions = Exclusions(definition),
            CountryDistribution = countryDistribution,
            DistributionStrategy = definition.DistributionStrategy with
            {
                TreatCountriesAsSingleSubdivision = MapCountryCodes(definition.DistributionStrategy.TreatCountriesAsSingleSubdivision, definition.DistributionStrategy)
            },
            CountryLocationFilters = ExpandCountryDictionary(definition.CountryLocationFilters).ApplyContinentBorderFilters(definition),
            CountryLocationPreferenceFilters = ExpandCountryDictionary(definition.CountryLocationPreferenceFilters),
        };
        return definitionWithDefaults;
    }

    private static Dictionary<string, T> ExpandCountryDictionary<T>(Dictionary<string, T> countryDictionary) =>
        countryDictionary == null
            ? new()
            : countryDictionary
                .SelectMany(x => MapCountryCodes([x.Key], new DistributionStrategy()).Select(y => (y, x.Value)))
                .GroupBy(x => x.y)
                .ToDictionary(x => x.Key, x => x.First().Value);

    private static Dictionary<string, string[]> Inclusions(MapDefinition definition) =>
        definition.CountryCodes switch
        {
            ["europe"] => definition.SubdivisionInclusions
                .Concat(new[]
                {
                    EuropeanTurkiye(),
                    EuropeanKazakhstan(),
                }).GroupBy(x => x.Key, x => x.Value).ToDictionary(x => x.Key, x => x.First()),
            ["africa"] => definition.SubdivisionInclusions
                .Concat(new[]
                {
                    AfricanSpain(),
                }).GroupBy(x => x.Key, x => x.Value).ToDictionary(x => x.Key, x => x.First()),
            ["asia"] => definition.SubdivisionInclusions
                .Concat(new[]
                {
                    AsianRussia(),
                }).GroupBy(x => x.Key, x => x.Value).ToDictionary(x => x.Key, x => x.First()),
            ["oceania"] => definition.SubdivisionInclusions
                .Concat(new[]
                {
                    new KeyValuePair<string, string[]>("US", ["US-HI"])
                }).GroupBy(x => x.Key, x => x.Value).ToDictionary(x => x.Key, x => x.First()),
            _ => definition.SubdivisionInclusions
        };

    private static Dictionary<string, string[]> Exclusions(MapDefinition definition) =>
        definition.CountryCodes switch
        {
            ["europe"] => definition.SubdivisionExclusions
                .Concat(new[]
                {
                    AfricanSpain(),
                    AsianRussia()
                }).GroupBy(x => x.Key, x => x.Value).ToDictionary(x => x.Key, x => x.First()),
            ["asia"] => definition.SubdivisionExclusions
                .Concat(new[]
                {
                    EuropeanTurkiye(),
                    EuropeanKazakhstan(),
                }).GroupBy(x => x.Key, x => x.Value).ToDictionary(x => x.Key, x => x.First()),
            ["northamerica"] => definition.SubdivisionExclusions
                .Concat(new[]
                {
                    new KeyValuePair<string, string[]>("US", ["US-HI"])
                }).GroupBy(x => x.Key, x => x.Value).ToDictionary(x => x.Key, x => x.First()),
            _ => definition.SubdivisionExclusions
        };

    private static Dictionary<string, string> ApplyContinentBorderFilters(this Dictionary<string, string> countryFilters, MapDefinition definition) =>
        definition.CountryCodes switch
        {
            ["europe"] => countryFilters
                .Concat(new[]
                {
                    new KeyValuePair<string, string>("RU", "Lng lt 61"),
                    new KeyValuePair<string, string>("TR", "Lng lt 29"),
                    new KeyValuePair<string, string>("KZ", "(Lat lt 51.17 and Lat gt 49.92 and Lng lt 51.38) or (Lat lt 47.16 and Lat gt 47.10 and Lng lt 51.92) or (Lat gt 51.22 and Lng lt 51.83)"),
                })
                .GroupBy(x => x.Key, x => x.Value).ToDictionary(x => x.Key, x => x.First()),
            ["asia"] => countryFilters
                .Concat(new[]
                {
                    new KeyValuePair<string, string>("KZ", "((SubdivisionCode eq 'KZ-27' or SubdivisionCode eq 'KZ-47') and ((Lat lt 51.17 and Lat gt 49.92 and Lng gt 51.38) or (Lat lt 47.16 and Lat gt 47.10 and Lng gt 51.92) or (Lat gt 51.22 and Lng gt 51.83))) or (SubdivisionCode neq 'KZ-27' and SubdivisionCode neq 'KZ-47')"),
                })
                .GroupBy(x => x.Key, x => x.Value).ToDictionary(x => x.Key, x => x.First()),
            _ => countryFilters
        };

    private static KeyValuePair<string, string[]> EuropeanTurkiye() =>
        new("TR", ["TR-22", "TR-39", "TR-59", "TR-34"]);

    private static KeyValuePair<string, string[]> EuropeanKazakhstan() =>
        new("KZ", ["KZ-23", "KZ-27"]);

    private static KeyValuePair<string, string[]> AsianRussia() =>
        new("RU", [ "RU-AL", "RU-BU", "RU-KK", "RU-SA", "RU-TY", "RU-ALT", "RU-KAM", "RU-KHA", "RU-KYA", "RU-PRI", "RU-ZAB", "RU-AMU", "RU-IRK", "RU-KEM", "RU-KGN", "RU-MAG", "RU-NVS", "RU-OMS", "RU-SAK", "RU-TOM", "RU-TYU", "RU-YEV", "RU-KHM", "RU-YAN" ]);

    private static KeyValuePair<string, string[]> AfricanSpain() =>
        new("ES", [ "ES-CN", "ES-CE", "ES-ML" ]);

    public static string[] MapCountryCodes(string[] countryCodes, DistributionStrategy distributionStrategy) =>
        countryCodes
            .SelectMany(c => c.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()))
            .SelectMany(s => ExpandCountryCode(s, distributionStrategy))
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
            ["*"] => Distribution(defaultDistribution, definition),
            ["europe"] => Distribution(Weights.Europe, definition),
            ["asia"] => Distribution(Weights.Asia, definition),
            ["africa"] => Distribution(Weights.Africa, definition),
            ["southamerica"] => Distribution(Weights.SouthAmerica, definition),
            ["northamerica"] => Distribution(Weights.NorthAmerica, definition),
            ["oceania"] => Distribution(Weights.Oceania, definition),
            _ when definition.CountryDistribution.Count == 0 => defaultDistribution.Where(x => countryCodes.Contains(x.Item1))
                .ToDictionary(x => x.Item1, x => x.Item2),
            _ => definition.CountryDistribution
        };
    }

    public static (string, int)[] DefaultDistribution(DistributionStrategy distributionStrategy) =>
        distributionStrategy.CountryDistributionFromMap?.ToLowerInvariant() switch
        {
            "aarw" => Weights.ArbitraryRuralWorld,
            "aaw" => Weights.World,
            "acw" => Weights.CommunityWorld,
            "abw" => Weights.BalancedWorld,
            "aiw" => Weights.ImprovedWorld,
            "proworld" => Weights.ProWorld,
            { Length: > 0 } => [],
            _ => Weights.CommunityWorld
        };

    private static Dictionary<string, int> Distribution((string, int)[] weights, MapDefinition mapDefinition) =>
        mapDefinition switch
        {
            { CountryDistribution.Count: > 0 } => mapDefinition.CountryDistribution,
            _ => weights.Where(x => x.Item2 > 0).ToDictionary(x => x.Item1, x => x.Item2)
        };
}