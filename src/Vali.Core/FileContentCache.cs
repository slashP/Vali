using System.Collections.Concurrent;

namespace Vali.Core;

/// <summary>
/// Parsed file content cached per path for the life of the process, and read again when the file's length or
/// last write time changes. A long-running host (GMC's nightly update-maps) generates in one process for days
/// while the files it reads are rewritten, so a cache keyed by path alone would keep serving the first read.
/// </summary>
public sealed class FileContentCache<T>
{
    private readonly ConcurrentDictionary<string, Lazy<Entry>> _entries = new();

    public int Count => _entries.Count;

    public T Get(string path, Func<string, T> load)
    {
        // Stamped before the read: a file rewritten while it is being read gets a newer stamp and is read again next time.
        var stamp = FileStamp.Of(path);
        var cached = _entries.GetOrAdd(path, p => new Lazy<Entry>(() => new Entry(stamp, load(p))));
        if (cached.Value.Stamp == stamp)
        {
            return cached.Value.Value;
        }

        var fresh = new Lazy<Entry>(() => new Entry(stamp, load(path)));
        return _entries.AddOrUpdate(path, fresh, (_, current) => current == cached ? fresh : current).Value.Value;
    }

    public void Clear() => _entries.Clear();

    private sealed record Entry(FileStamp Stamp, T Value);

    private readonly record struct FileStamp(long Length, DateTime LastWriteTimeUtc)
    {
        public static FileStamp Of(string path)
        {
            var info = new FileInfo(path);
            return info.Exists ? new FileStamp(info.Length, info.LastWriteTimeUtc) : new FileStamp(-1, default);
        }
    }
}
