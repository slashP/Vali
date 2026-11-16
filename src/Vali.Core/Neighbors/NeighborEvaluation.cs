namespace Vali.Core.Neighbors;

/// <summary>
/// Whether max-min-distance placement evaluates neighbor filters lazily, only for candidates that pass spacing
/// (env VALI_LAZY_NEIGHBORS=1 turns it on). Off by default: on the 2026-11-15 bench set it was 5.6 % slower than eager
/// evaluation on the radius grid (world-of-hills +24 %), because placement then bisects over the unfiltered candidates.
/// </summary>
public static class NeighborEvaluation
{
    public static bool Lazy { get; set; } = FromEnvironment(Environment.GetEnvironmentVariable("VALI_LAZY_NEIGHBORS"));

    public static bool FromEnvironment(string? value) => value == "1";
}
