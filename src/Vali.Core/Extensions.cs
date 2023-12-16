using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Vali.Core;

public static class Extensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double rad(double angle) => angle * 0.017453292519943295769236907684886127d; // = angle * Math.Pi / 180.0d
        double havf(double diff) => Math.Pow(Math.Sin(rad(diff) / 2d), 2); // = sin²(diff / 2)
        return 12745.6 * Math.Asin(Math.Sqrt(havf(lat2 - lat1) + Math.Cos(rad(lat1)) * Math.Cos(rad(lat2)) * havf(lon2 - lon1))) * 1000; // earth radius 6.372,8‬km x 2 = 12745.6
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal CalculateDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        double rad(double angle) => angle * 0.017453292519943295769236907684886127d; // = angle * Math.Pi / 180.0d
        double havf(double diff) => Math.Pow(Math.Sin(rad(diff) / 2d), 2); // = sin²(diff / 2)
        var asin = (decimal)(12745.6 * Math.Asin(Math.Sqrt(havf((double)(lat2 - lat1)) + Math.Cos(rad((double)lat1)) * Math.Cos(rad((double)lat2)) * havf((double)(lon2 - lon1)))) * 1000);
        return asin; // earth radius 6.372,8‬km x 2 = 12745.6
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool PointsAreCloserThan(double lat1, double lon1, double lat2, double lon2, int meters)
    {
        if (meters < 1000 && (Math.Abs(lat1 - lat2) > .1 || Math.Abs(lon1 - lon2) > .1))
        {
            return false;
        }

        return CalculateDistance(lat1, lon1, lat2, lon2) < meters;
    }

    public static bool PointsAreCloserThan(decimal lat1, decimal lon1, decimal lat2, decimal lon2, int meters)
    {
        if (meters > 1000)
        {
            throw new ArgumentException($"Distance cannot be more than 1000 meters. Use {nameof(CalculateDistance)} directly.");
        }

        if (Math.Abs(lat1 - lat2) > .1m || Math.Abs(lon1 - lon2) > .1m)
        {
            return false;
        }

        return CalculateDistance(lat1, lon1, lat2, lon2) < meters;
    }

    public static string? SafeSubstring(this string value, int length) => value?.Length > length ? value.Substring(0, length) : value;

    public static string Format(this decimal d) => d.ToString(CultureInfo.InvariantCulture);
    public static string Format(this double d) => d.ToString(CultureInfo.InvariantCulture);
    public static decimal Round(this decimal d, int precision) => decimal.Round(d, precision);
    public static int RoundToInt(this decimal d) => (int)Math.Round(d, 0);
    public static int RoundToInt(this double d) => (int)Math.Round(d, 0);
    public static decimal ParseAsDecimal(this string s) => decimal.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
    public static double ParseAsDouble(this string s) => double.Parse(s, CultureInfo.InvariantCulture);
    public static int ParseAsInt(this string s) => int.Parse(s, CultureInfo.InvariantCulture);

    public static string FirstCharToLowerCase(this string str)
    {
        if (!string.IsNullOrEmpty(str) && char.IsUpper(str[0]))
        {
            return str.Length == 1 ? char.ToLower(str[0]).ToString() : char.ToLower(str[0]) + str[1..];
        }

        return str;
    }

    public static string Merge(this IEnumerable<string?> enumerable, string separator) =>
        string.Join(separator, enumerable.Where(s => !string.IsNullOrWhiteSpace(s)));

    private static void Swap<T>(this IList<T> list, int i, int j)
    {
        if (i == j)   //This check is not required but Partition function may make many calls so its for perf reason
            return;
        (list[i], list[j]) = (list[j], list[i]);
    }

    public static T PickRandom<T>(this IList<T> source)
    {
        var randIndex = Random.Shared.Next(source.Count);
        return source[randIndex];
    }

    public static IEnumerable<string> NonEmptyStrings(this IEnumerable<string?> values) => values.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!);

    public static async Task<IReadOnlyCollection<(T1 Result, T2 Data)>> ChunkAsync<T1, T2>(this ICollection<T2> source, Func<T2, Task<T1>> task, int chunkSize, string progressMessage, bool silent = false)
    {
        var results = new List<(T1 Result, T2 Data)>();
        var counter = 0;
        var startTime = DateTime.UtcNow;
        foreach (var chunk in source.Chunk(chunkSize))
        {
            var tasks = chunk.Select(x => new
            {
                Task = task(x),
                Data = x
            }).ToArray();
            await Task.WhenAll(tasks.Select(x => x.Task));
            results.AddRange(tasks.Select(x => (x.Task.Result, x.Data)));
            counter += chunkSize;
        }

        return results;
    }

    public static void Shuffle<T>(this IList<T> list)
    {
        var n = list.Count;
        while (n > 1)
        {
            n--;
            var k = Random.Shared.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    public static IEnumerable<T> TakeRandom<T>(this IEnumerable<T> collection, int take) =>
        collection.Count() <= take ? collection : collection.OrderBy(_ => Random.Shared.Next()).Take(take);

    public static T ProtoDeserializeFromFile<T>(string path)
    {
        using var file = File.OpenRead(path);
        return ProtoBuf.Serializer.Deserialize<T>(file);
    }

    public static string ReadManifestData(string embeddedFileName)
    {
        var assembly = typeof(Extensions).GetTypeInfo().Assembly;
        var resourceName = assembly.GetManifestResourceNames().First(s => s.EndsWith(embeddedFileName, StringComparison.CurrentCultureIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException("Could not load manifest resource stream.");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static IEnumerable<int> AllIndexesOf(this string str, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("the string to find may not be empty", "value");
        }

        for (var index = 0; ; index += value.Length)
        {
            index = str.IndexOf(value, index, StringComparison.InvariantCulture);
            if (index == -1)
            {
                break;
            }

            yield return index;
        }
    }

    public const char PlaceholderValue = '$';

    public static (string expressionWithPlaceholders, List<(string oldValue, string newValue)>) ReplaceValuesInSingleQuotesWithPlaceHolders(this string input)
    {
        var counter = 0;
        var list = new List<(string oldValue, string newValue)>();
        input = input.Replace("\\'", "$01");
        var matches = Regex.Matches(input, "'([^']*)'");
        foreach (Match match in matches)
        {
            var matchedValue = match.Groups[1].Value;
            var newValue = $"{PlaceholderValue}{counter++}";
            list.Add((matchedValue, newValue));
            input = input.Replace(matchedValue, newValue);
        }

        list.Add(("\\'", "$01"));
        return (input, list);
    }

    public static string RemoveMultipleSpaces(this string input) => Regex.Replace(input, @"\s+", " ");
    public static string RemoveParentheses(this string input) => input.Replace("(", "").Replace(")", "");
    public static string SpacePad(this string input) => $" {input} ";

    public static string SpacePadParentheses(this string input) =>
        input.Replace("(", "(".SpacePad()).Replace(")", ")".SpacePad());
}