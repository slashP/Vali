using System.Diagnostics;
using System.Globalization;
using Vali.Core;
using Vali.Core.Neighbors;

namespace Vali.Benchmarks;

/// <summary>
/// Real-data identity check for neighbor filters: runs the old geohash scan (<see cref="LegacyNeighborFilterer"/>) and
/// the new grid evaluator side by side on whole lake subdivisions, for a sample of candidates and a matrix of filters,
/// and compares the kept NodeIds per candidate. Exit code 0 when every comparison matches.
///
/// <c>neighbor-harness [--sample=N] [--subdivisions=GB/GB-ENG[:N],...]</c> (N after a colon overrides the sample size
/// for that subdivision).
/// </summary>
public static class NeighborHarness
{
    private static readonly string[] DefaultSubdivisions = ["GB/GB-ENG", "US/US-CA", "FR/FR-75C", "NO/NO-55", "NO/NO-56", "NO/NO-21"];
    private static readonly int[] Radii = [120, 250, 500, 2000];

    public static int Run(string[] args)
    {
        ConsoleLogger.Silent = true;
        var sample = 5_000;
        var subdivisions = DefaultSubdivisions;
        foreach (var arg in args.Skip(1))
        {
            if (arg.StartsWith("--sample=", StringComparison.Ordinal)) sample = int.Parse(arg["--sample=".Length..], CultureInfo.InvariantCulture);
            else if (arg.StartsWith("--subdivisions=", StringComparison.Ordinal)) subdivisions = arg["--subdivisions=".Length..].Split(',', StringSplitOptions.RemoveEmptyEntries);
            else
            {
                Console.Error.WriteLine($"Unknown argument {arg}. Usage: neighbor-harness [--sample=N] [--subdivisions=GB/GB-ENG[:N],...]");
                return 2;
            }
        }

        var filters = Filters();
        // legacyS and newS include building the bucket dictionary or grid the filter is the first to use.
        Console.WriteLine("subdivision, bound, limit, dir, radius, expression, candidates, legacyKept, newKept, mismatches, legacyS, newS");
        long totalMismatches = 0;
        var perRadius = new SortedDictionary<int, (double legacy, double next)>();
        foreach (var entry in subdivisions)
        {
            var (spec, subdivisionSample) = entry.Split(':') is [var s, var size] ? (s, int.Parse(size, CultureInfo.InvariantCulture)) : (entry, sample);
            var (countryCode, subdivision) = spec.Split('/') is [var c, var d] ? (c, d) : throw new ArgumentException($"Not CC/SUBDIVISION: {spec}");
            var path = Path.Combine(Scenarios.LakeRoot, countryCode, $"{countryCode}+{subdivision}.bin");
            var all = Extensions.ProtoDeserializeFromFile<Location[]>(path);
            var sorted = all.OrderBy(l => l.NodeId).ToArray();
            var step = Math.Max(1, sorted.Length / Math.Max(1, subdivisionSample));
            var candidates = sorted.Where((_, i) => i % step == 0).Take(subdivisionSample).ToArray();
            var index = new NeighborIndex(all);
            var legacyBuckets = new Dictionary<Vali.Core.Hash.HashPrecision, Dictionary<ulong, List<Location>>>();
            Console.Error.WriteLine($"{spec}: {all.Length:N0} locations, {candidates.Length:N0} candidates (sample {subdivisionSample:N0})");

            foreach (var filter in filters)
            {
                var mapDefinition = new MapDefinition { NeighborFilters = [filter] };
                var precision = mapDefinition.HashPrecisionFromNeighborFiltersRadius()!.Value;
                // Both sides pay for their structures on first use: the bucket dictionary per precision here, each grid
                // (radius, neighbor-only pre-filter) inside the new evaluator.
                var sw = Stopwatch.StartNew();
                if (!legacyBuckets.TryGetValue(precision, out var buckets))
                {
                    buckets = LocationLookupService.Bucketize(all, precision);
                    legacyBuckets[precision] = buckets;
                }

                var legacy = LegacyNeighborFilterer.FilterByNeighbors(candidates, buckets, filter, mapDefinition).Select(l => l.NodeId).ToHashSet();
                var legacyS = sw.Elapsed.TotalSeconds;
                sw.Restart();
                var next = NeighborFilterer.FilterByNeighbors(candidates, index, filter).Select(l => l.NodeId).ToHashSet();
                var newS = sw.Elapsed.TotalSeconds;

                var mismatches = legacy.Count + next.Count - 2 * legacy.Count(next.Contains);
                totalMismatches += mismatches;
                var (l, n) = perRadius.GetValueOrDefault(filter.Radius);
                perRadius[filter.Radius] = (l + legacyS, n + newS);
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"{spec}, {filter.Bound}, {filter.Limit?.ToString(CultureInfo.InvariantCulture) ?? "null"}, {filter.CheckEachCardinalDirectionSeparately}, {filter.Radius}, \"{filter.Expression}\", {candidates.Length}, {legacy.Count}, {next.Count}, mismatches {mismatches}, {legacyS:F3}, {newS:F3}"));
            }

            legacyBuckets.Clear();
            GC.Collect();
        }

        Console.WriteLine($"Total mismatches {totalMismatches}");
        foreach (var (radius, (legacy, next)) in perRadius)
        {
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"Radius {radius,5}: legacy {legacy,9:F2} s, new {next,8:F2} s, speed-up {(next > 0 ? legacy / next : 0),6:F1}x"));
        }

        return totalMismatches == 0 ? 0 : 1;
    }

    private static List<NeighborFilter> Filters()
    {
        var bounds = new (string bound, int? limit)[]
        {
            ("some", null), ("gte", 0), ("gte", 1), ("gte", 3), ("gte", null), ("lte", 0), ("lte", 2), ("lte", null),
            ("none", null), ("all", null), ("percentage-gte", 50), ("percentage-gte", 75), ("percentage-lte", 25),
            ("percentage-gte", null),
        };
        var expressions = new[] { "", "*", "Year gte 2018", "current:Year gt Year", "Year gte 2015 and current:Year lte Year" };
        var filters = new List<NeighborFilter>();
        foreach (var radius in Radii)
        foreach (var (bound, limit) in bounds)
        foreach (var directions in new[] { false, true })
        foreach (var expression in expressions)
        {
            if (bound == "lte" && limit is null && directions)
            {
                continue; // throws in both
            }

            filters.Add(new NeighborFilter { Bound = bound, Limit = limit, CheckEachCardinalDirectionSeparately = directions, Expression = expression, Radius = radius });
        }

        // The maps' own filters: Narnia's cross part, then the lake-only filters of world-of-hills, end-of-coverage
        // and yellowbelly as their definitions expand them.
        filters.Add(new NeighborFilter { Bound = "some", Radius = 2000, Expression = "current:WayId eq WayId" });
        foreach (var map in new[] { @"meme\world-of-hills", @"meme\end-of-coverage", @"general\yellowbelly" })
        {
            var definition = Serializer.Deserialize<MapDefinition>(File.ReadAllText(Path.Combine(@"C:\dev\priv\map-definitions", map, "map.json")))!.ApplyDefaults();
            filters.AddRange(definition.NeighborFilters);
            filters.AddRange(definition.GlobalLocationPreferenceFilters.SelectMany(p => p.NeighborFilters));
        }

        return filters.DistinctBy(f => (f.Bound, f.Limit, f.CheckEachCardinalDirectionSeparately, f.Radius, f.Expression)).ToList();
    }
}
