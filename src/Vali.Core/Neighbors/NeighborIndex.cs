using System.Collections.Concurrent;

namespace Vali.Core.Neighbors;

/// <summary>
/// The neighbor set of one work item (a subdivision's or a country's lake locations) and the grids built from it,
/// one per (radius, neighbor-only pre-filter), each on first use. Creating an index does no work.
/// </summary>
public sealed class NeighborIndex
{
    private readonly IReadOnlyList<Location> _source;
    private readonly ConcurrentDictionary<(int radius, string? expression), Lazy<NeighborGrid>> _grids = new();
    private readonly Lazy<double> _referenceAbsLat;
    private readonly Lazy<bool> _hasDuplicateNodeIds;

    public NeighborIndex() : this([])
    {
    }

    public NeighborIndex(IReadOnlyList<Location> source)
    {
        _source = source;
        _referenceAbsLat = new Lazy<double>(() =>
        {
            var max = 0.0;
            for (var i = 0; i < _source.Count; i++)
            {
                var a = Math.Abs(_source[i].Lat);
                if (a > max) max = a;
            }

            return max;
        });
        _hasDuplicateNodeIds = new Lazy<bool>(() =>
        {
            var seen = new HashSet<long>(_source.Count);
            for (var i = 0; i < _source.Count; i++)
            {
                if (!seen.Add(_source[i].NodeId)) return true;
            }

            return false;
        });
    }

    /// <summary>Max |lat| over the whole source, so grids built from a pre-filtered subset share one reference.</summary>
    public double ReferenceAbsLat => _referenceAbsLat.Value;

    public bool HasDuplicateNodeIds => _hasDuplicateNodeIds.Value;

    /// <summary>
    /// The grid of the source points within reach of <paramref name="radiusMeters"/>, keeping only those that pass
    /// <paramref name="neighborOnlyExpression"/> when it is not null. Built once per key; thread-safe.
    /// </summary>
    public NeighborGrid GridFor(int radiusMeters, string? neighborOnlyExpression) =>
        _grids.GetOrAdd((radiusMeters, neighborOnlyExpression), key => new Lazy<NeighborGrid>(() => Build(key.radius, key.expression))).Value;

    private NeighborGrid Build(int radiusMeters, string? neighborOnlyExpression)
    {
        using (GenPerf.Measure(GenPerf.Phase.NeighborGrid))
        {
            IReadOnlyList<Location> points = _source;
            if (neighborOnlyExpression != null)
            {
                var predicate = LocationLakeFilterer.CompileBoolLocationExpression(neighborOnlyExpression);
                var kept = new List<Location>();
                for (var i = 0; i < _source.Count; i++)
                {
                    if (predicate(_source[i])) kept.Add(_source[i]);
                }

                points = kept;
            }

            return new NeighborGrid(points, radiusMeters, ReferenceAbsLat);
        }
    }
}
