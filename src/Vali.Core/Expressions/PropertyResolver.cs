using Loc = Vali.Core.Location;

namespace Vali.Core.Expressions;

public sealed class PropertyResolver
{
    private readonly Dictionary<string, string> _propertyToParent;
    private readonly bool _directAccess;

    private PropertyResolver(Dictionary<string, string> propertyToParent, bool directAccess)
    {
        _propertyToParent = propertyToParent;
        _directAccess = directAccess;
    }

    public static PropertyResolver ForLocation() => new(new Dictionary<string, string>
    {
        [nameof(Loc.Osm.Surface)] = "Osm",
        [nameof(Loc.Osm.Buildings10)] = "Osm",
        [nameof(Loc.Osm.Buildings25)] = "Osm",
        [nameof(Loc.Osm.Buildings100)] = "Osm",
        [nameof(Loc.Osm.Buildings200)] = "Osm",
        [nameof(Loc.Osm.Roads0)] = "Osm",
        [nameof(Loc.Osm.Roads10)] = "Osm",
        [nameof(Loc.Osm.Roads25)] = "Osm",
        [nameof(Loc.Osm.Roads50)] = "Osm",
        [nameof(Loc.Osm.Roads100)] = "Osm",
        [nameof(Loc.Osm.Roads200)] = "Osm",
        [nameof(Loc.Osm.Tunnels10)] = "Osm",
        [nameof(Loc.Osm.Tunnels200)] = "Osm",
        [nameof(Loc.Osm.IsResidential)] = "Osm",
        [nameof(Loc.Osm.ClosestCoast)] = "Osm",
        [nameof(Loc.Osm.ClosestLake)] = "Osm",
        [nameof(Loc.Osm.ClosestRiver)] = "Osm",
        [nameof(Loc.Osm.ClosestRailway)] = "Osm",
        [nameof(Loc.Osm.HighwayType)] = "Osm",
        [nameof(Loc.Osm.HighwayTypeCount)] = "Osm",
        [nameof(Loc.Osm.WayId)] = "Osm",
        [nameof(Loc.Google.Month)] = "Google",
        [nameof(Loc.Google.Year)] = "Google",
        [nameof(Loc.Google.Lat)] = "Google",
        [nameof(Loc.Google.Lng)] = "Google",
        [nameof(Loc.Google.Heading)] = "Google",
        [nameof(Loc.Google.DrivingDirectionAngle)] = "Google",
        [nameof(Loc.Google.ArrowCount)] = "Google",
        [nameof(Loc.Google.Elevation)] = "Google",
        [nameof(Loc.Google.DescriptionLength)] = "Google",
        [nameof(Loc.Google.IsScout)] = "Google",
        [nameof(Loc.Google.ResolutionHeight)] = "Google",
        [nameof(Loc.Nominatim.CountryCode)] = "Nominatim",
        [nameof(Loc.Nominatim.SubdivisionCode)] = "Nominatim",
        [nameof(Loc.Nominatim.County)] = "Nominatim",
    }, directAccess: false);

    public static PropertyResolver ForMapCheckrLocation() => new(new Dictionary<string, string>
    {
        ["lat"] = "",
        ["lng"] = "",
        ["countryCode"] = "",
        ["arrowCount"] = "",
        ["descriptionLength"] = "",
        ["drivingDirectionAngle"] = "",
        ["heading"] = "",
        ["month"] = "",
        ["year"] = "",
        ["isScout"] = "",
        ["resolutionHeight"] = "",
        ["subdivision"] = "",
        ["panoramaCount"] = "",
        ["elevation"] = "",
    }, directAccess: true);

    public string Resolve(string propertyName, string lambdaParam)
    {
        if (_directAccess)
        {
            return $"{lambdaParam}.{propertyName}";
        }

        if (_propertyToParent.TryGetValue(propertyName, out var parent))
        {
            return $"{lambdaParam}.{parent}.{propertyName}";
        }

        throw new InvalidOperationException($"Unknown property '{propertyName}'.");
    }

    public bool IsValidProperty(string name) => _propertyToParent.ContainsKey(name);

    public IReadOnlyCollection<string> ValidPropertyNames => _propertyToParent.Keys;

    public string? FindClosestMatch(string name)
    {
        var lower = name.ToLowerInvariant();
        return _propertyToParent.Keys
            .OrderBy(k => LevenshteinDistance(k.ToLowerInvariant(), lower))
            .FirstOrDefault();
    }

    private static int LevenshteinDistance(string s, string t)
    {
        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];
        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;
        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = s[i - 1] == t[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }
}
