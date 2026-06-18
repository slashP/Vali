using System.Diagnostics;
using Shouldly;
using Spectre.Console;
using Vali.Core.Generation;
using Xunit;

namespace Vali.Core.Tests;

public class LiveGridViewTests
{
    private static (IAnsiConsole console, StringWriter output) ForcedInteractiveConsole(int width)
    {
        var sw = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            Interactive = InteractionSupport.Yes,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(sw)
        });
        console.Profile.Width = width;
        console.Profile.Height = 30;
        return (console, sw);
    }

    private static (IList<Location> locations, int regionGoalCount, int minDistance)[] Result(int produced, int goal) =>
    [
        (Enumerable.Range(0, produced).Select(_ => new Location
        {
            Google = new GoogleData(),
            Osm = new OsmData(),
            Nominatim = new NominatimData()
        }).ToList<Location>(), goal, 0)
    ];

    // CSI "0 A" is treated by terminals as "cursor up 1" (0 defaults to 1), so a one-line Live
    // region drifts upward each repaint. The view must keep the region >= 2 lines tall.
    [Fact]
    public async Task Single_country_repaints_in_place_not_drifting_upward()
    {
        var (console, output) = ForcedInteractiveConsole(width: 120); // wide enough that nothing wraps
        var progress = new GenerationProgress();
        progress.RegisterCountry("AD", 1, goal: 1000);
        progress.ReportCompleted("AD", "AD-1", Result(400, 1000));
        var view = new LiveGridView(progress, Stopwatch.StartNew(), console);

        await view.RunUntil(Task.CompletedTask);

        var raw = output.ToString();
        raw.ShouldContain("[1A");    // repositions one line up -> repaints in place
        raw.ShouldNotContain("[0A"); // the buggy "up 0" that drifts upward
    }

    [Fact]
    public async Task Multi_country_repaints_in_place_not_drifting_upward()
    {
        var (console, output) = ForcedInteractiveConsole(width: 120);
        var progress = new GenerationProgress();
        progress.RegisterCountry("AD", 1, goal: 1000);
        progress.RegisterCountry("FR", 1, goal: 1000);
        progress.ReportCompleted("AD", "AD-1", Result(400, 1000));
        var view = new LiveGridView(progress, Stopwatch.StartNew(), console);

        await view.RunUntil(Task.CompletedTask);

        output.ToString().ShouldNotContain("[0A");
    }
}
