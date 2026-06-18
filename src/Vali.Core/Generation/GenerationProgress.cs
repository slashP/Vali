namespace Vali.Core.Generation;

public enum CountryStatus
{
    Queued,
    Working,
    Done,
    Short
}

public sealed record RegionOutcome(string CountryCode, string SubdivisionCode, int Produced, int Goal, int MinDistance);

public sealed record CountryProgress(string Code, int Completed, int Total, CountryStatus Status, int ProducedSoFar);

public sealed record ProgressSnapshot(
    IReadOnlyList<CountryProgress> Countries,
    int CountriesComplete,
    int CountriesTotal,
    int TotalLocations,
    int TotalGoal);

/// <summary>
/// The single thread-safe source of truth for generation progress. Populated in Phase A
/// (<see cref="RegisterCountry"/>), mutated from worker threads in Phase B
/// (<see cref="ReportCompleted"/>), read by the view (<see cref="Snapshot"/>) and the
/// post-run summary (<see cref="Outcomes"/>). All access is under one lock.
/// </summary>
public sealed class GenerationProgress
{
    private readonly object _lock = new();
    private readonly Dictionary<string, CountryState> _countries = new();
    private readonly List<RegionOutcome> _outcomes = new();
    private int _totalLocations;
    private int _totalGoal;

    private sealed class CountryState
    {
        public int Total;
        public int Completed;
        public int Produced;
    }

    public void RegisterCountry(string countryCode, int totalWorkItems, int goal = 0)
    {
        lock (_lock)
        {
            _countries[countryCode] = new CountryState { Total = totalWorkItems };
            _totalGoal += goal;
        }
    }

    public void ReportCompleted(string countryCode, string? subdivisionCode, (IList<Location> locations, int regionGoalCount, int minDistance)[] results)
    {
        lock (_lock)
        {
            var state = _countries[countryCode];
            state.Completed++;
            foreach (var result in results)
            {
                var produced = result.locations.Count;
                state.Produced += produced;
                _totalLocations += produced;
                _outcomes.Add(new RegionOutcome(
                    countryCode,
                    ResolveSubdivisionCode(subdivisionCode, result.locations),
                    produced,
                    result.regionGoalCount,
                    result.minDistance));
            }
        }
    }

    /// <summary>
    /// Prefers the explicit work-item subdivision code — known even when a region yields zero
    /// locations — and falls back to the code carried on the produced locations when the work item
    /// spans multiple subdivisions (and so has no single code of its own).
    /// </summary>
    internal static string ResolveSubdivisionCode(string? workItemSubdivisionCode, IList<Location> locations) =>
        workItemSubdivisionCode is { Length: > 0 }
            ? workItemSubdivisionCode
            : locations.FirstOrDefault()?.Nominatim.SubdivisionCode ?? "";

    public ProgressSnapshot Snapshot()
    {
        lock (_lock)
        {
            var shortCountries = new HashSet<string>();
            foreach (var outcome in _outcomes)
            {
                if (QualifiesAsShort(outcome.Produced, outcome.Goal))
                {
                    shortCountries.Add(outcome.CountryCode);
                }
            }

            var countries = new List<CountryProgress>(_countries.Count);
            var complete = 0;
            foreach (var (code, state) in _countries)
            {
                var status = DeriveStatus(state, shortCountries.Contains(code));
                if (status is CountryStatus.Done or CountryStatus.Short)
                {
                    complete++;
                }

                countries.Add(new CountryProgress(code, state.Completed, state.Total, status, state.Produced));
            }

            return new ProgressSnapshot(countries, complete, _countries.Count, _totalLocations, _totalGoal);
        }
    }

    public IReadOnlyList<RegionOutcome> Outcomes()
    {
        lock (_lock)
        {
            return _outcomes.ToList();
        }
    }

    public int TotalWorkItems(string countryCode)
    {
        lock (_lock)
        {
            return _countries.TryGetValue(countryCode, out var state) ? state.Total : 0;
        }
    }

    /// <summary>A region qualifies as short iff it is at least 3 below goal AND at least 5% below goal.</summary>
    public static bool QualifiesAsShort(int produced, int goal)
    {
        var shortfall = goal - produced;
        return shortfall >= 3 && shortfall * 20 >= goal;
    }

    private static CountryStatus DeriveStatus(CountryState state, bool isShort)
    {
        if (state.Total == 0 || state.Completed >= state.Total)
        {
            return isShort ? CountryStatus.Short : CountryStatus.Done;
        }

        return state.Completed == 0 ? CountryStatus.Queued : CountryStatus.Working;
    }
}
