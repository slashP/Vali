using Spectre.Console;
using System.Text.Json;

namespace Vali.Core.Validation;

public class LiveGenerateValidator
{
    public static LiveGenerateMapDefinition? TryDeserialize(string json)
    {
        try
        {
            return Serializer.Deserialize<LiveGenerateMapDefinition>(json);
        }
        catch (JsonException e)
        {
            ConsoleLogger.Error("The JSON file is not properly formatted. Try checking if it's valid JSON on https://jsonlint.com/");
            AnsiConsole.WriteLine(e.Message);
            return null;
        }
    }
}