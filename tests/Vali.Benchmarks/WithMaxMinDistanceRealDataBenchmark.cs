using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using Vali.Core;

namespace Vali.Benchmarks;

/// <summary>Quick "feel" config: few invocations, no multi-minute iterations.</summary>
public class QuickConfig : ManualConfig
{
    public QuickConfig()
    {
        AddJob(Job.Default
            .WithStrategy(RunStrategy.Monitoring)
            .WithLaunchCount(1)
            .WithWarmupCount(1)
            .WithIterationCount(3)
            .WithInvocationCount(1)
            .WithUnrollFactor(1));
        AddDiagnoser(MemoryDiagnoser.Default);
        AddColumnProvider(DefaultColumnProviders.Instance);
        AddLogger(BenchmarkDotNet.Loggers.ConsoleLogger.Default); // NOT Vali.Core.ConsoleLogger
    }
}

public record Scenario(string Name, string CountryCode, string Subdivision, int GoalCount, int MinMinDistance);

public static class Scenarios
{
    public const string LakeRoot = @"C:\dev\priv\location-lake";

    // Default suite — excludes GB-ENG (RAM-heavy).
    public static readonly Scenario[] Default =
    [
        new("NoSmall",  "NO", "NO-34", 40,     100),  // common: few points, dense subdivision -> coarse end
        new("NoMedium", "NO", "NO-34", 800,    100),  // p95-ish goal
        new("NoMin25",  "NO", "NO-34", 800,    25),   // minMin sweep
        new("NoMin200", "NO", "NO-34", 800,    200),  // minMin sweep
        new("UsCa",     "US", "US-CA", 4000,   50),   // large subdivision
        new("BrMg",     "BR", "BR-MG", 4000,   50),   // large subdivision
        new("Fallback", "NO", "NO-03", 200000, 100),  // goal > achievable -> all probes fail -> index 0
    ];

    // Opt-in stress (run explicitly via --filter '*Stress*').
    public static readonly Scenario[] Stress =
    [
        new("UsTx",  "US", "US-TX",  5000, 50),
        new("GbEng", "GB", "GB-ENG", 6000, 50),
    ];

    public static Location[] Load(Scenario s)
    {
        var path = System.IO.Path.Combine(LakeRoot, s.CountryCode, $"{s.CountryCode}+{s.Subdivision}.bin");
        return Extensions.ProtoDeserializeFromFile<Location[]>(path);
    }
}

[Config(typeof(QuickConfig))]
public class WithMaxMinDistanceRealDataBenchmark
{
    private Location[] _locations = null!;
    private readonly LocationProbability _probability = new() { DefaultWeight = 1 };

    [ParamsSource(nameof(ScenarioSource))]
    public Scenario Scenario { get; set; } = null!;

    public static IEnumerable<Scenario> ScenarioSource => Scenarios.Default;

    [GlobalSetup]
    public void Setup()
    {
        ConsoleLogger.Silent = true; // Vali.Core.ConsoleLogger
        _locations = Scenarios.Load(Scenario);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _locations = null!;
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    [Benchmark]
    public (IList<Location> locations, int minDistance) Run() =>
        LocationDistributor.WithMaxMinDistance<Location, long>(
            _locations, Scenario.GoalCount, locationProbability: _probability,
            locationsAlreadyInMap: null, avoidShuffle: false, minMinDistance: Scenario.MinMinDistance);
}

[Config(typeof(QuickConfig))]
public class WithMaxMinDistanceStressBenchmark
{
    private Location[] _locations = null!;
    private readonly LocationProbability _probability = new() { DefaultWeight = 1 };

    [ParamsSource(nameof(ScenarioSource))]
    public Scenario Scenario { get; set; } = null!;

    public static IEnumerable<Scenario> ScenarioSource => Scenarios.Stress;

    [GlobalSetup]
    public void Setup()
    {
        ConsoleLogger.Silent = true;
        _locations = Scenarios.Load(Scenario);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _locations = null!;
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    [Benchmark]
    public (IList<Location> locations, int minDistance) Run() =>
        LocationDistributor.WithMaxMinDistance<Location, long>(
            _locations, Scenario.GoalCount, locationProbability: _probability,
            locationsAlreadyInMap: null, avoidShuffle: false, minMinDistance: Scenario.MinMinDistance);
}

/// <summary>Prints (scenario, minDistance, count) so quality can be compared before/after the change.</summary>
public static class QualityReport
{
    public static void Run()
    {
        ConsoleLogger.Silent = true;
        var probability = new LocationProbability { DefaultWeight = 1 };
        foreach (var s in Scenarios.Default)
        {
            var locations = Scenarios.Load(s);
            var (locs, dist) = LocationDistributor.WithMaxMinDistance<Location, long>(
                locations, s.GoalCount, locationProbability: probability,
                locationsAlreadyInMap: null, avoidShuffle: true, minMinDistance: s.MinMinDistance);
            Console.WriteLine($"{s.Name,-10} goal={s.GoalCount,7} minMin={s.MinMinDistance,4} -> count={locs.Count,7} minDistance={dist,7}");
        }
    }
}
