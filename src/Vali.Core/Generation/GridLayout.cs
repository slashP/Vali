namespace Vali.Core.Generation;

/// <summary>
/// Pure formatting for the live grid + header. Emits Spectre **markup strings** (e.g. "[green]…[/]")
/// so it carries no Spectre dependency and is fully unit-testable. The view turns these strings
/// into renderables. Cell content is ASCII/box-drawing only, so no markup escaping is needed.
/// </summary>
public static class GridLayout
{
    public const int BarWidth = 5;

    // "CC ▓▓▓░░ 100%" = 13 visible chars; + 2-space gutter = 15.
    public const int CellWidth = 15;

    public const int HeaderBarWidth = 20;

    public static int ColumnCount(int consoleWidth) => Math.Max(1, consoleWidth / CellWidth);

    /// <summary>
    /// The per-country grid is only worth showing when there is more than one country; for a single
    /// country the header line already conveys its progress.
    /// </summary>
    public static bool ShowCountryGrid(ProgressSnapshot snapshot) => snapshot.CountriesTotal > 1;

    public static string HeaderLine(ProgressSnapshot snapshot, TimeSpan elapsed)
    {
        var elapsedText = elapsed.ToString(@"hh\:mm\:ss");

        // Without a fixed location goal (max-count / evenly strategies) there is no denominator
        // to draw a bar against, so just show the running count.
        if (snapshot.TotalGoal <= 0)
        {
            return $"[bold]Generating[/] · {snapshot.TotalLocations:N0} locations · {elapsedText}";
        }

        var ratio = Math.Clamp((double)snapshot.TotalLocations / snapshot.TotalGoal, 0, 1);
        var filled = Math.Clamp((int)Math.Round(ratio * HeaderBarWidth), 0, HeaderBarWidth);
        var bar = $"[green]{new string('▓', filled)}[/][grey]{new string('░', HeaderBarWidth - filled)}[/]";
        var pct = (int)Math.Round(ratio * 100);
        return $"[bold]Generating[/] · {bar} {snapshot.TotalLocations:N0} / {snapshot.TotalGoal:N0} ({pct}%) · {elapsedText}";
    }

    public static IReadOnlyList<string> Build(ProgressSnapshot snapshot, int consoleWidth)
    {
        var columns = ColumnCount(consoleWidth);
        var ordered = snapshot.Countries.OrderBy(c => c.Code, StringComparer.Ordinal).ToArray();
        var rows = new List<string>();
        for (var i = 0; i < ordered.Length; i += columns)
        {
            var cells = ordered.Skip(i).Take(columns).Select(Cell);
            rows.Add(string.Join("  ", cells));
        }

        return rows;
    }

    private static string Cell(CountryProgress country)
    {
        var color = country.Status switch
        {
            CountryStatus.Queued => "grey",
            CountryStatus.Working => "cyan",
            CountryStatus.Short => "olive",
            _ => "green"
        };

        // Every country renders the same way: a 0–100% completion bar, regardless of how many
        // work items it has. A country with one work item is simply 0% or 100%; a country with
        // zero work items (nothing to do) is complete, so 100%.
        var ratio = country.Total > 0
            ? Math.Clamp((double)country.Completed / country.Total, 0, 1)
            : 1.0;
        var filled = Math.Clamp((int)Math.Round(ratio * BarWidth), 0, BarWidth);
        var bar = new string('▓', filled) + new string('░', BarWidth - filled);
        var indicator = $"{(int)Math.Round(ratio * 100)}%";

        var visible = $"{country.Code,-2} {bar} {indicator,4}";
        return $"[{color}]{visible}[/]";
    }
}
