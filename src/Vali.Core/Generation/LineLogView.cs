using Spectre.Console;

namespace Vali.Core.Generation;

/// <summary>
/// Non-interactive fallback: prints one concise line per completed region/country in completion
/// order. Per-completion calls arrive from worker threads, so printing is serialized under a lock.
/// Prints nothing while <see cref="ConsoleLogger.Silent"/> is set.
/// </summary>
public sealed class LineLogView : IGenerationProgressView
{
    private readonly object _lock = new();
    private readonly GenerationProgress _progress;

    public LineLogView(GenerationProgress progress) => _progress = progress;

    public void OnCompleted(string countryCode, string? subdivisionCode, (IList<Location> locations, int regionGoalCount, int minDistance)[] results)
    {
        if (ConsoleLogger.Silent)
        {
            return;
        }

        lock (_lock)
        {
            // Multi-work-item countries log one line per subdivision (the work item == one subdivision);
            // single-chunk countries log one aggregated line for the whole country.
            if (_progress.TotalWorkItems(countryCode) > 1)
            {
                foreach (var result in results)
                {
                    var sub = GenerationProgress.ResolveSubdivisionCode(subdivisionCode, result.locations);
                    var label = sub.Length > 0 ? sub : countryCode;
                    AnsiConsole.MarkupLine(FormatLine(label, result.locations.Count, result.regionGoalCount));
                }
            }
            else
            {
                var produced = results.Sum(r => r.locations.Count);
                var goal = results.Sum(r => r.regionGoalCount);
                AnsiConsole.MarkupLine(FormatLine(countryCode, produced, goal));
            }
        }
    }

    // No repaint loop — awaiting the scheduler is enough. Faults propagate to the caller.
    public Task RunUntil(Task schedulerTask) => schedulerTask;

    public static string FormatLine(string label, int produced, int goal)
    {
        if (goal <= 0)
        {
            return $"[green][[done]] {label,-8} {produced,7:N0}[/]";
        }

        if (GenerationProgress.QualifiesAsShort(produced, goal))
        {
            var shortfall = goal - produced;
            var pct = (int)Math.Round(100.0 * shortfall / goal);
            return $"[olive][[done]] {label,-8} {produced,6:N0}/{goal,-6:N0} -{shortfall} (-{pct}%)[/]";
        }

        var metPct = (int)Math.Round(100.0 * produced / goal);
        return $"[green][[done]] {label,-8} {produced,6:N0}/{goal,-6:N0} {metPct}%[/]";
    }
}
