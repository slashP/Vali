using System.Text.Json.Serialization;
using Vali.Core.Hash;

namespace Vali.Core;

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
    public Dictionary<string, ProximityFilter> CountryProximityFilters { get; init; } = [];
    public Dictionary<string, PolygonFilter[]> CountryPolygonFilters { get; init; } = [];
    public Dictionary<string, Dictionary<string, string>> SubdivisionLocationFilters { get; init; } = [];
    public Dictionary<string, Dictionary<string, ProximityFilter>> SubdivisionProximityFilters { get; init; } = [];
    public Dictionary<string, Dictionary<string, PolygonFilter[]>> SubdivisionPolygonFilters { get; init; } = [];
    public LocationPreferenceFilter[] GlobalLocationPreferenceFilters { get; init; } = [];
    public Dictionary<string, LocationPreferenceFilter[]> CountryLocationPreferenceFilters { get; init; } = [];
    public Dictionary<string, Dictionary<string, LocationPreferenceFilter[]>> SubdivisionLocationPreferenceFilters { get; init; } = [];
    public LocationOutput Output { get; set; } = new();
    public ProximityFilter ProximityFilter { get; set; } = new();
    public NeighborFilter[] NeighborFilters { get; init; } = [];
    public PolygonFilter[] PolygonFilters { get; init; } = [];
    public Dictionary<string, string> NamedExpressions { get; set; } = new();
    public string[] UsedLocationsPaths { get; set; } = [];
    public bool EnableDefaultLocationFilters { get; set; }
}

public record ProximityFilter
{
    public string LocationsPath { get; set; } = "";
    public int Radius { get; set; }
}

public record NeighborFilter
{
    public bool CheckEachCardinalDirectionSeparately { get; set; }
    public int Radius { get; set; }
    public string Expression { get; set; } = "";
    public int? Limit { get; set; }
    public string Bound { get; set; } = "";
}

public record PolygonFilter
{
    public string PolygonsPath { get; set; } = "";
    public bool InsidePolygon { get; set; } = true;
    [JsonIgnore]
    public HashPrecision? precision { get; set; }
}

public record LiveGenerateMapDefinition
{
    public Dictionary<string, int> Countries { get; init; } = [];
    public LocationDistribution? Distribution { get; set; }
    public string[] LocationTags { get; init; } = [];
    public string? FromDate { get; set; }
    public string? ToDate { get; set; }
    public string? HeadingMode { get; set; }
    public int? HeadingDelta { get; set; }
    public string? PitchMode { get; set; }
    public int? Pitch { get; set; }
    public int? RandomPitchMin { get; set; }
    public int? RandomPitchMax { get; set; }
    public string? ZoomMode { get; set; }
    public int? Zoom { get; set; }
    public double? RandomZoomMin { get; set; }
    public double? RandomZoomMax { get; set; }
    public int BoxPrecision { get; set; } = 4;
    public int ParallelRequests { get; set; } = 100;
    public int Radius { get; set; } = 100;
    public string LocationFilter { get; set; } = "";
    public string? PanoSelectionStrategy { get; set; }
    public DateOnly? PanoVerificationStart { get; set; }
    public DateOnly? PanoVerificationEnd { get; set; }
    public string[] GeoJsonFiles { get; set; } = [];
    public bool RejectLocationsWithoutDescription { get; set; } = true;
    public string? AcceptedCoverage { get; set; }
    public int BatchSize { get; set; } = 10_000;
    public bool CheckLinkedPanoramas { get; set; }

    public record LocationDistribution
    {
        public int MinMinDistance { get; init; }
        public int OvershootFactor { get; set; } = 1;
    }
}

public record DistributionStrategy
{
    public string? Key { get; init; }
    public int LocationCountGoal { get; init; }
    public int MinMinDistance { get; init; }
    public int FixedMinDistance { get; init; }
    public string[] TreatCountriesAsSingleSubdivision { get; init; } = [];
    public string? CountryDistributionFromMap { get; init; }
}

public record LocationPreferenceFilter
{
    public required string Expression { get; init; }
    public int? Percentage { get; init; }
    public bool Fill { get; init; }
    public string? LocationTag { get; init; }
    public int? MinMinDistance { get; init; }
    public ProximityFilter ProximityFilter { get; set; } = new();
    public PolygonFilter[] PolygonFilters { get; set; } = [];
    public NeighborFilter[] NeighborFilters { get; init; } = [];
}

public record LocationOutput
{
    public string[] LocationTags { get; init; } = [];
    public string[] PanoIdCountryCodes { get; init; } = [];
    public string? GlobalHeadingExpression { get; init; }
    public Dictionary<string, string> CountryHeadingExpressions { get; init; } = [];
    public double? GlobalZoom { get; set; }
    public double? GlobalPitch { get; set; }
    public string? PanoVerificationStrategy { get; set; }
    public Dictionary<string, string?> CountryPanoVerificationPanning { get; set; } = new();
    public string? PanoVerificationExpression { get; set; }
    public DateOnly? PanoVerificationStart { get; set; }
    public DateOnly? PanoVerificationEnd { get; set; }
}
