using Shouldly;
using Xunit;

namespace Vali.Core.Tests;

public class FileContentCacheTests : IDisposable
{
    private readonly string _file = Path.GetTempFileName();

    public void Dispose() => File.Delete(_file);

    [Fact]
    public void Get_loads_once_while_the_file_is_unchanged()
    {
        File.WriteAllText(_file, "abc");
        var cache = new FileContentCache<string>();
        var loads = 0;
        string Load(string path) { loads++; return File.ReadAllText(path); }

        cache.Get(_file, Load).ShouldBe("abc");
        cache.Get(_file, Load).ShouldBe("abc");

        loads.ShouldBe(1);
    }

    [Fact]
    public void Get_reloads_when_the_length_changes()
    {
        File.WriteAllText(_file, "abc");
        var cache = new FileContentCache<string>();
        cache.Get(_file, File.ReadAllText);

        File.WriteAllText(_file, "abcd");

        cache.Get(_file, File.ReadAllText).ShouldBe("abcd");
    }

    [Fact]
    public void Get_reloads_when_only_the_last_write_time_changes()
    {
        File.WriteAllText(_file, "abc");
        File.SetLastWriteTimeUtc(_file, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var cache = new FileContentCache<string>();
        cache.Get(_file, File.ReadAllText);

        File.WriteAllText(_file, "xyz");
        File.SetLastWriteTimeUtc(_file, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));

        cache.Get(_file, File.ReadAllText).ShouldBe("xyz");
    }

    [Fact]
    public void Get_of_a_missing_file_uses_the_loader_and_is_read_again_once_the_file_exists()
    {
        File.Delete(_file);
        var cache = new FileContentCache<string>();
        cache.Get(_file, p => File.Exists(p) ? File.ReadAllText(p) : "missing").ShouldBe("missing");

        File.WriteAllText(_file, "abc");

        cache.Get(_file, p => File.Exists(p) ? File.ReadAllText(p) : "missing").ShouldBe("abc");
    }

    [Fact]
    public void Clear_drops_cached_content()
    {
        File.WriteAllText(_file, "abc");
        var cache = new FileContentCache<string>();
        var loads = 0;
        string Load(string path) { loads++; return File.ReadAllText(path); }
        cache.Get(_file, Load);

        cache.Clear();

        cache.Count.ShouldBe(0);
        cache.Get(_file, Load);
        loads.ShouldBe(2);
    }
}
