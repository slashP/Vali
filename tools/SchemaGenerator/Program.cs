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

var mapDefinitionSchema = JsonSchema.FromType<MapDefinition>(settings);
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

await File.WriteAllTextAsync(mapDefinitionPath, mapDefinitionSchema.ToJson());
await File.WriteAllTextAsync(liveGeneratePath, liveGenerateSchema.ToJson());

Console.WriteLine($"✓ Generated: {Path.GetFullPath(mapDefinitionPath)}");
Console.WriteLine($"✓ Generated: {Path.GetFullPath(liveGeneratePath)}");
Console.WriteLine("Schemas generated successfully!");

static string[] GetValidLocationTags(bool isLiveGenerate = false)
{
    var tags = new HashSet<string>();

    // Properties excluded - either internal data or context-specific
    var excludedProperties = new HashSet<string>
    {
        "Lat",
        "Lng",
    };

    // ResolutionHeight only excluded from regular map definitions (not populated in pre-processed data)
    if (!isLiveGenerate)
    {
        excludedProperties.Add("ResolutionHeight");
    }

    foreach (var validProperty in LocationLakeFilterer.ValidProperties().Where(x => !excludedProperties.Contains(x)))
    {
        tags.Add(validProperty);
    }

    //derived tags
    tags.Add("Season");
    tags.Add("YearMonth");
    tags.Add("HighwayType");

    return tags.OrderBy(t => t).ToArray();
}

static string[] GetBucketableLocationTags(bool isLiveGenerate = false)
{
    var bucketable = new HashSet<string>();
    foreach (var prop in typeof(OsmData).GetProperties()
                 .Concat(typeof(GoogleData).GetProperties())
                 .Where(prop => (Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType) == typeof(int)))
    {
        bucketable.Add(prop.Name);
    }

    // ResolutionHeight only available in live-generate
    if (!isLiveGenerate)
    {
        bucketable.Remove("ResolutionHeight");
    }

    return bucketable.OrderBy(t => t).ToArray();
}

static void AddMapDefinitionConstraints(JsonSchema schema)
{
    var countryCodes = schema.Properties[nameof(MapDefinition.CountryCodes).Prop()];
    countryCodes.Description = "ISO 3166-1 alpha-2 country codes, or special keywords like 'europe', 'asia', 'africa', '*', etc.";
    countryCodes.MinItems = 1;

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
    locationCountGoal.Description = "Target number of locations (used with FixedCountByMaxMinDistance)";
    locationCountGoal.Minimum = 1;

    var minMinDistance = distStrategyDef.Properties[nameof(DistributionStrategy.MinMinDistance).Prop()];
    minMinDistance.Description = "Minimum distance in meters between locations";
    minMinDistance.Minimum = 0;

    var fixedMinDistance = distStrategyDef.Properties[nameof(DistributionStrategy.FixedMinDistance).Prop()];
    fixedMinDistance.Description = "Fixed minimum distance in meters between locations (used with MaxCountByFixedMinDistance)";
    fixedMinDistance.Minimum = 1;

    var countryDist = distStrategyDef.Properties[nameof(DistributionStrategy.CountryDistributionFromMap).Prop()];
    countryDist.Description = "Use country distribution from a famous map";
    countryDist.Enumeration.Clear();
    foreach (var dist in MapDefinitionDefaults.ValidMapShortNames) // #ENUM
    {
        countryDist.Enumeration.Add(dist);
    }

    var globalFilter = schema.Properties[nameof(MapDefinition.GlobalLocationFilter).Prop()];
    globalFilter.Description = "Expression to filter locations globally (e.g., 'ClosestCoast lt 100 and Buildings200 gte 4')";

    var countryDistribution = schema.Properties[nameof(MapDefinition.CountryDistribution).Prop()];
    countryDistribution.Description = "Custom distribution weights per country (integers representing relative weights)";

    var subdivisionDistribution = schema.Properties[nameof(MapDefinition.SubdivisionDistribution).Prop()];
    subdivisionDistribution.Description = "Custom distribution weights per subdivision/region (integers representing relative weights)";

    var namedExpressions = schema.Properties[nameof(MapDefinition.NamedExpressions).Prop()];
    namedExpressions.Description = "Reusable filter expressions (must start with $$)";

    var output = schema.Properties[nameof(MapDefinition.Output).Prop()];
    output.Description = "Output configuration for generated locations";

    var outputDef = schema.Definitions[nameof(LocationOutput)];
    outputDef.AllowAdditionalProperties = true;

    var outputLocationTags = outputDef.Properties[nameof(LocationOutput.LocationTags).Prop()];
    outputLocationTags.Description = "Tags to add to generated locations. Numeric tags can be extended with -<number> for bucketing (e.g., Buildings25-5, ClosestCoast-100).";
    if (outputLocationTags.Item != null)
    {
        var validTags = GetValidLocationTags(isLiveGenerate: false);
        var bucketableTags = GetBucketableLocationTags(isLiveGenerate: false);

        // Create oneOf schema: enum for base tags OR pattern for bucketed variants
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

        outputLocationTags.Item.OneOf.Clear();
        outputLocationTags.Item.OneOf.Add(enumSchema);
        outputLocationTags.Item.OneOf.Add(patternSchema);
    }

    var panoStrategy = outputDef.Properties[nameof(LocationOutput.PanoVerificationStrategy).Prop()];
    panoStrategy.Description = "Strategy for selecting panorama IDs";
    panoStrategy.Enumeration.Clear();
    foreach (var strat in Enum.GetValues<PanoStrategy>().Where(x => x != PanoStrategy.None))
    {
        panoStrategy.Enumeration.Add(strat.ToString());
    }

    var headingExpr = outputDef.Properties[nameof(LocationOutput.GlobalHeadingExpression).Prop()];
    headingExpr.Description = "Expression to calculate heading (e.g., 'DrivingDirectionAngle + 90')";

    var zoom = outputDef.Properties[nameof(LocationOutput.GlobalZoom).Prop()];
    zoom.Description = "Zoom level for all locations (0-3.6)";
    zoom.Minimum = 0;
    zoom.Maximum = 3.6m;

    var pitch = outputDef.Properties[nameof(LocationOutput.GlobalPitch).Prop()];
    pitch.Description = "Pitch angle for all locations (-90 to 90)";
    pitch.Minimum = -90;
    pitch.Maximum = 90;

    var neighborFilters = schema.Properties[nameof(MapDefinition.NeighborFilters).Prop()];
    neighborFilters.Description = "Filter locations based on nearby neighbors";

    var neighborFilterDef = schema.Definitions[nameof(NeighborFilter)];
    neighborFilterDef.AllowAdditionalProperties = true;

    var radius = neighborFilterDef.Properties[nameof(NeighborFilter.Radius).Prop()];
    radius.Description = "Radius in meters to check for neighbors";
    radius.Minimum = 0;

    var expression = neighborFilterDef.Properties[nameof(NeighborFilter.Expression).Prop()];
    expression.Description = "Filter expression for neighbor locations (can reference current location with 'current:' prefix)";

    var bound = neighborFilterDef.Properties[nameof(NeighborFilter.Bound).Prop()];
    bound.Description = "How many neighbors must match the expression";
    bound.Enumeration.Clear();
    foreach (var b in FilterValidation.ValidNeighborFilterBounds) // #ENUM
    {
        bound.Enumeration.Add(b);
    }

    var limit = neighborFilterDef.Properties[nameof(NeighborFilter.Limit).Prop()];
    limit.Description = "Number or percentage of neighbors required (depending on bound)";

    var checkCardinal = neighborFilterDef.Properties[nameof(NeighborFilter.CheckEachCardinalDirectionSeparately).Prop()];
    checkCardinal.Description = "Check north, south, east, west directions separately";

    var geometryFilters = schema.Properties[nameof(MapDefinition.GeometryFilters).Prop()];
    geometryFilters.Description = "Filter locations within GeoJSON-defined areas";

    var geometryFilterDef = schema.Definitions[nameof(GeometryFilter)];
    geometryFilterDef.AllowAdditionalProperties = true;

    var filePath = geometryFilterDef.Properties[nameof(GeometryFilter.FilePath).Prop()];
    filePath.Description = "Path to GeoJSON file defining the area";

    var inclusionMode = geometryFilterDef.Properties[nameof(GeometryFilter.InclusionMode).Prop()];
    inclusionMode.Description = "Whether to include or exclude locations in this area";
    inclusionMode.Enumeration.Clear();
    foreach (var mode in new[] { "include", "exclude" }) // #ENUM
    {
        inclusionMode.Enumeration.Add(mode);
    }

    var combinationMode = geometryFilterDef.Properties[nameof(GeometryFilter.CombinationMode).Prop()];
    combinationMode.Description = "How to combine multiple geometry filters (first filter only)";
    combinationMode.Enumeration.Clear();
    foreach (var mode in new[] { "intersection", "union" }) // #ENUM
    {
        combinationMode.Enumeration.Add(mode);
    }
}

static void AddLiveGenerateConstraints(JsonSchema schema)
{
    var countries = schema.Properties[nameof(LiveGenerateMapDefinition.Countries).Prop()];
    countries.Description = "Countries with their location count goals (e.g., {\"NO\": 1000, \"SE\": 2000})";

    var locationFilter = schema.Properties[nameof(LiveGenerateMapDefinition.LocationFilter).Prop()];
    locationFilter.Description = "Filter expression for locations (e.g., 'ArrowCount gte 2 and Year gt 2015')";

    var locationTags = schema.Properties[nameof(LiveGenerateMapDefinition.LocationTags).Prop()];
    locationTags.Description = "Tags to add to generated locations. Numeric tags can be extended with -<number> for bucketing (e.g., Buildings25-5, ClosestCoast-100).";
    if (locationTags.Item != null)
    {
        var validTags = GetValidLocationTags(isLiveGenerate: true);
        var bucketableTags = GetBucketableLocationTags(isLiveGenerate: true);

        // Create oneOf schema: enum for base tags OR pattern for bucketed variants
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

        locationTags.Item.OneOf.Clear();
        locationTags.Item.OneOf.Add(enumSchema);
        locationTags.Item.OneOf.Add(patternSchema);
    }

    var fromDate = schema.Properties[nameof(LiveGenerateMapDefinition.FromDate).Prop()];
    fromDate.Description = "Start date for coverage filter (format: YYYY-MM-DD)";
    fromDate.Pattern = @"^\d{4}-\d{2}-\d{2}$";

    var toDate = schema.Properties[nameof(LiveGenerateMapDefinition.ToDate).Prop()];
    toDate.Description = "End date for coverage filter (format: YYYY-MM-DD)";
    toDate.Pattern = @"^\d{4}-\d{2}-\d{2}$";

    var radius = schema.Properties[nameof(LiveGenerateMapDefinition.Radius).Prop()];
    radius.Description = "Search radius in meters for finding locations";
    radius.Minimum = 1;

    var parallelRequests = schema.Properties[nameof(LiveGenerateMapDefinition.ParallelRequests).Prop()];
    parallelRequests.Description = "Number of parallel API requests to Google";
    parallelRequests.Minimum = 1;
    parallelRequests.Maximum = 200;

    var batchSize = schema.Properties[nameof(LiveGenerateMapDefinition.BatchSize).Prop()];
    batchSize.Description = "Number of locations to process in each batch";
    batchSize.Minimum = 100;

    var boxPrecision = schema.Properties[nameof(LiveGenerateMapDefinition.BoxPrecision).Prop()];
    boxPrecision.Description = "Geohash precision for subdividing areas (1-7, higher = smaller boxes)";
    boxPrecision.Minimum = 1;
    boxPrecision.Maximum = 7;

    var rejectNoDesc = schema.Properties[nameof(LiveGenerateMapDefinition.RejectLocationsWithoutDescription).Prop()];
    rejectNoDesc.Description = "Reject locations without a description field (filters out some unofficial coverage)";

    var checkLinked = schema.Properties[nameof(LiveGenerateMapDefinition.CheckLinkedPanoramas).Prop()];
    checkLinked.Description = "Check linked panoramas for better coverage";

    var distributionDef = schema.Definitions[nameof(LiveGenerateMapDefinition.LocationDistribution)];
    distributionDef.AllowAdditionalProperties = true;

    var minMinDistance = distributionDef.Properties[nameof(LiveGenerateMapDefinition.LocationDistribution.MinMinDistance).Prop()];
    minMinDistance.Description = "Minimum distance in meters between generated locations";
    minMinDistance.Minimum = 0;

    var overshoot = distributionDef.Properties[nameof(LiveGenerateMapDefinition.LocationDistribution.OvershootFactor).Prop()];
    overshoot.Description = "Multiplier for initial location count (e.g., 2 means generate 2x locations then filter down). Higher = better coverage but slower.";
    overshoot.Minimum = 1;
    overshoot.Maximum = 10;

    var panoStrategy = schema.Properties[nameof(LiveGenerateMapDefinition.PanoSelectionStrategy).Prop()];
    panoStrategy.Description = "Strategy for selecting panorama IDs";
    panoStrategy.Enumeration.Clear();
    foreach (var strat in Enum.GetValues<PanoStrategy>())
    {
        if (strat != PanoStrategy.None)
        {
            panoStrategy.Enumeration.Add(strat.ToString());
        }
    }

    var coverage = schema.Properties[nameof(LiveGenerateMapDefinition.AcceptedCoverage).Prop()];
    coverage.Description = "Type of coverage to accept from Google Street View";
    coverage.Enumeration.Clear();
    foreach (var type in new[] { "official", "unofficial", "all" }) // #ENUM
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

    var headingMode = schema.Properties[nameof(LiveGenerateMapDefinition.HeadingMode).Prop()];
    headingMode.Description = "How to set the camera heading/direction";
    headingMode.Enumeration.Clear();
    foreach (var mode in new[] { "Default", "Random", "InDrivingDirection", "AwayFromDrivingDirection" }) // #ENUM
    {
        headingMode.Enumeration.Add(mode);
    }

    var headingDelta = schema.Properties[nameof(LiveGenerateMapDefinition.HeadingDelta).Prop()];
    headingDelta.Description = "Degrees to add to heading (used with heading modes)";
    headingDelta.Minimum = -180;
    headingDelta.Maximum = 180;

    var pitchMode = schema.Properties[nameof(LiveGenerateMapDefinition.PitchMode).Prop()];
    pitchMode.Description = "How to set the camera pitch/tilt";
    pitchMode.Enumeration.Clear();
    foreach (var mode in new[] { "Default", "Fixed", "Random" }) // #ENUM
    {
        pitchMode.Enumeration.Add(mode);
    }

    var pitch = schema.Properties[nameof(LiveGenerateMapDefinition.Pitch).Prop()];
    pitch.Description = "Fixed pitch angle in degrees (used with pitchMode: Fixed)";
    pitch.Minimum = -90;
    pitch.Maximum = 90;

    var pitchMin = schema.Properties[nameof(LiveGenerateMapDefinition.RandomPitchMin).Prop()];
    pitchMin.Description = "Minimum pitch for random mode";
    pitchMin.Minimum = -90;
    pitchMin.Maximum = 90;

    var pitchMax = schema.Properties[nameof(LiveGenerateMapDefinition.RandomPitchMax).Prop()];
    pitchMax.Description = "Maximum pitch for random mode";
    pitchMax.Minimum = -90;
    pitchMax.Maximum = 90;

    var zoomMode = schema.Properties[nameof(LiveGenerateMapDefinition.ZoomMode).Prop()];
    zoomMode.Description = "How to set the camera zoom level";
    zoomMode.Enumeration.Clear();
    foreach (var mode in new[] { "Default", "Fixed", "Random" }) // #ENUM
    {
        zoomMode.Enumeration.Add(mode);
    }

    var zoom = schema.Properties[nameof(LiveGenerateMapDefinition.Zoom).Prop()];
    zoom.Description = "Fixed zoom level (used with zoomMode: Fixed)";
    zoom.Minimum = 0;
    zoom.Maximum = 3;

    var zoomMin = schema.Properties[nameof(LiveGenerateMapDefinition.RandomZoomMin).Prop()];
    zoomMin.Description = "Minimum zoom for random mode";
    zoomMin.Minimum = 0;
    zoomMin.Maximum = 3.6m;

    var zoomMax = schema.Properties[nameof(LiveGenerateMapDefinition.RandomZoomMax).Prop()];
    zoomMax.Description = "Maximum zoom for random mode";
    zoomMax.Minimum = 0;
    zoomMax.Maximum = 3.6m;

    var panoStart = schema.Properties[nameof(LiveGenerateMapDefinition.PanoVerificationStart).Prop()];
    panoStart.Description = "Start date for panorama verification (format: YYYY-MM-DD)";

    var panoEnd = schema.Properties[nameof(LiveGenerateMapDefinition.PanoVerificationEnd).Prop()];
    panoEnd.Description = "End date for panorama verification (format: YYYY-MM-DD)";

    var geoJson = schema.Properties[nameof(LiveGenerateMapDefinition.GeoJsonFiles).Prop()];
    geoJson.Description = "Paths to GeoJSON files defining areas to search within";
}
