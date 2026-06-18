using Shouldly;
using Vali.Core.Generation;
using Xunit;

namespace Vali.Core.Tests;

public class LineLogViewTests
{
    [Fact]
    public void Met_goal_shows_fraction_and_percent()
    {
        var line = LineLogView.FormatLine("DE", 16, 16);
        line.ShouldContain("DE");
        line.ShouldContain("16/16");
        line.ShouldContain("100%");
        line.ShouldNotContain("-");
        line.ShouldContain("green");
    }

    [Fact]
    public void Shortfall_beyond_margin_shows_olive_suffix()
    {
        var line = LineLogView.FormatLine("CO-GUV", 4, 11);
        line.ShouldContain("4/11");
        line.ShouldContain("-7");
        line.ShouldContain("-64%");
        line.ShouldContain("olive");
    }

    [Fact]
    public void Shortfall_within_margin_shows_plain_percent_no_suffix()
    {
        var line = LineLogView.FormatLine("XX", 99, 100); // short 1, does not qualify
        line.ShouldContain("99%");
        line.ShouldNotContain("-1");
        line.ShouldContain("green");
    }

    [Fact]
    public void No_goal_shows_only_produced()
    {
        var line = LineLogView.FormatLine("NO", 1234, -1);
        line.ShouldContain("1,234");
        line.ShouldNotContain("1,234/"); // no goal -> no produced/goal fraction ([/] markup terminator aside)
    }
}
