using Spectre.Console;
using Spectre.Console.Json;
using Vali.Core.Data;

namespace Vali.Core;

public class DistributionExport
{
    public static async Task SubdivisionExport(
        string? code,
        DataFormat dataFormat,
        bool lightWeight)
    {
        if (dataFormat == DataFormat.Code)
        {
            await SubdivisionSuggester.GenerateSubdivisionFromFiles(code, lightWeight);
            return;
        }
        if (code != null)
        {
            var response = new Dictionary<string, Dictionary<string, int>>();
            var countryCodes = MapDefinitionDefaults.MapCountryCodes([code], new());
            if (countryCodes.Any())
            {
                foreach (var countryCode in countryCodes)
                {
                    if (SubdivisionWeights.CountryToSubdivision.TryGetValue(countryCode, out var subDivisionWeights))
                    {
                        response.Add(countryCode, subDivisionWeights.ToDictionary(s => s.Key, s => s.Value.weight));
                    }
                }

                WriteOutput(response, dataFormat);
            }
            else
            {
                ConsoleLogger.Warn($"No subdivision distribution yet for {CountryCodes.Name(code)} / {code}.");
            }

            return;
        }

        ConsoleLogger.Error("Nothing found.");
    }

    private static void WriteOutput(Dictionary<string, Dictionary<string, int>> response, DataFormat dataFormat)
    {
        Action a = dataFormat switch
        {
            DataFormat.Text => () =>
            {
                // Create a table
                var table = new Table();

                // Add some columns
                table.AddColumn("Code");
                table.AddColumn("Name");
                table.AddColumn("Weight");
                foreach (var subdivisionPrintRow in response.SelectMany(x => x.Value.Select(y => new SubdivisionPrintRow
                         {
                             Code = y.Key,
                             Name = SubdivisionWeights.SubdivisionName(x.Key, y.Key) ?? "N/A",
                             Weight = y.Value
                         })))
                {
                    table.AddRow($"[blue]{subdivisionPrintRow.Code}[/]", $"[blue]{subdivisionPrintRow.Name}[/]", $"[green]{subdivisionPrintRow.Weight}[/]");
                }
                AnsiConsole.Write(table);
            },
            DataFormat.Json => () => AnsiConsole.Write(new JsonText(Serializer.Serialize(response))),
            _ => throw new ArgumentOutOfRangeException(nameof(dataFormat), dataFormat, null)
        };
        a();
    }

    record SubdivisionPrintRow
    {
        public required string Code { get; set; }
        public required string Name { get; set; }
        public int Weight { get; set; }
    }

    public enum DataFormat
    {
        Json,
        Text,
        Code
    }

    public static async Task Report(string? code, string? property, bool byCountry, RunMode runMode)
    {
        if (code == null || property == null)
        {
            return;
        }

        var groups = new List<GroupInformation>();

        Func<Location, string?> selector = property switch
        {
            "SubdivisionCode" => TagsGenerator.SubdivisionCode,
            "County" => TagsGenerator.County,
            "Year" => l => TagsGenerator.Year(l.Google.Year),
            "Month" => l => TagsGenerator.Month(l.Google.Month),
            "YearMonth" => l => TagsGenerator.YearMonth(l.Google.Year, l.Google.Month),
            "Surface" => TagsGenerator.Surface,
            _ => _ => "Invalid property"
        };
        Func<NominatimData, string> keySelector = byCountry ? x => x.CountryCode : x => x.SubdivisionCode;
        Func<NominatimData, string> nameSelector = byCountry ? x => CountryCodes.Name(x.CountryCode) : x => SubdivisionWeights.SubdivisionName(x.CountryCode, x.SubdivisionCode) ?? "N/A";
        var keyHeading = byCountry ? "Country code" : "Subdivision code";

        foreach (var countryCode in MapDefinitionDefaults.MapCountryCodes([code], new DistributionStrategy()))
        {
            var subdivisions = SubdivisionWeights.AllSubdivisionFiles(countryCode, runMode);
            var files = subdivisions.Select(x => x.file).ToArray();
            await DataDownloadService.EnsureFilesDownloaded(countryCode, files);
            AnsiConsole.Progress()
                .Start(ctx =>
                {
                    var task = ctx.AddTask($"[green]Analyzing files for {CountryCodes.Name(countryCode)}[/]", maxValue: subdivisions.Length);
                    foreach (var file in files)
                    {
                        var locations = Extensions.ProtoDeserializeFromFile<Location[]>(file);
                        groups.AddRange(locations.GroupBy(selector).Where(x => x.Key != null).Select(x =>
                        {
                            var nominatim = x.First().Nominatim;
                            return new GroupInformation
                            {
                                GroupKey = keySelector(nominatim),
                                GroupName = nameSelector(nominatim),
                                Value = x.Key,
                                LocationCount = x.Count()
                            };
                        }));
                        task.Increment(1);
                    }
                });
        }

        // Create a table
        var table = new Table();

        // Add some columns
        table.AddColumn(keyHeading);
        table.AddColumn("Name");
        table.AddColumn(property);
        table.AddColumn("Location count");
        table.Columns[3].RightAligned();

        foreach (var group in groups.GroupBy(x => new{ x.GroupKey, x.GroupName, x.Value }).OrderBy(x => x.Key.GroupKey).ThenBy(x => x.Key.Value))
        {
            table.AddRow($"[blue]{group.Key.GroupKey}[/]", $"[blue]{group.Key.GroupName}[/]", $"[green]{group.Key.Value}[/]", $"[grey]{group.Sum(c => c.LocationCount):N0}[/]");
        }
        AnsiConsole.Write(table);
    }

    public static void CountryExport(string? countryCode, DataFormat dataFormat, string? distributionName)
    {
        if (countryCode == null)
        {
            return;
        }

        var mapDefinition = new MapDefinition
        {
            CountryCodes = [countryCode],
            DistributionStrategy = new DistributionStrategy
            {
                CountryDistributionFromMap = !string.IsNullOrEmpty(distributionName) ? distributionName : null,
            }
        };
        var defaultDistribution = MapDefinitionDefaults.DefaultDistribution(mapDefinition.DistributionStrategy);
        if (!defaultDistribution.Any())
        {
            ConsoleLogger.Error($"Unknown distribution {distributionName}.");
            return;
        }

        var distribution = MapDefinitionDefaults.CountryDistribution(mapDefinition).OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        Action a = dataFormat switch
        {
            DataFormat.Text => () =>
            {
                // Create a table
                var table = new Table();

                // Add some columns
                table.AddColumn("Country code");
                table.AddColumn("Name");
                table.AddColumn("Weight");
                foreach (var subdivisionPrintRow in distribution.Select(x => new SubdivisionPrintRow
                         {
                             Code = x.Key,
                             Name = CountryCodes.Name(x.Key),
                             Weight = x.Value
                         }))
                {
                    table.AddRow($"[blue]{subdivisionPrintRow.Code}[/]", $"[blue]{subdivisionPrintRow.Name}[/]", $"[green]{subdivisionPrintRow.Weight}[/]");
                }
                AnsiConsole.Write(table);
            }
            ,
            DataFormat.Json => () => AnsiConsole.Write(new JsonText(Serializer.Serialize(new Dictionary<string, Dictionary<string, int>>
            {
                { nameof(MapDefinition.CountryDistribution).FirstCharToLowerCase(), distribution }
            }))),
            _ => throw new ArgumentOutOfRangeException(nameof(dataFormat), dataFormat, null)
        };
        a();
    }
}

public record GroupInformation
{
    public required string GroupKey { get; set; }
    public string? GroupName { get; set; }
    public string? Value { get; set; }
    public int LocationCount { get; set; }
}