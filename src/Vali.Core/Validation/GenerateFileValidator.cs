using System.Text.Json;
using Spectre.Console;

namespace Vali.Core.Validation;

public static class GenerateFileValidator
{
    public static MapDefinition? Validate(this MapDefinition mapDefinition) =>
        mapDefinition
            .ValidateDistribution()
            ?.ValidateFilters()
            ?.ValidatePreferenceFilters()
            ?.ValidateDistributionStrategy()
            ?.ValidateInclusions()
            ?.ValidateExclusions();

    public static async Task<MapDefinition?> TryDeserialize(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        if (!File.Exists(path))
        {
            ConsoleLogger.Error($"File {path} does not exist.");
            return null;
        }

        try
        {
            return Serializer.Deserialize<MapDefinition>(json);
        }
        catch (JsonException e)
        {
            ConsoleLogger.Error("The JSON file is not properly formatted. Try checking if it's valid JSON on https://jsonlint.com/");
            AnsiConsole.WriteLine(e.Message);
            return null;
        }
    }
}