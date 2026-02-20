namespace Vali.Core.Expressions;

public enum TokenKind
{
    IntegerLiteral,
    DecimalLiteral,
    StringLiteral,
    BooleanLiteral,
    NullLiteral,
    Property,
    ExternalProperty,
    ParentProperty,
    Eq,
    Neq,
    Lt,
    Lte,
    Gt,
    Gte,
    And,
    Or,
    Plus,
    Minus,
    Multiply,
    Divide,
    Modulo,
    OpenParen,
    CloseParen,
    Wildcard,
    EndOfExpression
}

public readonly record struct Token(TokenKind Kind, string Value, int Position, int Length);

public readonly record struct TextSpan(int Start, int Length);
