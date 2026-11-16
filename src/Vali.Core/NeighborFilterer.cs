using Vali.Core.Neighbors;
using Loc = Vali.Core.Location;

namespace Vali.Core;

public class NeighborFilterer
{
    /// <summary>
    /// Keeps the locations that pass <paramref name="neighborFilter"/> against the neighbors in
    /// <paramref name="neighborIndex"/>. Throws at once for a combination with no meaning.
    /// </summary>
    public static IEnumerable<Loc> FilterByNeighbors(
        IEnumerable<Loc> locations,
        NeighborIndex neighborIndex,
        NeighborFilter neighborFilter)
    {
        var check = NeighborCheck.For([neighborFilter], neighborIndex)!;
        // Inner PLINQ takes only a fair share of cores: the outer generation pass already runs many
        // work items concurrently, so a ProcessorCount-wide scan per item would starve the pool.
        return locations.AsParallel().WithDegreeOfParallelism(GenerationConcurrency.InnerDegreeOfParallelism()).Where(check.Passes);
    }
}
