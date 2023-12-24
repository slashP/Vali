using System.Text.Json;
using System.Text.Json.Nodes;
using Spectre.Console;
using Map = Vali.Core.MapDefinition;

namespace Vali.Core.Validation;

public static class GenerateFileValidator
{
    public static Map? Validate(this Map mapDefinition) =>
        mapDefinition
            .ValidateDistribution()
            ?.ValidateFilters()
            ?.ValidatePreferenceFilters()
            ?.ValidateDistributionStrategy()
            ?.ValidateInclusions()
            ?.ValidateExclusions()
            ?.ValidateOutput();

    public static async Task<string?> ReadFile(string path)
    {
        if (!File.Exists(path))
        {
            ConsoleLogger.Error($"File {path} does not exist.");
            return null;
        }

        var json = await File.ReadAllTextAsync(path);
        return json;
    }

    public static Map? TryDeserialize(string json)
    {
        try
        {
            return Serializer.Deserialize<Map>(json);
        }
        catch (JsonException e)
        {
            ConsoleLogger.Error("The JSON file is not properly formatted. Try checking if it's valid JSON on https://jsonlint.com/");
            AnsiConsole.WriteLine(e.Message);
            return null;
        }
    }

    public static string? HumanReadableError(string json)
    {
        JsonNode jsonNode;
        var wrongJsonError = "The JSON file is not properly formatted. Try checking if it's valid JSON on https://jsonlint.com/";
        try
        {
            var node = JsonNode.Parse(json, new JsonNodeOptions
            {
                PropertyNameCaseInsensitive = true
            }, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });
            if (node == null)
            {
                return wrongJsonError;
            }

            jsonNode = node;
        }
        catch (JsonException)
        {
            return wrongJsonError;
        }

        if (jsonNode.GetValueKind() == JsonValueKind.Array)
        {
            return "The JSON file contains an array i.e. starts with [, but should be an object - i.e. start with {";
        }

        if (jsonNode.GetValueKind() is JsonValueKind.Undefined or JsonValueKind.False or JsonValueKind.True
            or JsonValueKind.Null or JsonValueKind.Number or JsonValueKind.String)
        {
            return "The JSON file must be an object - i.e. start with {";
        }

        var countryCodes = jsonNode[nameof(Map.CountryCodes)];
        if (countryCodes is not null && countryCodes.GetValueKind() != JsonValueKind.Array)
        {
            return $@"{nameof(Map.CountryCodes).FirstCharToLowerCase()} must be an array. F.ex. [""IT""]";
        }

        var subdivisionInclusions = jsonNode[nameof(Map.SubdivisionInclusions)];
        if (subdivisionInclusions is not null &&
            (subdivisionInclusions.GetValueKind() != JsonValueKind.Object ||
             subdivisionInclusions.FirstObjectValueKind() != JsonValueKind.Array))
        {
            return $$"""
                     {{nameof(Map.SubdivisionInclusions).FirstCharToLowerCase()}} does not have the correct format. Correct example:
                     {
                       "FR": ["FR-11"]
                     }
                     """;
        }

        var subdivisionExclusions = jsonNode[nameof(Map.SubdivisionExclusions)];
        if (subdivisionExclusions is not null &&
            (subdivisionExclusions.GetValueKind() != JsonValueKind.Object ||
             subdivisionExclusions.FirstObjectValueKind() != JsonValueKind.Array))
        {
            return $$"""
                     {{nameof(Map.SubdivisionExclusions).FirstCharToLowerCase()}} does not have the correct format. Correct example:
                     {
                       "FR": ["FR-11"]
                     }
                     """;
        }

        var countryDistribution = jsonNode[nameof(Map.CountryDistribution)];
        if (countryDistribution is not null &&
            (countryDistribution.GetValueKind() != JsonValueKind.Object ||
             countryDistribution.FirstObjectValueKind() != JsonValueKind.Number ||
             !int.TryParse(countryDistribution.FirstOrDefaultObject()?.AsValue().ToJsonString(), out _)))
        {
            return $$"""
                     {{nameof(Map.CountryDistribution).FirstCharToLowerCase()}} does not have the correct format. NB: Only integers. Correct example:
                     {
                       "FR": 12,
                       "IT": 10
                     }
                     """;
        }

        var subdivisionDistribution = jsonNode[nameof(Map.SubdivisionDistribution)];
        if (subdivisionDistribution != null &&
            subdivisionDistribution.ToJsonString() != "{}" &&
            (subdivisionDistribution.GetValueKind() != JsonValueKind.Object ||
             subdivisionDistribution.FirstObjectValueKind() != JsonValueKind.Object ||
             subdivisionDistribution.FirstOrDefaultObject()?.FirstObjectValueKind() != JsonValueKind.Number ||
             !int.TryParse(subdivisionDistribution.FirstOrDefaultObject()?.FirstOrDefaultObject()?.AsValue().ToJsonString(), out _)))
        {
            return $$"""
                     {{nameof(Map.SubdivisionDistribution).FirstCharToLowerCase()}} does not have the correct format. NB: Only integers. Correct example:
                     {
                       "FR": {
                         "FR-11": 12,
                         "FR-28": 25
                       }
                     }
                     """;
        }

        return null;
    }

    private static JsonValueKind? FirstObjectValueKind(this JsonNode node) => node.FirstOrDefaultObject()?.GetValueKind();
    private static JsonNode? FirstOrDefaultObject(this JsonNode node) => node.AsObject().FirstOrDefault().Value;
}