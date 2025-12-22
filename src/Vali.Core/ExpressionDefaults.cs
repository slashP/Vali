namespace Vali.Core;

public static class ExpressionDefaults
{
    public static string Expand(Dictionary<string, string> namedExpressions, string expression)
    {
        var newExpression = expression;
        bool somethingChanged;
        var iterations = 0;
        do
        {
            var newExpr = newExpression;
            foreach (var namedExpression in namedExpressions)
            {
                newExpr = newExpr.Replace(namedExpression.Key, $"({namedExpression.Value})");
            }

            somethingChanged = newExpr != newExpression;
            newExpression = newExpr;
        } while (somethingChanged && iterations++ < 10_000);

        return newExpression;
    }
}
