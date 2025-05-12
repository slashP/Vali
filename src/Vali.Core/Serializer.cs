using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vali.Core;

public static class Serializer
{
    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
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
    public static Task SerializeAsync<T>(Stream stream, T obj) => JsonSerializer.SerializeAsync(stream, obj, JsonSerializerOptions);
    public static Task PrettySerializeAsync<T>(Stream stream, T obj) => JsonSerializer.SerializeAsync(stream, obj, PrettyPrintJsonSerializerOptions);

    public static string PrettySerialize<T>(T obj) => JsonSerializer.Serialize(obj, PrettyPrintJsonSerializerOptions);

    public static T? Deserialize<T>(string res) => JsonSerializer.Deserialize<T>(res, JsonSerializerOptions);
    public static ValueTask<T?> DeserializeAsync<T>(Stream stream) => JsonSerializer.DeserializeAsync<T>(stream, JsonSerializerOptions);
}
