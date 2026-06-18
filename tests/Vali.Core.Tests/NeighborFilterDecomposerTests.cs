using Shouldly;
using Vali.Core.Expressions;
using Xunit;

namespace Vali.Core.Tests;

public class NeighborFilterDecomposerTests
{
    [Theory]
    // Drops the cross-referencing (current:) conjunct, keeps the neighbor-only ones verbatim.
    [InlineData(
        "external:Buildings eq 'No' and (external:RoadLines eq 'No' or external:Surface eq 'gravel') and current:WayId eq WayId",
        "external:Buildings eq 'No' and (external:RoadLines eq 'No' or external:Surface eq 'gravel')")]
    // Only a current: conjunct -> nothing neighbor-only -> null.
    [InlineData("current:WayId eq WayId", null)]
    // A single neighbor-only operand is returned unchanged.
    [InlineData("external:Buildings eq 'No'", "external:Buildings eq 'No'")]
    [InlineData("Surface eq 'gravel' and current:WayId eq WayId", "Surface eq 'gravel'")]
    // A top-level OR containing a current: reference cannot be split safely -> null.
    [InlineData("external:Buildings eq 'No' or current:WayId eq WayId", null)]
    // No current: anywhere -> the whole expression is neighbor-only.
    [InlineData("Buildings25 eq 0 and Roads50 eq 1", "Buildings25 eq 0 and Roads50 eq 1")]
    // A parenthesised neighbor-only operand keeps its parentheses (sliced verbatim via its span).
    [InlineData(
        "(external:RoadLines eq 'No' or external:Surface eq 'gravel') and current:WayId eq WayId",
        "(external:RoadLines eq 'No' or external:Surface eq 'gravel')")]
    // Outer parentheses are unwrapped before splitting; the kept operand carries no extra parens.
    [InlineData("(external:X eq 'No' and current:WayId eq WayId)", "external:X eq 'No'")]
    // Empty / whitespace / wildcard -> null (no pre-filter).
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("*", null)]
    public void Should_extract_neighbor_only_sub_expression(string? input, string? expected)
    {
        NeighborFilterDecomposer.NeighborOnlyExpression(input).ShouldBe(expected);
    }

    [Fact]
    public void Should_return_null_for_null_expression()
    {
        NeighborFilterDecomposer.NeighborOnlyExpression(null).ShouldBeNull();
    }

    [Fact]
    public void Should_return_null_instead_of_throwing_on_unparseable_expression()
    {
        // Correctness guarantee: decomposition must never throw; an unanalysable expression
        // simply yields "no pre-filter" so FilterByNeighbors falls back to the full bucket set.
        NeighborFilterDecomposer.NeighborOnlyExpression("external:Buildings eq").ShouldBeNull();
    }
}
