namespace Vali.Core;

/// <summary>
/// Coordinates the two levels of parallelism during map generation.
///
/// The <em>outer</em> level runs up to <c>ProcessorCount</c> subdivision/country work items
/// concurrently (<see cref="LocationLakeMapGenerator"/> + <see cref="MemoryAwareScheduler"/>).
/// The <em>inner</em> level is the PLINQ used inside a single work item — the neighbor-filter
/// scan (<see cref="NeighborFilterer"/>) and the coverage-density <c>GetSome</c> pass.
///
/// Without coordination each work item spawns a <c>ProcessorCount</c>-wide PLINQ on top of an
/// already-saturated thread pool, so the inner partition tasks queue behind the outer tasks and
/// the pool injects replacement threads only ~1-2/s. CPU then collapses (measured ~18% of cores
/// on a neighbor-filter-heavy world map) and the first work item finishes very late. Splitting the
/// machine evenly across the work items currently in flight keeps total thread demand ~=
/// <c>ProcessorCount</c>: 32 work items in flight ⇒ inner DOP 1 (rely on the outer fan-out);
/// a single country-as-single work item ⇒ inner DOP 32 (the PLINQ is the only parallelism).
/// </summary>
public static class GenerationConcurrency
{
    private static int _inFlight;

    /// <summary>Work items currently executing (between <see cref="EnterWorkItem"/> and dispose).</summary>
    public static int InFlight => Volatile.Read(ref _inFlight);

    /// <summary>Marks a work item as executing; dispose when it finishes.</summary>
    public static IDisposable EnterWorkItem()
    {
        Interlocked.Increment(ref _inFlight);
        return new WorkItemScope();
    }

    /// <summary>
    /// Cores one in-flight work item should use for its inner PLINQ right now: an even split of
    /// the machine across the work items currently in flight (always &gt;= 1).
    /// </summary>
    public static int InnerDegreeOfParallelism() =>
        InnerDegreeOfParallelism(Environment.ProcessorCount, InFlight);

    public static int InnerDegreeOfParallelism(int processorCount, int inFlight) =>
        Math.Max(1, processorCount / Math.Max(1, inFlight));

    private sealed class WorkItemScope : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                Interlocked.Decrement(ref _inFlight);
            }
        }
    }
}
