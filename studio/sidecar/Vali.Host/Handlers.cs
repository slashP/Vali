using Vali.Core;
using Vali.Core.Data;
using Vali.Core.Expressions;
using Vali.Core.Validation;

namespace Vali.Host;

public sealed class Handlers(ProtocolWriter writer, CaptureTextWriter capture)
{
    public void Ping(Request request) =>
        writer.Respond(request.Id, new
        {
            version = "0.1.0",
            dataFolder = DataDownloadService.DownloadFolder(RunMode.Default),
            expressionProperties = LocationLakeFilterer.ValidProperties(),
            expressionOperators = LocationLakeFilterer.ValidOperators()
        });

    public async Task Validate(Request request)
    {
        var json = request.GetString("json");
        if (json is null)
        {
            writer.RespondError(request.Id, "Missing 'json' parameter.");
            return;
        }

        await ConsoleGate.Lock.WaitAsync();
        try
        {
            var (valid, errors, effectiveDefinition) = ValidateCore(json);
            writer.Respond(request.Id, new { valid, errors, effectiveDefinition });
        }
        finally
        {
            ConsoleGate.Lock.Release();
        }
    }

    private (bool valid, string[] errors, MapDefinition? effectiveDefinition) ValidateCore(string json)
    {
        capture.DrainLines();
        var humanReadableError = GenerateFileValidator.HumanReadableError(json);
        if (humanReadableError is not null)
        {
            return (false, [humanReadableError], null);
        }

        var definition = GenerateFileValidator.TryDeserialize(json);
        if (definition is null)
        {
            var messages = capture.DrainLines();
            return (false, messages.Length > 0 ? messages : ["The JSON could not be read as a map definition."], null);
        }

        MapDefinition applied;
        try
        {
            applied = definition.ApplyDefaults();
        }
        catch (Exception e)
        {
            return (false, [$"Applying defaults failed: {e.Message}"], null);
        }

        var validated = applied.Validate();
        var errors = capture.DrainLines();
        return validated is not null
            ? (true, [], validated)
            : (false, errors.Length > 0 ? errors : ["Validation failed."], applied);
    }

    public void ExpressionCheck(Request request)
    {
        var expression = request.GetString("expression") ?? "";
        var context = request.GetString("context") ?? "location";
        var allowParent = request.GetBool("allowParent");
        var namedExpressions = request.GetStringDictionary("namedExpressions");
        if (namedExpressions is { Count: > 0 })
        {
            expression = ExpressionDefaults.Expand(namedExpressions, expression);
        }

        if (string.IsNullOrWhiteSpace(expression) || expression == "*")
        {
            writer.Respond(request.Id, new { valid = true, expanded = expression });
            return;
        }

        var resolver = context == "mapCheckr" ? PropertyResolver.ForMapCheckrLocation() : PropertyResolver.ForLocation();
        var error = ExpressionCompiler.Validate(expression, resolver, allowParent);
        writer.Respond(request.Id, error is null
            ? new { valid = true, expanded = expression }
            : (object)new
            {
                valid = false,
                expanded = expression,
                error = new { message = error.Message, position = error.Position, length = error.Length, detail = error.Detail }
            });
    }

    public async Task EffectiveDefinition(Request request)
    {
        var json = request.GetString("json");
        if (json is null)
        {
            writer.RespondError(request.Id, "Missing 'json' parameter.");
            return;
        }

        await ConsoleGate.Lock.WaitAsync();
        try
        {
            capture.DrainLines();
            var definition = GenerateFileValidator.TryDeserialize(json);
            if (definition is null)
            {
                writer.RespondError(request.Id, "The JSON could not be read as a map definition.", string.Join(Environment.NewLine, capture.DrainLines()));
                return;
            }

            writer.Respond(request.Id, new { definition = definition.ApplyDefaults() });
        }
        finally
        {
            ConsoleGate.Lock.Release();
        }
    }

    public void Countries(Request request) =>
        writer.Respond(request.Id, new
        {
            countries = CountryCodes.Countries
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => new
                {
                    code = x.Key,
                    name = x.Value,
                    hasSubdivisionData = SubdivisionWeights.CountryToSubdivision.ContainsKey(x.Key)
                })
        });

    public void Subdivisions(Request request)
    {
        var countryCode = (request.GetString("countryCode") ?? "").ToUpperInvariant();
        var subdivisions = SubdivisionWeights.CountryToSubdivision.GetValueOrDefault(countryCode)
                           ?? new Dictionary<string, (int weight, string subdivisionName)>();
        writer.Respond(request.Id, new
        {
            subdivisions = subdivisions
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => new { code = x.Key, name = x.Value.subdivisionName, weight = x.Value.weight })
        });
    }

    public void DataStatus(Request request)
    {
        var dataFolder = DataDownloadService.DownloadFolder(RunMode.Default);
        if (!Directory.Exists(dataFolder))
        {
            writer.Respond(request.Id, new { dataFolder, countries = Array.Empty<object>() });
            return;
        }

        var countries = Directory.GetDirectories(dataFolder)
            .Select(directory => new DirectoryInfo(directory))
            .Where(directory => directory.Name.Length == 2 && directory.Name.All(char.IsAsciiLetterUpper))
            .Select(directory =>
            {
                var files = directory.GetFiles("*.bin");
                return new
                {
                    code = directory.Name,
                    files = files.Length,
                    bytes = files.Sum(f => f.Length),
                    lastWriteUtc = files.Length > 0 ? files.Max(f => f.LastWriteTimeUtc) : (DateTime?)null
                };
            })
            .Where(x => x.files > 0)
            .OrderBy(x => x.code, StringComparer.Ordinal)
            .ToArray();
        writer.Respond(request.Id, new { dataFolder, countries });
    }
}
