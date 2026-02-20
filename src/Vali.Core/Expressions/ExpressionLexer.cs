namespace Vali.Core.Expressions;

public static class ExpressionLexer
{
    public static Token[] Tokenize(string expression)
    {
        if (expression == "*")
        {
            return [new Token(TokenKind.Wildcard, "*", 0, 1), new Token(TokenKind.EndOfExpression, "", 1, 0)];
        }

        var tokens = new List<Token>();
        var i = 0;
        while (i < expression.Length)
        {
            if (char.IsWhiteSpace(expression[i]))
            {
                i++;
                continue;
            }

            if (expression[i] == '\'')
            {
                tokens.Add(ReadString(expression, ref i));
                continue;
            }

            if (expression[i] == '(')
            {
                tokens.Add(new Token(TokenKind.OpenParen, "(", i, 1));
                i++;
                continue;
            }

            if (expression[i] == ')')
            {
                tokens.Add(new Token(TokenKind.CloseParen, ")", i, 1));
                i++;
                continue;
            }

            if (expression[i] == '[')
            {
                tokens.Add(new Token(TokenKind.OpenBracket, "[", i, 1));
                i++;
                continue;
            }

            if (expression[i] == ']')
            {
                tokens.Add(new Token(TokenKind.CloseBracket, "]", i, 1));
                i++;
                continue;
            }

            if (expression[i] == ',')
            {
                tokens.Add(new Token(TokenKind.Comma, ",", i, 1));
                i++;
                continue;
            }

            if (expression[i] == '+')
            {
                tokens.Add(new Token(TokenKind.Plus, "+", i, 1));
                i++;
                continue;
            }

            if (expression[i] == '/')
            {
                tokens.Add(new Token(TokenKind.Divide, "/", i, 1));
                i++;
                continue;
            }

            if (expression[i] == '*')
            {
                tokens.Add(new Token(TokenKind.Multiply, "*", i, 1));
                i++;
                continue;
            }

            if (expression[i] == '-')
            {
                var isUnaryMinus = tokens.Count == 0 ||
                    tokens[^1].Kind is TokenKind.OpenParen or TokenKind.OpenBracket or TokenKind.Comma or
                        TokenKind.Eq or TokenKind.Neq or TokenKind.Lt or TokenKind.Lte or
                        TokenKind.Gt or TokenKind.Gte or TokenKind.And or TokenKind.Or or
                        TokenKind.Plus or TokenKind.Minus or TokenKind.Multiply or
                        TokenKind.Divide or TokenKind.Modulo;

                if (isUnaryMinus && i + 1 < expression.Length && (char.IsDigit(expression[i + 1]) || expression[i + 1] == '.'))
                {
                    tokens.Add(ReadNumber(expression, ref i));
                }
                else
                {
                    tokens.Add(new Token(TokenKind.Minus, "-", i, 1));
                    i++;
                }
                continue;
            }

            if (char.IsDigit(expression[i]) || (expression[i] == '.' && i + 1 < expression.Length && char.IsDigit(expression[i + 1])))
            {
                tokens.Add(ReadNumber(expression, ref i));
                continue;
            }

            if (char.IsLetter(expression[i]) || expression[i] == '_' || expression[i] == '$')
            {
                tokens.Add(ReadIdentifierOrKeyword(expression, ref i));
                continue;
            }

            throw new ExpressionCompilationException(expression, i, 1,
                $"Unexpected character '{expression[i]}'.");
        }

        tokens.Add(new Token(TokenKind.EndOfExpression, "", expression.Length, 0));
        return tokens.ToArray();
    }

    private static Token ReadString(string expression, ref int i)
    {
        var start = i;
        i++; // skip opening quote
        var sb = new System.Text.StringBuilder();
        while (i < expression.Length)
        {
            if (expression[i] == '\\' && i + 1 < expression.Length && expression[i + 1] == '\'')
            {
                sb.Append('\'');
                i += 2;
                continue;
            }

            if (expression[i] == '\'')
            {
                i++; // skip closing quote
                var value = sb.ToString();
                return new Token(TokenKind.StringLiteral, value, start, i - start);
            }

            sb.Append(expression[i]);
            i++;
        }

        throw new ExpressionCompilationException(expression, start, expression.Length - start,
            $"Unterminated string starting at position {start}.");
    }

    private static Token ReadNumber(string expression, ref int i)
    {
        var start = i;
        var hasDecimalPoint = false;

        if (expression[i] == '-')
        {
            i++;
        }

        while (i < expression.Length && (char.IsDigit(expression[i]) || expression[i] == '.'))
        {
            if (expression[i] == '.')
            {
                if (hasDecimalPoint)
                {
                    break;
                }

                if (i + 1 >= expression.Length || !char.IsDigit(expression[i + 1]))
                {
                    break;
                }

                hasDecimalPoint = true;
            }

            i++;
        }

        var value = expression[start..i];
        var kind = hasDecimalPoint ? TokenKind.DecimalLiteral : TokenKind.IntegerLiteral;
        return new Token(kind, value, start, i - start);
    }

    private static Token ReadIdentifierOrKeyword(string expression, ref int i)
    {
        var start = i;
        while (i < expression.Length && (char.IsLetterOrDigit(expression[i]) || expression[i] == '_' || expression[i] == ':' || expression[i] == '$'))
        {
            i++;
        }

        var value = expression[start..i];

        var kind = value.ToLowerInvariant() switch
        {
            "eq" => TokenKind.Eq,
            "neq" => TokenKind.Neq,
            "lt" => TokenKind.Lt,
            "lte" => TokenKind.Lte,
            "gt" => TokenKind.Gt,
            "gte" => TokenKind.Gte,
            "and" => TokenKind.And,
            "or" => TokenKind.Or,
            "modulo" => TokenKind.Modulo,
            "in" => TokenKind.In,
            "true" => TokenKind.BooleanLiteral,
            "false" => TokenKind.BooleanLiteral,
            "null" => TokenKind.NullLiteral,
            _ when value.StartsWith("external:") => TokenKind.ExternalProperty,
            _ when value.StartsWith("current:") => TokenKind.ParentProperty,
            _ => TokenKind.Property,
        };

        return new Token(kind, value, start, i - start);
    }
}
