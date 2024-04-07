using Geohash;
using Spectre.Console;
using Vali.Core.Hash;

namespace Vali.Core;

public class SubdivisionSuggester
{
    public static async Task GenerateSubdivisionFromFiles(string? countryCode, bool lightWeight)
    {
        if (countryCode == null)
        {
            ConsoleLogger.Error("Must specify country.");
            return;
        }

        var allSubdivisions = SubdivisionWeights.AllSubdivisionFiles(countryCode, RunMode.Default);
        var files = allSubdivisions.Select(x => x.file).ToArray();
        await DataDownloadService.EnsureFilesDownloaded(countryCode, files);
        var subdivisions = SubdivisionWeights.GetSubdivisions()[countryCode];
        var availableSubdivisions = files
            .Select(f =>
            {
                var subdivisionCode = Path.GetFileNameWithoutExtension(f).Replace($"{countryCode}+", "");
                return new
                {
                    File = f,
                    SubdivisionCode = subdivisionCode,
                    Subdivision = subdivisions.FirstOrDefault(y => subdivisionCode == y.SubdivisionCode)
                };
            })
            .ToArray();

        var missing = availableSubdivisions.Where(a => a.Subdivision == null).ToArray();
        if (missing.Any() && lightWeight)
        {
            ConsoleLogger.Warn($"""
                                Missing
                                {missing.Select(x => x.SubdivisionCode).Merge(Environment.NewLine)}
                                """);
            return;
        }

        var entries = new List<SubdivisionDistributionEntry>();
        var geoHasher = new Geohasher();
        if (!lightWeight)
        {
            AnsiConsole.Progress()
                .Start(ctx =>
                {
                    var task = ctx.AddTask("[green]Analyzing files[/]", maxValue: files.Length);
                    foreach (var file in files)
                    {
                        var locations = Extensions.ProtoDeserializeFromFile<Location[]>(file);
                        var subDivision = locations.First().Nominatim.SubdivisionCode;
                        var some = locations.GroupBy(x => Hasher.Encode(x.Lat, x.Lng, HashPrecision.Size_km_39x20));
                        var minDistanceBetweenLocations = 500;
                        var someOfThem = some.SelectMany(x => LocationDistributor.GetSome<Location, long>(x.ToArray(), 1_000_000, minDistanceBetweenLocations: minDistanceBetweenLocations + 100)).ToArray();
                        var distribution = LocationDistributor.GetSome<Location, long>(someOfThem, 1_000_000, minDistanceBetweenLocations: minDistanceBetweenLocations);
                        entries.Add(new SubdivisionDistributionEntry
                        {
                            Code = subDivision,
                            Weight = distribution.Count,
                            Subdivision = subdivisions.FirstOrDefault(y => subDivision == y.SubdivisionCode)
                        });
                        task.Increment(1);
                    }
                });

            ConsoleLogger.Info(entries.Select(x => $"{{ \"{x.Code}\", {x.Weight} }}, // {x.Subdivision?.Name}").Merge(Environment.NewLine));
        }
        else
        {
            var f2 = availableSubdivisions
                .Select(x => $"{{ \"{x.SubdivisionCode}\", {Weight(x.Subdivision!, x.File)} }}, // {x.Subdivision!.Name}").Merge(Environment.NewLine);
            ConsoleLogger.Info(f2);
        }

        int Weight(SubdivisionWeights.SubdivisionInfo subdivision, string file)
        {
            decimal fileSize = new FileInfo(file).Length;
            var totalFileSize = files.Sum(f => new FileInfo(f).Length);
            decimal area = subdivision.Area;
            var totalArea = subdivisions.Sum(s => s.Area);
            decimal population = subdivision.Inhabitants;
            var totalPopulation = subdivisions.Sum(s => s.Inhabitants);
            var fileSizePercentage = fileSize / totalFileSize;
            var areaPercentage = area / totalArea;
            var populationPercentage = population / totalPopulation;
            var res = (fileSizePercentage * .1m +
                       areaPercentage * .8m +
                       populationPercentage * .1m)
                      * 1000 * files.Length;
            return res.RoundToInt();
        }
    }

    record SubdivisionDistributionEntry
    {
        public required string Code { get; set; }
        public int Weight { get; set; }
        public SubdivisionWeights.SubdivisionInfo? Subdivision { get; set; }
    }
}