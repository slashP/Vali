using NJsonSchema;
using NJsonSchema.Generation;
using SchemaGenerator;
using Vali.Core;
using Vali.Core.Validation;
using static Vali.Core.Google.GoogleApi;

Console.WriteLine("Generating JSON schemas for Vali...");

var settings = new SystemTextJsonSchemaGeneratorSettings
{
    DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull,
    GenerateEnumMappingDescription = true,
    SchemaType = SchemaType.JsonSchema,
    AllowReferencesWithProperties = true,
    SerializerOptions = new System.Text.Json.JsonSerializerOptions
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    }
};

var excludedProperties = new[]
{
    nameof(MapDefinition.GlobalExternalDataFiles).Prop(),
    nameof(MapDefinition.CountryExternalDataFiles).Prop(),
    nameof(MapDefinition.SubdivisionExternalDataFiles).Prop(),
};

var mapDefinitionSchema = JsonSchema.FromType<MapDefinition>(settings);
foreach (var excludedProperty in excludedProperties)
{
    mapDefinitionSchema.Properties.Remove(excludedProperty);
}

mapDefinitionSchema.Title = "Vali Map Definition";
mapDefinitionSchema.Description = "Configuration for generating maps";
mapDefinitionSchema.AllowAdditionalProperties = true;
AddMapDefinitionConstraints(mapDefinitionSchema);

var liveGenerateSchema = JsonSchema.FromType<LiveGenerateMapDefinition>(settings);
liveGenerateSchema.Title = "Vali Live Generate Map Definition";
liveGenerateSchema.Description = "Configuration for live-generating maps using Google API";
liveGenerateSchema.AllowAdditionalProperties = true;
AddLiveGenerateConstraints(liveGenerateSchema);

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var outputDir = Path.Combine(repoRoot, "src", "Vali", "Schemas");
Directory.CreateDirectory(outputDir);

var mapDefinitionPath = Path.Combine(outputDir, "vali.schema.json");
var liveGeneratePath = Path.Combine(outputDir, "vali-live-generate.schema.json");

const string draft04 = "http://json-schema.org/draft-04/schema#";
const string draft07 = "http://json-schema.org/draft-07/schema#";
await File.WriteAllTextAsync(mapDefinitionPath, mapDefinitionSchema.ToJson().Replace(draft04, draft07));
await File.WriteAllTextAsync(liveGeneratePath, liveGenerateSchema.ToJson().Replace(draft04, draft07));

Console.WriteLine($"✓ Generated: {Path.GetFullPath(mapDefinitionPath)}");
Console.WriteLine($"✓ Generated: {Path.GetFullPath(liveGeneratePath)}");
Console.WriteLine("Schemas generated successfully!");

// --- Helper methods ---

static string ExpressionSyntaxHelp()
{
    var operators = string.Join(", ", LocationLakeFilterer.ValidOperators());
    var properties = string.Join(", ", LocationLakeFilterer.ValidProperties().Except(["Lat", "Lng"]).OrderBy(p => p));
    return $"Operators: {operators}. Properties: {properties}. " +
           "Use $$name to reference named expressions. String literals use single quotes.";
}

static string[] GetValidLocationTags() =>
    LocationLakeFilterer.ValidProperties().Except(["Lat", "Lng"]).Concat(["Season", "YearMonth"]).OrderBy(t => t).ToArray();

static string[] GetBucketableLocationTags() =>
    typeof(OsmData).GetProperties()
        .Concat(typeof(GoogleData).GetProperties())
        .Where(prop => (Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType) == typeof(int))
        .Select(t => t.Name)
        .Distinct()
        .OrderBy(t => t)
        .ToArray();

static void AddExamples(JsonSchema schema, params object[] examples)
{
    schema.ExtensionData ??= new Dictionary<string, object?>();
    schema.ExtensionData["examples"] = examples;
}

static void SetPropertyNamesPattern(JsonSchema schema, string pattern)
{
    schema.ExtensionData ??= new Dictionary<string, object?>();
    schema.ExtensionData["propertyNames"] = new Dictionary<string, object> { ["pattern"] = pattern };
}

static void SetComment(JsonSchema schema, string comment)
{
    schema.ExtensionData ??= new Dictionary<string, object?>();
    schema.ExtensionData["$comment"] = comment;
}

static void AddLocationTagsConstraints(JsonSchema locationTagsProperty)
{
    locationTagsProperty.Description = "Tags to add to generated locations. Numeric tags can be extended with -<number> for bucketing (e.g., Buildings25-5, ClosestCoast-100).";
    locationTagsProperty.UniqueItems = true;
    if (locationTagsProperty.Item != null)
    {
        var validTags = GetValidLocationTags();
        var bucketableTags = GetBucketableLocationTags();

        var enumSchema = new JsonSchema { Type = JsonObjectType.String };
        foreach (var tag in validTags)
        {
            enumSchema.Enumeration.Add(tag);
        }

        var patternSchema = new JsonSchema
        {
            Type = JsonObjectType.String,
            Pattern = "^(" + string.Join("|", bucketableTags) + ")-\\d+$"
        };

        locationTagsProperty.Item.OneOf.Clear();
        locationTagsProperty.Item.OneOf.Add(enumSchema);
        locationTagsProperty.Item.OneOf.Add(patternSchema);
    }
}

// --- Map Definition constraints ---

static void AddMapDefinitionConstraints(JsonSchema schema)
{
    // Root required properties
    schema.RequiredProperties.Add(nameof(MapDefinition.CountryCodes).Prop());

    // --- countryCodes ---
    var countryCodes = schema.Properties[nameof(MapDefinition.CountryCodes).Prop()];
    countryCodes.Description = "ISO 3166-1 alpha-2 country codes, or special keywords like 'europe', 'asia', 'africa', 'world', '*', etc.";
    countryCodes.MinItems = 1;
    countryCodes.UniqueItems = true;
    AddExamples(countryCodes, new[] { "NO", "SE", "DK" }, new[] { "europe" }, new[] { "*" });

    // --- subdivisionInclusions / subdivisionExclusions ---
    var subdivisionInclusions = schema.Properties[nameof(MapDefinition.SubdivisionInclusions).Prop()];
    subdivisionInclusions.Description = "Include only these subdivisions per country (e.g., {\"US\": [\"US-CA\", \"US-NY\"]})";
    SetPropertyNamesPattern(subdivisionInclusions, "^[A-Z]{2}$");

    var subdivisionExclusions = schema.Properties[nameof(MapDefinition.SubdivisionExclusions).Prop()];
    subdivisionExclusions.Description = "Exclude these subdivisions per country";
    SetPropertyNamesPattern(subdivisionExclusions, "^[A-Z]{2}$");

    // --- countryDistribution / subdivisionDistribution ---
    var countryDistribution = schema.Properties[nameof(MapDefinition.CountryDistribution).Prop()];
    countryDistribution.Description = "Custom distribution weights per country (integers representing relative weights)";
    SetPropertyNamesPattern(countryDistribution, "^[A-Z]{2}$");

    var subdivisionDistribution = schema.Properties[nameof(MapDefinition.SubdivisionDistribution).Prop()];
    subdivisionDistribution.Description = "Custom distribution weights per subdivision (outer key = country code, inner key = subdivision code)";
    SetPropertyNamesPattern(subdivisionDistribution, "^[A-Z]{2}$");

    // --- DistributionStrategy ---
    var distStrategy = schema.Properties[nameof(MapDefinition.DistributionStrategy).Prop()];
    distStrategy.Description = "Strategy for distributing locations across regions";

    var distStrategyDef = schema.Definitions[nameof(DistributionStrategy)];
    distStrategyDef.AllowAdditionalProperties = true;

    var key = distStrategyDef.Properties[nameof(DistributionStrategy.Key).Prop()];
    key.Description = "Distribution algorithm to use";
    key.Enumeration.Clear();
    foreach (var strategy in DistributionStrategies.ValidStrategyKeys)
    {
        key.Enumeration.Add(strategy);
    }

    var locationCountGoal = distStrategyDef.Properties[nameof(DistributionStrategy.LocationCountGoal).Prop()];
    locationCountGoal.Description = "Target number of locations. Required for FixedCountByMaxMinDistance and FixedCountByCoverageDensity strategies.";
    locationCountGoal.Minimum = 1;

    var minMinDistance = distStrategyDef.Properties[nameof(DistributionStrategy.MinMinDistance).Prop()];
    minMinDistance.Description = "Minimum distance in meters between locations";
    minMinDistance.Minimum = 0;

    var fixedMinDistance = distStrategyDef.Properties[nameof(DistributionStrategy.FixedMinDistance).Prop()];
    fixedMinDistance.Description = "Fixed minimum distance in meters between locations. Required for MaxCountByFixedMinDistance strategy.";
    fixedMinDistance.Minimum = 1;

    var treatAsSingle = distStrategyDef.Properties[nameof(DistributionStrategy.TreatCountriesAsSingleSubdivision).Prop()];
    treatAsSingle.Description = "Country codes to treat as having a single subdivision (skip internal subdivision-level distribution)";

    var countryDistFromMap = distStrategyDef.Properties[nameof(DistributionStrategy.CountryDistributionFromMap).Prop()];
    countryDistFromMap.Description = "Use country distribution weights from a well-known map";
    countryDistFromMap.Enumeration.Clear();
    foreach (var dist in MapDefinitionDefaults.ValidMapShortNames)
    {
        countryDistFromMap.Enumeration.Add(dist);
    }

    var tuningFactor = distStrategyDef.Properties[nameof(DistributionStrategy.CoverageDensityTuningFactor).Prop()];
    tuningFactor.Description = "Adjust if lower coverage areas should be upweighted/have lower distance between locations than higher coverage areas. 1 is absurd, 0.01 is almost no adjustment compared to linearly weighted according to number of locations available.";
    tuningFactor.Minimum = 0.01m;
    tuningFactor.Maximum = 1;
    tuningFactor.Default = 0.6;

    var clusterSize = distStrategyDef.Properties[nameof(DistributionStrategy.CoverageDensityClusterSize).Prop()];
    clusterSize.Description = "Cluster size for coverage density calculation. 1 ~ 5000x5000 km, 2 ~ 1250x625 km, 3 ~ 156x156 km, 4 ~ 39x20 km, 5 ~ 5x5 km, 6 ~ 1x1 km.";
    clusterSize.Minimum = 1;
    clusterSize.Maximum = 6;
    clusterSize.Default = 3;

    // if/then conditionals for strategy-specific required properties
    distStrategyDef.ExtensionData ??= new Dictionary<string, object?>();
    distStrategyDef.ExtensionData["allOf"] = new object[]
    {
        StrategyConditional("FixedCountByMaxMinDistance", nameof(DistributionStrategy.LocationCountGoal).Prop()),
        StrategyConditional("MaxCountByFixedMinDistance", nameof(DistributionStrategy.FixedMinDistance).Prop()),
        StrategyConditional("FixedCountByCoverageDensity", nameof(DistributionStrategy.LocationCountGoal).Prop()),
    };

    // --- globalLocationFilter ---
    var globalFilter = schema.Properties[nameof(MapDefinition.GlobalLocationFilter).Prop()];
    globalFilter.Description = $"Expression to filter locations globally. {ExpressionSyntaxHelp()}";
    AddExamples(globalFilter,
        "ClosestCoast lt 100 and Buildings200 gte 4",
        "Year gte 2020",
        "Surface eq 'asphalt' or Surface eq 'paved'",
        "HighwayType in ('Primary', 'Secondary', 'Tertiary')");

    // --- filter dictionaries ---
    var countryLocationFilters = schema.Properties[nameof(MapDefinition.CountryLocationFilters).Prop()];
    countryLocationFilters.Description = "Per-country location filter expressions (key = country code, value = filter expression)";
    SetPropertyNamesPattern(countryLocationFilters, "^[A-Z]{2}$");

    var countryProximityFilters = schema.Properties[nameof(MapDefinition.CountryProximityFilters).Prop()];
    countryProximityFilters.Description = "Per-country proximity filters (include locations near reference points)";
    SetPropertyNamesPattern(countryProximityFilters, "^[A-Z]{2}$");

    var countryGeometryFilters = schema.Properties[nameof(MapDefinition.CountryGeometryFilters).Prop()];
    countryGeometryFilters.Description = "Per-country geometry filters (GeoJSON polygon inclusion/exclusion)";
    SetPropertyNamesPattern(countryGeometryFilters, "^[A-Z]{2}$");

    var subdivisionLocationFilters = schema.Properties[nameof(MapDefinition.SubdivisionLocationFilters).Prop()];
    subdivisionLocationFilters.Description = "Per-subdivision location filter expressions (outer key = country, inner key = subdivision)";
    SetPropertyNamesPattern(subdivisionLocationFilters, "^[A-Z]{2}$");

    var subdivisionProximityFilters = schema.Properties[nameof(MapDefinition.SubdivisionProximityFilters).Prop()];
    subdivisionProximityFilters.Description = "Per-subdivision proximity filters";
    SetPropertyNamesPattern(subdivisionProximityFilters, "^[A-Z]{2}$");

    var subdivisionGeometryFilters = schema.Properties[nameof(MapDefinition.SubdivisionGeometryFilters).Prop()];
    subdivisionGeometryFilters.Description = "Per-subdivision geometry filters";
    SetPropertyNamesPattern(subdivisionGeometryFilters, "^[A-Z]{2}$");

    // --- preference filters ---
    var globalPrefFilters = schema.Properties[nameof(MapDefinition.GlobalLocationPreferenceFilters).Prop()];
    globalPrefFilters.Description = "Percentage-based location allocation filters applied globally. Percentages must sum to 100 or less.";

    var countryPrefFilters = schema.Properties[nameof(MapDefinition.CountryLocationPreferenceFilters).Prop()];
    countryPrefFilters.Description = "Per-country percentage-based location allocation filters";
    SetPropertyNamesPattern(countryPrefFilters, "^[A-Z]{2}$");

    var subdivisionPrefFilters = schema.Properties[nameof(MapDefinition.SubdivisionLocationPreferenceFilters).Prop()];
    subdivisionPrefFilters.Description = "Per-subdivision percentage-based location allocation filters";
    SetPropertyNamesPattern(subdivisionPrefFilters, "^[A-Z]{2}$");

    // --- output ---
    var output = schema.Properties[nameof(MapDefinition.Output).Prop()];
    output.Description = "Output configuration for generated locations";

    var outputDef = schema.Definitions[nameof(LocationOutput)];
    outputDef.AllowAdditionalProperties = true;

    var outputLocationTags = outputDef.Properties[nameof(LocationOutput.LocationTags).Prop()];
    AddLocationTagsConstraints(outputLocationTags);

    var panoIdCountryCodes = outputDef.Properties[nameof(LocationOutput.PanoIdCountryCodes).Prop()];
    panoIdCountryCodes.Description = "Country codes for which to look up specific panorama IDs during verification";
    panoIdCountryCodes.UniqueItems = true;

    var headingExpr = outputDef.Properties[nameof(LocationOutput.GlobalHeadingExpression).Prop()];
    headingExpr.Description = "Expression to calculate heading for all locations";
    AddExamples(headingExpr, "DrivingDirectionAngle", "DrivingDirectionAngle + 90", "DrivingDirectionAngle + 180");

    var countryHeadingExpressions = outputDef.Properties[nameof(LocationOutput.CountryHeadingExpressions).Prop()];
    countryHeadingExpressions.Description = "Per-country heading expressions (key = country code, value = heading expression)";
    SetPropertyNamesPattern(countryHeadingExpressions, "^[A-Z]{2}$");

    var zoom = outputDef.Properties[nameof(LocationOutput.GlobalZoom).Prop()];
    zoom.Description = "Zoom level for all locations (0-3.6)";
    zoom.Minimum = 0;
    zoom.Maximum = 3.6m;

    var pitch = outputDef.Properties[nameof(LocationOutput.GlobalPitch).Prop()];
    pitch.Description = "Pitch angle for all locations (-90 to 90)";
    pitch.Minimum = -90;
    pitch.Maximum = 90;

    var panoStrategy = outputDef.Properties[nameof(LocationOutput.PanoVerificationStrategy).Prop()];
    panoStrategy.Description = "Strategy for selecting panorama IDs";
    panoStrategy.Enumeration.Clear();
    foreach (var strat in Enum.GetValues<PanoStrategy>().Where(x => x != PanoStrategy.None))
    {
        panoStrategy.Enumeration.Add(strat.ToString());
    }

    var countryPanning = outputDef.Properties[nameof(LocationOutput.CountryPanoVerificationPanning).Prop()];
    countryPanning.Description = "Per-country panning configuration for panorama verification (key = country code)";
    SetPropertyNamesPattern(countryPanning, "^[A-Z]{2}$");

    var panoVerifExpr = outputDef.Properties[nameof(LocationOutput.PanoVerificationExpression).Prop()];
    panoVerifExpr.Description = "Filter expression applied during panorama verification";

    var panoVerifParallelism = outputDef.Properties[nameof(LocationOutput.PanoVerificationParallelism).Prop()];
    panoVerifParallelism.Description = "Number of parallel panorama verification requests";
    panoVerifParallelism.Minimum = 1;
    panoVerifParallelism.Default = 100;

    var panoVerifStart = outputDef.Properties[nameof(LocationOutput.PanoVerificationStart).Prop()];
    panoVerifStart.Description = "Start date for panorama verification (format: YYYY-MM-DD)";

    var panoVerifEnd = outputDef.Properties[nameof(LocationOutput.PanoVerificationEnd).Prop()];
    panoVerifEnd.Description = "End date for panorama verification (format: YYYY-MM-DD)";

    // --- proximityFilter (global/legacy) ---
    var proximityFilter = schema.Properties[nameof(MapDefinition.ProximityFilter).Prop()];
    proximityFilter.Description = "Global proximity filter (include locations near reference points)";
    SetComment(proximityFilter, "For per-country/subdivision proximity, use countryProximityFilters/subdivisionProximityFilters instead.");

    // --- ProximityFilter definition ---
    var proximityFilterDef = schema.Definitions[nameof(ProximityFilter)];

    var pfLocPath = proximityFilterDef.Properties[nameof(ProximityFilter.LocationsPath).Prop()];
    pfLocPath.Description = "Path to JSON file containing reference locations";

    var pfRadius = proximityFilterDef.Properties[nameof(ProximityFilter.Radius).Prop()];
    pfRadius.Description = "Include locations within this radius in meters of reference points";
    pfRadius.Minimum = 1;
    pfRadius.Maximum = 30_000;

    // --- neighborFilters ---
    var neighborFilters = schema.Properties[nameof(MapDefinition.NeighborFilters).Prop()];
    neighborFilters.Description = "Filter locations based on properties of nearby neighbors";

    var neighborFilterDef = schema.Definitions[nameof(NeighborFilter)];
    neighborFilterDef.AllowAdditionalProperties = true;
    neighborFilterDef.RequiredProperties.Add(nameof(NeighborFilter.Expression).Prop());
    neighborFilterDef.RequiredProperties.Add(nameof(NeighborFilter.Bound).Prop());
    neighborFilterDef.RequiredProperties.Add(nameof(NeighborFilter.Radius).Prop());

    var nfRadius = neighborFilterDef.Properties[nameof(NeighborFilter.Radius).Prop()];
    nfRadius.Description = "Radius in meters to check for neighbors";
    nfRadius.Minimum = 1;
    nfRadius.Maximum = 5000;

    var nfExpression = neighborFilterDef.Properties[nameof(NeighborFilter.Expression).Prop()];
    nfExpression.Description = "Filter expression for neighbor locations. Use 'current:Property' to reference the current location being evaluated.";
    AddExamples(nfExpression, "Buildings200 gte 1", "current:Surface eq Surface");

    var bound = neighborFilterDef.Properties[nameof(NeighborFilter.Bound).Prop()];
    bound.Description = "How many neighbors must match the expression";
    bound.Enumeration.Clear();
    foreach (var b in FilterValidation.ValidNeighborFilterBounds)
    {
        bound.Enumeration.Add(b);
    }

    var limit = neighborFilterDef.Properties[nameof(NeighborFilter.Limit).Prop()];
    limit.Description = "Number or percentage of neighbors required (depends on bound). Not used with 'all'/'none'/'some'.";

    var checkCardinal = neighborFilterDef.Properties[nameof(NeighborFilter.CheckEachCardinalDirectionSeparately).Prop()];
    checkCardinal.Description = "Check north, south, east, west directions separately";

    // --- geometryFilters ---
    var geometryFilters = schema.Properties[nameof(MapDefinition.GeometryFilters).Prop()];
    geometryFilters.Description = "Filter locations within GeoJSON-defined areas";

    var geometryFilterDef = schema.Definitions[nameof(GeometryFilter)];
    geometryFilterDef.AllowAdditionalProperties = true;
    geometryFilterDef.RequiredProperties.Add(nameof(GeometryFilter.FilePath).Prop());

    var filePath = geometryFilterDef.Properties[nameof(GeometryFilter.FilePath).Prop()];
    filePath.Description = "Path to GeoJSON file defining the area";

    var inclusionMode = geometryFilterDef.Properties[nameof(GeometryFilter.InclusionMode).Prop()];
    inclusionMode.Description = "Whether to include or exclude locations in this area. Defaults to 'include'.";
    inclusionMode.Enumeration.Clear();
    foreach (var mode in new[] { "include", "exclude" })
    {
        inclusionMode.Enumeration.Add(mode);
    }
    inclusionMode.Default = "include";

    var combinationMode = geometryFilterDef.Properties[nameof(GeometryFilter.CombinationMode).Prop()];
    combinationMode.Description = "How to combine multiple geometry filters at the same level. All filters at the same level must use the same mode.";
    combinationMode.Enumeration.Clear();
    foreach (var mode in new[] { "intersection", "union" })
    {
        combinationMode.Enumeration.Add(mode);
    }

    // --- LocationPreferenceFilter definition ---
    var prefFilterDef = schema.Definitions[nameof(LocationPreferenceFilter)];
    prefFilterDef.RequiredProperties.Add(nameof(LocationPreferenceFilter.Expression).Prop());

    var pfExpression = prefFilterDef.Properties[nameof(LocationPreferenceFilter.Expression).Prop()];
    pfExpression.Description = "Boolean filter expression for matching locations. Use '*' to match all locations.";
    AddExamples(pfExpression, "Year gte 2020", "IsResidential eq true", "*");

    var pfPercentage = prefFilterDef.Properties[nameof(LocationPreferenceFilter.Percentage).Prop()];
    pfPercentage.Description = "Percentage of total locations that should match this filter (1-100). Percentages across filters at the same level must sum to 100 or less.";
    pfPercentage.Minimum = 1;
    pfPercentage.Maximum = 100;

    var pfFill = prefFilterDef.Properties[nameof(LocationPreferenceFilter.Fill).Prop()];
    pfFill.Description = "If true, fill remaining allocation with locations matching this filter. Only one filter per level can have fill=true.";

    var pfTag = prefFilterDef.Properties[nameof(LocationPreferenceFilter.LocationTag).Prop()];
    pfTag.Description = "Tag to assign to locations matched by this filter";

    var pfMinDist = prefFilterDef.Properties[nameof(LocationPreferenceFilter.MinMinDistance).Prop()];
    pfMinDist.Description = "Minimum distance in meters between locations matched by this filter";
    pfMinDist.Minimum = 0;

    // --- namedExpressions ---
    var namedExpressions = schema.Properties[nameof(MapDefinition.NamedExpressions).Prop()];
    namedExpressions.Description = "Reusable named filter expressions. Keys must start with $$. Keys must not overlap (e.g., $$urban and $$urban_area would conflict). Reference in filters as $$name.";
    AddExamples(namedExpressions,
        new Dictionary<string, string>
        {
            ["$$urban"] = "Buildings200 gte 4 and Roads25 gte 2",
            ["$$recent"] = "Year gte 2020"
        });

    // --- usedLocationsPaths ---
    var usedLocationsPaths = schema.Properties[nameof(MapDefinition.UsedLocationsPaths).Prop()];
    usedLocationsPaths.Description = "Paths to previously generated location JSON files. Locations in these files will be avoided to prevent duplicates.";
    usedLocationsPaths.UniqueItems = true;

    // --- enableDefaultLocationFilters ---
    var enableDefaultFilters = schema.Properties[nameof(MapDefinition.EnableDefaultLocationFilters).Prop()];
    enableDefaultFilters.Description = "Apply built-in default location filters (e.g., minimum coverage quality)";

    // --- LocationProbability ---
    var globalProbability = schema.Properties[nameof(MapDefinition.GlobalLocationProbability).Prop()];
    globalProbability.Description = "Global weight-based location selection probability. Locations matching weight override expressions are more/less likely to be selected.";

    var countryProbabilities = schema.Properties[nameof(MapDefinition.CountryLocationProbabilities).Prop()];
    countryProbabilities.Description = "Per-country location selection probability weights";
    SetPropertyNamesPattern(countryProbabilities, "^[A-Z]{2}$");

    var subdivisionProbabilities = schema.Properties[nameof(MapDefinition.SubdivisionLocationProbabilities).Prop()];
    subdivisionProbabilities.Description = "Per-subdivision location selection probability weights";
    SetPropertyNamesPattern(subdivisionProbabilities, "^[A-Z]{2}$");

    var probabilityDef = schema.Definitions[nameof(LocationProbability)];

    var probDefaultWeight = probabilityDef.Properties[nameof(LocationProbability.DefaultWeight).Prop()];
    probDefaultWeight.Description = "Default weight for locations not matching any override. Must be greater than 0 when weight overrides are used.";
    probDefaultWeight.Minimum = 1;

    var probWeightOverrides = probabilityDef.Properties[nameof(LocationProbability.WeightOverrides).Prop()];
    probWeightOverrides.Description = "Weight overrides based on filter expressions. Locations matching an expression use that weight instead of defaultWeight.";

    var weightOverrideDef = schema.Definitions[nameof(LocationWeightOverride)];
    weightOverrideDef.RequiredProperties.Add(nameof(LocationWeightOverride.Expression).Prop());
    weightOverrideDef.RequiredProperties.Add(nameof(LocationWeightOverride.Weight).Prop());

    var woExpression = weightOverrideDef.Properties[nameof(LocationWeightOverride.Expression).Prop()];
    woExpression.Description = "Boolean filter expression to match locations";

    var woWeight = weightOverrideDef.Properties[nameof(LocationWeightOverride.Weight).Prop()];
    woWeight.Description = "Selection weight for matched locations (higher = more likely). Must be greater than 0.";
    woWeight.Minimum = 1;

    // --- CountryPanning definition ---
    var countryPanningDef = schema.Definitions[nameof(CountryPanning)];

    var cpDefaultPanning = countryPanningDef.Properties[nameof(CountryPanning.DefaultPanning).Prop()];
    cpDefaultPanning.Description = "Default panning heading expression for this country";

    var cpPanningExpressions = countryPanningDef.Properties[nameof(CountryPanning.PanningExpressions).Prop()];
    cpPanningExpressions.Description = "Conditional panning expressions. First matching expression is used, then falls back to defaultPanning.";

    // --- PanningExpression definition ---
    var panningExprDef = schema.Definitions[nameof(PanningExpression)];
    panningExprDef.RequiredProperties.Add(nameof(PanningExpression.Expression).Prop());
    panningExprDef.RequiredProperties.Add(nameof(PanningExpression.Panning).Prop());

    var peExpression = panningExprDef.Properties[nameof(PanningExpression.Expression).Prop()];
    peExpression.Description = "Boolean filter expression to match locations";

    var pePanning = panningExprDef.Properties[nameof(PanningExpression.Panning).Prop()];
    pePanning.Description = "Heading expression to use when this condition matches";
}

// --- Live Generate constraints ---

static void AddLiveGenerateConstraints(JsonSchema schema)
{
    // Root required properties
    schema.RequiredProperties.Add(nameof(LiveGenerateMapDefinition.Countries).Prop());
    schema.RequiredProperties.Add(nameof(LiveGenerateMapDefinition.LocationFilter).Prop());

    // --- countries ---
    var countries = schema.Properties[nameof(LiveGenerateMapDefinition.Countries).Prop()];
    countries.Description = "Countries with their location count goals (e.g., {\"NO\": 1000, \"SE\": 2000})";
    SetPropertyNamesPattern(countries, "^[A-Z]{2}$");

    // --- distribution ---
    var distribution = schema.Properties[nameof(LiveGenerateMapDefinition.Distribution).Prop()];
    distribution.Description = "Location distribution settings (minimum distance, overshoot factor)";

    // --- locationFilter ---
    var locationFilter = schema.Properties[nameof(LiveGenerateMapDefinition.LocationFilter).Prop()];
    locationFilter.Description = $"Filter expression for locations. {ExpressionSyntaxHelp()}";
    AddExamples(locationFilter, "ArrowCount gte 2 and Year gt 2015", "Year gte 2020", "*");

    // --- locationTags ---
    var locationTags = schema.Properties[nameof(LiveGenerateMapDefinition.LocationTags).Prop()];
    AddLocationTagsConstraints(locationTags);

    // --- dates ---
    var fromDate = schema.Properties[nameof(LiveGenerateMapDefinition.FromDate).Prop()];
    fromDate.Description = "Start date for coverage filter (format: YYYY-MM-DD)";
    fromDate.Pattern = @"^\d{4}-\d{2}-\d{2}$";

    var toDate = schema.Properties[nameof(LiveGenerateMapDefinition.ToDate).Prop()];
    toDate.Description = "End date for coverage filter (format: YYYY-MM-DD)";
    toDate.Pattern = @"^\d{4}-\d{2}-\d{2}$";

    // --- heading ---
    var headingMode = schema.Properties[nameof(LiveGenerateMapDefinition.HeadingMode).Prop()];
    headingMode.Description = "How to set the camera heading/direction";
    headingMode.Enumeration.Clear();
    foreach (var mode in new[] { "Default", "Random", "InDrivingDirection", "AwayFromDrivingDirection" })
    {
        headingMode.Enumeration.Add(mode);
    }

    var headingDelta = schema.Properties[nameof(LiveGenerateMapDefinition.HeadingDelta).Prop()];
    headingDelta.Description = "Degrees to add to heading (used with heading modes)";
    headingDelta.Minimum = -180;
    headingDelta.Maximum = 180;

    // --- pitch ---
    var pitchMode = schema.Properties[nameof(LiveGenerateMapDefinition.PitchMode).Prop()];
    pitchMode.Description = "How to set the camera pitch/tilt";
    pitchMode.Enumeration.Clear();
    foreach (var mode in new[] { "Default", "Fixed", "Random" })
    {
        pitchMode.Enumeration.Add(mode);
    }

    var liveGenPitch = schema.Properties[nameof(LiveGenerateMapDefinition.Pitch).Prop()];
    liveGenPitch.Description = "Fixed pitch angle in degrees (used with pitchMode: Fixed)";
    liveGenPitch.Minimum = -90;
    liveGenPitch.Maximum = 90;

    var pitchMin = schema.Properties[nameof(LiveGenerateMapDefinition.RandomPitchMin).Prop()];
    pitchMin.Description = "Minimum pitch for random mode";
    pitchMin.Minimum = -90;
    pitchMin.Maximum = 90;

    var pitchMax = schema.Properties[nameof(LiveGenerateMapDefinition.RandomPitchMax).Prop()];
    pitchMax.Description = "Maximum pitch for random mode";
    pitchMax.Minimum = -90;
    pitchMax.Maximum = 90;

    // --- zoom ---
    var zoomMode = schema.Properties[nameof(LiveGenerateMapDefinition.ZoomMode).Prop()];
    zoomMode.Description = "How to set the camera zoom level";
    zoomMode.Enumeration.Clear();
    foreach (var mode in new[] { "Default", "Fixed", "Random" })
    {
        zoomMode.Enumeration.Add(mode);
    }

    var liveGenZoom = schema.Properties[nameof(LiveGenerateMapDefinition.Zoom).Prop()];
    liveGenZoom.Description = "Fixed zoom level (used with zoomMode: Fixed)";
    liveGenZoom.Minimum = 0;
    liveGenZoom.Maximum = 3;

    var zoomMin = schema.Properties[nameof(LiveGenerateMapDefinition.RandomZoomMin).Prop()];
    zoomMin.Description = "Minimum zoom for random mode";
    zoomMin.Minimum = 0;
    zoomMin.Maximum = 3.6m;

    var zoomMax = schema.Properties[nameof(LiveGenerateMapDefinition.RandomZoomMax).Prop()];
    zoomMax.Description = "Maximum zoom for random mode";
    zoomMax.Minimum = 0;
    zoomMax.Maximum = 3.6m;

    // --- API settings ---
    var boxPrecision = schema.Properties[nameof(LiveGenerateMapDefinition.BoxPrecision).Prop()];
    boxPrecision.Description = "Geohash precision for subdividing areas (1-7, higher = smaller boxes)";
    boxPrecision.Minimum = 1;
    boxPrecision.Maximum = 7;
    boxPrecision.Default = 4;

    var parallelRequests = schema.Properties[nameof(LiveGenerateMapDefinition.ParallelRequests).Prop()];
    parallelRequests.Description = "Number of parallel API requests to Google";
    parallelRequests.Minimum = 1;
    parallelRequests.Maximum = 200;
    parallelRequests.Default = 100;

    var radius = schema.Properties[nameof(LiveGenerateMapDefinition.Radius).Prop()];
    radius.Description = "Search radius in meters for finding locations";
    radius.Minimum = 1;
    radius.Default = 100;

    var batchSize = schema.Properties[nameof(LiveGenerateMapDefinition.BatchSize).Prop()];
    batchSize.Description = "Number of locations to process in each batch";
    batchSize.Minimum = 100;
    batchSize.Default = 10_000;

    // --- filtering ---
    var rejectNoDesc = schema.Properties[nameof(LiveGenerateMapDefinition.RejectLocationsWithoutDescription).Prop()];
    rejectNoDesc.Description = "Reject locations without a description field (filters out some unofficial coverage)";
    rejectNoDesc.Default = true;

    var checkLinked = schema.Properties[nameof(LiveGenerateMapDefinition.CheckLinkedPanoramas).Prop()];
    checkLinked.Description = "Check linked panoramas for better coverage";

    var coverage = schema.Properties[nameof(LiveGenerateMapDefinition.AcceptedCoverage).Prop()];
    coverage.Description = "Type of coverage to accept from Google Street View";
    coverage.Enumeration.Clear();
    foreach (var type in new[] { "official", "unofficial", "all" })
    {
        coverage.Enumeration.Add(type);
    }

    var badCamStrategy = schema.Properties[nameof(LiveGenerateMapDefinition.BadCamStrategy).Prop()];
    badCamStrategy.Description = "How to handle bad camera generations (e.g., gen1/gen2 cameras)";
    badCamStrategy.Enumeration.Clear();
    foreach (var strat in Enum.GetValues<BadCamStrategy>())
    {
        badCamStrategy.Enumeration.Add(strat.ToString());
    }

    // --- pano selection ---
    var panoStrategy = schema.Properties[nameof(LiveGenerateMapDefinition.PanoSelectionStrategy).Prop()];
    panoStrategy.Description = "Strategy for selecting panorama IDs";
    panoStrategy.Enumeration.Clear();
    foreach (var strat in Enum.GetValues<PanoStrategy>().Where(x => x != PanoStrategy.None))
    {
        panoStrategy.Enumeration.Add(strat.ToString());
    }

    var panoStart = schema.Properties[nameof(LiveGenerateMapDefinition.PanoVerificationStart).Prop()];
    panoStart.Description = "Start date for panorama verification (format: YYYY-MM-DD)";

    var panoEnd = schema.Properties[nameof(LiveGenerateMapDefinition.PanoVerificationEnd).Prop()];
    panoEnd.Description = "End date for panorama verification (format: YYYY-MM-DD)";

    // --- geoJson ---
    var geoJson = schema.Properties[nameof(LiveGenerateMapDefinition.GeoJsonFiles).Prop()];
    geoJson.Description = "Paths to GeoJSON files defining areas to search within";
    geoJson.UniqueItems = true;

    // --- LocationDistribution definition ---
    var distributionDef = schema.Definitions[nameof(LiveGenerateMapDefinition.LocationDistribution)];
    distributionDef.AllowAdditionalProperties = true;

    var lgMinMinDistance = distributionDef.Properties[nameof(LiveGenerateMapDefinition.LocationDistribution.MinMinDistance).Prop()];
    lgMinMinDistance.Description = "Minimum distance in meters between generated locations";
    lgMinMinDistance.Minimum = 0;

    var overshoot = distributionDef.Properties[nameof(LiveGenerateMapDefinition.LocationDistribution.OvershootFactor).Prop()];
    overshoot.Description = "Multiplier for initial location count (e.g., 2 = generate 2x then filter down). Higher = better distribution but slower.";
    overshoot.Minimum = 1;
    overshoot.Maximum = 10;
    overshoot.Default = 1;
}

static Dictionary<string, object> StrategyConditional(string strategyKey, string requiredProperty) =>
    new()
    {
        ["if"] = new Dictionary<string, object>
        {
            ["properties"] = new Dictionary<string, object>
            {
                [nameof(DistributionStrategy.Key).Prop()] = new Dictionary<string, object> { ["enum"] = new[] { strategyKey } }
            },
            ["required"] = new[] { nameof(DistributionStrategy.Key).Prop() }
        },
        ["then"] = new Dictionary<string, object>
        {
            ["required"] = new[] { requiredProperty }
        }
    };
