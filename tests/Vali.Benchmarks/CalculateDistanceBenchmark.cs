using BenchmarkDotNet.Attributes;
using Vali.Core;

namespace Vali.Benchmarks;

public class CalculateDistanceBenchmark
{
    private readonly string _definitionPath;
    private readonly MapDefinition _mapDefinition;

    public CalculateDistanceBenchmark()
    {
        var countryCode = "LU";
        _definitionPath = @$"C:\dev\priv\vali-maps\{countryCode}.json";
        _mapDefinition = Serializer.Deserialize<MapDefinition>(File.ReadAllText(_definitionPath))!.ApplyDefaults();
    }

    [Benchmark]
    public async Task GenerateMap() =>
        await LocationLakeMapGenerator.Generate(_mapDefinition, _definitionPath, RunMode.Default);
}
