using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Vali.Core;

public static class GenPerf
{
    public enum Phase
    {
        Deserialize = 0,
        ExternalMerge = 1,
        Bucketize = 2,
        Filter = 3,
        Distribute = 4,
        StoreProjection = 5,
        // Nested: time spent inside Filter (B3) or Distribute (lazy neighbor filters), so left out of the CPU sum.
        NeighborGrid = 6,
        NeighborEval = 7,
    }

    private const int PhaseCount = 8;
    private const int TopLevelPhaseCount = 6;

    private static readonly long[] _ticks = new long[PhaseCount];

    // One (candidate, filter) neighbor evaluation per call from every generation thread: striped per processor and
    // padded to a cache line each, so 32 threads do not contend on one counter.
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    private struct NeighborStripe
    {
        [FieldOffset(0)] public long Evaluations;
        [FieldOffset(8)] public long Visits;
        [FieldOffset(16)] public long Ticks;
    }

    private static readonly NeighborStripe[] _neighborStripes = new NeighborStripe[Math.Max(Environment.ProcessorCount, 1)];
    private static readonly ConcurrentDictionary<string, double> _countryWorkSeconds = new();
    private static long _allocatedBaseline;
    private static TimeSpan _cpuBaseline;
    private static readonly int[] _collectionBaseline = new int[3];
    private static TimeSpan _pauseBaseline;

    public static bool Enabled { get; set; } =
        Environment.GetEnvironmentVariable("VALI_GEN_PERF") == "1";

    public static void Reset()
    {
        Array.Clear(_ticks, 0, _ticks.Length);
        Array.Clear(_neighborStripes);
        _countryWorkSeconds.Clear();
        _allocatedBaseline = GC.GetTotalAllocatedBytes(precise: true);
        for (var generation = 0; generation < _collectionBaseline.Length; generation++)
        {
            _collectionBaseline[generation] = GC.CollectionCount(generation);
        }

        _pauseBaseline = GC.GetTotalPauseDuration();
        using var process = Process.GetCurrentProcess();
        _cpuBaseline = process.TotalProcessorTime;
    }

    public static Scope Measure(Phase phase) => new(phase);

    public static void AddCountryWork(string countryCode, TimeSpan elapsed) =>
        _countryWorkSeconds.AddOrUpdate(countryCode, elapsed.TotalSeconds, (_, v) => v + elapsed.TotalSeconds);

    /// <summary>
    /// One neighbor-filter evaluation of one candidate: <paramref name="visits"/> within-radius neighbors looked at,
    /// <paramref name="elapsedTicks"/> added to <see cref="Phase.NeighborEval"/>. Callers skip the call (and the
    /// timestamps) when <see cref="Enabled"/> is false.
    /// </summary>
    public static void AddNeighborEvaluation(int visits, long elapsedTicks)
    {
        ref var stripe = ref _neighborStripes[Thread.GetCurrentProcessorId() % _neighborStripes.Length];
        Interlocked.Increment(ref stripe.Evaluations);
        Interlocked.Add(ref stripe.Visits, visits);
        Interlocked.Add(ref stripe.Ticks, elapsedTicks);
    }

    public static (long evaluations, long visits) NeighborWork()
    {
        long evaluations = 0, visits = 0;
        for (var i = 0; i < _neighborStripes.Length; i++)
        {
            evaluations += Interlocked.Read(ref _neighborStripes[i].Evaluations);
            visits += Interlocked.Read(ref _neighborStripes[i].Visits);
        }

        return (evaluations, visits);
    }

    public static double PhaseSeconds(Phase phase) =>
        PhaseTicks(phase) / (double)Stopwatch.Frequency;

    private static long PhaseTicks(Phase phase)
    {
        var ticks = Interlocked.Read(ref _ticks[(int)phase]);
        if (phase == Phase.NeighborEval)
        {
            for (var i = 0; i < _neighborStripes.Length; i++)
            {
                ticks += Interlocked.Read(ref _neighborStripes[i].Ticks);
            }
        }

        return ticks;
    }

    public static IReadOnlyList<(string cc, double seconds)> CountryWork() =>
        _countryWorkSeconds.Select(kvp => (kvp.Key, kvp.Value)).ToArray();

    public static void Report(TimeSpan wall)
    {
        var phaseSeconds = new double[PhaseCount];
        for (var i = 0; i < PhaseCount; i++)
        {
            phaseSeconds[i] = PhaseSeconds((Phase)i);
        }

        using var process = Process.GetCurrentProcess();
        var peak = process.PeakWorkingSet64;
        var allocated = GC.GetTotalAllocatedBytes(precise: true) - _allocatedBaseline;
        var cpuSeconds = (process.TotalProcessorTime - _cpuBaseline).TotalSeconds;

        Console.WriteLine(BuildReport(
            phaseSeconds,
            CountryWork(),
            wall.TotalSeconds,
            Environment.ProcessorCount,
            peak,
            allocated));
        Console.WriteLine(BuildCpuUtilizationLine(cpuSeconds, wall.TotalSeconds, Environment.ProcessorCount));
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"GC mode:           {(System.Runtime.GCSettings.IsServerGC ? "Server" : "Workstation")}"));
        Console.WriteLine(BuildGcLine(
            GC.GetConfigurationVariables(),
            GC.CollectionCount(0) - _collectionBaseline[0],
            GC.CollectionCount(1) - _collectionBaseline[1],
            GC.CollectionCount(2) - _collectionBaseline[2],
            (GC.GetTotalPauseDuration() - _pauseBaseline).TotalSeconds,
            wall.TotalSeconds));
        Console.WriteLine($"{"Runtime",-18}{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
    }

    public static string BuildReport(
        double[] phaseSeconds,
        IReadOnlyList<(string cc, double seconds)> countryWork,
        double wallSeconds,
        int processorCount,
        long peakWorkingSetBytes,
        long allocatedBytes)
    {
        var labels = new[]
        {
            "deserialize", "external merge", "bucketize", "filter", "distribute", "store/projection", "neighbor grid", "neighbor eval"
        };
        var cpuSummed = phaseSeconds.Take(TopLevelPhaseCount).Sum();
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("-- Map generation timing (skipVerification) --");
        sb.AppendLine(string.Create(inv, $"{"Phase",-18}{"CPU(sum)",10}{"Share",8}"));
        for (var i = 0; i < phaseSeconds.Length; i++)
        {
            var share = cpuSummed > 0 ? phaseSeconds[i] / cpuSummed * 100 : 0;
            var nested = i >= TopLevelPhaseCount ? "  (inside filter/distribute)" : "";
            sb.AppendLine(string.Create(inv, $"{labels[i],-18}{phaseSeconds[i],8:N2} s{share,6:N0}%{nested}"));
        }

        sb.AppendLine(string.Create(inv, $"{"CPU summed",-18}{cpuSummed,8:N2} s"));
        sb.AppendLine(string.Create(inv, $"{"Wall clock",-18}{wallSeconds,8:N2} s"));
        var parallelism = wallSeconds > 0 ? cpuSummed / wallSeconds : 0;
        sb.AppendLine(string.Create(inv, $"{"Parallelism",-18}{parallelism,7:N1}x  (of {processorCount} cores)"));
        var perCountry = string.Join(" · ", countryWork
            .OrderByDescending(c => c.seconds)
            .Select(c => string.Create(inv, $"{c.cc} {c.seconds:N2}s")));
        sb.AppendLine($"Per-country work:  {perCountry}");
        sb.AppendLine(string.Create(inv, $"Peak working set:  {peakWorkingSetBytes / (1024.0 * 1024 * 1024):N2} GB"));
        sb.AppendLine(string.Create(inv, $"Allocated (run):   {allocatedBytes / (1024.0 * 1024 * 1024):N2} GB"));
        var (evaluations, visits) = NeighborWork();
        sb.AppendLine(string.Create(inv, $"Neighbor work:     {evaluations:N0} evaluations, {visits:N0} neighbor visits"));
        return sb.ToString();
    }

    // Actual CPU consumed vs the machine's capacity over the wall. Unlike "Parallelism" (CPU-summed
    // wall-in-phase / wall, which counts a thread blocked inside a phase as "busy"), this reflects
    // real core occupancy — a low value means cores sat idle (e.g. thread-pool starvation), not that
    // the work was cheap.
    public static string BuildCpuUtilizationLine(double cpuSeconds, double wallSeconds, int processorCount)
    {
        var capacity = wallSeconds * processorCount;
        var utilization = capacity > 0 ? cpuSeconds / capacity * 100 : 0;
        return string.Create(CultureInfo.InvariantCulture,
            $"{"Actual CPU",-18}{cpuSeconds,8:N2} s  ({utilization:N0}% of {processorCount} cores x wall)");
    }

    // Collections and pause time over the run, with the GC configuration that produced them: under Workstation GC
    // one thread collects while every worker waits, which is what made production generation 2-4x slower in 2026-11.
    public static string BuildGcLine(IReadOnlyDictionary<string, object> configuration, int gen0, int gen1, int gen2, double pauseSeconds, double wallSeconds)
    {
        var server = configuration.TryGetValue("ServerGC", out var s) && Convert.ToBoolean(s, CultureInfo.InvariantCulture);
        var heaps = configuration.TryGetValue("HeapCount", out var h) ? Convert.ToInt64(h, CultureInfo.InvariantCulture) : 1;
        var datas = configuration.TryGetValue("GCDynamicAdaptationMode", out var d) && Convert.ToInt64(d, CultureInfo.InvariantCulture) == 1;
        var pauseShare = wallSeconds > 0 ? pauseSeconds / wallSeconds * 100 : 0;
        return string.Create(CultureInfo.InvariantCulture,
            $"{"GC",-18}{(server ? "Server" : "Workstation")}, {heaps} heap{(heaps == 1 ? "" : "s")}, DATAS {(datas ? "on" : "off")}; " +
            $"{gen0} gen0 / {gen1} gen1 / {gen2} gen2; paused {pauseSeconds:N2} s ({pauseShare:N0}% of wall)");
    }

    public readonly struct Scope : IDisposable
    {
        private readonly Phase _phase;
        private readonly long _start;
        private readonly bool _active;

        internal Scope(Phase phase)
        {
            _phase = phase;
            _active = Enabled;
            _start = _active ? Stopwatch.GetTimestamp() : 0L;
        }

        public void Dispose()
        {
            if (_active)
            {
                Interlocked.Add(ref _ticks[(int)_phase], Stopwatch.GetTimestamp() - _start);
            }
        }
    }
}
