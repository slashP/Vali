using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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

                    var filesFromDisk = ExistingFilesForCountry(code);
                    var filesToDownload = filesFromBlob.Where(blobFile =>
                    {
                        var matchingFileFromDisk = filesFromDisk.FirstOrDefault(diskFile => diskFile.Name == blobFile.FileName);
                        return matchingFileFromDisk == null || matchingFileFromDisk.LastWriteTimeUtc < blobFile.LastModified || matchingFileFromDisk.Length < blobFile.ContentLength;
                    }).ToArray();
                    var filesToDelete = filesFromDisk.Where(diskFile =>
                    {
                        var matchingFileFromBlob = filesFromBlob.FirstOrDefault(blobFile => blobFile.FileName == diskFile.Name);
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
                    foreach (var blobFile in filesToDownload)
                    {
                        await DownloadFile(blobContainerClient, code, blobFile.BlobName, blobFile.FileName);
                        task.Increment(blobFile.ContentLength ?? 0);
                    }

                    task.StopTask();
                }
            });
        AnsiConsole.MarkupLine("[yellow]Downloads finished.[/]");
    }

    private static async Task DownloadFile(BlobContainerClient blobContainerClient, string countryCode, string blobName, string fileName)
    {
        var blobClient = blobContainerClient.GetBlobClient(blobName);
        var countryFolder = CountryFolder(countryCode);
        Directory.CreateDirectory(countryFolder);
        var filePath = Path.Combine(countryFolder, fileName);
        var fileMode = File.Exists(filePath) ? FileMode.Truncate : FileMode.CreateNew;
        await using var fs = new FileStream(filePath, fileMode);
        await blobClient.DownloadToAsync(fs);
    }

    private static BlobContainerClient CreateBlobServiceClient() =>
        new BlobServiceClient(new Uri("https://valistorage.blob.core.windows.net/")).GetBlobContainerClient("countries");

    private static FileInfo[] ExistingFilesForCountry(string countryCode)
    {
        var folder = CountryFolder(countryCode);
        if (!Directory.Exists(folder))
        {
            return Array.Empty<FileInfo>();
        }

        return Directory.GetFiles(folder, $"*{FileExtension}").Select(x => new FileInfo(x)).ToArray();
    }

    public static string CountryFolder(string countryCode)
    {
        var defaultDownloadFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var downloadFolder = ApplicationSettingsService.ReadApplicationSettings().DownloadDirectory ?? defaultDownloadFolder;
        return Path.Combine(downloadFolder, "Vali", countryCode);
    }

    private static async Task<IReadOnlyCollection<BlobFile>> GetFilesFrom(string countryCode, BlobContainerClient blobContainerClient)
    {
        var resultSegment = blobContainerClient.GetBlobsAsync(BlobTraits.Metadata, prefix: countryCode).AsPages(default, 10);
        var files = new List<BlobFile>();
        await foreach (var blobPage in resultSegment)
        {
            files.AddRange(blobPage.Values.Select(blobItem =>
            {
                var fileName = Path.GetFileName(blobItem.Name);
                return new BlobFile
                {
                    BlobName = blobItem.Name,
                    FileName = fileName,
                    LastModified = blobItem.Properties.LastModified,
                    ContentLength = blobItem.Properties.ContentLength
                };
            }).Where(x => Path.GetExtension(x.FileName) == FileExtension));
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
                var blobName = $"{countryCode}/{fileName}";
                ConsoleLogger.Warn($"Downloading {CountryCodes.Name(countryCode)} data.");
                await DownloadFile(blobContainerClient, countryCode, blobName, fileName);
            }
        }
    }
}

internal record BlobFile
{
    public required string BlobName { get; set; }
    public required DateTimeOffset? LastModified { get; set; }
    public required string FileName { get; set; }
    public long? ContentLength { get; set; }
}
