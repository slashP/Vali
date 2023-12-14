namespace Vali.Core;

public static class MapDefinitionDefaults
{
    public static readonly string[] HardcodedPanoIdCountries = ["ML", "JE", "IM", "CW", "BT"];

    public static MapDefinition ApplyDefaults(this MapDefinition definition)
    {
        var countryCodes = MapCountryCodes(definition.CountryCodes, definition.DistributionStrategy);
        var countryDistribution = CountryDistribution(definition, countryCodes);
        return definition with
        {
            CountryCodes = countryCodes,
            PanoIdCountryCodes = MapCountryCodes(definition.PanoIdCountryCodes, definition.DistributionStrategy).Concat(HardcodedPanoIdCountries).Distinct().ToArray(),
            SubdivisionInclusions = Inclusions(definition),
            SubdivisionExclusions = Exclusions(definition),
            CountryDistribution = countryDistribution,
            DistributionStrategy = definition.DistributionStrategy with
            {
                TreatCountriesAsSingleSubdivision = MapCountryCodes(definition.DistributionStrategy.TreatCountriesAsSingleSubdivision, definition.DistributionStrategy)
            }
        };
    }

    private static Dictionary<string, string[]> Inclusions(MapDefinition definition) =>
        definition.CountryCodes switch
        {
            ["europe"] => definition.SubdivisionInclusions
                .Concat(new[]
                {
                    EuropeanTurkiye(),
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
                    EuropeanTurkiye()
                }).GroupBy(x => x.Key, x => x.Value).ToDictionary(x => x.Key, x => x.First()),
            ["northamerica"] => definition.SubdivisionExclusions
                .Concat(new[]
                {
                    new KeyValuePair<string, string[]>("US", ["US-HI"])
                }).GroupBy(x => x.Key, x => x.Value).ToDictionary(x => x.Key, x => x.First()),
            _ => definition.SubdivisionExclusions
        };

    private static KeyValuePair<string, string[]> EuropeanTurkiye() =>
        new("TR", [ "TR-22", "TR-39", "TR-59", "TR-34" ]);

    private static KeyValuePair<string, string[]> AsianRussia() =>
        new("RU", [ "RU-AL", "RU-BU", "RU-KK", "RU-SA", "RU-TY", "RU-ALT", "RU-KAM", "RU-KHA", "RU-KYA", "RU-PRI", "RU-ZAB", "RU-AMU", "RU-IRK", "RU-KEM", "RU-KGN", "RU-MAG", "RU-NVS", "RU-OMS", "RU-SAK", "RU-TOM", "RU-TYU", "RU-YEV", "RU-KHM", "RU-YAN" ]);

    private static KeyValuePair<string, string[]> AfricanSpain() =>
        new("ES", [ "ES-CN", "ES-CE", "ES-ML" ]);

    private static string[] MapCountryCodes(string[] countryCodes, DistributionStrategy distributionStrategy) =>
        countryCodes
            .SelectMany(c => c.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()))
            .SelectMany(s => ExpandCountryCode(s, distributionStrategy))
            .ToArray();

    public static string[] ExpandCountryCode(string countryCode, DistributionStrategy distributionStrategy)
    {
        var defaultDistribution = DefaultDistribution(distributionStrategy);
        return countryCode switch
        {
            "*" => CodesFromWeights(defaultDistribution),
            "europe" => CodesFromWeights(Weights.Europe),
            "asia" => CodesFromWeights(Weights.Asia),
            "africa" => CodesFromWeights(Weights.Africa),
            "southamerica" => CodesFromWeights(Weights.SouthAmerica),
            "northamerica" => CodesFromWeights(Weights.NorthAmerica),
            "oceania" => CodesFromWeights(Weights.Oceania),
            _ => [countryCode]
        };
    }

    private static string[] CodesFromWeights((string, int)[] weights) =>
        weights.Where(x => x.Item2 > 0).Select(x => x.Item1).ToArray();

    private static Dictionary<string, int> CountryDistribution(MapDefinition definition, string[] countryCodes)
    {
        var defaultDistribution = DefaultDistribution(definition.DistributionStrategy);
        return definition.CountryCodes switch
        {
            ["*"] => Distribution(defaultDistribution),
            ["europe"] => Distribution(Weights.Europe),
            ["asia"] => Distribution(Weights.Asia),
            ["africa"] => Distribution(Weights.Africa),
            ["southamerica"] => Distribution(Weights.SouthAmerica),
            ["northamerica"] => Distribution(Weights.NorthAmerica),
            ["oceania"] => Distribution(Weights.Oceania),
            _ when definition.CountryDistribution.Count == 0 => defaultDistribution.Where(x => countryCodes.Contains(x.Item1))
                .ToDictionary(x => x.Item1, x => x.Item2),
            _ => definition.CountryDistribution
        };
    }

    private static (string, int)[] DefaultDistribution(DistributionStrategy distributionStrategy) =>
        distributionStrategy.DefaultDistribution switch
        {
            "aarw" => Weights.ArbitraryRuralWorld,
            "aaw" => Weights.World,
            _ => Weights.CommunityWorld
        };

    private static Dictionary<string, int> Distribution((string, int)[] weights) => weights.Where(x => x.Item2 > 0).ToDictionary(x => x.Item1, x => x.Item2);
}