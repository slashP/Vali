using System.Text;

namespace Vali.Core.Google;

/// <summary>What panning rules and the override hook see about one pano.</summary>
public record PanningContext(
    string PanoId,
    string? CountryCode,
    string? SubdivisionCode,
    double Lat,
    double Lng,
    int Year,
    int Month,
    int ResolutionHeight);

/// <summary>A direction and the chance (0–100) of using it; the opposite direction is used otherwise.</summary>
public record PanningDecision(string Panning, int Percentage);

public static class PanningResolver
{
    public const string InDrivingDirection = "indrivingdirection";
    public const string AwayFromDrivingDirection = "awayfromdrivingdirection";

    /// <summary>Smallest angle between two headings, 0–180, across the 0/360 wrap.</summary>
    public static decimal AngleDiff(decimal a, decimal b)
    {
        var diff = Math.Abs(a - b) % 360;
        return diff > 180 ? 360 - diff : diff;
    }

    public static string Opposite(string panning) =>
        panning.ToLowerInvariant() switch
        {
            InDrivingDirection => AwayFromDrivingDirection,
            AwayFromDrivingDirection => InDrivingDirection,
            _ => panning
        };

    /// <summary>Random.Shared normally; a per-pano seeded Random when generation is deterministic.</summary>
    public static Random RandomFor(string panoId) =>
        GenerationDeterminism.Deterministic ? SeededRandom(panoId) : Random.Shared;

    /// <summary>FNV-1a over UTF-8 — stable across processes, unlike string.GetHashCode.</summary>
    public static Random SeededRandom(string panoId)
    {
        var hash = 2166136261u;
        foreach (var b in Encoding.UTF8.GetBytes(panoId))
        {
            hash = unchecked((hash ^ b) * 16777619u);
        }

        return new Random(unchecked((int)hash));
    }

    /// <summary>
    /// The final direction for a pano: the override hook if it returns a decision, else the
    /// country's first matching expression, else its default — then the percentage roll.
    /// Null when the country has no rule (the caller then picks a random arrow).
    /// </summary>
    public static string? ResolvePanning(
        Dictionary<string, CountryPanning?>? countryPannings,
        Func<PanningContext, PanningDecision?>? panningOverride,
        PanningContext context,
        Random rng)
    {
        var decision = panningOverride?.Invoke(context) ?? RuleDecision(countryPannings, context);
        if (decision == null)
        {
            return null;
        }

        return decision.Percentage >= 100 || rng.Next(100) < decision.Percentage
            ? decision.Panning
            : Opposite(decision.Panning);
    }

    private static PanningDecision? RuleDecision(Dictionary<string, CountryPanning?>? countryPannings, PanningContext context)
    {
        if (countryPannings == null || !countryPannings.TryGetValue(context.CountryCode ?? "", out var countryPanning) || countryPanning == null)
        {
            return null;
        }

        var location = new MapCheckrLocation
        {
            countryCode = context.CountryCode,
            resolutionHeight = context.ResolutionHeight,
            year = context.Year,
            month = context.Month,
            lat = context.Lat,
            lng = context.Lng
        };
        foreach (var panningExpression in countryPanning.PanningExpressions)
        {
            if (LocationLakeFilterer.CompileBoolMapCheckrExpression(panningExpression.Expression, false)(location))
            {
                return new PanningDecision(panningExpression.Panning, panningExpression.Percentage ?? 100);
            }
        }

        return new PanningDecision(countryPanning.DefaultPanning, countryPanning.Percentage ?? 100);
    }

    /// <summary>
    /// Picks the arrow closest to (in) or farthest from (away) the driving direction. With more
    /// than two arrows only the pair closest to 180° apart is considered. Without a panning, a
    /// random arrow; without arrows, the driving direction itself.
    /// </summary>
    public static decimal PickHeading(decimal[] arrowHeadings, decimal drivingDirection, string? panning, Random rng)
    {
        var candidates = arrowHeadings.Length > 2
            ? arrowHeadings.GetPermutations(2).MinBy(x => Math.Abs(x.Max() - x.Min() - 180))!
            : arrowHeadings;
        return (arrowHeadings.Length, panning?.ToLowerInvariant()) switch
        {
            (> 1, InDrivingDirection) => candidates.MinBy(x => AngleDiff(x, drivingDirection)),
            (> 1, AwayFromDrivingDirection) => candidates.MaxBy(x => AngleDiff(x, drivingDirection)),
            (> 2, _) => candidates[rng.Next(candidates.Length)],
            (> 0, _) => arrowHeadings[rng.Next(arrowHeadings.Length)],
            _ => drivingDirection
        };
    }
}
