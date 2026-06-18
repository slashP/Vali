using Spectre.Console;

namespace Vali.Core.Generation;

public sealed record SubdivisionShortfall(string Code, string? Name, int Produced, int Goal, int Shortfall, int PctBelow);

public sealed record CountryShortfall(
    string Code,
    int Produced,
    int Goal,
    int Shortfall,
    int PctBelow,
    IReadOnlyList<SubdivisionShortfall> Subdivisions);

public sealed record ShortfallReport(IReadOnlyList<CountryShortfall> Countries)
{
    public bool AnyShortfall => Countries.Count > 0;
}

/// <summary>
/// Builds the end-of-run drill-down: countries with one or more qualifying-short subdivisions,
/// worst-first, each country aggregated over ALL its subdivisions but listing only the qualifying
/// ones. Pure <see cref="Select"/> + thin Spectre <see cref="Render"/>.
/// </summary>
public static class ShortfallSummary
{
    public static ShortfallReport Select(IReadOnlyList<RegionOutcome> outcomes)
    {
        var countries = outcomes
            .GroupBy(o => o.CountryCode)
            .Select(group =>
            {
                var qualifying = group
                    .Where(o => GenerationProgress.QualifiesAsShort(o.Produced, o.Goal))
                    .Select(ToSubdivisionShortfall)
                    .OrderByDescending(s => s.Shortfall)
                    .ToArray();
                if (qualifying.Length == 0)
                {
                    return (Country: (CountryShortfall?)null, SortKey: 0);
                }

                var produced = group.Sum(o => o.Produced);
                var goal = group.Sum(o => o.Goal);
                var shortfall = goal - produced;
                var pct = goal > 0 ? (int)Math.Round(100.0 * shortfall / goal) : 0;
                var sortKey = qualifying.Sum(s => s.Shortfall);
                return (Country: new CountryShortfall(group.Key, produced, goal, shortfall, pct, qualifying), SortKey: sortKey);
            })
            .Where(x => x.Country is not null)
            .OrderByDescending(x => x.SortKey)
            .Select(x => x.Country!)
            .ToArray();

        return new ShortfallReport(countries);
    }

    public static void Render(ShortfallReport report)
    {
        if (!report.AnyShortfall)
        {
            AnsiConsole.MarkupLine("[green]All regions met their goal (within 5%).[/]");
            return;
        }

        AnsiConsole.MarkupLine("[bold]Regions short of goal (≥5% and ≥3 below target):[/]");
        foreach (var country in report.Countries)
        {
            AnsiConsole.MarkupLine(
                $"[olive]  {country.Code}  {country.Produced,6:N0} / {country.Goal,-6:N0} -{country.Shortfall} (-{country.PctBelow}%)[/]");
            foreach (var sub in country.Subdivisions)
            {
                var label = sub.Code.Length > 0 ? sub.Code : country.Code;
                var name = sub.Name is { Length: > 0 } ? $"  {Markup.Escape(sub.Name)}" : "";
                AnsiConsole.MarkupLine(
                    $"[olive]    {label,-8} {sub.Produced,5:N0} / {sub.Goal,-5:N0} -{sub.Shortfall} (-{sub.PctBelow}%){name}[/]");
            }
        }
    }

    private static SubdivisionShortfall ToSubdivisionShortfall(RegionOutcome outcome)
    {
        var shortfall = outcome.Goal - outcome.Produced;
        var pct = outcome.Goal > 0 ? (int)Math.Round(100.0 * shortfall / outcome.Goal) : 0;
        var name = SafeSubdivisionName(outcome.CountryCode, outcome.SubdivisionCode);
        return new SubdivisionShortfall(outcome.SubdivisionCode, name, outcome.Produced, outcome.Goal, shortfall, pct);
    }

    private static string? SafeSubdivisionName(string countryCode, string subdivisionCode)
    {
        if (subdivisionCode.Length == 0)
        {
            return null;
        }

        try
        {
            return SubdivisionWeights.SubdivisionName(countryCode, subdivisionCode);
        }
        catch (KeyNotFoundException)
        {
            return null; // country not in any subdivision table
        }
    }
}
