using Shouldly;
using Vali.Core.Generation;
using Xunit;

namespace Vali.Core.Tests;

public class GridLayoutTests
{
    private static ProgressSnapshot Snap(params CountryProgress[] countries) =>
        new(countries, countries.Count(c => c.Status is CountryStatus.Done or CountryStatus.Short), countries.Length, 0, 0);

    [Theory]
    [InlineData(110, 7)]
    [InlineData(80, 5)]
    [InlineData(40, 2)]
    [InlineData(10, 1)] // narrower than a cell -> still one column
    public void ColumnCount_scales_with_width(int width, int expected) =>
        GridLayout.ColumnCount(width).ShouldBe(expected);

    [Theory]
    [InlineData(0, false)] // nothing to show
    [InlineData(1, false)] // single country -> header conveys it; no per-country grid
    [InlineData(2, true)]
    [InlineData(50, true)]
    public void ShowCountryGrid_only_when_more_than_one_country(int countriesTotal, bool expected) =>
        GridLayout.ShowCountryGrid(new ProgressSnapshot([], 0, countriesTotal, 0, 0)).ShouldBe(expected);

    [Fact]
    public void Header_with_goal_shows_progress_bar_locations_and_elapsed()
    {
        var snap = new ProgressSnapshot([], CountriesComplete: 84, CountriesTotal: 120, TotalLocations: 40_075, TotalGoal: 110_000);
        var header = GridLayout.HeaderLine(snap, TimeSpan.FromSeconds(83));
        header.ShouldContain("40,075");
        header.ShouldContain("110,000");
        header.ShouldContain("00:01:23");
        header.ShouldContain("▓"); // filled portion of the bar
        header.ShouldContain("░"); // empty portion of the bar
        header.ShouldContain("36%"); // 40,075 / 110,000 ≈ 36%
        header.ShouldNotContain("countries");
    }

    [Fact]
    public void Header_without_goal_shows_running_count_no_bar()
    {
        var snap = new ProgressSnapshot([], CountriesComplete: 5, CountriesTotal: 10, TotalLocations: 1203, TotalGoal: 0);
        var header = GridLayout.HeaderLine(snap, TimeSpan.FromSeconds(83));
        header.ShouldContain("1,203");
        header.ShouldContain("00:01:23");
        header.ShouldNotContain("▓");
        header.ShouldNotContain("░");
    }

    [Fact]
    public void Multi_item_working_cell_shows_partial_bar_and_percent()
    {
        var rows = GridLayout.Build(Snap(new CountryProgress("DE", 3, 5, CountryStatus.Working, 0)), 30);
        rows.Count.ShouldBe(1);
        rows[0].ShouldContain("DE");
        rows[0].ShouldContain("▓▓▓░░"); // ▓▓▓░░ for 60%
        rows[0].ShouldContain("60%");
        rows[0].ShouldContain("cyan");
    }

    [Fact]
    public void Multi_item_done_cell_is_full_green_100()
    {
        var rows = GridLayout.Build(Snap(new CountryProgress("US", 16, 16, CountryStatus.Done, 0)), 30);
        rows[0].ShouldContain("▓▓▓▓▓");
        rows[0].ShouldContain("100%");
        rows[0].ShouldContain("green");
    }

    [Fact]
    public void Short_cell_is_olive()
    {
        var rows = GridLayout.Build(Snap(new CountryProgress("CO", 11, 11, CountryStatus.Short, 0)), 30);
        rows[0].ShouldContain("olive");
    }

    [Fact]
    public void Single_item_queued_shows_empty_bar_zero_percent_grey()
    {
        var rows = GridLayout.Build(Snap(new CountryProgress("DE", 0, 1, CountryStatus.Queued, 0)), 30);
        rows[0].ShouldContain("░░░░░");
        rows[0].ShouldContain("0%");
        rows[0].ShouldContain("grey");
    }

    [Fact]
    public void Single_item_done_shows_full_bar_hundred_percent()
    {
        var rows = GridLayout.Build(Snap(new CountryProgress("DE", 1, 1, CountryStatus.Done, 0)), 30);
        rows[0].ShouldContain("▓▓▓▓▓");
        rows[0].ShouldContain("100%");
    }

    [Fact]
    public void Zero_work_item_country_shows_full_bar_hundred_percent()
    {
        var rows = GridLayout.Build(Snap(new CountryProgress("XX", 0, 0, CountryStatus.Done, 0)), 30);
        rows[0].ShouldContain("▓▓▓▓▓");
        rows[0].ShouldContain("100%");
    }

    [Fact]
    public void Cells_are_alphabetical_row_major()
    {
        var snap = Snap(
            new CountryProgress("ZZ", 1, 1, CountryStatus.Done, 0),
            new CountryProgress("AA", 1, 1, CountryStatus.Done, 0),
            new CountryProgress("MM", 1, 1, CountryStatus.Done, 0));
        var rows = GridLayout.Build(snap, 30); // width 30 -> 2 columns
        rows.Count.ShouldBe(2);
        rows[0].IndexOf("AA", StringComparison.Ordinal).ShouldBeLessThan(rows[0].IndexOf("MM", StringComparison.Ordinal));
        rows[1].ShouldContain("ZZ");
    }
}
