using Vali.Core.Data;

namespace Vali.Core.Validation;

public static class DistributionValidation
{
    public static MapDefinition? ValidateDistribution(this MapDefinition definition)
    {
        var countryCodes = definition.CountryCodes;
        var countryWeights = LocationLakeMapGenerator.CountryWeights(definition);
        var inCountryCodesButNotInWeights = countryCodes.Except(countryWeights.Select(x => x.Key)).ToArray();
        if (inCountryCodesButNotInWeights.Any())
        {
            ConsoleLogger.Error($"Countries in '{nameof(MapDefinition.CountryCodes)}' do not have country distribution weight. When specifying your own 'countryDistribution' all countries must be there. Add the following:");
            ConsoleLogger.Info($"{inCountryCodesButNotInWeights.Merge(", ")}");
            return null;
        }

        var inDistributionButNotInCountryCodes = countryWeights.ExceptBy(countryCodes, pair => pair.Key).ToArray();
        if (inDistributionButNotInCountryCodes.Any())
        {
            ConsoleLogger.Error($"Countries in '{nameof(MapDefinition.CountryDistribution)}' are not in '{nameof(MapDefinition.CountryCodes)}':");
            ConsoleLogger.Info($"{inCountryCodesButNotInWeights.Merge(", ")}");
            return null;
        }

        var subdivisionDistribution = definition.SubdivisionDistribution;
        foreach (var sub in subdivisionDistribution)
        {
            if (!countryCodes.Contains(sub.Key))
            {
                ConsoleLogger.Error($"{sub.Key} specified in '{nameof(MapDefinition.SubdivisionDistribution)}' but the country is not included in '{nameof(MapDefinition.CountryCodes)}'");
                return null;
            }

            var subdivisionWeights = SubdivisionWeights.CountryToSubdivision.TryGetValue(sub.Key, out var divisions)
                ? divisions
                : new();

            foreach (var subdivisionWeight in sub.Value)
            {
                if (!subdivisionWeights.ContainsKey(subdivisionWeight.Key))
                {
                    ConsoleLogger.Error($"""
                                         Subdivision code {subdivisionWeight.Key} is not valid in {nameof(MapDefinition.SubdivisionDistribution)}
                                         Must be one of {subdivisionWeights.Select(x => x.Key).Merge(", ")}
                                         """);
                    return null;
                }
            }
        }

        var countryCodesMissingSubdivisionDistribution = countryCodes.Where(code =>
            !SubdivisionWeights.CountryToSubdivision.ContainsKey(code) &&
            !definition.DistributionStrategy.TreatCountriesAsSingleSubdivision.Contains(code)).ToArray();
        if (countryCodesMissingSubdivisionDistribution.Any())
        {
            ConsoleLogger.Error($"""
                                 Countries missing {nameof(MapDefinition.SubdivisionDistribution)}
                                 {countryCodesMissingSubdivisionDistribution.Select(x => $"{CountryCodes.Name(x)} / {x}").Merge(Environment.NewLine)}
                                 """);
            return null;
        }

        foreach (var countryCode in countryCodes)
        {
            var subdivisions = SubdivisionWeights.CountryToSubdivision[countryCode];
            var countryFolder = DataDownloadService.CountryFolder(countryCode);
            var files = Directory.Exists(countryFolder) ? Directory.GetFiles(countryFolder, "*.bin") : [];
            var missingFiles = subdivisions.Where(s => !files.Any(f => f.EndsWith($"{s.Key}.bin"))).ToArray();
            if (missingFiles.Any())
            {
                ConsoleLogger.Error($"""
                                     Data for subdivisions missing.
                                     {missingFiles.Select(x => $"{SubdivisionWeights.SubdivisionName(countryCode, x.Key)} / {x.Key}").Merge(Environment.NewLine)}

                                     Download missing data for {CountryCodes.Name(countryCode)} with 'vali download'"
                                     """);
                return null;
            }

            var missingDefinitions = files.Where(f => !subdivisions.Any(s => f.EndsWith($"{s.Key}.bin"))).ToArray();
            if (missingDefinitions.Any())
            {
                ConsoleLogger.Error($"""
                                     Files do not have entries in subdivision weights.
                                     {missingDefinitions.Merge(Environment.NewLine)}
                                     """);
                return null;
            }
        }

        return definition;
    }
}