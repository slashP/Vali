using System.Collections.Concurrent;
using System.Globalization;

namespace Vali.Core;

/// <summary>
/// Opt-in log of every max-min-distance placement (env VALI_DISTANCE_LOG=path): per call the work item, the
/// preference stage, the candidate count, the bisection's first probe and chosen distance, and a hash of what was
/// placed. Comparing two runs' logs tells which subdivisions differ and whether their distance changed.
/// </summary>
public static class DistanceLog
{
    private const string Header = "country\tsubdivision\tstage\tcandidates\tseedDistance\tchosenDistance\tplaced\tplacedHash";
    private static readonly ConcurrentQueue<Entry> _entries = new();
    private static long _sequence;

    public static string? Path { get; set; } = Environment.GetEnvironmentVariable("VALI_DISTANCE_LOG");

    public static bool Enabled => !string.IsNullOrEmpty(Path);

    public static void Reset()
    {
        _entries.Clear();
        Interlocked.Exchange(ref _sequence, 0);
    }

    /// <param name="stage">The preference filter's index, or -1 without preference filters.</param>
    public static void Record(string countryCode, string subdivision, int stage, int candidates, int seedDistance, int chosenDistance, IList<Location> placed) =>
        _entries.Enqueue(new Entry(Interlocked.Increment(ref _sequence), countryCode, subdivision, stage, candidates, seedDistance, chosenDistance, placed.Count, PlacedHash(placed)));

    /// <summary>Data rows sorted by (country, subdivision, stage); rows with the same key keep their call order.</summary>
    public static IReadOnlyList<string> Lines() => _entries
        .OrderBy(e => e.Sequence)
        .OrderBy(e => e.CountryCode, StringComparer.Ordinal)
        .ThenBy(e => e.Subdivision, StringComparer.Ordinal)
        .ThenBy(e => e.Stage)
        .Select(e => string.Create(CultureInfo.InvariantCulture,
            $"{e.CountryCode}\t{e.Subdivision}\t{e.Stage}\t{e.Candidates}\t{e.SeedDistance}\t{e.ChosenDistance}\t{e.Placed}\t{e.PlacedHash}"))
        .ToArray();

    public static void WriteIfEnabled()
    {
        if (!Enabled)
        {
            return;
        }

        File.WriteAllLines(Path!, Lines().Prepend(Header));
    }

    /// <summary>FNV-1a 64 over the placed NodeIds in order (8 bytes each, little-endian), as 16 lowercase hex digits.</summary>
    public static string PlacedHash(IList<Location> placed)
    {
        const ulong offsetBasis = 0xcbf29ce484222325;
        const ulong prime = 0x100000001b3;
        var hash = offsetBasis;
        foreach (var location in placed)
        {
            var id = (ulong)location.NodeId;
            for (var b = 0; b < 8; b++)
            {
                hash ^= (id >> (8 * b)) & 0xff;
                hash *= prime;
            }
        }

        return hash.ToString("x16", CultureInfo.InvariantCulture);
    }

    private sealed record Entry(long Sequence, string CountryCode, string Subdivision, int Stage, int Candidates, int SeedDistance, int ChosenDistance, int Placed, string PlacedHash);
}
