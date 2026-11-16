using System.Globalization;
using Vali.Core;
using Vali.Core.Expressions;
using Vali.Core.Neighbors;

namespace Vali.Benchmarks;

/// <summary>
/// Retained memory of the neighbor structures one work item keeps for a map's neighbor filters, old against new,
/// measured with full collections around each build (peak working set under Server GC follows GC budgets and free
/// memory, not live data). Old: the geohash bucket dictionary at the map's precision plus one pre-filtered copy per
/// pre-filter-safe filter with a neighbor-only part (all alive together while Filter enumerates). New: the
/// NeighborIndex with every grid NeighborCheck builds for those filters.
///
/// <c>neighbor-memory [--subdivisions=GB/GB-ENG,...]</c>
/// </summary>
public static class NeighborMemory
{
    private static readonly string[] DefaultSubdivisions = ["GB/GB-ENG", "US/US-CA", "NO/NO-55"];
    private static readonly string[] Maps =
        [@"general\narnia", @"meme\end-of-coverage", @"meme\world-of-hills", @"collab\new-world", @"general\yellowbelly", @"pro\5KABLEWORLD", @"general\world-slim"];

    public static int Run(string[] args)
    {
        ConsoleLogger.Silent = true;
        var subdivisions = DefaultSubdivisions;
        foreach (var arg in args.Skip(1))
        {
            if (arg.StartsWith("--subdivisions=", StringComparison.Ordinal)) subdivisions = arg["--subdivisions=".Length..].Split(',', StringSplitOptions.RemoveEmptyEntries);
            else
            {
                Console.Error.WriteLine($"Unknown argument {arg}. Usage: neighbor-memory [--subdivisions=GB/GB-ENG,...]");
                return 2;
            }
        }

        Console.WriteLine("subdivision, map, filters, locations, locationsMB, oldMB, newMB, newOverOld, extraShareOfLocations");
        foreach (var spec in subdivisions)
        {
            var (countryCode, subdivision) = spec.Split('/') is [var c, var d] ? (c, d) : throw new ArgumentException($"Not CC/SUBDIVISION: {spec}");
            var file = Path.Combine(Scenarios.LakeRoot, countryCode, $"{countryCode}+{subdivision}.bin");
            // What the work item holds anyway: its locations, the yardstick for the neighbor structures' share.
            var locationsBytes = Retained(() => Extensions.ProtoDeserializeFromFile<Location[]>(file));
            foreach (var map in Maps)
            {
                var path = map == @"general\world-slim" ? @"C:\dev\priv\vali-maps\world-slim.json" : Path.Combine(@"C:\dev\priv\map-definitions", map, "map.json");
                var definition = Serializer.Deserialize<MapDefinition>(File.ReadAllText(path))!.ApplyDefaults();
                var filters = definition.AllNeighborFilters().ToArray();
                if (filters.Length == 0)
                {
                    continue;
                }

                // Fresh per map, with that map's external labels merged as generation does (Narnia's and world-slim's
                // neighbor-only parts read them).
                var all = Extensions.ProtoDeserializeFromFile<Location[]>(file);
                foreach (var externalFile in definition.GlobalExternalDataFiles.Concat(definition.CountryExternalDataFiles.GetValueOrDefault(countryCode, [])))
                {
                    var data = Extensions.TryJsonDeserializeFromFile<Dictionary<string, Dictionary<string, string>>>(externalFile, []);
                    foreach (var location in all)
                    {
                        if (data.TryGetValue(location.LocationId.ToString(), out var values))
                        {
                            foreach (var (key, value) in values) location.ExternalData[key] = value;
                        }
                    }
                }

                var oldBytes = Retained(() =>
                {
                    var buckets = LocationLookupService.Bucketize(all, definition.HashPrecisionFromNeighborFiltersRadius());
                    var kept = new List<object> { buckets };
                    foreach (var f in filters)
                    {
                        if (f.Bound is "some" or "gte" or "lte" or "none" && !string.IsNullOrEmpty(f.Expression) &&
                            NeighborFilterDecomposer.NeighborOnlyExpression(f.Expression) is { } neighborOnly)
                        {
                            kept.Add(PreFilterBuckets(buckets, LocationLakeFilterer.CompileBoolLocationExpression(neighborOnly)));
                        }
                    }

                    return kept;
                });
                var newBytes = Retained(() =>
                {
                    var index = new NeighborIndex(all);
                    // Valid filters only; NeighborCheck.For builds every grid they need.
                    var check = NeighborCheck.For(filters, index);
                    return new object?[] { index, check };
                });
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"{spec}, {map}, {filters.Length}, {all.Length}, {locationsBytes / 1048576.0:F0}, {oldBytes / 1048576.0:F1}, {newBytes / 1048576.0:F1}, {(double)newBytes / Math.Max(oldBytes, 1):F2}, {(newBytes - oldBytes) * 100.0 / locationsBytes:F1}%"));
            }
        }

        return 0;
    }

    private static long Retained(Func<object> build)
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        var before = GC.GetTotalMemory(forceFullCollection: true);
        var built = build();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        var after = GC.GetTotalMemory(forceFullCollection: true);
        GC.KeepAlive(built);
        return after - before;
    }

    // Same as the old NeighborFilterer.PreFilterBuckets.
    private static Dictionary<ulong, List<Location>> PreFilterBuckets(Dictionary<ulong, List<Location>> buckets, Func<Location, bool> predicate)
    {
        var result = new Dictionary<ulong, List<Location>>(buckets.Count);
        foreach (var (key, list) in buckets)
        {
            List<Location>? kept = null;
            foreach (var loc in list)
            {
                if (predicate(loc))
                {
                    (kept ??= new List<Location>()).Add(loc);
                }
            }

            if (kept != null)
            {
                result[key] = kept;
            }
        }

        return result;
    }
}
