namespace Vali.Core;

public static class ExpressionDefaults
{
    public static string Expand(Dictionary<string, string> namedExpressions, string expression) =>
        namedExpressions.Aggregate(expression,
            (current, namedExpression) =>
                current.Replace(namedExpression.Key, $"({namedExpression.Value})"));
}
