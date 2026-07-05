namespace Vali.Host;

/// <summary>
/// Coordinates exclusive use of a country's data files between generation (which reads them)
/// and downloads (which rewrite them). Conflicts fail fast with a clear message instead of
/// colliding on Windows file sharing mid-operation.
/// </summary>
public static class CountryDataLocks
{
    private static readonly object Lock = new();
    private static readonly Dictionary<string, string> Owners = new();

    public static bool TryAcquire(IReadOnlyCollection<string> countryCodes, string operation, out string? conflict)
    {
        lock (Lock)
        {
            foreach (var code in countryCodes)
            {
                if (Owners.TryGetValue(code, out var owner))
                {
                    conflict = $"{code} is busy ({owner} in progress)";
                    return false;
                }
            }

            foreach (var code in countryCodes)
            {
                Owners[code] = operation;
            }

            conflict = null;
            return true;
        }
    }

    public static void Release(IReadOnlyCollection<string> countryCodes)
    {
        lock (Lock)
        {
            foreach (var code in countryCodes)
            {
                Owners.Remove(code);
            }
        }
    }
}
