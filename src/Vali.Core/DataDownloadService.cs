using ICSharpCode.SharpZipLib.BZip2;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Json;
using Vali.Core.Data;

namespace Vali.Core;

public static class DataDownloadService
{
    private const string FileExtension = ".bin";
    private const string CountriesBucketName = "countries-v2";
    private const string CountryUpdatesBucketName = "country-updates-v2";

    private static readonly HttpClient R2BucketClient = new()
    {
        BaseAddress = new Uri("https://vali-download.slashp.workers.dev"),
        Timeout = TimeSpan.FromMinutes(10)
    };

    public static async Task DownloadFiles(string? countryCode, bool full, bool updates)
    {
        if (!EnsureDownloadFolderIsWritable())
        {
            return;
        }

        var sw = Stopwatch.StartNew();
        var logger = ValiLogger.Factory.CreateLogger<LocationLakeMapGenerator>();

        var downloadOperations =
            new (string bucketName,
                Action<string, RunMode> preDownloadAction,
                Action<string, RunMode, DownloadMetadata.File> deleteAction,
                Func<string, string, IReadOnlyCollection<R2Object>, RunMode, ProgressTask, Task> downloadAction,
                Func<R2Object, string> groupBy,
                string downloadConsoleSuffix,
                Func<string, RunMode, IReadOnlyCollection<R2Object>, Task> downloadedAction,
                bool force)[]
                {
                    (CountriesBucketName, (_, _) => {}, DeleteDataFile, DownloadDataFiles, _ => "", "", SaveDataFilesDownloaded, full),
                    (CountryUpdatesBucketName, RemoveUpdateFiles, (_, _, _) => {}, DownloadUpdateFile, f => f.Key.RemoveDatePrefix(), " updates", SaveUpdateFilesDownloaded, updates),
                };
        var countryCodes = string.IsNullOrEmpty(countryCode) ?
            CountryCodes.Countries.Keys.ToArray() :
            CountryCodes.Countries.Keys.Intersect(MapDefinitionDefaults.ExpandCountryCode(countryCode, new DistributionStrategy())).ToArray();
        var runMode = RunMode.Default;
        foreach (var code in countryCodes)
        {
            await EnsureDownloadMetadataFileExists(code, runMode);
        }

        AnsiConsole.MarkupLine("[yellow]Downloads starting. Press s to stop.[/]");
        var exitRequested = false;
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                foreach (var downloadOperation in downloadOperations)
                {
                    foreach (var code in countryCodes)
                    {
                        downloadOperation.preDownloadAction(code, runMode);
                        var filesFromR2 = await GetFilesFrom(code, downloadOperation.bucketName);

                        var localFiles = ExistingFilesInMetadata(code, runMode);
                        var filesToDownload = filesFromR2.Where(r2File =>
                        {
                            var localFile = localFiles.FirstOrDefault(file => Path.GetFileNameWithoutExtension(file.Name) == Path.GetFileNameWithoutExtension(r2File.Key));
                            return localFile == null || localFile.LastWriteTimeUtc < r2File.Uploaded || downloadOperation.force;
                        }).ToArray();
                        var filesToDelete = localFiles.Where(diskFile =>
                        {
                            var matchingFileFromR2 = filesFromR2.FirstOrDefault(r2File => Path.GetFileNameWithoutExtension(r2File.Key) == Path.GetFileNameWithoutExtension(diskFile.Name));
                            return matchingFileFromR2 == null;
                        }).ToArray();
                        foreach (var diskFile in filesToDelete)
                        {
                            downloadOperation.deleteAction(code, runMode, diskFile);
                        }

                        if (!filesToDownload.Any())
                        {
                            continue;
                        }

                        var task = ctx.AddTask($"[green]{CountryCodes.Name(code)}{downloadOperation.downloadConsoleSuffix}[/]", maxValue: filesToDownload.Sum(f => f.Size ?? 0));
                        await Task.Delay(5);
                        var r2FilesDownloaded = new List<R2Object>();
                        foreach (var r2Files in filesToDownload.GroupBy(downloadOperation.groupBy))
                        {
                            exitRequested = Console.KeyAvailable && Console.ReadKey().KeyChar == 's';
                            if (exitRequested)
                            {
                                task.StopTask();
                                break;
                            }

                            var r2FilesToDownload = r2Files.ToArray();
                            await downloadOperation.downloadAction(downloadOperation.bucketName, code, r2FilesToDownload, runMode, task);
                            r2FilesDownloaded.AddRange(r2FilesToDownload);
                        }

                        await downloadOperation.downloadedAction(code, runMode, r2FilesDownloaded);
                        task.StopTask();
                        if (exitRequested)
                        {
                            return;
                        }
                    }
                }
            });
        AnsiConsole.MarkupLine("[yellow]Downloads finished.[/]");

        logger.FilesDownloaded(countryCode, sw.Elapsed, exitRequested);
    }

    private static async Task SaveDataFilesDownloaded(string countryCode, RunMode runMode, IReadOnlyCollection<R2Object> files)
    {
        var metadataPath = DownloadMetadataPath(countryCode, runMode);
        var metadata = Extensions.TryJsonDeserializeFromFile(metadataPath, new DownloadMetadata());
        var newFiles = files.Select(f => new DownloadMetadata.File
            {
                Name = Path.GetFileNameWithoutExtension(f.Key),
                LastWriteTimeUtc = f.Uploaded
            })
            .Concat(metadata.Files
                .Where(f => files.All(x => Path.GetFileNameWithoutExtension(x.Key) != Path.GetFileNameWithoutExtension(f.Name.RemoveDatePrefix()))))
            .DistinctBy(f => f.Name)
            .ToArray();
        metadata = metadata with
        {
            Files = newFiles
        };
        await Extensions.PrettyJsonSerializeToFile(metadataPath, metadata);
    }

    private static async Task SaveUpdateFilesDownloaded(string countryCode, RunMode runMode, IReadOnlyCollection<R2Object> files)
    {
        var metadataPath = DownloadMetadataPath(countryCode, runMode);
        var metadata = Extensions.TryJsonDeserializeFromFile(metadataPath, new DownloadMetadata());
        var newFiles = files.Select(f => new DownloadMetadata.File
            {
                Name = Path.GetFileNameWithoutExtension(f.Key),
                LastWriteTimeUtc = f.Uploaded
            })
            .Concat(metadata.Files)
            .DistinctBy(f => f.Name)
            .ToArray();
        metadata = metadata with
        {
            Files = newFiles
        };
        await Extensions.PrettyJsonSerializeToFile(metadataPath, metadata);
        var updatesFolder = UpdatesFolder(countryCode, runMode);
        if (Directory.Exists(updatesFolder))
        {
            Directory.Delete(updatesFolder, true);
        }
    }

    private static void RemoveUpdateFiles(string countryCode, RunMode runMode)
    {
        var updatesFolder = UpdatesFolder(countryCode, runMode);
        if (Directory.Exists(updatesFolder))
        {
            Directory.Delete(updatesFolder, true);
        }
    }

    private static void DeleteDataFile(string countryCode, RunMode runMode, DownloadMetadata.File file)
    {
        var folder = CountryFolder(countryCode, runMode);
        var fullPath = Directory.GetFiles(folder).FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == file.Name);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private static async Task DownloadDataFiles(string bucketName, string countryCode, IReadOnlyCollection<R2Object> filesToDownload, RunMode runMode, ProgressTask? task)
    {
        var folder = CountryFolder(countryCode, runMode);
        await filesToDownload.RunLimitedNumberAtATime(file => DownloadFile(bucketName, file, folder), 10, t => task?.Increment(t.file.Size ?? 0));
    }

    private static async Task DownloadUpdateFile(string bucketName, string countryCode, IReadOnlyCollection<R2Object> r2Files, RunMode runMode, ProgressTask task)
    {
        var updatesFolder = UpdatesFolder(countryCode, runMode);
        var newLocations = new List<Location>();
        var chunkSize = 20;
        var uploadedFiles = await r2Files.RunLimitedNumberAtATime(file => DownloadFile(bucketName, file, updatesFolder), chunkSize, (t) => task.Increment(t.file.Size ?? 0));
        var updateFilePath = uploadedFiles.LastOrDefault().filePath ?? "";
        foreach (var uploadedFile in uploadedFiles)
        {
            newLocations.AddRange(Extensions.ProtoDeserializeFromFile<Location[]>(uploadedFile.filePath));
        }

        var countryFolder = CountryFolder(countryCode, runMode);
        var dataFilePath = Path.Combine(countryFolder, Path.GetFileName(updateFilePath).RemoveDatePrefix());
        await ApplyUpdatesToDataFile(dataFilePath, newLocations);
    }

    private static async Task<(string filePath, R2Object file)> DownloadFile(string bucketName, R2Object file, string folder)
    {
        const int maxRetries = 3;
        Directory.CreateDirectory(folder);
        var key = file.Key;
        var destinationFileName = Path.GetFileNameWithoutExtension(key) + FileExtension;
        var filePath = Path.Combine(folder, destinationFileName);
        var r2Url = $"{bucketName}/{key}";

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var fileMode = File.Exists(filePath) ? FileMode.Truncate : FileMode.CreateNew;
                await using var fileOnDiskStream = new FileStream(filePath, fileMode);
                using var response = await R2BucketClient.GetAsync(r2Url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using var memoryStream = new MemoryStream();
                await response.Content.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                BZip2.Decompress(memoryStream, fileOnDiskStream, true);
                return (filePath, file);
            }
            catch (Exception ex) when (attempt < maxRetries && ex is HttpRequestException or TaskCanceledException or IOException)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(delay);
            }
        }

        // Final attempt — let exceptions propagate
        {
            var fileMode = File.Exists(filePath) ? FileMode.Truncate : FileMode.CreateNew;
            await using var fileOnDiskStream = new FileStream(filePath, fileMode);
            using var response = await R2BucketClient.GetAsync(r2Url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var memoryStream = new MemoryStream();
            await response.Content.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            BZip2.Decompress(memoryStream, fileOnDiskStream, true);
            return (filePath, file);
        }
    }

    private static async Task ApplyUpdatesToDataFile(string dataFilePath, IEnumerable<Location> updateLocations)
    {
        var newLocations = updateLocations.ToArray();
        await Extensions.ProtoAppendSerializeToFile(dataFilePath, newLocations);
    }

    private static string RemoveDatePrefix(this string filename) =>
        new string(filename.SkipWhile(c => char.IsNumber(c) || c == '-').ToArray());

    private static async Task EnsureDownloadMetadataFileExists(string countryCode, RunMode runMode)
    {
        if (!Directory.Exists(CountryFolder(countryCode, runMode)))
        {
            return;
        }

        var metadataPath = DownloadMetadataPath(countryCode, runMode);
        if (File.Exists(metadataPath))
        {
            return;
        }

        var files = ExistingDataFilesForCountryOnDisk(countryCode, runMode);
        var data = new DownloadMetadata
        {
            Files = files.Select(f => new DownloadMetadata.File
            {
                Name = Path.GetFileNameWithoutExtension(f.Name),
                LastWriteTimeUtc = f.LastWriteTimeUtc.AddTicks(-(f.LastWriteTimeUtc.Ticks % TimeSpan.TicksPerSecond))
            }).ToArray()
        };
        await Extensions.PrettyJsonSerializeToFile(metadataPath, data);
    }

    private static FileInfo[] ExistingDataFilesForCountryOnDisk(string countryCode, RunMode runMode)
    {
        var folder = CountryFolder(countryCode, runMode);
        return Directory.Exists(folder)
            ? Directory.GetFiles(folder, $"*{FileExtension}").Select(x => new FileInfo(x)).ToArray()
            : [];
    }

    private static DownloadMetadata.File[] ExistingFilesInMetadata(string countryCode, RunMode runMode)
    {
        var metadataPath = DownloadMetadataPath(countryCode, runMode);
        var metadata = Extensions.TryJsonDeserializeFromFile(metadataPath, new DownloadMetadata());
        return metadata.Files;
    }

    private static string DownloadMetadataPath(string countryCode, RunMode runMode)
    {
        var folder = CountryFolder(countryCode, runMode);
        return Path.Combine(folder, "downloads.json");
    }

    public static string CountryFolder(string countryCode, RunMode runMode)
    {
        var downloadFolder = DownloadFolder(runMode);
        return Path.Combine(downloadFolder, countryCode);
    }

    public static string DownloadFolder(RunMode runMode)
    {
        var applicationSettings = ApplicationSettingsService.ReadApplicationSettings();
        if (runMode == RunMode.Localhost && !string.IsNullOrEmpty(applicationSettings.LocalhostDownloadDirectory))
        {
            return applicationSettings.LocalhostDownloadDirectory!;
        }

        var defaultDownloadFolderEnvironment = ApplicationSettingsService.ReadDownloadFolderFromEnvironmentVariable();
        var defaultDownloadFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var downloadFolder = defaultDownloadFolderEnvironment ?? applicationSettings.DownloadDirectory ?? defaultDownloadFolder;
        return Path.Combine(downloadFolder, "Vali");
    }

    private static string UpdatesFolder(string countryCode, RunMode runMode)
    {
        var countryFolder = CountryFolder(countryCode, runMode);
        return Path.Combine(countryFolder, "updates");
    }

    private static async Task<R2Object[]> GetFilesFrom(string countryCode, string bucketName)
    {
        var listingName = bucketName.Contains("updates", StringComparison.InvariantCultureIgnoreCase)
            ? "list-country-updates"
            : "list-countries";
        var r2ApiUrl = $"{listingName}/{countryCode}";
        return await R2BucketClient.GetFromJsonAsync<R2Object[]>(r2ApiUrl) ?? [];
    }

    public static async Task EnsureFilesDownloaded(string countryCode, string[] subdivisionFiles)
    {
        if (!EnsureDownloadFolderIsWritable())
        {
            return;
        }

        if (subdivisionFiles.All(File.Exists))
        {
            return;
        }

        var filesFromR2 = await GetFilesFrom(countryCode, CountriesBucketName);
        var localFiles = ExistingFilesInMetadata(countryCode, RunMode.Default);
        var filesToDownload = filesFromR2.Where(r2File =>
        {
            var localFile = localFiles.FirstOrDefault(file => Path.GetFileNameWithoutExtension(file.Name) == Path.GetFileNameWithoutExtension(r2File.Key));
            return localFile == null || localFile.LastWriteTimeUtc < r2File.Uploaded;
        }).ToArray();
        if (filesToDownload.Length > 0)
        {
            ConsoleLogger.Warn($"Downloading {CountryCodes.Name(countryCode)} data.");
            try
            {
                await DownloadDataFiles(CountriesBucketName, countryCode, filesToDownload, RunMode.Default, null);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or AggregateException)
            {
                ConsoleLogger.Error($"Failed to download {CountryCodes.Name(countryCode)} data. Check your internet connection and try again, or run 'vali download {countryCode}' manually.");
                throw;
            }
        }
    }

    private static bool EnsureDownloadFolderIsWritable()
    {
        var currentDownloadFolder = DownloadFolder(RunMode.Default);
        Extensions.TryCreateDirectory(currentDownloadFolder);
        if (!Extensions.IsDirectoryWritable(currentDownloadFolder))
        {
            var downloadFolder = AnsiConsole.Ask("Please specify a folder where vali should store downloaded files.", "");
            if (!Directory.Exists(downloadFolder))
            {
                ConsoleLogger.Error($"Folder '{downloadFolder}' does not exist.");
                return false;
            }

            var generalDownloadFolder = Path.GetFullPath(downloadFolder);
            var fullDownloadPath = Path.Combine(generalDownloadFolder, "Vali");
            Extensions.TryCreateDirectory(fullDownloadPath);
            if (!Extensions.IsDirectoryWritable(fullDownloadPath))
            {
                ConsoleLogger.Error($"Vali does not have access to write files to '{fullDownloadPath}'");
                return false;
            }

            ApplicationSettingsService.SetDownloadFolder(generalDownloadFolder);
            ConsoleLogger.Success($"'{DownloadFolder(RunMode.Default)}' set as download folder. Use 'vali set-download-folder' if you want to change it.");
        }

        return true;
    }

    public static async Task EnsureRoadFilesDownloaded()
    {
        if (!EnsureDownloadFolderIsWritable())
        {
            return;
        }

        var roadsFolder = RoadsFolder();
        if (Directory.Exists(roadsFolder) && Directory.GetDirectories(roadsFolder).Length != 0)
        {
            return;
        }

        ConsoleLogger.Info("Downloading roads data.");
        Directory.CreateDirectory(roadsFolder);
        
        var r2Url = "roads-v2/roads.zip";
        using var response = await R2BucketClient.GetAsync(r2Url);
        response.EnsureSuccessStatusCode();
        
        using var memoryStream = new MemoryStream();
        await response.Content.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        ZipFile.ExtractToDirectory(memoryStream, roadsFolder);
        ConsoleLogger.Info("Downloaded roads data.");

        foreach (var oldRoadsDataFolderName in OldRoadsDataFolderNames)
        {
            var currentDownloadFolder = DownloadFolder(RunMode.Default);
            var oldRoadsFolder = Path.Combine(currentDownloadFolder, oldRoadsDataFolderName);
            if (Directory.Exists(oldRoadsFolder))
            {
                Directory.Delete(oldRoadsFolder, true);
            }
        }
    }

    private static readonly string[] OldRoadsDataFolderNames = ["roads-v1"];

    public static string RoadsFolder()
    {
        var currentDownloadFolder = DownloadFolder(RunMode.Default);
        var roadsFolder = Path.Combine(currentDownloadFolder, "roads-v2");
        return roadsFolder;
    }
}

internal record DownloadMetadata
{
    public File[] Files { get; init; } = [];

    internal record File
    {
        public required string Name { get; init; }
        public required DateTime LastWriteTimeUtc { get; init; }
    }
}

internal record R2Object
{
    public required string Key { get; init; }
    public required DateTime Uploaded { get; init; }
    public long? Size { get; init; }
}

public enum RunMode
{
    Default,
    Localhost
}