namespace Vali.Core;

/// <summary>
/// Off-by-default verification toggle (env VALI_DETERMINISTIC=1). When None, the generator's
/// randomness sources are neutralized so two runs — and pre/post any refactor — produce
/// byte-identical output. NOT a MapDefinition option; real maps keep their randomness.
/// </summary>
public static class GenerationDeterminism
{
    public enum DistributionRandomness
    {
        Random,
        None
    }

    public static DistributionRandomness Randomness { get; set; } =
        Environment.GetEnvironmentVariable("VALI_DETERMINISTIC") == "1"
            ? DistributionRandomness.None
            : DistributionRandomness.Random;

    public static bool Deterministic => Randomness == DistributionRandomness.None;

    /// <summary>
    /// Canonical, scheduling-independent order (by NodeId) when deterministic; input order otherwise.
    /// Used to neutralize the unordered PLINQ in the filter/country paths before distribution.
    /// </summary>
    public static IEnumerable<Location> InCanonicalOrder(IEnumerable<Location> locations) =>
        Deterministic ? locations.OrderBy(l => l.NodeId) : locations;
}
