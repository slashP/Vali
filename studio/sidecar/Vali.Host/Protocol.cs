using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vali.Host;

public static class Json
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };
}

public sealed record Request(string? Id, string Cmd, JsonElement Params)
{
    public static Request Parse(string line)
    {
        using var document = JsonDocument.Parse(line);
        var root = document.RootElement;
        var id = root.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.String ? idElement.GetString() : null;
        var cmd = root.TryGetProperty("cmd", out var cmdElement) && cmdElement.ValueKind == JsonValueKind.String ? cmdElement.GetString() : null;
        if (string.IsNullOrEmpty(cmd))
        {
            throw new InvalidOperationException("Request has no 'cmd'.");
        }

        var parameters = root.TryGetProperty("params", out var paramsElement) ? paramsElement.Clone() : default;
        return new Request(id, cmd, parameters);
    }

    public string? GetString(string name) =>
        Params.ValueKind == JsonValueKind.Object && Params.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    public int? GetInt(string name) =>
        Params.ValueKind == JsonValueKind.Object && Params.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : null;

    public bool GetBool(string name) =>
        Params.ValueKind == JsonValueKind.Object && Params.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    public Dictionary<string, string>? GetStringDictionary(string name) =>
        Params.ValueKind == JsonValueKind.Object && Params.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object
            ? value.EnumerateObject()
                .Where(x => x.Value.ValueKind == JsonValueKind.String)
                .ToDictionary(x => x.Name, x => x.Value.GetString() ?? "")
            : null;
}

public sealed class ProtocolWriter(TextWriter stdout)
{
    private readonly object _lock = new();

    public void Respond(string? id, object result) => Write(new { id, ok = true, result });

    public void RespondError(string? id, string message, string? detail = null, string? code = null, object? data = null) =>
        Write(new { id, ok = false, error = new { message, detail, code, data } });

    public void Emit(string @event, string? id, object data) => Write(new { @event, id, data });

    private void Write(object envelope)
    {
        var json = JsonSerializer.Serialize(envelope, Json.Options);
        lock (_lock)
        {
            stdout.WriteLine(json);
        }
    }
}

/// <summary>
/// Vali.Core's validators report problems by printing to the console. The host redirects
/// Console.Out here so those messages can be returned as structured validation errors.
/// </summary>
public sealed class CaptureTextWriter : TextWriter
{
    private readonly StringBuilder _buffer = new();
    private readonly object _lock = new();

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
        lock (_lock)
        {
            _buffer.Append(value);
        }
    }

    public override void Write(string? value)
    {
        lock (_lock)
        {
            _buffer.Append(value);
        }
    }

    public string[] DrainLines()
    {
        lock (_lock)
        {
            var text = _buffer.ToString();
            _buffer.Clear();
            return text
                .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToArray();
        }
    }
}

/// <summary>
/// Serializes access to the captured console between handlers, so one command's validation
/// messages don't leak into another's response.
/// </summary>
public static class ConsoleGate
{
    public static readonly SemaphoreSlim Lock = new(1, 1);
}
