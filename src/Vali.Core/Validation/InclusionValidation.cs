namespace Vali.Core.Validation;

public static class InclusionValidation
{
    public static MapDefinition? ValidateInclusions(this MapDefinition definition)
    {
        foreach (var (countryCode, inclusions) in definition.SubdivisionInclusions)
        {
            if (Validate(countryCode, inclusions, nameof(MapDefinition.SubdivisionInclusions)))
            {
                return null;
            }
        }

        return definition;
    }

    public static MapDefinition? ValidateExclusions(this MapDefinition definition)
    {
        foreach (var (countryCode, exclusions) in definition.SubdivisionExclusions)
        {
            if (Validate(countryCode, exclusions, nameof(MapDefinition.SubdivisionExclusions)))
            {
                return null;
            }
        }

        return definition;
    }

    private static bool Validate(string countryCode, string[] inclusions, string propertyName)
    {
        var subdivisionWeights = SubdivisionWeights.CountryToSubdivision.TryGetValue(countryCode, out var divisions)
            ? divisions
            : new();
        foreach (var subdivisionCode in inclusions)
        {
            if (!subdivisionWeights.ContainsKey(subdivisionCode))
            {
                ConsoleLogger.Error($"""
                                     Subdivision code {subdivisionCode} is not valid in {propertyName}
                                     Must be one of {subdivisionWeights.Select(x => x.Key).Merge(", ")}
                                     """);
                return true;
            }
        }

        return false;
    }
}