using Spectre.Console;
using System.Linq;
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

        var allSubdivisions = SubdivisionWeights.AllSubdivisionFiles(countryCode, RunMode.Localhost);
        var files = allSubdivisions.Select(x => x.file).ToArray();
        await DataDownloadService.EnsureFilesDownloaded(countryCode, files);
        var subdivisions = SubdivisionWeights.CountryToSubdivision[countryCode];
        var availableSubdivisions = files
            .Select(f =>
            {
                var subdivisionCode = Path.GetFileNameWithoutExtension(f).Replace($"{countryCode}+", "");
                return new
                {
                    File = f,
                    SubdivisionCode = subdivisionCode,
                    Subdivision = subdivisions.FirstOrDefault(y => subdivisionCode == y.Key)
                };
            })
            .ToArray();

        var entries = new List<SubdivisionDistributionEntry>();
        if (!lightWeight)
        {
            AnsiConsole.Progress()
                .Start(ctx =>
                {
                    var task = ctx.AddTask("[green]Analyzing files[/]", maxValue: files.Length);
                    foreach (var file in files)
                    {
                        var locations = Extensions.ProtoDeserializeFromFile<Location[]>(file);
                        var subDivision = locations.FirstOrDefault()?.Nominatim.SubdivisionCode;
                        if (subDivision == null)
                        {
                            entries.Add(new SubdivisionDistributionEntry
                            {
                                Code = "FIX",
                                Weight = 0,
                                Name = "nope"
                            });
                            continue;
                        }

                        var some = locations.GroupBy(x => Hasher.Encode(x.Lat, x.Lng, HashPrecision.Size_km_39x20));
                        var minDistanceBetweenLocations = 2000;
                        var someOfThem = some.SelectMany(x => LocationDistributor.GetSome<Location, long>(x.ToArray(), 1_000_000, minDistanceBetweenLocations: minDistanceBetweenLocations + 100, new())).ToArray();
                        var distribution = LocationDistributor.GetSome<Location, long>(someOfThem, 1_000_000, minDistanceBetweenLocations: minDistanceBetweenLocations, new());
                        entries.Add(new SubdivisionDistributionEntry
                        {
                            Code = subDivision,
                            Weight = distribution.Count,
                            Name = subdivisions[subDivision].subdivisionName
                        });
                        task.Increment(1);
                    }
                });

            ConsoleLogger.Info(entries.Select(x => $"{{ \"{x.Code}\", ({x.Weight}, \"{x.Name}\") }},").Merge(Environment.NewLine));
        }
    }

    record SubdivisionDistributionEntry
    {
        public required string Code { get; set; }
        public int Weight { get; set; }
        public required string Name { get; set; }
    }
}