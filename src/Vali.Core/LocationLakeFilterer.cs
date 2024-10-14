using System.Collections.Concurrent;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Vali.Core.Hash;
using Loc = Vali.Core.Location;

namespace Vali.Core;

public static class LocationLakeFilterer
{
    public static readonly string[] CountryCodesAcceptableWithoutDescription =
    [
        "CX", "CC", "MP", "GU", "EG", "ML", "MG", "PN", "GL", "MN", "KR", "FO", "UG", "KG", "RW", "CR", "LB", "RE",
        "MQ", "NP", "PK", "BY", "UM"
    ];

    public static readonly string[] SubdivisionCodesAcceptableWithoutDescription =
    [
        "NO-21", "CA-NU", "US-AK", "BR-PE"
    ];

    public static Loc[] Filter(
        Loc[] locationsFromFile,
        string? locationFilterExpression,
        MapDefinition mapDefinition)
    {
        List<Func<Loc, bool>> defaultFilterSelectors =
        [
            x => x.Google.PanoId.Length < 36
        ];
        if (string.IsNullOrEmpty(locationFilterExpression) || !locationFilterExpression.Contains("Tunnels"))
        {
            defaultFilterSelectors.Add(x => x.Osm.Tunnels10 == 0);
        }

        if (string.IsNullOrEmpty(locationFilterExpression) || !locationFilterExpression.Contains(nameof(Loc.Google.DescriptionLength)))
        {
            defaultFilterSelectors.Add(x => x.Google.DescriptionLength is null or > 0 ||
                                            CountryCodesAcceptableWithoutDescription.Contains(x.Nominatim.CountryCode) ||
                                            SubdivisionCodesAcceptableWithoutDescription.Contains(x.Nominatim.SubdivisionCode));
        }

        var locations = defaultFilterSelectors.Aggregate(locationsFromFile.AsEnumerable(), (current, defaultFilterSelector) => current.Where(defaultFilterSelector));

        if (!string.IsNullOrEmpty(locationFilterExpression))
        {
            var typedExpression = CompileBoolLocationExpression(locationFilterExpression);
            locations = locations.Where(typedExpression);
        }

        if (mapDefinition.ProximityFilter.Radius > 0 && File.Exists(mapDefinition.ProximityFilter.LocationsPath))
        {
            locations = FilterByProximity(locations, mapDefinition.ProximityFilter);
        }

        if (mapDefinition.NeighbourFilter.Radius > 0)
        {
            locations = FilterByNeighbours(locations, locationsFromFile, mapDefinition.NeighbourFilter);
        }

        return locations.ToArray();
    }

    private static readonly ConcurrentDictionary<string, Func<Loc, int>> _cacheInt = new();

    private static readonly ConcurrentDictionary<string, Func<Loc, bool>> _cacheBool = new();

    public static Func<Loc, int> CompileIntLocationExpression(string initialExpression)
    {
        if (_cacheInt.TryGetValue(initialExpression, out var selector))
        {
            return selector;
        }

        var func = CompileLocationExpression(initialExpression, 0);
        _cacheInt.TryAdd(initialExpression, func);
        return func;
    }

    public static Func<Loc, bool> CompileBoolLocationExpression(string initialExpression)
    {
        if (_cacheBool.TryGetValue(initialExpression, out var selector))
        {
            return selector;
        }

        var func = CompileLocationExpression(initialExpression, true);
        _cacheBool.TryAdd(initialExpression, func);
        return func;
    }

    public static Func<Loc, T> CompileLocationExpression<T>(string initialExpression, T fallback)
    {
        if (initialExpression == "*")
        {
            return _ => fallback;
        }

        var (expressionWithPlaceholders, placeHolders) = initialExpression.ReplaceValuesInSingleQuotesWithPlaceHolders();
        var componentsInExpression = expressionWithPlaceholders
            .RemoveMultipleSpaces()
            .RemoveParentheses()
            .Split(' ')
            .ToArray();
        var totalExpression = ValidProperties()
            .Intersect(componentsInExpression.Select(x => x.Trim()))
            .Aggregate(expressionWithPlaceholders.SpacePadParentheses().SpacePad(), (current, validProperty) => current.Replace(validProperty.SpacePad(), LambdaExpressionFromProperty(validProperty).SpacePad()));
        var validOperators = ValidOperators();
        totalExpression = validOperators
            .Intersect(componentsInExpression.Select(x => x.Trim()))
            .Aggregate(totalExpression, (current, validOperator) => current.Replace(validOperator.SpacePad(), CSharpOperatorFromOperator(validOperator).SpacePad()));
        totalExpression = totalExpression.Replace("'", "\"");
        foreach (var placeHolder in placeHolders)
        {
            totalExpression = totalExpression.Replace(placeHolder.newValue, placeHolder.oldValue);
        }

        totalExpression = totalExpression.Replace("\\'", "'");
        var parameter = Expression.Parameter(typeof(Loc), "x");
        var expression = (Expression)DynamicExpressionParser.ParseLambda(new[] { parameter }, null, totalExpression);
        var typedExpression = ((Expression<Func<Loc, T>>)expression).Compile();
        return typedExpression;
    }

    public static IEnumerable<string> ValidProperties() => new[]
    {
        nameof(Loc.Osm.Surface),
        nameof(Loc.Osm.Buildings10),
        nameof(Loc.Osm.Buildings25),
        nameof(Loc.Osm.Buildings100),
        nameof(Loc.Osm.Buildings200),
        nameof(Loc.Osm.Roads0),
        nameof(Loc.Osm.Roads10),
        nameof(Loc.Osm.Roads25),
        nameof(Loc.Osm.Roads50),
        nameof(Loc.Osm.Roads100),
        nameof(Loc.Osm.Roads200),
        nameof(Loc.Osm.Tunnels10),
        nameof(Loc.Osm.Tunnels200),
        nameof(Loc.Osm.IsResidential),
        nameof(Loc.Osm.ClosestCoast),
        nameof(Loc.Osm.ClosestLake),
        nameof(Loc.Osm.ClosestRiver),
        nameof(Loc.Osm.ClosestRailway),
        nameof(Loc.Osm.HighwayType),
        nameof(Loc.Osm.HighwayTypeCount),
        nameof(Loc.Google.Month),
        nameof(Loc.Google.Year),
        nameof(Loc.Google.Lat),
        nameof(Loc.Google.Lng),
        nameof(Loc.Google.Heading),
        nameof(Loc.Google.DrivingDirectionAngle),
        nameof(Loc.Google.ArrowCount),
        nameof(Loc.Google.Elevation),
        nameof(Loc.Google.DescriptionLength),
        nameof(Loc.Nominatim.CountryCode),
        nameof(Loc.Nominatim.SubdivisionCode),
        nameof(Loc.Nominatim.County),
    };

    public static IEnumerable<string> ValidOperators() => new[]
    {
        "eq",
        "neq",
        "lt",
        "lte",
        "gt",
        "gte",
        "and",
        "or",
        "+",
        "-",
        "/",
        "*",
    };

    private static string LambdaExpressionFromProperty(string property) =>
        property switch
        {
            nameof(Loc.Osm.Surface) => $"x.Osm.{nameof(Loc.Osm.Surface)}",
            nameof(Loc.Osm.Buildings100) => $"x.Osm.{nameof(Loc.Osm.Buildings100)}",
            nameof(Loc.Osm.Buildings200) => $"x.Osm.{nameof(Loc.Osm.Buildings200)}",
            nameof(Loc.Osm.Buildings25) => $"x.Osm.{nameof(Loc.Osm.Buildings25)}",
            nameof(Loc.Osm.Roads100) => $"x.Osm.{nameof(Loc.Osm.Roads100)}",
            nameof(Loc.Osm.Roads200) => $"x.Osm.{nameof(Loc.Osm.Roads200)}",
            nameof(Loc.Osm.Roads25) => $"x.Osm.{nameof(Loc.Osm.Roads25)}",
            nameof(Loc.Osm.Roads50) => $"x.Osm.{nameof(Loc.Osm.Roads50)}",
            nameof(Loc.Osm.Tunnels10) => $"x.Osm.{nameof(Loc.Osm.Tunnels10)}",
            nameof(Loc.Osm.Tunnels200) => $"x.Osm.{nameof(Loc.Osm.Tunnels200)}",
            nameof(Loc.Osm.Buildings10) => $"x.Osm.{nameof(Loc.Osm.Buildings10)}",
            nameof(Loc.Osm.IsResidential) => $"x.Osm.{nameof(Loc.Osm.IsResidential)}",
            nameof(Loc.Osm.Roads10) => $"x.Osm.{nameof(Loc.Osm.Roads10)}",
            nameof(Loc.Osm.Roads0) => $"x.Osm.{nameof(Loc.Osm.Roads0)}",
            nameof(Loc.Osm.ClosestCoast) => $"x.Osm.{nameof(Loc.Osm.ClosestCoast)}",
            nameof(Loc.Osm.ClosestLake) => $"x.Osm.{nameof(Loc.Osm.ClosestLake)}",
            nameof(Loc.Osm.ClosestRiver) => $"x.Osm.{nameof(Loc.Osm.ClosestRiver)}",
            nameof(Loc.Osm.ClosestRailway) => $"x.Osm.{nameof(Loc.Osm.ClosestRailway)}",
            nameof(Loc.Osm.HighwayType) => $"x.Osm.{nameof(Loc.Osm.HighwayType)}",
            nameof(Loc.Osm.HighwayTypeCount) => $"x.Osm.{nameof(Loc.Osm.HighwayTypeCount)}",
            nameof(Loc.Google.Month) => $"x.Google.{nameof(Loc.Google.Month)}",
            nameof(Loc.Google.Year) => $"x.Google.{nameof(Loc.Google.Year)}",
            nameof(Loc.Google.Lat) => $"x.Google.{nameof(Loc.Google.Lat)}",
            nameof(Loc.Google.Lng) => $"x.Google.{nameof(Loc.Google.Lng)}",
            nameof(Loc.Google.Heading) => $"x.Google.{nameof(Loc.Google.Heading)}",
            nameof(Loc.Google.DrivingDirectionAngle) => $"x.Google.{nameof(Loc.Google.DrivingDirectionAngle)}",
            nameof(Loc.Google.ArrowCount) => $"x.Google.{nameof(Loc.Google.ArrowCount)}",
            nameof(Loc.Google.Elevation) => $"x.Google.{nameof(Loc.Google.Elevation)}",
            nameof(Loc.Google.DescriptionLength) => $"x.Google.{nameof(Loc.Google.DescriptionLength)}",
            nameof(Loc.Nominatim.CountryCode) => $"x.Nominatim.{nameof(Loc.Nominatim.CountryCode)}",
            nameof(Loc.Nominatim.SubdivisionCode) => $"x.Nominatim.{nameof(Loc.Nominatim.SubdivisionCode)}",
            nameof(Loc.Nominatim.County) => $"x.Nominatim.{nameof(Loc.Nominatim.County)}",
            _ => throw new ArgumentOutOfRangeException(nameof(property), property, null)
        };

    private static string CSharpOperatorFromOperator(string @operator) => @operator.ToLowerInvariant() switch
    {
        "eq" => "==",
        "neq" => "!=",
        "lt" => "<",
        "lte" => "<=",
        "gt" => ">",
        "gte" => ">=",
        "and" => "&&",
        "or" => "||",
        "+" => "+",
        "-" => "-",
        "/" => "/",
        "*" => "*",
        _ => throw new ArgumentOutOfRangeException(nameof(@operator), $"operator {@operator} not implemented.")
    };

    private static IEnumerable<Loc> FilterByProximity(IEnumerable<Loc> locations, ProximityFilter proximityFilter)
    {
        var proximityLocations = LocationReader.DeserializeLocationsFromFile(proximityFilter.LocationsPath)
            .Where(x => x.countryCode == locations.First().Nominatim.CountryCode)
            .ToArray();
        var proximityFilterRadius = proximityFilter.Radius;
        return locations.Where(l => proximityLocations.Any(x => Extensions.ApproximateDistance(l.Lat, l.Lng, x.lat, x.lng) < proximityFilterRadius));
    }

    private static IEnumerable<Loc> FilterByNeighbours(IEnumerable<Loc> locations, Loc[] allLocations, NeighbourFilter neighbourFilter)
    {
        var filterExpression = string.IsNullOrEmpty(neighbourFilter.Expression)
            ? _ => true
            : CompileBoolLocationExpression(neighbourFilter.Expression);
        var directions = neighbourFilter.InEitherCardinalDirection ? Enum.GetValues<CardinalDirection>().Cast<CardinalDirection?>().ToArray() : [null];
        return neighbourFilter.Limit switch
        {
            0 => locations
                .AsParallel()
                .Where(l => directions.Any(d => !allLocations.Any(l2 =>
                    IsInDirection(d, l, l2) &&
                    Extensions.ApproximateDistance(l.Lat, l.Lng, l2.Lat, l2.Lng) <= neighbourFilter.Radius &&
                    filterExpression(l2)))),
            _ => locations
                .AsParallel()
                .Where(l => directions.Any(d => allLocations.Count(l2 =>
                    IsInDirection(d, l, l2) &&
                    Extensions.ApproximateDistance(l.Lat, l.Lng, l2.Lat, l2.Lng) <= neighbourFilter.Radius &&
                    filterExpression(l2)) <= neighbourFilter.Limit))
        };
    }

    private static bool IsInDirection(CardinalDirection? direction, Loc x1, Loc x2)
    {
        return direction switch
        {
            CardinalDirection.West => x1.Lng < x2.Lng,
            CardinalDirection.East => x1.Lng > x2.Lng,
            CardinalDirection.North => x1.Lat > x2.Lat,
            CardinalDirection.South => x1.Lat < x2.Lat,
            _ => true
        };
    }
}