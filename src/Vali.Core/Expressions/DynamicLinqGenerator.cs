using System.Text;

namespace Vali.Core.Expressions;

public sealed class DynamicLinqGenerator
{
    private readonly PropertyResolver _resolver;
    private readonly string _primaryParam;
    private readonly string? _parentParam;
    private readonly string _originalExpression;

    public DynamicLinqGenerator(PropertyResolver resolver, string primaryParam, string? parentParam, string originalExpression)
    {
        _resolver = resolver;
        _primaryParam = primaryParam;
        _parentParam = parentParam;
        _originalExpression = originalExpression;
    }

    public string Generate(ExpressionNode node)
    {
        var sb = new StringBuilder();
        GenerateNode(node, sb);
        return sb.ToString();
    }

    private void GenerateNode(ExpressionNode node, StringBuilder sb)
    {
        switch (node)
        {
            case LiteralNode lit:
                GenerateLiteral(lit, sb);
                break;
            case PropertyNode prop:
                GenerateProperty(prop, sb);
                break;
            case ExternalPropertyNode ext:
                sb.Append($"({_primaryParam}.ExternalData.ContainsKey(\"{ext.Key}\") ? {_primaryParam}.ExternalData[\"{ext.Key}\"] : \"\")");
                break;
            case ParentPropertyNode parent:
                GenerateParentProperty(parent, sb);
                break;
            case BinaryNode binary:
                GenerateBinary(binary, sb);
                break;
            case UnaryMinusNode unary:
                sb.Append("(-");
                GenerateNode(unary.Operand, sb);
                sb.Append(')');
                break;
            case InNode inNode:
                GenerateIn(inNode, sb);
                break;
            case GroupNode group:
                sb.Append('(');
                GenerateNode(group.Inner, sb);
                sb.Append(')');
                break;
        }
    }

    private void GenerateLiteral(LiteralNode lit, StringBuilder sb)
    {
        switch (lit.Token.Kind)
        {
            case TokenKind.StringLiteral:
                sb.Append('"');
                sb.Append(lit.Token.Value.Replace("'", "\\'"));
                sb.Append('"');
                break;
            case TokenKind.NullLiteral:
                sb.Append("null");
                break;
            case TokenKind.BooleanLiteral:
                sb.Append(lit.Token.Value.ToLowerInvariant());
                break;
            case TokenKind.IntegerLiteral:
            case TokenKind.DecimalLiteral:
                sb.Append(lit.Token.Value);
                break;
        }
    }

    private void GenerateProperty(PropertyNode prop, StringBuilder sb)
    {
        if (!_resolver.IsValidProperty(prop.PropertyName))
        {
            var closest = _resolver.FindClosestMatch(prop.PropertyName);
            var suggestion = closest != null ? $" Did you mean '{closest}'?" : "";
            throw new ExpressionCompilationException(
                _originalExpression, prop.Token.Position, prop.Token.Length,
                $"Unknown property '{prop.PropertyName}'.{suggestion}");
        }

        sb.Append(_resolver.Resolve(prop.PropertyName, _primaryParam));
    }

    private void GenerateParentProperty(ParentPropertyNode parent, StringBuilder sb)
    {
        if (_parentParam == null)
        {
            throw new ExpressionCompilationException(
                _originalExpression, parent.Token.Position, parent.Token.Length,
                "Parent properties (current:) are not allowed in this context.");
        }

        if (!_resolver.IsValidProperty(parent.PropertyName))
        {
            var closest = _resolver.FindClosestMatch(parent.PropertyName);
            var suggestion = closest != null ? $" Did you mean '{closest}'?" : "";
            throw new ExpressionCompilationException(
                _originalExpression, parent.Token.Position, parent.Token.Length,
                $"Unknown property '{parent.PropertyName}'.{suggestion}");
        }

        sb.Append(_resolver.Resolve(parent.PropertyName, _parentParam));
    }

    private void GenerateIn(InNode inNode, StringBuilder sb)
    {
        sb.Append('(');
        for (var i = 0; i < inNode.Values.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(" || ");
            }
            sb.Append('(');
            GenerateNode(inNode.Operand, sb);
            sb.Append(" == ");
            GenerateLiteral(inNode.Values[i], sb);
            sb.Append(')');
        }
        sb.Append(')');
    }

    private void GenerateBinary(BinaryNode binary, StringBuilder sb)
    {
        sb.Append('(');
        GenerateNode(binary.Left, sb);
        sb.Append(' ');
        sb.Append(OperatorToString(binary.Operator.Kind));
        sb.Append(' ');
        GenerateNode(binary.Right, sb);
        sb.Append(')');
    }

    private static string OperatorToString(TokenKind kind) => kind switch
    {
        TokenKind.Eq => "==",
        TokenKind.Neq => "!=",
        TokenKind.Lt => "<",
        TokenKind.Lte => "<=",
        TokenKind.Gt => ">",
        TokenKind.Gte => ">=",
        TokenKind.And => "&&",
        TokenKind.Or => "||",
        TokenKind.Plus => "+",
        TokenKind.Minus => "-",
        TokenKind.Multiply => "*",
        TokenKind.Divide => "/",
        TokenKind.Modulo => "%",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}
