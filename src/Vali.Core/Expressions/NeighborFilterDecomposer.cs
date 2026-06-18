using System.Text;

namespace Vali.Core.Expressions;

/// <summary>
/// Splits a neighbor-filter expression on its top-level <c>and</c> operands and returns the
/// conjunction of the operands that reference only the neighbor (no <c>current:</c>), sliced
/// verbatim from the source. Used to pre-filter the neighbor bucket set: a neighbor that fails
/// this predicate cannot satisfy the full expression, so it can be dropped before scanning.
/// Returns null when there is no neighbor-only operand or the expression cannot be analysed.
/// </summary>
public static class NeighborFilterDecomposer
{
    public static string? NeighborOnlyExpression(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression) || expression == "*")
        {
            return null;
        }

        try
        {
            var tokens = ExpressionLexer.Tokenize(expression);
            var ast = ExpressionParser.Parse(tokens, expression);
            var operands = new List<ExpressionNode>();
            CollectTopLevelAndOperands(Unwrap(ast), operands);

            var neighborOnly = operands.Where(node => !ContainsParent(node)).ToList();
            if (neighborOnly.Count == 0)
            {
                return null;
            }

            var sb = new StringBuilder();
            for (var i = 0; i < neighborOnly.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(" and ");
                }

                var span = neighborOnly[i].Span;
                sb.Append(expression.Substring(span.Start, span.Length));
            }

            return sb.ToString();
        }
        catch
        {
            // Decomposition must never affect correctness; on any surprise, signal "no pre-filter".
            return null;
        }
    }

    // A parenthesised whole expression is semantically its inner node for and-splitting.
    private static ExpressionNode Unwrap(ExpressionNode node) =>
        node is GroupNode g ? Unwrap(g.Inner) : node;

    // Flatten the left-associative top-level `and` chain. Operands are kept as-is (GroupNodes
    // retain their parentheses via their span), so re-joining with " and " is precedence-safe.
    private static void CollectTopLevelAndOperands(ExpressionNode node, List<ExpressionNode> acc)
    {
        if (node is BinaryNode { Operator.Kind: TokenKind.And } b)
        {
            CollectTopLevelAndOperands(b.Left, acc);
            CollectTopLevelAndOperands(b.Right, acc);
        }
        else
        {
            acc.Add(node);
        }
    }

    private static bool ContainsParent(ExpressionNode node) => node switch
    {
        ParentPropertyNode => true,
        BinaryNode b => ContainsParent(b.Left) || ContainsParent(b.Right),
        GroupNode g => ContainsParent(g.Inner),
        UnaryMinusNode u => ContainsParent(u.Operand),
        InNode i => ContainsParent(i.Operand) || i.Values.Any(ContainsParent),
        _ => false
    };
}
