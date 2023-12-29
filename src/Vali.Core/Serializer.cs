using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vali.Core;

public static class Serializer
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private static readonly JsonSerializerOptions PrettyPrintJsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static string Serialize<T>(T obj) => JsonSerializer.Serialize(obj, JsonSerializerOptions);

    public static string PrettySerialize<T>(T obj) => JsonSerializer.Serialize(obj, PrettyPrintJsonSerializerOptions);

    public static T? Deserialize<T>(string res) => JsonSerializer.Deserialize<T>(res, JsonSerializerOptions);
}
