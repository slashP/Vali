using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Loc = Vali.Core.Location;

namespace Vali.Core;

public static class LocationLakeFilterer
{
    public static IList<Loc> Filter(
        IEnumerable<Loc> locationsFromFile,
        string[] locationFilterExpressions)
    {
        Func<Loc, bool> defaultTunnelSelector =
            locationFilterExpressions.Any(e =>
                e.Contains(nameof(Loc.Osm.Tunnels10)) || e.Contains(nameof(Loc.Osm.Tunnels200)))
                ? _ => true
                : x => x.Osm.Tunnels10 == 0;
        var locations = locationsFromFile.Where(defaultTunnelSelector);
        if (locationFilterExpressions.Any())
        {
            var initialExpression = locationFilterExpressions.Select(x => $"({x})").Merge(" && ");
            var typedExpression = CompileLocationExpression(initialExpression);
            locations = locations.Where(typedExpression);
        }

        return locations.ToArray();
    }

    public static Func<Loc, bool> CompileLocationExpression(string initialExpression)
    {
        if (initialExpression == "*")
        {
            return _ => true;
        }

        var (expressionWithPlaceholders, placeHolders) = initialExpression.ReplaceValuesInSingleQuotesWithPlaceHolders();
        var totalExpression = ValidProperties().Aggregate(expressionWithPlaceholders, (current, validProperty) => current.Replace(validProperty, LambdaExpressionFromProperty(validProperty)));
        totalExpression = ValidOperators().Aggregate(totalExpression, (current, validOperator) => current.Replace(validOperator, CSharpOperatorFromOperator(validOperator)));
        totalExpression = totalExpression.Replace("'", "\"");
        foreach (var placeHolder in placeHolders)
        {
            totalExpression = totalExpression.Replace(placeHolder.newValue, placeHolder.oldValue);
        }

        totalExpression = totalExpression.Replace("\\'", "'");
        var parameter = Expression.Parameter(typeof(Loc), "x");
        var expression = (Expression)DynamicExpressionParser.ParseLambda(new[] { parameter }, null, totalExpression);
        var typedExpression = ((Expression<Func<Loc, bool>>)expression).Compile();
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
        nameof(Loc.Google.Month),
        nameof(Loc.Google.Year),
        nameof(Loc.Google.Lat),
        nameof(Loc.Google.Lng),
        nameof(Loc.Google.Heading),
        nameof(Loc.Nominatim.CountryCode),
        nameof(Loc.Nominatim.SubdivisionCode),
        nameof(Loc.Nominatim.County),
    };

    public static IEnumerable<string> ValidOperators() => new[]
    {
        " eq ",
        " neq ",
        " lt ",
        " lte ",
        " gt ",
        " gte ",
        " and ",
        " or ",
    };

    private static string LambdaExpressionFromProperty(string property) => property switch
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
        nameof(Loc.Google.Month) => $"x.Google.{nameof(Loc.Google.Month)}",
        nameof(Loc.Google.Year) => $"x.Google.{nameof(Loc.Google.Year)}",
        nameof(Loc.Google.Lat) => $"x.Google.{nameof(Loc.Google.Lat)}",
        nameof(Loc.Google.Lng) => $"x.Google.{nameof(Loc.Google.Lng)}",
        nameof(Loc.Google.Heading) => $"x.Google.{nameof(Loc.Google.Heading)}",
        nameof(Loc.Nominatim.CountryCode) => $"x.Nominatim.{nameof(Loc.Nominatim.CountryCode)}",
        nameof(Loc.Nominatim.SubdivisionCode) => $"x.Nominatim.{nameof(Loc.Nominatim.SubdivisionCode)}",
        nameof(Loc.Nominatim.County) => $"x.Nominatim.{nameof(Loc.Nominatim.County)}",
        _ => throw new ArgumentOutOfRangeException(nameof(property), property, null)
    };

    private static string CSharpOperatorFromOperator(string @operator) => @operator.ToLowerInvariant() switch
    {
        " eq " => " == ",
        " neq " => " != ",
        " lt " => " < ",
        " lte " => " <= ",
        " gt " => " > ",
        " gte " => " >= ",
        " and " => " && ",
        " or " => " || ",
        _ => throw new ArgumentOutOfRangeException(nameof(@operator), $"operator {@operator} not implemented.")
    };
}