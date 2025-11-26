using NJsonSchema;
using NJsonSchema.Generation;
using Vali.Core;
using Vali.Core.Data;
using Vali.Core.Google;
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

static string[] GetValidLocationTags()
{
    var tags = new HashSet<string>();

    // internal data not exposed as tags
    var excludedProperties = new HashSet<string>
    {
        "Lat",
        "Lng",
        "PanoId",
        "CheckedAt",
        "DefaultHeading",
        "RoadType",
        "WayIds"
    };

    foreach (var prop in typeof(OsmData).GetProperties())
    {
        if (!excludedProperties.Contains(prop.Name))
        {
            tags.Add(prop.Name);
        }
    }

    foreach (var prop in typeof(GoogleData).GetProperties())
    {
        if (!excludedProperties.Contains(prop.Name))
        {
            tags.Add(prop.Name);
        }
    }

    foreach (var prop in typeof(NominatimData).GetProperties())
    {
        if (!excludedProperties.Contains(prop.Name))
        {
            tags.Add(prop.Name);
        }
    }

    //derived tags
    tags.Add("Season");
    tags.Add("YearMonth");
    tags.Add("HighwayType");

    return tags.OrderBy(t => t).ToArray();
}

static void AddMapDefinitionConstraints(JsonSchema schema)
{
    if (schema.Properties.TryGetValue("countryCodes", out var countryCodes))
    {
        countryCodes.Description = "ISO 3166-1 alpha-2 country codes, or special keywords like 'europe', 'asia', 'africa', '*', etc.";
        countryCodes.MinItems = 1;
    }

    if (schema.Properties.TryGetValue("distributionStrategy", out var distStrategy))
    {
        distStrategy.Description = "Strategy for distributing locations across regions";
    }

    if (schema.Definitions.TryGetValue("DistributionStrategy", out var distStrategyDef))
    {
        distStrategyDef.AllowAdditionalProperties = true;

        if (distStrategyDef.Properties.TryGetValue("key", out var key))
        {
            key.Description = "Distribution algorithm to use";
            key.Enumeration.Clear();
            foreach (var strategy in DistributionStrategies.ValidStrategyKeys)
            {
                key.Enumeration.Add(strategy);
            }
        }

        if (distStrategyDef.Properties.TryGetValue("locationCountGoal", out var locationCountGoal))
        {
            locationCountGoal.Description = "Target number of locations (used with FixedCountByMaxMinDistance)";
            locationCountGoal.Minimum = 1;
        }

        if (distStrategyDef.Properties.TryGetValue("minMinDistance", out var minMinDistance))
        {
            minMinDistance.Description = "Minimum distance in meters between locations";
            minMinDistance.Minimum = 0;
        }

        if (distStrategyDef.Properties.TryGetValue("fixedMinDistance", out var fixedMinDistance))
        {
            fixedMinDistance.Description = "Fixed minimum distance in meters between locations (used with MaxCountByFixedMinDistance)";
            fixedMinDistance.Minimum = 1;
        }

        if (distStrategyDef.Properties.TryGetValue("countryDistributionFromMap", out var countryDist))
        {
            countryDist.Description = "Use country distribution from a famous map";
            countryDist.Enumeration.Clear();
            foreach (var dist in new[] { "aarw", "aaw", "acw", "abw", "aiw", "proworld", "aow", "rainboltworld", "geotime", "lerg" }) // #ENUM
            {
                countryDist.Enumeration.Add(dist);
            }
        }
    }

    if (schema.Properties.TryGetValue("globalLocationFilter", out var globalFilter))
    {
        globalFilter.Description = "Expression to filter locations globally (e.g., 'ClosestCoast lt 100 and Buildings200 gte 4')";
    }

    if (schema.Properties.TryGetValue("countryDistribution", out var countryDistribution))
    {
        countryDistribution.Description = "Custom distribution weights per country (integers representing relative weights)";
    }

    if (schema.Properties.TryGetValue("subdivisionDistribution", out var subdivisionDistribution))
    {
        subdivisionDistribution.Description = "Custom distribution weights per subdivision/region (integers representing relative weights)";
    }

    if (schema.Properties.TryGetValue("namedExpressions", out var namedExpressions))
    {
        namedExpressions.Description = "Reusable filter expressions (must start with $$)";
    }

    if (schema.Properties.TryGetValue("output", out var output))
    {
        output.Description = "Output configuration for generated locations";
    }

    if (schema.Definitions.TryGetValue("LocationOutput", out var outputDef))
    {
        outputDef.AllowAdditionalProperties = true;

        if (outputDef.Properties.TryGetValue("locationTags", out var outputLocationTags))
        {
            outputLocationTags.Description = "Tags to add to generated locations";
            if (outputLocationTags.Item != null)
            {
                outputLocationTags.Item.Enumeration.Clear();
                var validTags = GetValidLocationTags();
                foreach (var tag in validTags)
                {
                    outputLocationTags.Item.Enumeration.Add(tag);
                }
            }
        }

        if (outputDef.Properties.TryGetValue("panoVerificationStrategy", out var panoStrategy))
        {
            panoStrategy.Description = "Strategy for selecting panorama IDs";
            panoStrategy.Enumeration.Clear();
            foreach (var strat in Enum.GetValues<PanoStrategy>())
            {
                if (strat != PanoStrategy.None)
                {
                    panoStrategy.Enumeration.Add(strat.ToString());
                }
            }
        }

        if (outputDef.Properties.TryGetValue("globalHeadingExpression", out var headingExpr))
        {
            headingExpr.Description = "Expression to calculate heading (e.g., 'DrivingDirectionAngle + 90')";
        }

        if (outputDef.Properties.TryGetValue("globalZoom", out var zoom))
        {
            zoom.Description = "Zoom level for all locations (0-3.6)";
            zoom.Minimum = 0;
            zoom.Maximum = 3.6m;
        }

        if (outputDef.Properties.TryGetValue("globalPitch", out var pitch))
        {
            pitch.Description = "Pitch angle for all locations (-90 to 90)";
            pitch.Minimum = -90;
            pitch.Maximum = 90;
        }
    }

    if (schema.Properties.TryGetValue("neighborFilters", out var neighborFilters))
    {
        neighborFilters.Description = "Filter locations based on nearby neighbors";
    }

    if (schema.Definitions.TryGetValue("NeighborFilter", out var neighborFilterDef))
    {
        neighborFilterDef.AllowAdditionalProperties = true;

        if (neighborFilterDef.Properties.TryGetValue("radius", out var radius))
        {
            radius.Description = "Radius in meters to check for neighbors";
            radius.Minimum = 0;
        }

        if (neighborFilterDef.Properties.TryGetValue("expression", out var expression))
        {
            expression.Description = "Filter expression for neighbor locations (can reference current location with 'current:' prefix)";
        }

        if (neighborFilterDef.Properties.TryGetValue("bound", out var bound))
        {
            bound.Description = "How many neighbors must match the expression";
            bound.Enumeration.Clear();
            foreach (var b in new[] { "gte", "lte", "all", "none", "some", "percentage-gte", "percentage-lte" }) // #ENUM
            {
                bound.Enumeration.Add(b);
            }
        }

        if (neighborFilterDef.Properties.TryGetValue("limit", out var limit))
        {
            limit.Description = "Number or percentage of neighbors required (depending on bound)";
        }

        if (neighborFilterDef.Properties.TryGetValue("checkEachCardinalDirectionSeparately", out var checkCardinal))
        {
            checkCardinal.Description = "Check north, south, east, west directions separately";
        }
    }

    if (schema.Properties.TryGetValue("geometryFilters", out var geometryFilters))
    {
        geometryFilters.Description = "Filter locations within GeoJSON-defined areas";
    }

    if (schema.Definitions.TryGetValue("GeometryFilter", out var geometryFilterDef))
    {
        geometryFilterDef.AllowAdditionalProperties = true;

        if (geometryFilterDef.Properties.TryGetValue("filePath", out var filePath))
        {
            filePath.Description = "Path to GeoJSON file defining the area";
        }

        if (geometryFilterDef.Properties.TryGetValue("inclusionMode", out var inclusionMode))
        {
            inclusionMode.Description = "Whether to include or exclude locations in this area";
            inclusionMode.Enumeration.Clear();
            foreach (var mode in new[] { "include", "exclude" }) // #ENUM
            {
                inclusionMode.Enumeration.Add(mode);
            }
        }

        if (geometryFilterDef.Properties.TryGetValue("combinationMode", out var combinationMode))
        {
            combinationMode.Description = "How to combine multiple geometry filters (first filter only)";
            combinationMode.Enumeration.Clear();
            foreach (var mode in new[] { "intersection", "union" }) // #ENUM
            {
                combinationMode.Enumeration.Add(mode);
            }
        }
    }
}

static void AddLiveGenerateConstraints(JsonSchema schema)
{
    if (schema.Properties.TryGetValue("countries", out var countries))
    {
        countries.Description = "Countries with their location count goals (e.g., {\"NO\": 1000, \"SE\": 2000})";
    }

    if (schema.Properties.TryGetValue("locationFilter", out var locationFilter))
    {
        locationFilter.Description = "Filter expression for locations (e.g., 'ArrowCount gte 2 and Year gt 2015')";
    }

    if (schema.Properties.TryGetValue("locationTags", out var locationTags))
    {
        locationTags.Description = "Tags to add to generated locations";
        if (locationTags.Item != null)
        {
            locationTags.Item.Enumeration.Clear();
            var validTags = GetValidLocationTags();
            foreach (var tag in validTags)
            {
                locationTags.Item.Enumeration.Add(tag);
            }
        }
    }

    if (schema.Properties.TryGetValue("fromDate", out var fromDate))
    {
        fromDate.Description = "Start date for coverage filter (format: YYYY-MM-DD)";
        fromDate.Pattern = @"^\d{4}-\d{2}-\d{2}$";
    }

    if (schema.Properties.TryGetValue("toDate", out var toDate))
    {
        toDate.Description = "End date for coverage filter (format: YYYY-MM-DD)";
        toDate.Pattern = @"^\d{4}-\d{2}-\d{2}$";
    }

    if (schema.Properties.TryGetValue("radius", out var radius))
    {
        radius.Description = "Search radius in meters for finding locations";
        radius.Minimum = 1;
    }

    if (schema.Properties.TryGetValue("parallelRequests", out var parallelRequests))
    {
        parallelRequests.Description = "Number of parallel API requests to Google";
        parallelRequests.Minimum = 1;
        parallelRequests.Maximum = 200;
    }

    if (schema.Properties.TryGetValue("batchSize", out var batchSize))
    {
        batchSize.Description = "Number of locations to process in each batch";
        batchSize.Minimum = 100;
    }

    if (schema.Properties.TryGetValue("boxPrecision", out var boxPrecision))
    {
        boxPrecision.Description = "Geohash precision for subdividing areas (1-7, higher = smaller boxes)";
        boxPrecision.Minimum = 1;
        boxPrecision.Maximum = 7;
    }

    if (schema.Properties.TryGetValue("rejectLocationsWithoutDescription", out var rejectNoDesc))
    {
        rejectNoDesc.Description = "Reject locations without a description field (filters out some unofficial coverage)";
    }

    if (schema.Properties.TryGetValue("checkLinkedPanoramas", out var checkLinked))
    {
        checkLinked.Description = "Check linked panoramas for better coverage";
    }

    if (schema.Definitions.TryGetValue("LocationDistribution", out var distributionDef))
    {
        distributionDef.AllowAdditionalProperties = true;

        if (distributionDef.Properties.TryGetValue("minMinDistance", out var minMinDistance))
        {
            minMinDistance.Description = "Minimum distance in meters between generated locations";
            minMinDistance.Minimum = 0;
        }

        if (distributionDef.Properties.TryGetValue("overshootFactor", out var overshoot))
        {
            overshoot.Description = "Multiplier for initial location count (e.g., 2 means generate 2x locations then filter down). Higher = better coverage but slower.";
            overshoot.Minimum = 1;
            overshoot.Maximum = 10;
        }
    }

    if (schema.Properties.TryGetValue("panoSelectionStrategy", out var panoStrategy))
    {
        panoStrategy.Description = "Strategy for selecting panorama IDs";
        panoStrategy.Enumeration.Clear();
        foreach (var strat in Enum.GetValues<PanoStrategy>())
        {
            if (strat != PanoStrategy.None)
            {
                panoStrategy.Enumeration.Add(strat.ToString());
            }
        }
    }

    if (schema.Properties.TryGetValue("acceptedCoverage", out var coverage))
    {
        coverage.Description = "Type of coverage to accept from Google Street View";
        coverage.Enumeration.Clear();
        foreach (var type in new[] { "official", "unofficial", "all" }) // #ENUM
        {
            coverage.Enumeration.Add(type);
        }
    }

    if (schema.Properties.TryGetValue("badCamStrategy", out var badCamStrategy))
    {
        badCamStrategy.Description = "How to handle bad camera generations (e.g., gen1/gen2 cameras)";
        badCamStrategy.Enumeration.Clear();
        foreach (var strat in Enum.GetValues<BadCamStrategy>())
        {
            badCamStrategy.Enumeration.Add(strat.ToString());
        }
    }

    if (schema.Properties.TryGetValue("headingMode", out var headingMode))
    {
        headingMode.Description = "How to set the camera heading/direction";
        headingMode.Enumeration.Clear();
        foreach (var mode in new[] { "Default", "Random", "InDrivingDirection", "AwayFromDrivingDirection" }) // #ENUM
        {
            headingMode.Enumeration.Add(mode);
        }
    }

    if (schema.Properties.TryGetValue("headingDelta", out var headingDelta))
    {
        headingDelta.Description = "Degrees to add to heading (used with heading modes)";
        headingDelta.Minimum = -180;
        headingDelta.Maximum = 180;
    }

    if (schema.Properties.TryGetValue("pitchMode", out var pitchMode))
    {
        pitchMode.Description = "How to set the camera pitch/tilt";
        pitchMode.Enumeration.Clear();
        foreach (var mode in new[] { "Default", "Fixed", "Random" }) // #ENUM
        {
            pitchMode.Enumeration.Add(mode);
        }
    }

    if (schema.Properties.TryGetValue("pitch", out var pitch))
    {
        pitch.Description = "Fixed pitch angle in degrees (used with pitchMode: Fixed)";
        pitch.Minimum = -90;
        pitch.Maximum = 90;
    }

    if (schema.Properties.TryGetValue("randomPitchMin", out var pitchMin))
    {
        pitchMin.Description = "Minimum pitch for random mode";
        pitchMin.Minimum = -90;
        pitchMin.Maximum = 90;
    }

    if (schema.Properties.TryGetValue("randomPitchMax", out var pitchMax))
    {
        pitchMax.Description = "Maximum pitch for random mode";
        pitchMax.Minimum = -90;
        pitchMax.Maximum = 90;
    }

    if (schema.Properties.TryGetValue("zoomMode", out var zoomMode))
    {
        zoomMode.Description = "How to set the camera zoom level";
        zoomMode.Enumeration.Clear();
        foreach (var mode in new[] { "Default", "Fixed", "Random" }) // #ENUM
        {
            zoomMode.Enumeration.Add(mode);
        }
    }

    if (schema.Properties.TryGetValue("zoom", out var zoom))
    {
        zoom.Description = "Fixed zoom level (used with zoomMode: Fixed)";
        zoom.Minimum = 0;
        zoom.Maximum = 3;
    }

    if (schema.Properties.TryGetValue("randomZoomMin", out var zoomMin))
    {
        zoomMin.Description = "Minimum zoom for random mode";
        zoomMin.Minimum = 0;
        zoomMin.Maximum = 3.6m;
    }

    if (schema.Properties.TryGetValue("randomZoomMax", out var zoomMax))
    {
        zoomMax.Description = "Maximum zoom for random mode";
        zoomMax.Minimum = 0;
        zoomMax.Maximum = 3.6m;
    }

    if (schema.Properties.TryGetValue("panoVerificationStart", out var panoStart))
    {
        panoStart.Description = "Start date for panorama verification (format: YYYY-MM-DD)";
    }

    if (schema.Properties.TryGetValue("panoVerificationEnd", out var panoEnd))
    {
        panoEnd.Description = "End date for panorama verification (format: YYYY-MM-DD)";
    }

    if (schema.Properties.TryGetValue("geoJsonFiles", out var geoJson))
    {
        geoJson.Description = "Paths to GeoJSON files defining areas to search within";
    }
}
