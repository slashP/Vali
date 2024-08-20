// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Globalization;
using BenchmarkDotNet.Running;
using Vali.Benchmarks;
using Vali.Core;

Console.WriteLine("Hello, World!");
var indo = Extensions.ProtoDeserializeFromFile<Location[]>(Path.Combine(@"C:\ProgramData\Vali\ID", "ID+ID-SR.bin"));
var norway = Extensions.ProtoDeserializeFromFile<Location[]>(Path.Combine(@"C:\ProgramData\Vali\NO", "NO+NO-21.bin"));
var s = norway.TakeRandom(1000).Max(x => Extensions.ApproximateDistance(x.Lat, x.Lng, x.Lat, x.Lng + .001));
ConsoleLogger.Info($"Distance {s}");
//return;
BenchmarkRunner.Run<CalculateDistanceBenchmark>();
return;

foreach (var i in Enumerable.Range(0, 1))
{
    var definitionPath = @"C:\dev\priv\vali-maps\LU.json";
    var mapDefinition = Serializer.Deserialize<MapDefinition>(await File.ReadAllTextAsync(definitionPath))!.ApplyDefaults();
    var sw = Stopwatch.StartNew();
    await LocationLakeMapGenerator.Generate(mapDefinition, definitionPath, RunMode.Default);
    Console.WriteLine($"Generated in {sw.Elapsed}.");
}
