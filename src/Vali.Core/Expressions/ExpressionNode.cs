namespace Vali.Core.Expressions;

public abstract record ExpressionNode(TextSpan Span);

public sealed record LiteralNode(Token Token) : ExpressionNode(new TextSpan(Token.Position, Token.Length));

public sealed record PropertyNode(Token Token, string PropertyName) : ExpressionNode(new TextSpan(Token.Position, Token.Length));

public sealed record ExternalPropertyNode(Token Token, string Key) : ExpressionNode(new TextSpan(Token.Position, Token.Length));

public sealed record ParentPropertyNode(Token Token, string PropertyName) : ExpressionNode(new TextSpan(Token.Position, Token.Length));

public sealed record BinaryNode(ExpressionNode Left, Token Operator, ExpressionNode Right)
    : ExpressionNode(new TextSpan(Left.Span.Start, Right.Span.Start + Right.Span.Length - Left.Span.Start));

public sealed record UnaryMinusNode(Token Operator, ExpressionNode Operand)
    : ExpressionNode(new TextSpan(Operator.Position, Operand.Span.Start + Operand.Span.Length - Operator.Position));

public sealed record GroupNode(ExpressionNode Inner, TextSpan GroupSpan) : ExpressionNode(GroupSpan);

public sealed record InNode(ExpressionNode Operand, Token InToken, LiteralNode[] Values, TextSpan InSpan) : ExpressionNode(InSpan);
