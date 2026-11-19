using Shouldly;
using Xunit;

namespace Vali.Core.Tests;

public class MapDefinitionPathsTests
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "vali-defs");
    private static readonly string MapData = Path.Combine(Path.GetTempPath(), "vali-map-data");
    private static readonly Dictionary<string, string> Tokens = new() { ["mapData"] = MapData };

    private static string Resolve(string path) => MapDefinitionPaths.ResolvePath(path, Root, Tokens);

    [Fact]
    public void RelativePathResolvesAgainstRoot() =>
        Resolve("informed/info-locs.json").ShouldBe(Path.Combine(Root, "informed", "info-locs.json"));

    [Fact]
    public void BackslashRelativePathResolves() =>
        Resolve("informed\\info-locs.json").ShouldBe(Path.Combine(Root, "informed", "info-locs.json"));

    [Fact]
    public void MapDataTokenResolves() =>
        Resolve("{mapData}/vision/export/loc-info/NO.json").ShouldBe(Path.Combine(MapData, "vision", "export", "loc-info", "NO.json"));

    [Fact]
    public void EmptyPathIsUnchanged() =>
        Resolve("").ShouldBe("");

    [Fact]
    public void UnknownTokenThrows()
    {
        var e = Should.Throw<MapDefinitionPathException>(() => Resolve("{mapdata}/x"));
        e.Message.ShouldContain("{mapdata}");
        e.Message.ShouldContain("mapData");
    }

    [Fact]
    public void AbsolutePathForThisOsIsUnchanged()
    {
        var absolute = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "x.json"));
        Resolve(absolute).ShouldBe(absolute);
    }

    [Fact]
    public void WindowsAbsolutePathOnLinuxThrows()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var e = Should.Throw<MapDefinitionPathException>(() => Resolve(@"C:\dev\priv\x.json"));
        e.Message.ShouldContain(@"C:\dev\priv\x.json");
        Should.Throw<MapDefinitionPathException>(() => Resolve(@"\\server\share\x.json"));
    }

    private static MapDefinition EveryPathField(string path) => new()
    {
        ProximityFilter = new ProximityFilter { LocationsPath = path, Radius = 10 },
        CountryProximityFilters = new() { ["NO"] = new ProximityFilter { LocationsPath = path } },
        SubdivisionProximityFilters = new() { ["NO"] = new() { ["NO-03"] = new ProximityFilter { LocationsPath = path } } },
        GeometryFilters = [new GeometryFilter { FilePath = path, InclusionMode = "exclude" }],
        CountryGeometryFilters = new() { ["NO"] = [new GeometryFilter { FilePath = path }] },
        SubdivisionGeometryFilters = new() { ["NO"] = new() { ["NO-03"] = [new GeometryFilter { FilePath = path }] } },
        GlobalLocationPreferenceFilters = [Preference(path)],
        CountryLocationPreferenceFilters = new() { ["NO"] = [Preference(path)] },
        SubdivisionLocationPreferenceFilters = new() { ["NO"] = new() { ["NO-03"] = [Preference(path)] } },
        UsedLocationsPaths = [path],
        GlobalExternalDataFiles = [path],
        CountryExternalDataFiles = new() { ["NO"] = [path] },
        SubdivisionExternalDataFiles = new() { ["NO"] = new() { ["NO-03"] = [path] } },
    };

    private static LocationPreferenceFilter Preference(string path) => new()
    {
        Expression = "1",
        ProximityFilter = new ProximityFilter { LocationsPath = path },
        GeometryFilters = [new GeometryFilter { FilePath = path }],
    };

    private static IEnumerable<string> AllPaths(MapDefinition d)
    {
        IEnumerable<string> Preferences(IEnumerable<LocationPreferenceFilter> filters) =>
            filters.SelectMany(f => f.GeometryFilters.Select(g => g.FilePath).Prepend(f.ProximityFilter.LocationsPath));

        return new[] { d.ProximityFilter.LocationsPath }
            .Concat(d.CountryProximityFilters.Values.Select(p => p.LocationsPath))
            .Concat(d.SubdivisionProximityFilters.Values.SelectMany(s => s.Values).Select(p => p.LocationsPath))
            .Concat(d.GeometryFilters.Select(g => g.FilePath))
            .Concat(d.CountryGeometryFilters.Values.SelectMany(g => g).Select(g => g.FilePath))
            .Concat(d.SubdivisionGeometryFilters.Values.SelectMany(s => s.Values).SelectMany(g => g).Select(g => g.FilePath))
            .Concat(Preferences(d.GlobalLocationPreferenceFilters))
            .Concat(Preferences(d.CountryLocationPreferenceFilters.Values.SelectMany(f => f)))
            .Concat(Preferences(d.SubdivisionLocationPreferenceFilters.Values.SelectMany(s => s.Values).SelectMany(f => f)))
            .Concat(d.UsedLocationsPaths)
            .Concat(d.GlobalExternalDataFiles)
            .Concat(d.CountryExternalDataFiles.Values.SelectMany(f => f))
            .Concat(d.SubdivisionExternalDataFiles.Values.SelectMany(s => s.Values).SelectMany(f => f))
            .ToArray();
    }

    [Fact]
    public void ResolveCoversEveryPathField()
    {
        var resolved = MapDefinitionPaths.Resolve(EveryPathField("informed/x.json"), Root, Tokens);

        var paths = AllPaths(resolved).ToArray();
        paths.Length.ShouldBe(16);
        paths.ShouldAllBe(p => p == Path.Combine(Root, "informed", "x.json"));
        resolved.ProximityFilter.Radius.ShouldBe(10);
        resolved.GeometryFilters[0].InclusionMode.ShouldBe("exclude");
    }

    [Fact]
    public void ResolveDoesNotModifyInput()
    {
        var definition = EveryPathField("informed/x.json");

        MapDefinitionPaths.Resolve(definition, Root, Tokens);

        AllPaths(definition).ShouldAllBe(p => p == "informed/x.json");
    }

    [Fact]
    public void ResolveIsIdempotent()
    {
        var once = MapDefinitionPaths.Resolve(EveryPathField("{mapData}/x.json"), Root, Tokens);
        var twice = MapDefinitionPaths.Resolve(once, Root, Tokens);

        AllPaths(twice).ShouldBe(AllPaths(once));
    }

    [Fact]
    public void FailureNamesTheField()
    {
        var definition = new MapDefinition { CountryExternalDataFiles = new() { ["NO"] = ["{bad}/x"] } };

        var e = Should.Throw<MapDefinitionPathException>(() => MapDefinitionPaths.Resolve(definition, Root, Tokens));
        e.Message.ShouldContain("countryExternalDataFiles");
        e.Message.ShouldContain("NO");
    }

    [Fact]
    public void HasRootedPathsDetectsAbsoluteEntry()
    {
        MapDefinitionPaths.HasRootedPaths(EveryPathField("informed/x.json")).ShouldBeFalse();
        MapDefinitionPaths.HasRootedPaths(new MapDefinition()).ShouldBeFalse();

        var absolute = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "x.json"));
        MapDefinitionPaths.HasRootedPaths(new MapDefinition { CountryExternalDataFiles = new() { ["NO"] = ["a.json", absolute] } }).ShouldBeTrue();
        MapDefinitionPaths.HasRootedPaths(new MapDefinition { GlobalLocationPreferenceFilters = [Preference(absolute)] }).ShouldBeTrue();
    }
}
