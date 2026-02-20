namespace Vali.Core.Expressions;

public static class ExpressionParser
{
    public static ExpressionNode Parse(Token[] tokens, string originalExpression)
    {
        if (tokens.Length >= 2 && tokens[0].Kind == TokenKind.Wildcard)
        {
            return new LiteralNode(tokens[0]);
        }

        var pos = 0;
        var result = ParseLogicalOr(tokens, ref pos, originalExpression);

        if (tokens[pos].Kind != TokenKind.EndOfExpression)
        {
            var token = tokens[pos];
            throw new ExpressionCompilationException(originalExpression, token.Position, token.Length,
                $"Unexpected token '{token.Value}' at position {token.Position}.");
        }

        return result;
    }

    private static ExpressionNode ParseLogicalOr(Token[] tokens, ref int pos, string expr)
    {
        var left = ParseLogicalAnd(tokens, ref pos, expr);
        while (tokens[pos].Kind == TokenKind.Or)
        {
            var op = tokens[pos];
            pos++;
            var right = ParseLogicalAnd(tokens, ref pos, expr);
            left = new BinaryNode(left, op, right);
        }

        return left;
    }

    private static ExpressionNode ParseLogicalAnd(Token[] tokens, ref int pos, string expr)
    {
        var left = ParseComparison(tokens, ref pos, expr);
        while (tokens[pos].Kind == TokenKind.And)
        {
            var op = tokens[pos];
            pos++;
            var right = ParseComparison(tokens, ref pos, expr);
            left = new BinaryNode(left, op, right);
        }

        return left;
    }

    private static ExpressionNode ParseComparison(Token[] tokens, ref int pos, string expr)
    {
        var left = ParseAddition(tokens, ref pos, expr);
        if (tokens[pos].Kind is TokenKind.Eq or TokenKind.Neq or TokenKind.Lt or TokenKind.Lte or TokenKind.Gt or TokenKind.Gte)
        {
            var op = tokens[pos];
            pos++;
            var right = ParseAddition(tokens, ref pos, expr);
            left = new BinaryNode(left, op, right);
        }

        return left;
    }

    private static ExpressionNode ParseAddition(Token[] tokens, ref int pos, string expr)
    {
        var left = ParseMultiplication(tokens, ref pos, expr);
        while (tokens[pos].Kind is TokenKind.Plus or TokenKind.Minus)
        {
            var op = tokens[pos];
            pos++;
            var right = ParseMultiplication(tokens, ref pos, expr);
            left = new BinaryNode(left, op, right);
        }

        return left;
    }

    private static ExpressionNode ParseMultiplication(Token[] tokens, ref int pos, string expr)
    {
        var left = ParseUnary(tokens, ref pos, expr);
        while (tokens[pos].Kind is TokenKind.Multiply or TokenKind.Divide or TokenKind.Modulo)
        {
            var op = tokens[pos];
            pos++;
            var right = ParseUnary(tokens, ref pos, expr);
            left = new BinaryNode(left, op, right);
        }

        return left;
    }

    private static ExpressionNode ParseUnary(Token[] tokens, ref int pos, string expr)
    {
        if (tokens[pos].Kind == TokenKind.Minus)
        {
            var op = tokens[pos];
            pos++;
            var operand = ParsePrimary(tokens, ref pos, expr);
            return new UnaryMinusNode(op, operand);
        }

        return ParsePrimary(tokens, ref pos, expr);
    }

    private static ExpressionNode ParsePrimary(Token[] tokens, ref int pos, string expr)
    {
        var token = tokens[pos];
        switch (token.Kind)
        {
            case TokenKind.OpenParen:
            {
                var openPos = token.Position;
                pos++;
                var inner = ParseLogicalOr(tokens, ref pos, expr);
                if (tokens[pos].Kind != TokenKind.CloseParen)
                {
                    throw new ExpressionCompilationException(expr, openPos, 1,
                        $"Unmatched '(' at position {openPos}.");
                }

                var closeToken = tokens[pos];
                pos++;
                var groupSpan = new TextSpan(openPos, closeToken.Position + 1 - openPos);
                return new GroupNode(inner, groupSpan);
            }
            case TokenKind.IntegerLiteral:
            case TokenKind.DecimalLiteral:
            case TokenKind.StringLiteral:
            case TokenKind.BooleanLiteral:
            case TokenKind.NullLiteral:
                pos++;
                return new LiteralNode(token);
            case TokenKind.Property:
                pos++;
                return new PropertyNode(token, token.Value);
            case TokenKind.ExternalProperty:
                pos++;
                var key = token.Value["external:".Length..];
                return new ExternalPropertyNode(token, key);
            case TokenKind.ParentProperty:
                pos++;
                var propName = token.Value["current:".Length..];
                return new ParentPropertyNode(token, propName);
            case TokenKind.EndOfExpression:
                throw new ExpressionCompilationException(expr, token.Position, 1,
                    "Unexpected end of expression.");
            default:
                throw new ExpressionCompilationException(expr, token.Position, token.Length,
                    $"Expected operand but found '{token.Value}' at position {token.Position}.");
        }
    }
}
