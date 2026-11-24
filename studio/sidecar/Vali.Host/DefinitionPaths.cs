using System.Text.Json;
using System.Text.Json.Nodes;

namespace Vali.Host;

/// <summary>
/// Rewrites relative file references inside a map definition to absolute paths
/// anchored at the definition's own folder. The CLI resolves proximityFilter
/// <c>locationsPath</c> / geometryFilter <c>filePath</c> relative to the process
/// working directory — typically the definition's folder when you run
/// <c>vali generate</c> there. Studio's engine is a long-lived sidecar with an
/// unrelated CWD (e.g. "/" for a Finder-launched macOS app), so without this a
/// definition that works in the CLI would fail validation ("File border.geojson
/// used in a geometryFilter does not exist") — or silently match a same-named
/// file in the sidecar's CWD. Rewriting a working copy (never the stored/edited
/// text) restores the CLI's semantics.
/// </summary>
public static class DefinitionPaths
{
    // Filter properties whose string value is a path to a user file on disk.
    private static readonly HashSet<string> PathProperties = new(StringComparer.Ordinal)
    {
        "filePath",     // geometryFilter
        "locationsPath" // proximityFilter
    };

    public static string ResolveRelativePaths(string json, string? baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            return json;
        }

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
        }
        catch
        {
            // Not parseable — let the normal validator produce the error message.
            return json;
        }

        if (root is null)
        {
            return json;
        }

        return Rewrite(root, baseDirectory) ? root.ToJsonString() : json;
    }

    private static bool Rewrite(JsonNode node, string baseDirectory)
    {
        var changed = false;
        switch (node)
        {
            case JsonObject obj:
                foreach (var (key, value) in obj.ToArray())
                {
                    if (value is JsonValue jsonValue &&
                        PathProperties.Contains(key) &&
                        jsonValue.TryGetValue<string>(out var path) &&
                        !string.IsNullOrWhiteSpace(path) &&
                        !Path.IsPathRooted(path))
                    {
                        obj[key] = Path.GetFullPath(Path.Combine(baseDirectory, path));
                        changed = true;
                    }
                    else if (value is not null)
                    {
                        changed |= Rewrite(value, baseDirectory);
                    }
                }

                break;
            case JsonArray array:
                foreach (var item in array)
                {
                    if (item is not null)
                    {
                        changed |= Rewrite(item, baseDirectory);
                    }
                }

                break;
        }

        return changed;
    }
}
