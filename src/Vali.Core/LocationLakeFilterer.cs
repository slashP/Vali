using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Vali.Core.Google;
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
        IReadOnlyCollection<Loc> locationsFromFile,
        Dictionary<string, List<Loc>> neighborLocationBuckets,
        Dictionary<string, List<ILatLng>> proximityLocationBuckets,
        string? locationFilterExpression,
        ProximityFilter proximityFilter,
        NeighborFilter[] neighborFilters,
        MapDefinition mapDefinition)
    {
        List<Func<Loc, bool>> defaultFilterSelectors = mapDefinition.EnableDefaultLocationFilters
            ?
            [
                x => x.Nominatim.CountryCode switch
                {
                    "FI" => x.Google.ResolutionHeight >= Resolution.Gen4 || x.Google.Year < 2022,
                    "EC" => x.Google.ResolutionHeight >= Resolution.Gen4 || x.Google.Year < 2021,
                    "NG" => x.Google.ResolutionHeight >= Resolution.Gen4 || x.Google.Year < 2021,
                    _ => true
                }
            ]
            : [];

        if (string.IsNullOrEmpty(locationFilterExpression) || !locationFilterExpression.Contains("Tunnels"))
        {
            defaultFilterSelectors.Add(x => x.Osm.Tunnels10 == 0);
        }

        if (string.IsNullOrEmpty(locationFilterExpression) || (!locationFilterExpression.Contains(nameof(Loc.Google.DescriptionLength)) && !locationFilterExpression.Contains(nameof(Loc.Google.IsScout))))
        {
            defaultFilterSelectors.Add(x => (x.Google.DescriptionLength is null or > 0 &&
                                            x.Google.IsScout == false) ||
                                            CountryCodesAcceptableWithoutDescription.Contains(x.Nominatim.CountryCode) ||
                                            SubdivisionCodesAcceptableWithoutDescription.Contains(x.Nominatim.SubdivisionCode));
        }

        var locations = defaultFilterSelectors.Aggregate(locationsFromFile.AsEnumerable(), (current, defaultFilterSelector) => current.Where(defaultFilterSelector));

        if (!string.IsNullOrEmpty(locationFilterExpression))
        {
            var typedExpression = CompileBoolLocationExpression(locationFilterExpression);
            locations = locations.Where(typedExpression);
        }

        if (proximityFilter.Radius > 0 && File.Exists(proximityFilter.LocationsPath))
        {
            var locs = locations.ToArray();
            locations = FilterByProximity(locs, proximityFilter, proximityLocationBuckets);
        }

        foreach (var neighborFilter in neighborFilters)
        {
            locations = FilterByNeighbors(locations, neighborLocationBuckets, neighborFilter, mapDefinition);
        }

        return locations.ToArray();
    }

    private static readonly ConcurrentDictionary<string, Func<Loc, int>> _cacheInt = new();

    private static readonly ConcurrentDictionary<string, Func<Loc, bool>> _cacheBool = new();

    private static readonly ConcurrentDictionary<string, Func<Loc, Loc, bool>> _cacheParentBool = new();

    public static Func<Loc, int> CompileIntLocationExpression(string initialExpression)
    {
        if (_cacheInt.TryGetValue(initialExpression, out var selector))
        {
            return selector;
        }

        var func = CompileExpression<Loc, int>(initialExpression, 0);
        _cacheInt.TryAdd(initialExpression, func);
        return func;
    }

    public static Func<Loc, bool> CompileBoolLocationExpression(string initialExpression)
    {
        if (_cacheBool.TryGetValue(initialExpression, out var selector))
        {
            return selector;
        }

        var func = CompileExpression<Loc, bool>(initialExpression, true);
        _cacheBool.TryAdd(initialExpression, func);
        return func;
    }

    public static Func<Loc, Loc, bool> CompileParentBoolLocationExpression(string initialExpression)
    {
        if (_cacheParentBool.TryGetValue(initialExpression, out var selector))
        {
            return selector;
        }

        var func = CompileExpressionWithParent<Loc, bool>(initialExpression, true);
        _cacheParentBool.TryAdd(initialExpression, func);
        return func;
    }

    public static Func<TLoc, T> CompileExpression<TLoc, T>(string initialExpression, T fallback)
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
        var validProperties = typeof(TLoc).Name switch
        {
            nameof(Location) => ValidProperties(),
            nameof(MapCheckrLocation) => ValidMapCheckrLocationProperties(),
            _ => throw new ArgumentOutOfRangeException()
        };
        Func<string, string> lambdaExpressionFunc = typeof(TLoc).Name switch
        {
            nameof(Location) => s => LocationLambdaExpressionFromProperty(s, "x"),
            nameof(MapCheckrLocation) => MapCheckrLocationLambdaExpressionFromProperty,
            _ => throw new ArgumentOutOfRangeException()
        };
        var totalExpression = validProperties
            .Intersect(componentsInExpression.Select(x => x.Trim()))
            .Aggregate(expressionWithPlaceholders.SpacePadParentheses().SpacePad(), (current, validProperty) => current.Replace(validProperty.SpacePad(), lambdaExpressionFunc(validProperty).SpacePad()));
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
        var parameter = Expression.Parameter(typeof(TLoc), "x");
        var expression = (Expression)DynamicExpressionParser.ParseLambda([parameter], null, totalExpression);
        var typedExpression = ((Expression<Func<TLoc, T>>)expression).Compile();
        return typedExpression;
    }

    public static Func<TLoc, TLoc, T> CompileExpressionWithParent<TLoc, T>(string initialExpression, T fallback)
    {
        if (initialExpression == "*")
        {
            return (_, _) => fallback;
        }

        var (expressionWithPlaceholders, placeHolders) = initialExpression.ReplaceValuesInSingleQuotesWithPlaceHolders();
        var componentsInExpression = expressionWithPlaceholders
            .RemoveMultipleSpaces()
            .RemoveParentheses()
            .Split(' ')
            .ToArray();
        const string primaryLambdaParameterName = "x";
        const string parentLambdaParameterName = "current:";
        var validProperties = (typeof(TLoc).Name switch
            {
                nameof(Location) => ValidProperties(),
                nameof(MapCheckrLocation) => ValidMapCheckrLocationProperties(),
                _ => throw new ArgumentOutOfRangeException()
            }).SelectMany(x => new (string PropertyName, string LambdaParameterName)[]
            {
                (PropertyName: x, primaryLambdaParameterName),
                ($"{parentLambdaParameterName}{x}", parentLambdaParameterName)
            })
            .ToArray();
        Func<string, string, string> lambdaExpressionFunc = typeof(TLoc).Name switch
        {
            nameof(Location) => LocationLambdaExpressionFromProperty,
            nameof(MapCheckrLocation) => (s, _) => MapCheckrLocationLambdaExpressionFromProperty(s),
            _ => throw new ArgumentOutOfRangeException()
        };
        var totalExpression = validProperties
            .IntersectBy(componentsInExpression.Select(x => x.Trim()), tuple => tuple.PropertyName)
            .Aggregate(expressionWithPlaceholders.SpacePadParentheses().SpacePad(), (current, validProperty) => current.Replace(validProperty.PropertyName.SpacePad(), lambdaExpressionFunc(validProperty.PropertyName, validProperty.LambdaParameterName).SpacePad()));
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
        var parameter = Expression.Parameter(typeof(TLoc), primaryLambdaParameterName);
        var parentParameter = Expression.Parameter(typeof(TLoc), new string(parentLambdaParameterName.Where(char.IsAsciiLetter).ToArray()));
        var expression = (Expression)DynamicExpressionParser.ParseLambda([parameter, parentParameter], null, totalExpression);
        var typedExpression = ((Expression<Func<TLoc, TLoc, T>>)expression).Compile();
        return typedExpression;
    }

    public static IReadOnlyCollection<string> ValidProperties() =>
    [
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
        nameof(Loc.Osm.WayId),
        nameof(Loc.Google.Month),
        nameof(Loc.Google.Year),
        nameof(Loc.Google.Lat),
        nameof(Loc.Google.Lng),
        nameof(Loc.Google.Heading),
        nameof(Loc.Google.DrivingDirectionAngle),
        nameof(Loc.Google.ArrowCount),
        nameof(Loc.Google.Elevation),
        nameof(Loc.Google.DescriptionLength),
        nameof(Loc.Google.IsScout),
        nameof(Loc.Google.ResolutionHeight),
        nameof(Loc.Nominatim.CountryCode),
        nameof(Loc.Nominatim.SubdivisionCode),
        nameof(Loc.Nominatim.County)
    ];

    public static IReadOnlyCollection<string> ValidMapCheckrLocationProperties() =>
    [
        nameof(MapCheckrLocation.lat),
        nameof(MapCheckrLocation.lng),
        nameof(MapCheckrLocation.countryCode),
        nameof(MapCheckrLocation.arrowCount),
        nameof(MapCheckrLocation.descriptionLength),
        nameof(MapCheckrLocation.drivingDirectionAngle),
        nameof(MapCheckrLocation.heading),
        nameof(MapCheckrLocation.month),
        nameof(MapCheckrLocation.year),
        nameof(MapCheckrLocation.isScout),
        nameof(MapCheckrLocation.resolutionHeight),
        nameof(MapCheckrLocation.subdivision),
        nameof(MapCheckrLocation.panoramaCount),
        nameof(MapCheckrLocation.elevation),
    ];

    public static IEnumerable<string> ValidOperators() =>
    [
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
        "*"
    ];

    private static string LocationLambdaExpressionFromProperty(string property, string lambdaParameterName)
    {
        var resultLambdaParameterName = new string(lambdaParameterName.Where(char.IsAsciiLetter).ToArray());
        return property.TrimStart(lambdaParameterName) switch
        {
            nameof(Loc.Osm.Surface) => $"{resultLambdaParameterName}.Osm.{nameof(Loc.Osm.Surface)}",
            nameof(Loc.Osm.Buildings100) => $"{resultLambdaParameterName}.Osm.{nameof(Loc.Osm.Buildings100)}",
            nameof(Loc.Osm.Buildings200) => $"{resultLambdaParameterName}.Osm.{nameof(Loc.Osm.Buildings200)}",
            nameof(Loc.Osm.Buildings25) => $"{resultLambdaParameterName}.Osm.{nameof(Loc.Osm.Buildings25)}",
            nameof(Loc.Osm.Roads100) => $"{resultLambdaParameterName}.Osm.{nameof(Loc.Osm.Roads100)}",
            nameof(Loc.Osm.Roads200) => $"{resultLambdaParameterName}.Osm.{nameof(Loc.Osm.Roads200)}",
            nameof(Loc.Osm.Roads25) => $"{resultLambdaParameterName}.Osm.{nameof(Loc.Osm.Roads25)}",
            nameof(Loc.Osm.Roads50) => $"{resultLambdaParameterName}.Osm.{nameof(Loc.Osm.Roads50)}",
            nameof(Loc.Osm.Tunnels10) => $"{resultLambdaParameterName}.Osm.{nameof(Loc.Osm.Tunnels10)}",
            nameof(Loc.Osm.Tunnels200) => $"{resultLambdaParameterName}.Osm.{nameof(Loc.Osm.Tunnels200)}",
            nameof(Loc.Osm.Buildings10) => $"{resultLambdaParameterName}.Osm.{nameof(Loc.Osm.Buildings10)}",
            nameof(Loc.Osm.IsResidential) => $"{resultLambdaParameterName}.Osm.{nameof(Loc.Osm.IsResidential)}",
            nameof(Loc.Osm.Roads10) => $"{resultLambdaParameterName}.Osm.{nameof(Loc.Osm.Roads10)}",
            nameof(Loc.Osm.Roads0) => $"{resultLambdaParameterName}.Osm.{nameof(Loc.Osm.Roads0)}",
            nameof(Loc.Osm.ClosestCoast) => $"{resultLambdaParameterName}.Osm.{nameof(Loc.Osm.ClosestCoast)}",
            nameof(Loc.Osm.ClosestLake) => $"{resultLambdaParameterName}.Osm.{nameof(Loc.Osm.ClosestLake)}",
            nameof(Loc.Osm.ClosestRiver) => $"{resultLambdaParameterName}.Osm.{nameof(Loc.Osm.ClosestRiver)}",
            nameof(Loc.Osm.ClosestRailway) => $"{resultLambdaParameterName}.Osm.{nameof(Loc.Osm.ClosestRailway)}",
            nameof(Loc.Osm.HighwayType) => $"{resultLambdaParameterName}.Osm.{nameof(Loc.Osm.HighwayType)}",
            nameof(Loc.Osm.HighwayTypeCount) => $"{resultLambdaParameterName}.Osm.{nameof(Loc.Osm.HighwayTypeCount)}",
            nameof(Loc.Osm.WayId) => $"{resultLambdaParameterName}.Osm.{nameof(Loc.Osm.WayId)}",
            nameof(Loc.Google.Month) => $"{resultLambdaParameterName}.Google.{nameof(Loc.Google.Month)}",
            nameof(Loc.Google.Year) => $"{resultLambdaParameterName}.Google.{nameof(Loc.Google.Year)}",
            nameof(Loc.Google.Lat) => $"{resultLambdaParameterName}.Google.{nameof(Loc.Google.Lat)}",
            nameof(Loc.Google.Lng) => $"{resultLambdaParameterName}.Google.{nameof(Loc.Google.Lng)}",
            nameof(Loc.Google.Heading) => $"{resultLambdaParameterName}.Google.{nameof(Loc.Google.Heading)}",
            nameof(Loc.Google.DrivingDirectionAngle) =>
                $"{resultLambdaParameterName}.Google.{nameof(Loc.Google.DrivingDirectionAngle)}",
            nameof(Loc.Google.ArrowCount) => $"{resultLambdaParameterName}.Google.{nameof(Loc.Google.ArrowCount)}",
            nameof(Loc.Google.Elevation) => $"{resultLambdaParameterName}.Google.{nameof(Loc.Google.Elevation)}",
            nameof(Loc.Google.DescriptionLength) =>
                $"{resultLambdaParameterName}.Google.{nameof(Loc.Google.DescriptionLength)}",
            nameof(Loc.Google.IsScout) => $"{resultLambdaParameterName}.Google.{nameof(Loc.Google.IsScout)}",
            nameof(Loc.Google.ResolutionHeight) =>
                $"{resultLambdaParameterName}.Google.{nameof(Loc.Google.ResolutionHeight)}",
            nameof(Loc.Nominatim.CountryCode) => $"{resultLambdaParameterName}.Nominatim.{nameof(Loc.Nominatim.CountryCode)}",
            nameof(Loc.Nominatim.SubdivisionCode) =>
                $"{resultLambdaParameterName}.Nominatim.{nameof(Loc.Nominatim.SubdivisionCode)}",
            nameof(Loc.Nominatim.County) => $"{resultLambdaParameterName}.Nominatim.{nameof(Loc.Nominatim.County)}",
            _ => throw new ArgumentOutOfRangeException(nameof(property), property, null)
        };
    }

    private static string MapCheckrLocationLambdaExpressionFromProperty(string property) => $"x.{property}";

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

    private static IEnumerable<Loc> FilterByProximity(
        IReadOnlyCollection<Loc> locations,
        ProximityFilter proximityFilter,
        Dictionary<string, List<ILatLng>> proximityLocationBuckets)
    {
        var precision = proximityFilter.HashPrecisionFromProximityFilter()!.Value;
        var proximityFilterRadius = proximityFilter.Radius;
        return locations.Where(l =>
        {
            var hash = Hasher.Encode(l.Lat, l.Lng, precision);
            return proximityLocationBuckets.TryGetValue(hash, out var p) && p.Any(x => Extensions.ApproximateDistance(l.Lat, l.Lng, x.Lat, x.Lng) < proximityFilterRadius);
        });
    }

    private static IEnumerable<Loc> FilterByNeighbors(
        IEnumerable<Loc> locations,
        Dictionary<string, List<Loc>> neighborLocationBuckets,
        NeighborFilter neighborFilter,
        MapDefinition mapDefinition)
    {
        var precision = mapDefinition.HashPrecisionFromNeighborFiltersRadius()!.Value;
        var filterExpression = string.IsNullOrEmpty(neighborFilter.Expression)
            ? (_, _) => true
            : CompileParentBoolLocationExpression(neighborFilter.Expression);
        var directions = neighborFilter.CheckEachCardinalDirectionSeparately ? Enum.GetValues<CardinalDirection>().Cast<CardinalDirection?>().ToArray() : [null];
        return neighborFilter switch
        {
            { Bound: "lower", CheckEachCardinalDirectionSeparately: true } => locations
                .Where(l => directions.Any(d => LocationsFromDictionary(neighborLocationBuckets, l, precision).Count(l2 =>
                    l.NodeId != l2.NodeId &&
                    IsInDirection(d, l, l2) &&
                    Extensions.ApproximateDistance(l.Lat, l.Lng, l2.Lat, l2.Lng) <= neighborFilter.Radius &&
                    filterExpression(l2, l)) >= neighborFilter.Limit)),
            { Limit: 1, Bound: "lower", CheckEachCardinalDirectionSeparately: false } or { Bound: "some" } => locations
                .Where(l => LocationsFromDictionary(neighborLocationBuckets, l, precision).Any(l2 =>
                    l.NodeId != l2.NodeId &&
                    Extensions.ApproximateDistance(l.Lat, l.Lng, l2.Lat, l2.Lng) <= neighborFilter.Radius &&
                    filterExpression(l2, l))),
            { Bound: "lower", CheckEachCardinalDirectionSeparately: false } => locations
                .Where(l => LocationsFromDictionary(neighborLocationBuckets, l, precision).Count(l2 =>
                    l.NodeId != l2.NodeId &&
                    Extensions.ApproximateDistance(l.Lat, l.Lng, l2.Lat, l2.Lng) <= neighborFilter.Radius &&
                    filterExpression(l2, l)) >= neighborFilter.Limit),
            { Limit: 0, Bound: "upper", CheckEachCardinalDirectionSeparately: true } or { CheckEachCardinalDirectionSeparately: true, Bound: "none" } => locations
                .Where(l => directions.Any(d => !LocationsFromDictionary(neighborLocationBuckets, l, precision).Any(l2 =>
                    l.NodeId != l2.NodeId &&
                    IsInDirection(d, l, l2) &&
                    Extensions.ApproximateDistance(l.Lat, l.Lng, l2.Lat, l2.Lng) <= neighborFilter.Radius &&
                    filterExpression(l2, l)))),
            { Limit: 0, Bound: "upper", CheckEachCardinalDirectionSeparately: false } or { Bound: "none" } => locations
                .Where(l => !LocationsFromDictionary(neighborLocationBuckets, l, precision).Any(l2 =>
                    l.NodeId != l2.NodeId &&
                    Extensions.ApproximateDistance(l.Lat, l.Lng, l2.Lat, l2.Lng) <= neighborFilter.Radius &&
                    filterExpression(l2, l))),
            { Limit: > 0, Bound: "upper", CheckEachCardinalDirectionSeparately: true } => locations
                .Where(l => directions.Any(d => neighborLocationBuckets[Hasher.Encode(l.Lat, l.Lng, HashPrecision.Size_km_39x20)].Count(l2 =>
                    l.NodeId != l2.NodeId &&
                    IsInDirection(d, l, l2) &&
                    Extensions.ApproximateDistance(l.Lat, l.Lng, l2.Lat, l2.Lng) <= neighborFilter.Radius &&
                    filterExpression(l2, l)) <= neighborFilter.Limit)),
            { Bound: "upper", CheckEachCardinalDirectionSeparately: false } => locations
                .Where(l => LocationsFromDictionary(neighborLocationBuckets, l, precision).Count(l2 =>
                    l.NodeId != l2.NodeId &&
                    Extensions.ApproximateDistance(l.Lat, l.Lng, l2.Lat, l2.Lng) <= neighborFilter.Radius &&
                    filterExpression(l2, l)) <= neighborFilter.Limit),
            { Bound: "all" } => locations
                .Where(l => LocationsFromDictionary(neighborLocationBuckets, l, precision).Where(l2 =>
                        l.NodeId != l2.NodeId &&
                        Extensions.ApproximateDistance(l.Lat, l.Lng, l2.Lat, l2.Lng) <= neighborFilter.Radius)
                    .AllAtLeastOne(l2 => filterExpression(l2, l))),
            _ => throw new InvalidOperationException($"Neighbor filter combination is not valid/implemented. Bound: {neighborFilter.Bound}. Check separately: {neighborFilter.CheckEachCardinalDirectionSeparately}. Limit: {neighborFilter.Limit}")
        };
    }

    private static IEnumerable<Loc> LocationsFromDictionary(Dictionary<string, List<Loc>> neighborLocationBuckets, Loc l, HashPrecision precision) =>
        neighborLocationBuckets.TryGetValue(Hasher.Encode(l.Lat, l.Lng, precision), out var locations)
            ? locations
            : [];

    private static bool IsInDirection(CardinalDirection? direction, Loc x1, Loc x2)
    {
        return direction switch
        {
            CardinalDirection.West => x1.Lng < x2.Lng,
            CardinalDirection.East => x1.Lng > x2.Lng,
            CardinalDirection.North => x1.Lat > x2.Lat,
            CardinalDirection.South => x1.Lat < x2.Lat,
            _ => false
        };
    }
}