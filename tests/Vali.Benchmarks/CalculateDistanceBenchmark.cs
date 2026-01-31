using BenchmarkDotNet.Attributes;
using Vali.Core;
using Vali.Core.Hash;

namespace Vali.Benchmarks;

[MemoryDiagnoser]
public class GetSomeBenchmark
{
    private Location[] _locations = null!;
    private readonly LocationProbability _probability = new() { DefaultWeight = 1 };

    [Params(500, 2000)]
    public int LocationCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        _locations = Enumerable.Range(0, LocationCount).Select(i => new Location
        {
            NodeId = i,
            Lat = 59.0 + rng.NextDouble() * 2,
            Lng = 10.0 + rng.NextDouble() * 2,
            Google = new GoogleData { PanoId = "", CountryCode = "NO" },
            Osm = new OsmData(),
            Nominatim = new NominatimData { CountryCode = "NO", SubdivisionCode = "NO-03" }
        }).ToArray();
    }

    [Benchmark]
    public IList<Location> GetSome() =>
        LocationDistributor.GetSome<Location, long>(_locations, 100, 500, _probability);
}

[MemoryDiagnoser]
public class WithMaxMinDistanceBenchmark
{
    private Location[] _locations = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        _locations = Enumerable.Range(0, 50_000).Select(i => new Location
        {
            NodeId = i,
            Lat = 59.0 + rng.NextDouble() * 2,
            Lng = 10.0 + rng.NextDouble() * 2,
            Google = new GoogleData { PanoId = "", CountryCode = "NO" },
            Osm = new OsmData(),
            Nominatim = new NominatimData { CountryCode = "NO", SubdivisionCode = "NO-03" }
        }).ToArray();
    }

    [Benchmark]
    public (IList<Location> locations, int minDistance) GetSome() =>
        LocationDistributor.WithMaxMinDistance<Location, long>(_locations, 2000, locationProbability: new(), [], avoidShuffle: true, minMinDistance: 100);
}

[MemoryDiagnoser]
public class CalculateDistanceBenchmark
{
    private readonly string _definitionPath;
    private readonly MapDefinition _mapDefinition;

    public CalculateDistanceBenchmark()
    {
        var countryCode = "LU";
        _definitionPath = @$"C:\dev\priv\vali-maps\{countryCode}.json";
        _mapDefinition = Serializer.Deserialize<MapDefinition>(File.ReadAllText(_definitionPath))!.ApplyDefaults();
        ConsoleLogger.Silent = true;
    }

    [Benchmark]
    public async Task GenerateMap() =>
        await LocationLakeMapGenerator.Generate(_mapDefinition, _definitionPath, RunMode.Default);
}

[MemoryDiagnoser]
public class HeavyMapCreationBenchmark
{
    private readonly string _definitionPath;
    private readonly MapDefinition _mapDefinition;

    public HeavyMapCreationBenchmark()
    {
        _definitionPath = @$"C:\dev\priv\vali-maps\heavy-subdivision.json";
        _mapDefinition = Serializer.Deserialize<MapDefinition>(File.ReadAllText(_definitionPath))!.ApplyDefaults();
        ConsoleLogger.Silent = true;
    }

    [Benchmark]
    public async Task GenerateMap() =>
        await LocationLakeMapGenerator.Generate(_mapDefinition, _definitionPath, RunMode.Default);
}

[MemoryDiagnoser]
public class PointsAreCloserThanBenchmark
{
    private readonly (double Lat, double Lng)[] _locations;

    public PointsAreCloserThanBenchmark()
    {
        var countryCode = "NO";
        var subdivisionCode = "NO-34";
        var locationsPath = @$"C:\dev\priv\location-lake\{countryCode}\{countryCode}+{subdivisionCode}.bin";
        _locations = Extensions.ProtoDeserializeFromFile<Location[]>(locationsPath)
            .Select(x => (x.Lat, x.Lng)).ToArray();
    }

    [Benchmark]
    public bool PointsAreCloserThan()
    {
        var point1 = _locations[Random.Shared.Next(0, _locations.Length)];
        var point2 = _locations[Random.Shared.Next(0, _locations.Length)];
        var meters = Random.Shared.Next(100, 5_000);
        var metersSquared = (double)meters * meters;
        return Extensions.PointsAreCloserThan(point1.Lat, point1.Lng, point2.Lat, point2.Lng, metersSquared);
    }
}
