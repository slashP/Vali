using Vali.Core.Data;

namespace Vali.Core.Validation;

public static class CountryDistribution
{
    public static MapDefinition? ValidateFilesExist(this MapDefinition definition)
    {
        var countryCodes = definition.CountryCodes
            .Concat(definition.CountryDistribution?.Select(x => x.Key) ?? Array.Empty<string>())
            .Concat(definition.CountryLocationFilters?.Select(x => x.Key) ?? Array.Empty<string>())
            .Concat(definition.CountryLocationPreferenceFilters?.Select(x => x.Key) ?? Array.Empty<string>())
            .ToArray();
        var codes = MapDefinitionDefaults.MapCountryCodes(countryCodes, new())
                .Intersect(CountryCodes.Countries.Keys)
                .Distinct();
        foreach (var code in codes)
        {
            if (!Directory.Exists(DataDownloadService.CountryFolder(code)))
            {
                ConsoleLogger.Error($"Missing data for {code} / {CountryCodes.Name(code)}. Use 'vali download' to download data.");
                return null;
            }
        }

        return definition;
    }

    public static MapDefinition? ValidateCountryCodes(this MapDefinition definition)
    {
        var countryCodes = definition.CountryCodes
            .Concat(definition.CountryDistribution?.Select(x => x.Key) ?? Array.Empty<string>())
            .Concat(definition.CountryLocationFilters?.Select(x => x.Key) ?? Array.Empty<string>())
            .Concat(definition.CountryLocationPreferenceFilters?.Select(x => x.Key) ?? Array.Empty<string>())
            .ToArray();
        var codes = MapDefinitionDefaults.MapCountryCodes(countryCodes, new());
        var invalidCountryCodes = codes.Where(c => CountryCodes.Countries.ContainsKey(c) == false).ToArray();
        if (invalidCountryCodes.Any())
        {
            ConsoleLogger.Error($"Invalid country code(s). Split multiple with ,");
            ConsoleLogger.Info($"{invalidCountryCodes.Merge(", ")}");
            return null;
        }

        return definition;
    }
}