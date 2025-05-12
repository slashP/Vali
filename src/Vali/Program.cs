using System.CommandLine;
using Microsoft.Extensions.Logging;
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

var currentDownloadFolder = DataDownloadService.DownloadFolder(RunMode.Default);
if (!string.IsNullOrEmpty(currentDownloadFolder))
{
    ConsoleLogger.Success($"Download/data folder: {currentDownloadFolder}");
}

// args = ["live-generate", "--file", @"C:\dev\priv\vali-maps\live.json"];

using var loggerFactory = ValiLogger.Initialize();

try
{
    var rootCommand = new RootCommand("Vali - create locations.");
    var countriesArgument = new Argument<string>("countries");
    var directoryArgument = new Argument<string>("directory");
    var countryOption = new Option<string>("--country") { IsRequired = false };
    var distributionOption = new Option<string>("--distribution") { IsRequired = false };
    var requiredCountryOption = new Option<string>("--country") { IsRequired = true };
    var propertyOption = new Option<string>("--prop") { IsRequired = true };
    Option<bool?> fullDownloadOption = new(name: "--full", description: "Force re-download of files.");
    Option<bool?> updatesDownloadOption = new(name: "--updates", description: "Force re-download of update files.");
    var downloadCommand = new Command("download", "Download data.");
    var setDownloadFolderCommand = new Command("set-download-folder", @"Set folder/directory to download data to. Default is c:\ProgramData\Vali");
    setDownloadFolderCommand.AddArgument(directoryArgument);
    rootCommand.Add(setDownloadFolderCommand);
    var unsetDownloadFolderCommand = new Command("unset-download-folder", @"Reset download folder to default.");
    rootCommand.AddCommand(unsetDownloadFolderCommand);
    var applicationSettingsCommand = new Command("application-settings");
    rootCommand.AddCommand(applicationSettingsCommand);

    downloadCommand.AddOption(countryOption);
    downloadCommand.AddOption(fullDownloadOption);
    downloadCommand.AddOption(updatesDownloadOption);
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
    Option<bool?> overwriteOption = new(name: "--overwrite", description: "Overwrite output file even if it exists.");
    var outputPathOption = new Option<string>("--outputPath") { IsRequired = false };

    var subdivisionsCommand = new Command("subdivisions", "Export subdivision distribution.");
    subdivisionsCommand.AddOption(countryOption);
    subdivisionsCommand.AddOption(asTextOption);
    subdivisionsCommand.AddOption(asCodeOption);
    subdivisionsCommand.AddOption(lightWeightOption);
    rootCommand.Add(subdivisionsCommand);

    var countriesCommand = new Command("countries", "Export country distribution.");
    countriesCommand.AddArgument(countriesArgument);
    countriesCommand.AddOption(distributionOption);
    countriesCommand.AddOption(asTextOption);
    rootCommand.Add(countriesCommand);

    var reportCommand = new Command("report", "Export which counties exist in a country.");
    reportCommand.AddOption(requiredCountryOption);
    reportCommand.AddOption(propertyOption);
    reportCommand.AddOption(byCountryOption);
    rootCommand.Add(reportCommand);

    var createFileCommand = new Command("create-file", "Create a JSON file to get started.");
    rootCommand.Add(createFileCommand);

    var distributeFromFileCommand = new Command("distribute-from-file", "Distributes locations from a specified JSON file.");
    var fixedMinDistanceOption = new Option<string>("--distance") { IsRequired = true, Description = "Fixed minimum distance between locations."};
    distributeFromFileCommand.AddOption(fileOption);
    distributeFromFileCommand.AddOption(fixedMinDistanceOption);
    distributeFromFileCommand.AddOption(overwriteOption);
    distributeFromFileCommand.AddOption(outputPathOption);
    rootCommand.Add(distributeFromFileCommand);

    var liveGenerateMapCommand = new Command("live-generate", "Generate a GeoGuessr map by calling Google's API on the fly.");
    liveGenerateMapCommand.AddOption(fileOption);
    rootCommand.Add(liveGenerateMapCommand);

    downloadCommand.SetHandler(async context =>
    {
        var countryOptionValue = context.ParseResult.GetValueForOption(countryOption);
        if (string.IsNullOrEmpty(countryOptionValue))
        {
            countryOptionValue =
                AnsiConsole.Ask(
                    "What do you want to download? * for all, use two letter country code for single country (f.ex. US) or specify continent (europe, africa, asia, oceania, southamerica, northamerica).", "");
        }

        var full = context.ParseResult.GetValueForOption(fullDownloadOption) == true;
        var updates = context.ParseResult.GetValueForOption(updatesDownloadOption) == true;

        await DataDownloadService.DownloadFiles(countryOptionValue, full, updates);
        context.ExitCode = 100;
    });

    generateMapCommand.SetHandler(async context =>
    {
        var fileOptionValue = context.ParseResult.GetValueForOption(fileOption)!;
        var mapJson = await GenerateFileValidator.ReadFile(fileOptionValue);
        if (mapJson == null)
        {
            return;
        }

        var validationMessage = GenerateFileValidator.HumanReadableError(mapJson);
        if (validationMessage != null)
        {
            ConsoleLogger.Error(validationMessage);
            return;
        }

        var mapDefinition = GenerateFileValidator.TryDeserialize(mapJson);
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

        var runMode = RunMode.Default;
#if DEBUG
        runMode = RunMode.Localhost;
#endif

        await LocationLakeMapGenerator.Generate(validatedMapDefinition, fileOptionValue, runMode);
        context.ExitCode = 100;
    });

    subdivisionsCommand.SetHandler(async context =>
    {
        var countryOptionValue = context.ParseResult.GetValueForOption(countryOption);
        var dataFormat = context.ParseResult.GetValueForOption(asTextOption) == true
            ? DistributionExport.DataFormat.Text
            : context.ParseResult.GetValueForOption(asCodeOption) == true
                ? DistributionExport.DataFormat.Code
                : DistributionExport.DataFormat.Json;
        var lightWeight = context.ParseResult.GetValueForOption(lightWeightOption) == true;

        await DistributionExport.SubdivisionExport(countryOptionValue, dataFormat, lightWeight);
        context.ExitCode = 100;
    });

    countriesCommand.SetHandler(context =>
    {
        var countries = context.ParseResult.GetValueForArgument(countriesArgument);
        var distributionOptionValue = context.ParseResult.GetValueForOption(distributionOption);
        var dataFormat = context.ParseResult.GetValueForOption(asTextOption) == true
            ? DistributionExport.DataFormat.Text
            : DistributionExport.DataFormat.Json;

        DistributionExport.CountryExport(countries, dataFormat, distributionOptionValue);
        context.ExitCode = 100;
    });

    reportCommand.SetHandler(async context =>
    {
        var countryOptionValue = context.ParseResult.GetValueForOption(requiredCountryOption);
        var propertyOptionValue = context.ParseResult.GetValueForOption(propertyOption);
        var byCountry = context.ParseResult.GetValueForOption(byCountryOption) == true;
        Console.WriteLine($"By country {byCountry}");
        await DistributionExport.Report(countryOptionValue, propertyOptionValue, byCountry);
        context.ExitCode = 100;
    });

    createFileCommand.SetHandler(async context =>
    {
        var countryCodes = AnsiConsole.Ask<string>("Which country or area do you want? Specify two letter country code, or continent name (europe, africa, asia, oceania, southamerica, northamerica). Split multiple values with commas (,).");
        var filename = AnsiConsole.Ask("What do you want the file name to be?", $"{countryCodes.Split(',').First()}.json");
        var filePath = new FileInfo(Path.GetFileNameWithoutExtension(filename) + ".json").FullName;
        var distributionStrategy = new
        {
            Key = DistributionStrategies.FixedCountByMaxMinDistance,
            MinMinDistance = 200,
            LocationCountGoal = 10_000,
            CountryDistributionFromMap = (string?)null
        };
        var output = new
        {
            locationTags = Array.Empty<string>(),
            panoIdCountryCodes = Array.Empty<string>(),
            globalHeadingExpression = (string?)null,
            countryHeadingExpressions = (string?)null,
            globalZoom = (double?)null,
            globalPitch = (double?)null,
        };
        var codes = countryCodes switch
        {
            "*" or
                "europe" or
                "asia" or
                "africa" or
                "southamerica" or
                "northamerica" or
                "oceania" or
                "lefthandtraffic" or
                "righthandtraffic" => countryCodes,
            _ => countryCodes.ToUpper()
        };
        if (File.Exists(filePath))
        {
            ConsoleLogger.Error($"{filePath} already exists. Remove it or choose a different name.");
            return;
        }

        await File.WriteAllTextAsync(filePath, Serializer.PrettySerialize(new Dictionary<string, object?>
        {
            { nameof(MapDefinition.CountryCodes).FirstCharToLowerCase(), new[] { codes } },
            { nameof(MapDefinition.DistributionStrategy).FirstCharToLowerCase(), distributionStrategy },
            { nameof(MapDefinition.Output).FirstCharToLowerCase(), output },
            { nameof(MapDefinition.GlobalLocationFilter).FirstCharToLowerCase(), "" }
        }));
        ConsoleLogger.Success($"Created file {filePath}");
        context.ExitCode = 100;
    });

    setDownloadFolderCommand.SetHandler(context =>
    {
        var directory = context.ParseResult.GetValueForArgument(directoryArgument);
        ApplicationSettingsService.SetDownloadFolder(directory);
        context.ExitCode = 100;
    });

    unsetDownloadFolderCommand.SetHandler(context =>
    {
        ApplicationSettingsService.UnsetDownloadFolder();
        context.ExitCode = 100;
    });

    applicationSettingsCommand.SetHandler(context =>
    {
        var settings = ApplicationSettingsService.ReadApplicationSettings();
        ConsoleLogger.Info(Serializer.PrettySerialize(settings));
        if (ApplicationSettingsService.ReadDownloadFolderFromEnvironmentVariable() != null)
        {
            ConsoleLogger.Info($"Vali environment variable {ApplicationSettingsService.DownloadFolderEnvironmentVariableName}: {ApplicationSettingsService.ReadDownloadFolderFromEnvironmentVariable()}");
        }

        context.ExitCode = 100;
    });

    distributeFromFileCommand.SetHandler(async context =>
    {
        var fixedMinDistance = context.ParseResult.GetValueForOption(fixedMinDistanceOption);
        if (!int.TryParse(fixedMinDistance, out var minDistance))
        {
            ConsoleLogger.Error("--distance must be an integer.");
        }

        var fileOptionValue = context.ParseResult.GetValueForOption(fileOption)!;
        var overwriteOptionValue = context.ParseResult.GetValueForOption(overwriteOption);
        var overwrite = overwriteOptionValue == true;
        var outputPathOptionValue = context.ParseResult.GetValueForOption(outputPathOption);
        await FileDistributor.DistributeFromFile(
            path: fileOptionValue,
            fixedMinDistance: minDistance,
            overwrite: overwrite,
            outputPath: outputPathOptionValue);
        context.ExitCode = 100;
    });

    liveGenerateMapCommand.SetHandler(async context =>
    {
        var fileOptionValue = context.ParseResult.GetValueForOption(fileOption)!;
        var mapJson = await GenerateFileValidator.ReadFile(fileOptionValue);
        if (mapJson == null)
        {
            return;
        }

        var mapDefinition = LiveGenerateValidator.TryDeserialize(mapJson);
        if (mapDefinition == null)
        {
            context.ExitCode = -1;
            return;
        }

        var validatedMapDefinition = mapDefinition.Validate();
        if (validatedMapDefinition == null)
        {
            context.ExitCode = -1;
            return;
        }

        await LiveGenerate.Generate(mapDefinition, fileOptionValue);
    });

    await rootCommand.InvokeAsync(args);

}
catch (Exception e)
{
    var logger = loggerFactory.CreateLogger("Program");
    logger.LogError(e, "Unhandled exception.");
    throw;
}
