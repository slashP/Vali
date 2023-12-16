﻿namespace Vali.Core;

public record MapDefinition
{
    public string[] CountryCodes { get; init; } = [];
    public Dictionary<string, string[]> SubdivisionInclusions { get; init; } = [];
    public Dictionary<string, string[]> SubdivisionExclusions { get; init; } = [];
    public Dictionary<string, int> CountryDistribution { get; init; } = [];
    public Dictionary<string, Dictionary<string, int>> SubdivisionDistribution { get; init; } = [];
    public DistributionStrategy DistributionStrategy { get; init; } = new();
    public string? GlobalLocationFilter { get; init; }
    public Dictionary<string, string> CountryLocationFilters { get; init; } = [];
    public Dictionary<string, Dictionary<string, string>> SubdivisionLocationFilters { get; init; } = [];
    public LocationPreferenceFilter[] GlobalLocationPreferenceFilters { get; init; } = [];
    public Dictionary<string, LocationPreferenceFilter[]> CountryLocationPreferenceFilters { get; init; } = [];
    public Dictionary<string, Dictionary<string, LocationPreferenceFilter[]>> SubdivisionLocationPreferenceFilters { get; init; } = [];
    public LocationOutput Output { get; set; } = new();
}

public record DistributionStrategy
{
    public string? Key { get; init; }
    public int LocationCountGoal { get; init; }
    public int MinMinDistance { get; init; }
    public int FixedMinDistance { get; init; }
    public string[] TreatCountriesAsSingleSubdivision { get; init; } = [];
    public string? DefaultDistribution { get; init; }
}

public record LocationPreferenceFilter
{
    public required string Expression { get; init; }
    public int? Percentage { get; init; }
    public bool Fill { get; init; }
    public string? LocationTag { get; init; }
    public int? MinMinDistance { get; init; }
}

public record LocationOutput
{
    public string[] LocationTags { get; init; } = [];
    public string[] PanoIdCountryCodes { get; init; } = [];
    public string? GlobalHeadingExpression { get; init; }
    public Dictionary<string, string> CountryHeadingExpressions { get; init; } = [];
    public double? GlobalZoom { get; set; }
    public double? GlobalPitch { get; set; }
}
