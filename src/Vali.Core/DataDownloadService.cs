using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ICSharpCode.SharpZipLib.BZip2;
using Spectre.Console;
using Vali.Core.Data;

namespace Vali.Core;

public class DataDownloadService
{
    private const string FileExtension = ".bin";

    public static async Task DownloadFiles(string? countryCode)
    {
        var blobContainerClient = CreateBlobServiceClient();
        var countryCodes = string.IsNullOrEmpty(countryCode) ?
            CountryCodes.Countries.Keys :
            CountryCodes.Countries.Keys.Intersect(MapDefinitionDefaults.ExpandCountryCode(countryCode, new DistributionStrategy()));
        AnsiConsole.MarkupLine("[yellow]Downloads starting.[/]");
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                foreach (var code in countryCodes)
                {
                    var filesFromBlob = await GetFilesFrom(code, blobContainerClient);

                    var filesFromDisk = ExistingFilesForCountry(code, RunMode.Default);
                    var filesToDownload = filesFromBlob.Where(blobFile =>
                    {
                        var matchingFileFromDisk = filesFromDisk.FirstOrDefault(diskFile => Path.GetFileNameWithoutExtension(diskFile.Name) == Path.GetFileNameWithoutExtension(blobFile.BlobName));
                        return matchingFileFromDisk == null || matchingFileFromDisk.LastWriteTimeUtc < blobFile.LastModified || matchingFileFromDisk.Length < blobFile.ContentLength;
                    }).ToArray();
                    var filesToDelete = filesFromDisk.Where(diskFile =>
                    {
                        var matchingFileFromBlob = filesFromBlob.FirstOrDefault(blobFile => Path.GetFileNameWithoutExtension(blobFile.BlobName) == Path.GetFileNameWithoutExtension(diskFile.Name));
                        return matchingFileFromBlob == null;
                    }).ToArray();
                    foreach (var diskFile in filesToDelete)
                    {
                        File.Delete(diskFile.FullName);
                    }

                    if (!filesToDownload.Any())
                    {
                        continue;
                    }

                    var task = ctx.AddTask($"[green]{CountryCodes.Name(code)}[/]", maxValue: filesToDownload.Sum(f => f.ContentLength ?? 0));
                    await Task.Delay(5);
                    var maxFileSize = filesToDownload.Any() ? filesToDownload.Max(x => x.ContentLength) : 0;
                    var chunkSize = (maxFileSize / 1024 / 1024) switch
                    {
                        > 100 => 1,
                        < 5 => 6,
                        _ => 3
                    };
                    foreach (var blobFiles in filesToDownload.Chunk(chunkSize))
                    {
                        await Task.WhenAll(blobFiles.Select(file => DownloadFile(blobContainerClient, code, file.BlobName, RunMode.Default)));
                        task.Increment(blobFiles.Sum(x => x.ContentLength ?? 0));
                    }

                    task.StopTask();
                }
            });
        AnsiConsole.MarkupLine("[yellow]Downloads finished.[/]");
    }

    private static async Task DownloadFile(BlobContainerClient blobContainerClient, string countryCode, string blobName, RunMode runMode)
    {
        var blobClient = blobContainerClient.GetBlobClient(blobName);
        var countryFolder = CountryFolder(countryCode, runMode);
        Directory.CreateDirectory(countryFolder);
        var destinationFileName = Path.GetFileNameWithoutExtension(blobName) + FileExtension;
        var filePath = Path.Combine(countryFolder, destinationFileName);
        var fileMode = File.Exists(filePath) ? FileMode.Truncate : FileMode.CreateNew;
        await using var fileOnDiskStream = new FileStream(filePath, fileMode);
        using var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream);
        memoryStream.Position = 0;
        BZip2.Decompress(memoryStream, fileOnDiskStream, true);
    }

    private static BlobContainerClient CreateBlobServiceClient() =>
        new BlobServiceClient(new Uri("https://valistorage.blob.core.windows.net/")).GetBlobContainerClient("countries-v1");

    public static FileInfo[] ExistingFilesForCountry(string countryCode, RunMode runMode)
    {
        var folder = CountryFolder(countryCode, runMode);
        if (!Directory.Exists(folder))
        {
            return Array.Empty<FileInfo>();
        }

        return Directory.GetFiles(folder, $"*{FileExtension}").Select(x => new FileInfo(x)).ToArray();
    }

    public static string CountryFolder(string countryCode, RunMode runMode)
    {
        var applicationSettings = ApplicationSettingsService.ReadApplicationSettings();
        if (runMode == RunMode.Localhost)
        {
            return Path.Combine(applicationSettings.LocalhostDownloadDirectory!, countryCode);
        }

        var defaultDownloadFolderEnvironment = ApplicationSettingsService.ReadDownloadFolderFromEnvironmentVariable();
        var defaultDownloadFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var downloadFolder = defaultDownloadFolderEnvironment ?? applicationSettings.DownloadDirectory ?? defaultDownloadFolder;
        return Path.Combine(downloadFolder, "Vali", countryCode);
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
                await DownloadFile(blobContainerClient, countryCode, blobName, RunMode.Default);
            }
        }
    }
}

internal record BlobFile
{
    public required string BlobName { get; set; }
    public required DateTimeOffset? LastModified { get; set; }
    public long? ContentLength { get; set; }
}

public enum RunMode
{
    Default,
    Localhost
}