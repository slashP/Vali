using System.Text.RegularExpressions;

namespace Vali.Core;

/// <summary>
/// Resolves the file paths in a map definition: relative paths against the map-definitions root,
/// a leading <c>{name}</c> token through a token map, and either separator on any OS.
/// </summary>
public static class MapDefinitionPaths
{
    private static readonly Regex DrivePath = new(@"^[A-Za-z]:[\\/]");
    private static readonly Regex LeadingToken = new(@"^\{([^{}]*)\}");

    public static string ResolvePath(string path, string definitionsRoot, IReadOnlyDictionary<string, string> tokens)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        var token = LeadingToken.Match(path);
        if (token.Success)
        {
            var name = token.Groups[1].Value;
            if (!tokens.TryGetValue(name, out var value))
            {
                var known = tokens.Count == 0 ? "none" : string.Join(", ", tokens.Keys.Select(k => $"{{{k}}}"));
                throw new MapDefinitionPathException($"Unknown token {{{name}}} in '{path}'. Known tokens: {known}.");
            }

            var rest = Normalise(path[token.Length..]).TrimStart(Path.DirectorySeparatorChar);
            return rest.Length == 0 ? value : Path.Combine(value, rest);
        }

        if (!OperatingSystem.IsWindows() && (DrivePath.IsMatch(path) || path.StartsWith(@"\\", StringComparison.Ordinal)))
        {
            throw new MapDefinitionPathException($"'{path}' is a Windows absolute path; use a path relative to the map-definitions root or a {{mapData}} token.");
        }

        var normalised = Normalise(path);
        return Path.IsPathRooted(normalised)
            ? normalised
            : Path.GetFullPath(Path.Combine(definitionsRoot, normalised));
    }

    /// <summary>A copy of <paramref name="definition"/> with every path field resolved. The input is not modified.</summary>
    public static MapDefinition Resolve(MapDefinition definition, string definitionsRoot, IReadOnlyDictionary<string, string> tokens) =>
        Map(definition, (field, path) =>
        {
            try
            {
                return ResolvePath(path, definitionsRoot, tokens);
            }
            catch (MapDefinitionPathException e)
            {
                throw new MapDefinitionPathException($"{field}: {e.Message}");
            }
        });

    /// <summary>True when any path field holds an absolute path, for this OS or a Windows one.</summary>
    public static bool HasRootedPaths(MapDefinition definition)
    {
        var rooted = false;
        Map(definition, (_, path) =>
        {
            rooted |= !string.IsNullOrEmpty(path) &&
                      (Path.IsPathRooted(path) || DrivePath.IsMatch(path) || path.StartsWith(@"\\", StringComparison.Ordinal));
            return path;
        });
        return rooted;
    }

    private static string Normalise(string path) =>
        path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

    private static MapDefinition Map(MapDefinition d, Func<string, string, string> f)
    {
        ProximityFilter Proximity(string field, ProximityFilter p) =>
            p with { LocationsPath = f($"{field}.locationsPath", p.LocationsPath) };

        GeometryFilter[] Geometries(string field, GeometryFilter[] filters) =>
            filters.Select((g, i) => g with { FilePath = f($"{field}[{i}].filePath", g.FilePath) }).ToArray();

        string[] Files(string field, string[] paths) =>
            paths.Select((p, i) => f($"{field}[{i}]", p)).ToArray();

        LocationPreferenceFilter[] Preferences(string field, LocationPreferenceFilter[] filters) =>
            filters.Select((p, i) => p with
            {
                ProximityFilter = Proximity($"{field}[{i}].proximityFilter", p.ProximityFilter),
                GeometryFilters = Geometries($"{field}[{i}].geometryFilters", p.GeometryFilters),
            }).ToArray();

        Dictionary<string, T> PerKey<T>(string field, Dictionary<string, T> values, Func<string, T, T> map) =>
            values.ToDictionary(kv => kv.Key, kv => map($"{field}[{kv.Key}]", kv.Value));

        Dictionary<string, Dictionary<string, T>> PerSubdivision<T>(string field, Dictionary<string, Dictionary<string, T>> values, Func<string, T, T> map) =>
            PerKey(field, values, (countryField, subdivisions) => PerKey(countryField, subdivisions, map));

        return d with
        {
            ProximityFilter = Proximity("proximityFilter", d.ProximityFilter),
            CountryProximityFilters = PerKey("countryProximityFilters", d.CountryProximityFilters, Proximity),
            SubdivisionProximityFilters = PerSubdivision("subdivisionProximityFilters", d.SubdivisionProximityFilters, Proximity),
            GeometryFilters = Geometries("geometryFilters", d.GeometryFilters),
            CountryGeometryFilters = PerKey("countryGeometryFilters", d.CountryGeometryFilters, Geometries),
            SubdivisionGeometryFilters = PerSubdivision("subdivisionGeometryFilters", d.SubdivisionGeometryFilters, Geometries),
            GlobalLocationPreferenceFilters = Preferences("globalLocationPreferenceFilters", d.GlobalLocationPreferenceFilters),
            CountryLocationPreferenceFilters = PerKey("countryLocationPreferenceFilters", d.CountryLocationPreferenceFilters, Preferences),
            SubdivisionLocationPreferenceFilters = PerSubdivision("subdivisionLocationPreferenceFilters", d.SubdivisionLocationPreferenceFilters, Preferences),
            UsedLocationsPaths = Files("usedLocationsPaths", d.UsedLocationsPaths),
            GlobalExternalDataFiles = Files("globalExternalDataFiles", d.GlobalExternalDataFiles),
            CountryExternalDataFiles = PerKey("countryExternalDataFiles", d.CountryExternalDataFiles, Files),
            SubdivisionExternalDataFiles = PerSubdivision("subdivisionExternalDataFiles", d.SubdivisionExternalDataFiles, Files),
        };
    }
}

public class MapDefinitionPathException(string message) : Exception(message);
