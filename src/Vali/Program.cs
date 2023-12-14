using System.CommandLine;
using Spectre.Console;
using Vali.Core;
using Vali.Core.Validation;

AnsiConsole.MarkupLine(
  """
  [blue]
                    888 d8b
                    888 Y8P
                    888
  888  888  8888b.  888 888
  888  888     "88b 888 888
  Y88  88P .d888888 888 888
   Y8bd8P  888  888 888 888
    Y88P   "Y888888 888 888
  [/]
  """);

//args = new[] { "generate",  "--file" , @"C:\dev\priv\location-lake\maps\map-testing\aefr.json" };

var rootCommand = new RootCommand("Vali - create locations.");
var countryOption = new Option<string>("--country") { IsRequired = false };
var requiredCountryOption = new Option<string>("--country") { IsRequired = true };
var propertyOption = new Option<string>("--prop") { IsRequired = true };
var downloadCommand = new Command("download", "Download data.");
downloadCommand.AddOption(countryOption);
rootCommand.Add(downloadCommand);

var generateMapCommand = new Command("generate", "Generate a GeoGuessr map.");
var fileOption = new Option<string>("--file") { IsRequired = true };
generateMapCommand.AddOption(fileOption);
rootCommand.Add(generateMapCommand);

Option<bool?> byCountryOption = new(name: "--byCountry", description: "Group info by country instead of subdivision.");
Option<bool?> asTextOption = new(name: "--text", description: "Output as textual description instead of JSON.");
Option<bool?> asCodeOption = new(name: "--code", description: "Output as C# code.")
{
    IsHidden = true
};
Option<bool?> lightWeightOption = new(name: "--lightWeight", description: "Light version.")
{
    IsHidden = true
};

var subdivisionsCommand = new Command("subdivisions", "Export country or subdivision distribution.");
subdivisionsCommand.AddOption(countryOption);
subdivisionsCommand.AddOption(asTextOption);
subdivisionsCommand.AddOption(asCodeOption);
subdivisionsCommand.AddOption(lightWeightOption);
rootCommand.Add(subdivisionsCommand);

var reportCommand = new Command("report", "Export which counties exist in a country.");
reportCommand.AddOption(requiredCountryOption);
reportCommand.AddOption(propertyOption);
reportCommand.AddOption(byCountryOption);
rootCommand.Add(reportCommand);

var createFileCommand = new Command("create-file", "Create a JSON file to get started.");
rootCommand.Add(createFileCommand);

downloadCommand.SetHandler(async context =>
{
    var countryOptionValue = context.ParseResult.GetValueForOption(countryOption);
    if (string.IsNullOrEmpty(countryOptionValue))
    {
        countryOptionValue =
            AnsiConsole.Ask(
                "What do you want to download? Leave empty for all, use two letter country code for single country (f.ex. US) or specify continent (europe, africa, asia, oceania, southamerica, northamerica).", "");
    }

    await DataDownloadService.DownloadFiles(countryOptionValue);
    context.ExitCode = 100;
});

generateMapCommand.SetHandler(async context =>
{
    var fileOptionValue = context.ParseResult.GetValueForOption(fileOption)!;
    var mapDefinition = await GenerateFileValidator.TryDeserialize(fileOptionValue);
    if (mapDefinition == null)
    {
        context.ExitCode = -1;
        return;
    }

    var validatedMapDefinition = mapDefinition.ApplyDefaults().Validate();
    if (validatedMapDefinition == null)
    {
        context.ExitCode = -1;
        return;
    }

    await LocationLakeMapGenerator.Generate(validatedMapDefinition, fileOptionValue);
    context.ExitCode = 100;
});

subdivisionsCommand.SetHandler(context =>
{
    var countryOptionValue = context.ParseResult.GetValueForOption(countryOption);
    var dataFormat = context.ParseResult.GetValueForOption(asTextOption) == true
        ? DistributionExport.DataFormat.Text
        : context.ParseResult.GetValueForOption(asCodeOption) == true
            ? DistributionExport.DataFormat.Code
            : DistributionExport.DataFormat.Json;
    var lightWeight = context.ParseResult.GetValueForOption(lightWeightOption) == true;

    DistributionExport.Export(countryOptionValue, dataFormat, lightWeight);
    context.ExitCode = 100;
});

reportCommand.SetHandler(context =>
{
    var countryOptionValue = context.ParseResult.GetValueForOption(requiredCountryOption);
    var propertyOptionValue = context.ParseResult.GetValueForOption(propertyOption);
    var byCountry = context.ParseResult.GetValueForOption(byCountryOption) == true;
    Console.WriteLine($"By country {byCountry}");
    DistributionExport.Report(countryOptionValue, propertyOptionValue, byCountry);
    context.ExitCode = 100;
});

createFileCommand.SetHandler(async context =>
{
    var countryCodes = AnsiConsole.Ask<string>("Which country or area do you want? Specify two letter country code, or continent name (europe, africa, asia, oceania, southamerica, northamerica). Split multiple values with commas (,).");
    var filename = AnsiConsole.Ask("What do you want the file name to be?", $"{countryCodes.Split(',').First()}.json");
    var filePath = new FileInfo(Path.GetFileNameWithoutExtension(filename) + ".json").FullName;
    var distributionStrategy = new DistributionStrategy
    {
        Key = DistributionStrategies.FixedCountByMaxMinDistance,
        MinMinDistance = 200,
        LocationCountGoal = 10_000
    };
    if (File.Exists(filePath))
    {
        ConsoleLogger.Error($"{filePath} already exists. Remove it or choose a different name.");
        return;
    }

    await File.WriteAllTextAsync(filePath, Serializer.PrettySerialize(new Dictionary<string, object>
    {
        { nameof(MapDefinition.CountryCodes).FirstCharToLowerCase(), new[] { countryCodes } },
        { nameof(MapDefinition.DistributionStrategy).FirstCharToLowerCase(), distributionStrategy },
    }));
    ConsoleLogger.Success($"Created file {filePath}");
    context.ExitCode = 100;
});

await rootCommand.InvokeAsync(args);
