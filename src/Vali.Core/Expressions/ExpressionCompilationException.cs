namespace Vali.Core.Expressions;

public sealed class ExpressionCompilationException : Exception
{
    public string OriginalExpression { get; }
    public int Position { get; }
    public int Length { get; }
    public string Detail { get; }

    public ExpressionCompilationException(string originalExpression, int position, int length, string detail)
        : base(FormatMessage(originalExpression, position, length, detail))
    {
        OriginalExpression = originalExpression;
        Position = position;
        Length = length;
        Detail = detail;
    }

    public ExpressionCompilationException(string originalExpression, string detail)
        : base($"Error in expression: {detail}\n  {originalExpression}")
    {
        OriginalExpression = originalExpression;
        Position = 0;
        Length = originalExpression.Length;
        Detail = detail;
    }

    private static string FormatMessage(string expression, int position, int length, string detail)
    {
        var caretLength = Math.Max(1, Math.Min(length, expression.Length - position));
        var carets = new string('^', caretLength);
        var padding = new string(' ', position);
        return $"""
                Error in expression at position {position}:
                  {expression}
                  {padding}{carets}
                {detail}
                """;
    }
}
