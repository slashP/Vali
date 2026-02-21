using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

namespace Vali.Core.Expressions;

public static class ExpressionCompiler
{
    public static Func<TLoc, T> Compile<TLoc, T>(string expression, T fallback, PropertyResolver resolver)
    {
        if (expression == "*")
        {
            return _ => fallback;
        }

        var dynamicLinq = GenerateDynamicLinq(expression, resolver, "x", null);
        var parameter = Expression.Parameter(typeof(TLoc), "x");
        try
        {
            var parsed = (System.Linq.Expressions.Expression)DynamicExpressionParser.ParseLambda([parameter], null, dynamicLinq);
            return ((Expression<Func<TLoc, T>>)parsed).Compile();
        }
        catch (Exception ex) when (ex is not ExpressionCompilationException)
        {
            throw new ExpressionCompilationException(expression,
                $"Failed to compile expression: {ex.Message}");
        }
    }

    public static Func<TLoc, TLoc, T> CompileWithParent<TLoc, T>(string expression, T fallback, PropertyResolver resolver)
    {
        if (expression == "*")
        {
            return (_, _) => fallback;
        }

        const string primaryParam = "x";
        const string parentParam = "current";
        var dynamicLinq = GenerateDynamicLinq(expression, resolver, primaryParam, parentParam);
        var parameter = Expression.Parameter(typeof(TLoc), primaryParam);
        var parentParameter = Expression.Parameter(typeof(TLoc), parentParam);
        try
        {
            var parsed = (System.Linq.Expressions.Expression)DynamicExpressionParser.ParseLambda([parameter, parentParameter], null, dynamicLinq);
            return ((Expression<Func<TLoc, TLoc, T>>)parsed).Compile();
        }
        catch (Exception ex) when (ex is not ExpressionCompilationException)
        {
            throw new ExpressionCompilationException(expression,
                $"Failed to compile expression: {ex.Message}");
        }
    }

    public static ExpressionCompilationException? Validate(string expression, PropertyResolver resolver, bool allowParentProperties)
    {
        if (expression == "*")
        {
            return null;
        }

        try
        {
            var tokens = ExpressionLexer.Tokenize(expression);
            var ast = ExpressionParser.Parse(tokens, expression);
            var generator = new DynamicLinqGenerator(resolver, "x", allowParentProperties ? "current" : null, expression);
            generator.Generate(ast);
            return null;
        }
        catch (ExpressionCompilationException ex)
        {
            return ex;
        }
    }

    private static string GenerateDynamicLinq(string expression, PropertyResolver resolver, string primaryParam, string? parentParam)
    {
        var tokens = ExpressionLexer.Tokenize(expression);
        var ast = ExpressionParser.Parse(tokens, expression);
        var generator = new DynamicLinqGenerator(resolver, primaryParam, parentParam, expression);
        return generator.Generate(ast);
    }
}
