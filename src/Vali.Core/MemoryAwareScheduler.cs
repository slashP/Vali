namespace Vali.Core;

/// <summary>
/// Runs <paramref name="items"/> through <paramref name="body"/> concurrently, bounded by both a
/// core cap and a byte budget. Admits in <see cref="SpreadLargestAcrossSchedule{TItem}"/> order
/// while (inFlightCost + cost &lt;= budgetBytes) AND (running &lt; maxConcurrency); if nothing is
/// running it force-admits a single item even when it exceeds the budget, so a huge item never
/// deadlocks. Body exceptions propagate (the whole run faults), matching the previous
/// RunLimitedNumberAtATime.
/// </summary>
public static class MemoryAwareScheduler
{
    /// <summary>
    /// When <paramref name="cancellationToken"/> is signalled, admission of new items stops but
    /// already-running items are drained to completion; the partial results gathered so far are
    /// returned (cancellation is cooperative — it does not throw and does not abort in-flight work).
    /// </summary>
    public static async Task<IReadOnlyList<TResult>> RunWithMemoryBudget<TItem, TResult>(
        IReadOnlyList<TItem> items,
        Func<TItem, long> costBytes,
        Func<TItem, Task<TResult>> body,
        int maxConcurrency,
        long budgetBytes,
        CancellationToken cancellationToken = default)
    {
        var cap = Math.Max(1, maxConcurrency);
        var ordered = SpreadLargestAcrossSchedule(items, costBytes);
        var results = new List<TResult>(ordered.Length);
        var running = new List<(Task<TResult> task, long cost)>();
        long inFlightCost = 0;
        var next = 0;

        while ((next < ordered.Length && !cancellationToken.IsCancellationRequested) || running.Count > 0)
        {
            while (next < ordered.Length && !cancellationToken.IsCancellationRequested)
            {
                var cost = costBytes(ordered[next]);
                var fits = inFlightCost + cost <= budgetBytes && running.Count < cap;
                if (!fits && running.Count > 0)
                {
                    break; // wait for an in-flight item to free a slot / budget
                }

                running.Add((body(ordered[next]), cost));
                inFlightCost += cost;
                next++;

                if (!fits)
                {
                    break; // forced a single over-budget item; let it run alone
                }
            }

            if (running.Count == 0)
            {
                break; // cancelled with nothing in flight (or no items at all)
            }

            var finished = await Task.WhenAny(running.Select(r => r.task));
            var idx = running.FindIndex(r => r.task == finished);
            inFlightCost -= running[idx].cost;
            running.RemoveAt(idx);
            results.Add(await finished);
        }

        return results;
    }

    /// <summary>
    /// Orders items so the concurrent working set stays roughly uniform rather than spiking when
    /// every largest item runs at once. Sorts by cost descending, then interleaves the heavy half
    /// with the light half: <c>[heaviest, median, 2nd-heaviest, median+1, …]</c>. The heaviest items
    /// still start first (so the longest jobs aren't left as a tail straggler — the makespan benefit
    /// of largest-first), but each is paired with a light item, so a full first wave is ~half heavy
    /// instead of all heavy. That lowers peak memory on roomy machines and, where the byte budget is
    /// the binding constraint (the distributed tool on modest RAM), lets light items fill the budget
    /// alongside the few big ones admitted — keeping cores busy instead of stalling on big-only.
    /// </summary>
    public static TItem[] SpreadLargestAcrossSchedule<TItem>(IReadOnlyList<TItem> items, Func<TItem, long> costBytes)
    {
        var desc = items.OrderByDescending(costBytes).ToArray();
        var result = new TItem[desc.Length];
        var mid = (desc.Length + 1) / 2; // heavy half = desc[0..mid), light half = desc[mid..]
        int heavy = 0, light = mid, w = 0;
        while (w < result.Length)
        {
            if (heavy < mid)
            {
                result[w++] = desc[heavy++];
            }

            if (light < desc.Length)
            {
                result[w++] = desc[light++];
            }
        }

        return result;
    }
}
