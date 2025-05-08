using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ICSharpCode.SharpZipLib.BZip2;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Diagnostics;
using Vali.Core.Data;

namespace Vali.Core;

public static class DataDownloadService
{
    private const string FileExtension = ".bin";

    public static async Task DownloadFiles(string? countryCode, bool full, bool updates)
    {
        if (!EnsureDownloadFolderIsWritable())
        {
            return;
        }

        var sw = Stopwatch.StartNew();
        var logger = ValiLogger.Factory.CreateLogger<LocationLakeMapGenerator>();

        var downloadOperations =
            new (BlobContainerClient client,
                Action<string, RunMode, DownloadMetadata.File> deleteAction,
                Func<BlobContainerClient, string, IReadOnlyCollection<BlobFile>, RunMode, ProgressTask, Task> downloadAction,
                Func<BlobFile, string> groupBy,
                string downloadConsoleSuffix,
                Func<string, RunMode, IReadOnlyCollection<BlobFile>, Task> downloadedAction,
                bool force)[]
                {
                    (CreateBlobServiceClient(), DeleteDataFile, DownloadDataFiles, _ => "", "", SaveDataFilesDownloaded, full),
                    (CreateBlobServiceClientForUpdates(), (_, _, _) => {}, DownloadUpdateFile, f => f.BlobName.RemoveDatePrefix(), " updates", SaveUpdateFilesDownloaded, updates),
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
                        var filesFromBlob = await GetFilesFrom(code, downloadOperation.client);

                        var localFiles = ExistingFilesInMetadata(code, runMode);
                        var filesToDownload = filesFromBlob.Where(blobFile =>
                        {
                            var localFile = localFiles.FirstOrDefault(file => Path.GetFileNameWithoutExtension(file.Name) == Path.GetFileNameWithoutExtension(blobFile.BlobName));
                            return localFile == null || localFile.LastWriteTimeUtc < blobFile.LastModified || downloadOperation.force;
                        }).ToArray();
                        var filesToDelete = localFiles.Where(diskFile =>
                        {
                            var matchingFileFromBlob = filesFromBlob.FirstOrDefault(blobFile => Path.GetFileNameWithoutExtension(blobFile.BlobName) == Path.GetFileNameWithoutExtension(diskFile.Name));
                            return matchingFileFromBlob == null;
                        }).ToArray();
                        foreach (var diskFile in filesToDelete)
                        {
                            downloadOperation.deleteAction(code, runMode, diskFile);
                        }

                        if (!filesToDownload.Any())
                        {
                            continue;
                        }

                        var task = ctx.AddTask($"[green]{CountryCodes.Name(code)}{downloadOperation.downloadConsoleSuffix}[/]", maxValue: filesToDownload.Sum(f => f.ContentLength ?? 0));
                        await Task.Delay(5);
                        var blobFilesDownloaded = new List<BlobFile>();
                        foreach (var blobFiles in filesToDownload.GroupBy(downloadOperation.groupBy))
                        {
                            exitRequested = Console.KeyAvailable && Console.ReadKey().KeyChar == 's';
                            if (exitRequested)
                            {
                                task.StopTask();
                                break;
                            }

                            var blobFilesToDownload = blobFiles.ToArray();
                            await downloadOperation.downloadAction(downloadOperation.client, code, blobFilesToDownload, runMode, task);
                            blobFilesDownloaded.AddRange(blobFilesToDownload);
                        }

                        await downloadOperation.downloadedAction(code, runMode, blobFilesDownloaded);
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

    private static async Task SaveDataFilesDownloaded(string countryCode, RunMode runMode, IReadOnlyCollection<BlobFile> files)
    {
        var metadataPath = DownloadMetadataPath(countryCode, runMode);
        var metadata = Extensions.TryJsonDeserializeFromFile(metadataPath, new DownloadMetadata());
        var newFiles = files.Select(f => new DownloadMetadata.File
            {
                Name = Path.GetFileNameWithoutExtension(f.BlobName),
                LastWriteTimeUtc = (f.LastModified ?? DateTimeOffset.UtcNow).UtcDateTime
            })
            .Concat(metadata.Files
                .Where(f => files.All(x => Path.GetFileNameWithoutExtension(x.BlobName) != Path.GetFileNameWithoutExtension(f.Name.RemoveDatePrefix()))))
            .DistinctBy(f => f.Name)
            .ToArray();
        metadata = metadata with
        {
            Files = newFiles
        };
        await Extensions.PrettyJsonSerializeToFile(metadataPath, metadata);
    }

    private static async Task SaveUpdateFilesDownloaded(string countryCode, RunMode runMode, IReadOnlyCollection<BlobFile> files)
    {
        var metadataPath = DownloadMetadataPath(countryCode, runMode);
        var metadata = Extensions.TryJsonDeserializeFromFile(metadataPath, new DownloadMetadata());
        var newFiles = files.Select(f => new DownloadMetadata.File
            {
                Name = Path.GetFileNameWithoutExtension(f.BlobName),
                LastWriteTimeUtc = (f.LastModified ?? DateTimeOffset.UtcNow).UtcDateTime
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

    private static void DeleteDataFile(string countryCode, RunMode runMode, DownloadMetadata.File file)
    {
        var folder = CountryFolder(countryCode, runMode);
        var fullPath = Directory.GetFiles(folder).FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == file.Name);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private static async Task DownloadDataFiles(BlobContainerClient blobContainerClient, string countryCode, IReadOnlyCollection<BlobFile> filesToDownload, RunMode runMode, ProgressTask? task)
    {
        var maxFileSize = filesToDownload.Any() ? filesToDownload.Max(x => x.ContentLength) : 0;
        var chunkSize = (maxFileSize / 1024 / 1024) switch
        {
            > 100 => 1,
            < 5 => 6,
            _ => 3
        };

        var folder = CountryFolder(countryCode, runMode);
        foreach (var fileGroup in filesToDownload.Chunk(chunkSize))
        {
            await Task.WhenAll(fileGroup.Select(file => DownloadFile(blobContainerClient, file.BlobName, folder)));
            task?.Increment(fileGroup.Sum(x => x.ContentLength ?? 0));
        }
    }

    private static async Task DownloadUpdateFile(BlobContainerClient blobContainerClient, string countryCode, IReadOnlyCollection<BlobFile> blobFiles, RunMode runMode, ProgressTask task)
    {
        var updatesFolder = UpdatesFolder(countryCode, runMode);
        var newLocations = new List<Location>();
        var updateFilePath = "";
        foreach (var file in blobFiles)
        {
            updateFilePath = await DownloadFile(blobContainerClient, file.BlobName, updatesFolder);
            newLocations.AddRange(Extensions.ProtoDeserializeFromFile<Location[]>(updateFilePath));
            task.Increment(file.ContentLength ?? 0);
        }

        var countryFolder = CountryFolder(countryCode, runMode);
        var dataFilePath = Path.Combine(countryFolder, Path.GetFileName(updateFilePath).RemoveDatePrefix());
        await ApplyUpdatesToDataFile(dataFilePath, newLocations);
    }

    private static async Task<string> DownloadFile(BlobContainerClient blobContainerClient, string blobName, string folder)
    {
        var blobClient = blobContainerClient.GetBlobClient(blobName);
        Directory.CreateDirectory(folder);
        var destinationFileName = Path.GetFileNameWithoutExtension(blobName) + FileExtension;
        var filePath = Path.Combine(folder, destinationFileName);
        var fileMode = File.Exists(filePath) ? FileMode.Truncate : FileMode.CreateNew;
        await using var fileOnDiskStream = new FileStream(filePath, fileMode);
        using var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream);
        memoryStream.Position = 0;
        BZip2.Decompress(memoryStream, fileOnDiskStream, true);
        return filePath;
    }

    private static async Task ApplyUpdatesToDataFile(string dataFilePath, IEnumerable<Location> updateLocations)
    {
        var existingLocations = Extensions.ProtoDeserializeFromFile<Location[]>(dataFilePath);
        var newLocations = updateLocations.Concat(existingLocations).DistinctBy(x => x.NodeId).ToArray();
        await Extensions.ProtoSerializeToFile(dataFilePath, newLocations);
    }

    private static BlobContainerClient CreateBlobServiceClient() => GetBlobServiceClient("countries-v1");

    private static BlobContainerClient CreateBlobServiceClientForUpdates() => GetBlobServiceClient("countries-updates-v1");

    private static BlobContainerClient GetBlobServiceClient(string blobContainerName) => new BlobServiceClient(new Uri("https://valistorage.blob.core.windows.net/")).GetBlobContainerClient(blobContainerName);

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
        var applicationSettings = ApplicationSettingsService.ReadApplicationSettings();
        if (runMode == RunMode.Localhost && !string.IsNullOrEmpty(applicationSettings.LocalhostDownloadDirectory))
        {
            return Path.Combine(applicationSettings.LocalhostDownloadDirectory!, countryCode);
        }

        var defaultDownloadFolderEnvironment = ApplicationSettingsService.ReadDownloadFolderFromEnvironmentVariable();
        var defaultDownloadFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var downloadFolder = defaultDownloadFolderEnvironment ?? applicationSettings.DownloadDirectory ?? defaultDownloadFolder;
        return Path.Combine(downloadFolder, "Vali", countryCode);
    }

    private static string UpdatesFolder(string countryCode, RunMode runMode)
    {
        var countryFolder = CountryFolder(countryCode, runMode);
        return Path.Combine(countryFolder, "updates");
    }

    private static async Task<IReadOnlyCollection<BlobFile>> GetFilesFrom(string countryCode, BlobContainerClient blobContainerClient)
    {
        var resultSegment = blobContainerClient.GetBlobsAsync(BlobTraits.Metadata, prefix: countryCode).AsPages(default, 10);
        var files = new List<BlobFile>();
        await foreach (var blobPage in resultSegment)
        {
            files.AddRange(blobPage.Values.Select(blobItem => new BlobFile
            {
                BlobName = blobItem.Name,
                LastModified = blobItem.Properties.LastModified,
                ContentLength = blobItem.Properties.ContentLength
            }));
        }

        return files;
    }

    public static async Task EnsureFilesDownloaded(string countryCode, string[] subdivisionFiles)
    {
        if (!EnsureDownloadFolderIsWritable())
        {
            return;
        }

        BlobContainerClient? blobContainerClient = null;

        foreach (var subdivisionFile in subdivisionFiles)
        {
            if (!File.Exists(subdivisionFile))
            {
                blobContainerClient ??= CreateBlobServiceClient();
                var fileName = Path.GetFileName(subdivisionFile);
                var blobFileName = Path.GetFileNameWithoutExtension(fileName) + ".zip";
                var blobName = $"{countryCode}/{blobFileName}";
                ConsoleLogger.Warn($"Downloading {CountryCodes.Name(countryCode)} data.");
                var filesToDownload = new[]
                {
                    new BlobFile
                    {
                        BlobName = blobName,
                        LastModified = DateTimeOffset.MinValue,
                        ContentLength = 0
                    }
                };
                await DownloadDataFiles(blobContainerClient, countryCode, filesToDownload, RunMode.Default, null);
            }
        }
    }

    private static bool EnsureDownloadFolderIsWritable()
    {
        var currentDownloadFolder = CurrentDownloadFolder();

        if (!Extensions.IsDirectoryWritable(currentDownloadFolder))
        {
            var downloadFolder = AnsiConsole.Ask("Please specify a folder where vali should store downloaded files.", "");
            if (!Directory.Exists(downloadFolder))
            {
                ConsoleLogger.Error($"Folder '{downloadFolder}' does not exist.");
                return false;
            }

            if (!Extensions.IsDirectoryWritable(downloadFolder))
            {
                ConsoleLogger.Error($"Vali does not have access to write files to '{downloadFolder}'");
                return false;
            }

            var fullDownloadPath = Path.GetFullPath(downloadFolder);
            ApplicationSettingsService.SetDownloadFolder(fullDownloadPath);
            ConsoleLogger.Success($"'{fullDownloadPath}' set as download folder. Use 'vali set-download-folder' if you want to change it.");
        }

        return true;
    }

    public static string? CurrentDownloadFolder()
    {
        var countryFolder = CountryFolder("XX", RunMode.Default);
        return !string.IsNullOrEmpty(countryFolder) ? new DirectoryInfo(countryFolder).Parent?.Parent?.FullName : null;
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

internal record BlobFile
{
    public required string BlobName { get; init; }
    public required DateTimeOffset? LastModified { get; init; }
    public long? ContentLength { get; init; }
}

public enum RunMode
{
    Default,
    Localhost
}