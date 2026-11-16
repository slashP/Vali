using Shouldly;
using Vali.Core.Neighbors;
using Xunit;

namespace Vali.Core.Tests;

public class NeighborEvaluationTests
{
    // Lazy evaluation measured slower than eager on world-of-hills (+24 %), so it stays opt-in.
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("0", false)]
    [InlineData("1", true)]
    public void Lazy_evaluation_is_opt_in(string? value, bool expected)
    {
        NeighborEvaluation.FromEnvironment(value).ShouldBe(expected);
    }
}
