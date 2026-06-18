using System.Diagnostics;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Vali.Core.Generation;

/// <summary>
/// Interactive view: owns the only repaint loop, on the main thread. State flows through
/// <see cref="GenerationProgress"/>, so <see cref="OnCompleted"/> is a no-op. The loop repaints
/// at <see cref="RefreshInterval"/> until the scheduler completes, then paints once more.
/// </summary>
public sealed class LiveGridView : IGenerationProgressView
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(125); // ~8 Hz

    private readonly GenerationProgress _progress;
    private readonly Stopwatch _stopwatch;
    private readonly IAnsiConsole _console;

    public LiveGridView(GenerationProgress progress, Stopwatch stopwatch, IAnsiConsole? console = null)
    {
        _progress = progress;
        _stopwatch = stopwatch;
        _console = console ?? AnsiConsole.Console;
    }

    public void OnCompleted(string countryCode, string? subdivisionCode, (IList<Location> locations, int regionGoalCount, int minDistance)[] results)
    {
        // No-op: state already flows through GenerationProgress; the repaint loop reads it.
    }

    public async Task RunUntil(Task schedulerTask)
    {
        await _console.Live(Render())
            .StartAsync(async ctx =>
            {
                while (!schedulerTask.IsCompleted)
                {
                    ctx.UpdateTarget(Render());
                    ctx.Refresh();
                    await Task.Delay(RefreshInterval);
                }

                ctx.UpdateTarget(Render()); // final paint
                ctx.Refresh();
            });
    }

    private IRenderable Render()
    {
        var snapshot = _progress.Snapshot();
        var lines = new List<IRenderable> { new Markup(GridLayout.HeaderLine(snapshot, _stopwatch.Elapsed)) };
        if (GridLayout.ShowCountryGrid(snapshot))
        {
            lines.AddRange(GridLayout.Build(snapshot, _console.Profile.Width).Select(row => (IRenderable)new Markup(row)));
        }
        else
        {
            // Spectre's Live emits "ESC[0A" to reposition a single-line region, but terminals treat
            // CSI 0 A as "cursor up 1" (the parameter 0 defaults to 1) — so a one-line display drifts
            // upward on each repaint instead of overwriting in place. Pad to a second (blank) line so
            // the region is two lines tall and Spectre emits the correct "ESC[1A".
            lines.Add(new Text(" "));
        }

        return new Rows(lines);
    }
}
