using System.Collections.Concurrent;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Vali.Core.Google;
using Loc = Vali.Core.Location;

namespace Vali.Core;

public static class LocationLakeFilterer
{
    public static IList<Loc> Filter(
        IEnumerable<Loc> locationsFromFile,
        string? locationFilterExpression)
    {
        Func<Loc, bool> defaultTunnelSelector =
            !string.IsNullOrEmpty(locationFilterExpression) &&
                (locationFilterExpression.Contains(nameof(Loc.Osm.Tunnels10)) || locationFilterExpression.Contains(nameof(Loc.Osm.Tunnels200)))
                ? x => x.Google.PanoId.Length < 36
                : x => x.Osm.Tunnels10 == 0 && x.Google.PanoId.Length < 36;
        var locations = locationsFromFile.Where(defaultTunnelSelector);
        if (!string.IsNullOrEmpty(locationFilterExpression))
        {
            var typedExpression = CompileBoolLocationExpression(locationFilterExpression);
            locations = locations.Where(typedExpression);
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
}