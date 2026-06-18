using Shouldly;
using Xunit;

namespace Vali.Core.Tests;

public class MemoryAwareSchedulerTests
{
    private sealed record Item(int Id, long Cost);

    [Fact]
    public async Task Runs_every_item_once_and_returns_all_results()
    {
        var items = Enumerable.Range(0, 50).Select(i => new Item(i, 10)).ToArray();
        var results = await MemoryAwareScheduler.RunWithMemoryBudget(
            items, x => x.Cost, async x => { await Task.Yield(); return x.Id; },
            maxConcurrency: 4, budgetBytes: 1000);
        results.OrderBy(x => x).ShouldBe(Enumerable.Range(0, 50));
    }

    [Fact]
    public async Task Never_exceeds_max_concurrency()
    {
        var current = 0;
        var max = 0;
        var gate = new object();
        var items = Enumerable.Range(0, 100).Select(i => new Item(i, 1)).ToArray();
        await MemoryAwareScheduler.RunWithMemoryBudget(
            items, x => x.Cost,
            async x =>
            {
                lock (gate) { current++; max = Math.Max(max, current); }
                await Task.Delay(5);
                lock (gate) { current--; }
                return x.Id;
            },
            maxConcurrency: 8, budgetBytes: long.MaxValue);
        max.ShouldBeLessThanOrEqualTo(8);
        max.ShouldBeGreaterThan(1); // proves it actually overlapped
    }

    [Fact]
    public async Task Never_exceeds_memory_budget_when_items_individually_fit()
    {
        long current = 0;
        long max = 0;
        var gate = new object();
        // cost 30, budget 100 -> at most 3 in flight (90<=100, 120>100)
        var items = Enumerable.Range(0, 60).Select(i => new Item(i, 30)).ToArray();
        await MemoryAwareScheduler.RunWithMemoryBudget(
            items, x => x.Cost,
            async x =>
            {
                lock (gate) { current += x.Cost; max = Math.Max(max, current); }
                await Task.Delay(5);
                lock (gate) { current -= x.Cost; }
                return x.Id;
            },
            maxConcurrency: 100, budgetBytes: 100);
        max.ShouldBeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task Runs_single_item_larger_than_budget()
    {
        var items = new[] { new Item(1, 10_000) };
        var results = await MemoryAwareScheduler.RunWithMemoryBudget(
            items, x => x.Cost, async x => { await Task.Yield(); return x.Id; },
            maxConcurrency: 4, budgetBytes: 100);
        results.ShouldBe(new[] { 1 });
    }

    [Fact]
    public async Task Oversized_items_run_one_at_a_time()
    {
        var current = 0;
        var max = 0;
        var gate = new object();
        var items = Enumerable.Range(0, 5).Select(i => new Item(i, 10_000)).ToArray(); // each > budget
        await MemoryAwareScheduler.RunWithMemoryBudget(
            items, x => x.Cost,
            async x =>
            {
                lock (gate) { current++; max = Math.Max(max, current); }
                await Task.Delay(5);
                lock (gate) { current--; }
                return x.Id;
            },
            maxConcurrency: 4, budgetBytes: 100);
        max.ShouldBe(1);
    }

    [Fact]
    public async Task Empty_input_returns_empty()
    {
        var results = await MemoryAwareScheduler.RunWithMemoryBudget(
            Array.Empty<Item>(), x => x.Cost, x => Task.FromResult(0),
            maxConcurrency: 4, budgetBytes: 100);
        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task Cancellation_stops_dispatch_drains_in_flight_and_returns_partial_results()
    {
        var started = 0;
        var gate = new object();
        var cts = new CancellationTokenSource();
        var items = Enumerable.Range(0, 200).Select(i => new Item(i, 1)).ToArray();

        var results = await MemoryAwareScheduler.RunWithMemoryBudget(
            items, x => x.Cost,
            async x =>
            {
                int count;
                lock (gate) { count = ++started; }
                if (count >= 10)
                {
                    cts.Cancel(); // ask to stop once enough work has started
                }

                await Task.Delay(5);
                return x.Id;
            },
            maxConcurrency: 4, budgetBytes: long.MaxValue, cancellationToken: cts.Token);

        started.ShouldBeLessThan(items.Length);              // stopped admitting new work
        started.ShouldBeGreaterThanOrEqualTo(10);            // ran up to the stop point
        results.Count.ShouldBe(started);                     // every started item finished and is returned
    }

    [Fact]
    public async Task Cancelled_before_start_returns_empty_without_running_anything()
    {
        var started = 0;
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var items = Enumerable.Range(0, 50).Select(i => new Item(i, 1)).ToArray();

        var results = await MemoryAwareScheduler.RunWithMemoryBudget(
            items, x => x.Cost,
            x => { Interlocked.Increment(ref started); return Task.FromResult(x.Id); },
            maxConcurrency: 4, budgetBytes: long.MaxValue, cancellationToken: cts.Token);

        results.ShouldBeEmpty();
        started.ShouldBe(0);
    }

    [Fact]
    public void SpreadLargestAcrossSchedule_is_a_permutation_with_the_largest_first()
    {
        var items = Enumerable.Range(0, 50).Select(i => new Item(i, i * 10)).ToArray();
        var spread = MemoryAwareScheduler.SpreadLargestAcrossSchedule(items, x => x.Cost);
        spread.Select(x => x.Id).OrderBy(x => x).ShouldBe(items.Select(x => x.Id).OrderBy(x => x));
        spread[0].Cost.ShouldBe(items.Max(x => x.Cost)); // heaviest still starts first
    }

    [Fact]
    public void SpreadLargestAcrossSchedule_interleaves_heavy_and_light_halves()
    {
        // costs 100,90,80,70,60,50,40,30 -> desc indices 0..7; mid=4.
        var items = new[] { 100, 90, 80, 70, 60, 50, 40, 30 }.Select((c, i) => new Item(i, c)).ToArray();
        var spread = MemoryAwareScheduler.SpreadLargestAcrossSchedule(items, x => x.Cost);
        spread.Select(x => x.Cost).ShouldBe(new long[] { 100, 60, 90, 50, 80, 40, 70, 30 });
    }

    [Fact]
    public void SpreadLargestAcrossSchedule_lowers_the_cost_of_the_first_concurrency_window()
    {
        // 64 items, the heaviest 32 are far heavier than the rest.
        var items = Enumerable.Range(0, 64).Select(i => new Item(i, i < 32 ? 1000 + i : 1)).ToArray();
        const int cap = 32;
        var descWindow = items.OrderByDescending(x => x.Cost).Take(cap).Sum(x => x.Cost);
        var spreadWindow = MemoryAwareScheduler.SpreadLargestAcrossSchedule(items, x => x.Cost).Take(cap).Sum(x => x.Cost);
        spreadWindow.ShouldBeLessThan(descWindow); // first wave holds fewer heavy items => lower peak
    }
}
