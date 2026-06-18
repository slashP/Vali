using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
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
    }

    private const int PhaseCount = 6;

    private static readonly long[] _ticks = new long[PhaseCount];
    private static readonly ConcurrentDictionary<string, double> _countryWorkSeconds = new();
    private static long _allocatedBaseline;
    private static TimeSpan _cpuBaseline;

    public static bool Enabled { get; set; } =
        Environment.GetEnvironmentVariable("VALI_GEN_PERF") == "1";

    public static void Reset()
    {
        Array.Clear(_ticks, 0, _ticks.Length);
        _countryWorkSeconds.Clear();
        _allocatedBaseline = GC.GetTotalAllocatedBytes(precise: true);
        using var process = Process.GetCurrentProcess();
        _cpuBaseline = process.TotalProcessorTime;
    }

    public static Scope Measure(Phase phase) => new(phase);

    public static void AddCountryWork(string countryCode, TimeSpan elapsed) =>
        _countryWorkSeconds.AddOrUpdate(countryCode, elapsed.TotalSeconds, (_, v) => v + elapsed.TotalSeconds);

    public static double PhaseSeconds(Phase phase) =>
        _ticks[(int)phase] / (double)Stopwatch.Frequency;

    public static IReadOnlyList<(string cc, double seconds)> CountryWork() =>
        _countryWorkSeconds.Select(kvp => (kvp.Key, kvp.Value)).ToArray();

    public static void Report(TimeSpan wall)
    {
        var phaseSeconds = new double[PhaseCount];
        for (var i = 0; i < PhaseCount; i++)
        {
            phaseSeconds[i] = _ticks[i] / (double)Stopwatch.Frequency;
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
            "deserialize", "external merge", "bucketize", "filter", "distribute", "store/projection"
        };
        var cpuSummed = phaseSeconds.Sum();
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("-- Map generation timing (skipVerification) --");
        sb.AppendLine(string.Create(inv, $"{"Phase",-18}{"CPU(sum)",10}{"Share",8}"));
        for (var i = 0; i < phaseSeconds.Length; i++)
        {
            var share = cpuSummed > 0 ? phaseSeconds[i] / cpuSummed * 100 : 0;
            sb.AppendLine(string.Create(inv, $"{labels[i],-18}{phaseSeconds[i],8:N2} s{share,6:N0}%"));
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
