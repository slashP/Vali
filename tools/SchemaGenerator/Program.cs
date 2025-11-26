using NJsonSchema;
using NJsonSchema.Generation;
using Vali.Core;

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

// Generate schema for MapDefinition (standard generate command)
var mapDefinitionSchema = JsonSchema.FromType<MapDefinition>(settings);
mapDefinitionSchema.Title = "Vali Map Definition";
mapDefinitionSchema.Description = "Configuration for generating GeoGuessr maps using Vali";
mapDefinitionSchema.AllowAdditionalProperties = true; // Allow $schema property

// Add custom descriptions and constraints
EnhanceMapDefinitionSchema(mapDefinitionSchema);

// Generate schema for LiveGenerateMapDefinition
var liveGenerateSchema = JsonSchema.FromType<LiveGenerateMapDefinition>(settings);
liveGenerateSchema.Title = "Vali Live Generate Map Definition";
liveGenerateSchema.Description = "Configuration for live-generating GeoGuessr maps using Google API";
liveGenerateSchema.AllowAdditionalProperties = true; // Allow $schema property

EnhanceLiveGenerateSchema(liveGenerateSchema);

// Output paths
var outputDir = Path.Combine("..", "..", "src", "Vali", "Schemas");
Directory.CreateDirectory(outputDir);

var mapDefinitionPath = Path.Combine(outputDir, "vali.schema.json");
var liveGeneratePath = Path.Combine(outputDir, "vali-live-generate.schema.json");

await File.WriteAllTextAsync(mapDefinitionPath, mapDefinitionSchema.ToJson());
await File.WriteAllTextAsync(liveGeneratePath, liveGenerateSchema.ToJson());

Console.WriteLine($"✓ Generated: {Path.GetFullPath(mapDefinitionPath)}");
Console.WriteLine($"✓ Generated: {Path.GetFullPath(liveGeneratePath)}");
Console.WriteLine("Schemas generated successfully!");

static void EnhanceMapDefinitionSchema(JsonSchema schema)
{
    // Add descriptions for key properties
    if (schema.Properties.TryGetValue("countryCodes", out var countryCodes))
    {
        countryCodes.Description = "ISO 3166-1 alpha-2 country codes, or special keywords like 'europe', 'asia', 'africa', '*', etc.";
        countryCodes.MinItems = 1;
    }

    if (schema.Properties.TryGetValue("distributionStrategy", out var distStrategy))
    {
        distStrategy.Description = "Strategy for distributing locations across regions";
    }

    // Enhance DistributionStrategy definition
    if (schema.Definitions.TryGetValue("DistributionStrategy", out var distStrategyDef))
    {
        distStrategyDef.AllowAdditionalProperties = true;

        if (distStrategyDef.Properties.TryGetValue("key", out var key))
        {
            key.Description = "Distribution algorithm to use";
            key.Enumeration.Clear();
            key.Enumeration.Add("FixedCountByMaxMinDistance");
            key.Enumeration.Add("MaxCountByFixedMinDistance");
            key.Enumeration.Add("EvenlyByDistanceWithinCountry");
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
            foreach (var dist in new[] { "aarw", "aaw", "acw", "abw", "aiw", "proworld", "aow", "rainboltworld", "geotime", "lerg" })
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

    // Enhance LocationOutput definition
    if (schema.Definitions.TryGetValue("LocationOutput", out var outputDef))
    {
        outputDef.AllowAdditionalProperties = true;

        if (outputDef.Properties.TryGetValue("panoVerificationStrategy", out var panoStrategy))
        {
            panoStrategy.Description = "Strategy for selecting panorama IDs";
            panoStrategy.Enumeration.Clear();
            foreach (var strat in new[] { "Newest", "Random", "RandomNotNewest", "RandomAvoidNewest", "RandomNotOldest", "RandomAvoidOldest", "SecondNewest", "Oldest", "SecondOldest" })
            {
                panoStrategy.Enumeration.Add(strat);
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

    // Enhance NeighborFilter definition
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
            foreach (var b in new[] { "gte", "lte", "all", "none", "some", "percentage-gte", "percentage-lte" })
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

    // Enhance GeometryFilter definition
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
            inclusionMode.Enumeration.Add("include");
            inclusionMode.Enumeration.Add("exclude");
        }

        if (geometryFilterDef.Properties.TryGetValue("combinationMode", out var combinationMode))
        {
            combinationMode.Description = "How to combine multiple geometry filters (first filter only)";
            combinationMode.Enumeration.Clear();
            combinationMode.Enumeration.Add("intersection");
            combinationMode.Enumeration.Add("union");
        }
    }
}

static void EnhanceLiveGenerateSchema(JsonSchema schema)
{
    if (schema.Properties.TryGetValue("countries", out var countries))
    {
        countries.Description = "Countries with their location count goals (e.g., {\"NO\": 1000, \"SE\": 2000})";
    }

    if (schema.Properties.TryGetValue("panoSelectionStrategy", out var panoStrategy))
    {
        panoStrategy.Description = "Strategy for selecting panorama IDs";
        panoStrategy.Enumeration.Clear();
        foreach (var strat in new[] { "Newest", "Random", "RandomNotNewest", "RandomAvoidNewest", "RandomNotOldest", "RandomAvoidOldest", "SecondNewest", "Oldest", "SecondOldest" })
        {
            panoStrategy.Enumeration.Add(strat);
        }
    }

    if (schema.Properties.TryGetValue("acceptedCoverage", out var coverage))
    {
        coverage.Description = "Type of coverage to accept";
        coverage.Enumeration.Clear();
        coverage.Enumeration.Add("official");
        coverage.Enumeration.Add("unofficial");
        coverage.Enumeration.Add("all");
    }

    if (schema.Properties.TryGetValue("badCamStrategy", out var badCamStrategy))
    {
        badCamStrategy.Description = "Strategy for handling bad camera generations";
        badCamStrategy.Enumeration.Clear();
        badCamStrategy.Enumeration.Add("Skip");
        badCamStrategy.Enumeration.Add("Include");
    }

    if (schema.Properties.TryGetValue("headingMode", out var headingMode))
    {
        headingMode.Enumeration.Clear();
        headingMode.Enumeration.Add("Default");
        headingMode.Enumeration.Add("Random");
        headingMode.Enumeration.Add("InDrivingDirection");
        headingMode.Enumeration.Add("AwayFromDrivingDirection");
    }

    if (schema.Properties.TryGetValue("pitchMode", out var pitchMode))
    {
        pitchMode.Enumeration.Clear();
        pitchMode.Enumeration.Add("Default");
        pitchMode.Enumeration.Add("Fixed");
        pitchMode.Enumeration.Add("Random");
    }

    if (schema.Properties.TryGetValue("zoomMode", out var zoomMode))
    {
        zoomMode.Enumeration.Clear();
        zoomMode.Enumeration.Add("Default");
        zoomMode.Enumeration.Add("Fixed");
        zoomMode.Enumeration.Add("Random");
    }
}
