using System.Net.Http.Json;
using ICSharpCode.SharpZipLib.BZip2;
using Vali.Core;
using Vali.Core.Data;

namespace Vali.Host;

/// <summary>
/// Re-implements Vali's DataDownloadService flow (list → delete stale → download BZip2 data
/// files → apply incremental updates by append → maintain downloads.json) without the console
/// coupling, emitting protocol progress events instead. The manifest format and freshness
/// predicates match the CLI exactly, so Studio and `vali download` stay interoperable.
/// </summary>
public sealed class DownloadRunner(ProtocolWriter writer)
{
    private const string CountriesBucket = "countries-v2";
    private const string UpdatesBucket = "country-updates-v2";
    private const string FileExtension = ".bin";
    private const int ParallelDownloads = 6;

    private static readonly HttpClient Client = new()
    {
        BaseAddress = new Uri("https://vali-download.slashp.workers.dev"),
        Timeout = TimeSpan.FromMinutes(10)
    };

    private readonly object _lock = new();
    private readonly Dictionary<string, (string CountryCode, CancellationTokenSource Cts)> _active = new();

    public sealed record MetadataFileEntry
    {
        public required string Name { get; init; }
        public required DateTime LastWriteTimeUtc { get; init; }
    }

    public sealed record Metadata
    {
        public MetadataFileEntry[] Files { get; init; } = [];
    }

    public sealed record RemoteFile
    {
        public required string Key { get; init; }
        public required DateTime Uploaded { get; init; }
        public long? Size { get; init; }
    }

    public bool TryCancel(string? targetId)
    {
        lock (_lock)
        {
            if (targetId is not null && _active.TryGetValue(targetId, out var active))
            {
                active.Cts.Cancel();
                return true;
            }
        }

        return false;
    }

    public async Task RemoteStatus(Request request)
    {
        var codes = request.Params.ValueKind == System.Text.Json.JsonValueKind.Object &&
                    request.Params.TryGetProperty("countryCodes", out var codesElement) &&
                    codesElement.ValueKind == System.Text.Json.JsonValueKind.Array
            ? codesElement.EnumerateArray()
                .Where(x => x.ValueKind == System.Text.Json.JsonValueKind.String)
                .Select(x => (x.GetString() ?? "").ToUpperInvariant())
                .Where(x => x.Length == 2)
                .Distinct()
                .ToArray()
            : [];

        var results = new List<object>();
        foreach (var code in codes)
        {
            try
            {
                results.Add(await RemoteStatusFor(code));
            }
            catch (Exception e)
            {
                results.Add(new { code, error = e.Message });
            }
        }

        writer.Respond(request.Id, new { countries = results });
    }

    private static async Task<object> RemoteStatusFor(string code)
    {
        await EnsureMetadataFileExists(code);
        var remoteData = await List(CountriesBucket, code);
        var remoteUpdates = await List(UpdatesBucket, code);
        var metadata = ReadMetadata(code);
        var toDownload = FilesToDownload(remoteData, metadata, force: false);
        var updatesToApply = FilesToDownload(remoteUpdates, metadata, force: false);

        var countryFolder = DataDownloadService.CountryFolder(code, RunMode.Default);
        var localFiles = Directory.Exists(countryFolder)
            ? Directory.GetFiles(countryFolder, $"*{FileExtension}").Select(f => new FileInfo(f)).ToArray()
            : [];

        return new
        {
            code,
            local = new
            {
                files = localFiles.Length,
                bytes = localFiles.Sum(f => f.Length),
                newestUtc = localFiles.Length > 0 ? localFiles.Max(f => f.LastWriteTimeUtc) : (DateTime?)null
            },
            remote = new
            {
                files = remoteData.Length,
                bytes = remoteData.Sum(f => f.Size ?? 0),
                newestUploadedUtc = remoteData.Length > 0 ? remoteData.Max(f => f.Uploaded) : (DateTime?)null
            },
            filesToDownload = toDownload.Length,
            updateFilesToApply = updatesToApply.Length,
            bytesToDownload = toDownload.Sum(f => f.Size ?? 0) + updatesToApply.Sum(f => f.Size ?? 0),
            upToDate = toDownload.Length == 0 && updatesToApply.Length == 0 && localFiles.Length > 0,
            error = (string?)null
        };
    }

    public async Task Run(Request request)
    {
        var requestId = request.Id ?? Guid.NewGuid().ToString("N");
        var countryCode = (request.GetString("countryCode") ?? "").ToUpperInvariant();
        if (!CountryCodes.Countries.ContainsKey(countryCode))
        {
            writer.RespondError(request.Id, $"Unknown country code '{countryCode}'.");
            return;
        }

        var downloadFolder = DataDownloadService.DownloadFolder(RunMode.Default);
        if (!IsWritable(downloadFolder))
        {
            writer.RespondError(request.Id, $"Vali's data folder is not writable: {downloadFolder}", null,
                code: "folderNotWritable", data: new { folder = downloadFolder });
            return;
        }

        if (!CountryDataLocks.TryAcquire([countryCode], "a download", out var conflict))
        {
            writer.RespondError(request.Id, $"Can't download: {conflict}.");
            return;
        }

        using var cts = new CancellationTokenSource();
        lock (_lock)
        {
            _active[requestId] = (countryCode, cts);
        }

        try
        {
            await RunCore(requestId, countryCode, request.GetBool("full"), request.GetBool("updates"), cts.Token);
        }
        catch (Exception e)
        {
            writer.RespondError(requestId, $"Download of {countryCode} failed: {e.Message}", e.ToString());
        }
        finally
        {
            lock (_lock)
            {
                _active.Remove(requestId);
            }

            CountryDataLocks.Release([countryCode]);
        }
    }

    private async Task RunCore(string requestId, string countryCode, bool full, bool updates, CancellationToken token)
    {
        var countryFolder = DataDownloadService.CountryFolder(countryCode, RunMode.Default);
        await EnsureMetadataFileExists(countryCode);

        var deletedFiles = 0;
        long bytesDownloaded = 0;

        var remoteData = await List(CountriesBucket, countryCode);
        var metadata = ReadMetadata(countryCode);
        var toDownload = FilesToDownload(remoteData, metadata, full);
        var toDelete = metadata.Files
            .Where(f => remoteData.All(r => Path.GetFileNameWithoutExtension(r.Key) != Path.GetFileNameWithoutExtension(f.Name)))
            .ToArray();
        foreach (var staleFile in toDelete)
        {
            var fullPath = Directory.Exists(countryFolder)
                ? Directory.GetFiles(countryFolder).FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == staleFile.Name)
                : null;
            if (fullPath is not null && File.Exists(fullPath))
            {
                File.Delete(fullPath);
                deletedFiles++;
            }
        }

        var downloadedData = await DownloadMany(requestId, countryCode, "data", CountriesBucket, toDownload, countryFolder, token);
        bytesDownloaded += downloadedData.Sum(x => x.File.Size ?? 0);
        await SaveDataMetadata(countryCode, downloadedData.Select(x => x.File).ToArray());

        var appliedUpdateFiles = 0;
        var cancelled = token.IsCancellationRequested;
        if (!cancelled)
        {
            var updatesFolder = Path.Combine(countryFolder, "updates");
            if (Directory.Exists(updatesFolder))
            {
                Directory.Delete(updatesFolder, true);
            }

            var remoteUpdates = await List(UpdatesBucket, countryCode);
            metadata = ReadMetadata(countryCode);
            var updatesToApply = FilesToDownload(remoteUpdates, metadata, updates);
            var appliedR2 = new List<RemoteFile>();
            foreach (var group in updatesToApply.GroupBy(f => RemoveDatePrefix(Path.GetFileName(f.Key))))
            {
                if (token.IsCancellationRequested)
                {
                    cancelled = true;
                    break;
                }

                var groupFiles = group.ToArray();
                var downloaded = await DownloadMany(requestId, countryCode, "updates", UpdatesBucket, groupFiles, updatesFolder, token);
                if (downloaded.Count < groupFiles.Length)
                {
                    cancelled = true;
                    break;
                }

                var newLocations = new List<Location>();
                foreach (var (path, _) in downloaded)
                {
                    newLocations.AddRange(Extensions.ProtoDeserializeFromFile<Location[]>(path));
                }

                var dataFilePath = Path.Combine(countryFolder, RemoveDatePrefix(Path.GetFileName(downloaded[^1].Path)));
                await Extensions.ProtoAppendSerializeToFile(dataFilePath, newLocations.ToArray());
                bytesDownloaded += downloaded.Sum(x => x.File.Size ?? 0);
                appliedUpdateFiles += downloaded.Count;
                appliedR2.AddRange(groupFiles);
            }

            await SaveUpdatesMetadata(countryCode, appliedR2);
            if (Directory.Exists(updatesFolder))
            {
                Directory.Delete(updatesFolder, true);
            }
        }

        writer.Respond(requestId, new
        {
            countryCode,
            downloadedFiles = downloadedData.Count,
            deletedFiles,
            appliedUpdateFiles,
            bytesDownloaded,
            cancelled = cancelled || token.IsCancellationRequested
        });
    }

    private async Task<List<(string Path, RemoteFile File)>> DownloadMany(
        string requestId,
        string countryCode,
        string phase,
        string bucket,
        RemoteFile[] files,
        string folder,
        CancellationToken token)
    {
        var results = new List<(string Path, RemoteFile File)>();
        if (files.Length == 0)
        {
            return results;
        }

        var bytesTotal = files.Sum(f => f.Size ?? 0);
        long bytesDone = 0;
        var filesDone = 0;
        using var gate = new SemaphoreSlim(ParallelDownloads);
        var tasks = new List<Task>();
        foreach (var file in files)
        {
            await gate.WaitAsync(CancellationToken.None);
            if (token.IsCancellationRequested)
            {
                gate.Release();
                break;
            }

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var path = await DownloadOne(bucket, file, folder, token);
                    long bytesSnapshot;
                    int filesSnapshot;
                    lock (results)
                    {
                        results.Add((path, file));
                        bytesDone += file.Size ?? 0;
                        filesDone++;
                        bytesSnapshot = bytesDone;
                        filesSnapshot = filesDone;
                    }

                    writer.Emit("downloadProgress", requestId, new
                    {
                        countryCode,
                        phase,
                        fileName = Path.GetFileNameWithoutExtension(file.Key),
                        filesDone = filesSnapshot,
                        filesTotal = files.Length,
                        bytesDone = bytesSnapshot,
                        bytesTotal
                    });
                }
                catch (OperationCanceledException)
                {
                    // Cancelled mid-transfer — leave this file out of results;
                    // RunCore reports the download as cancelled.
                }
                finally
                {
                    gate.Release();
                }
            }, CancellationToken.None));
        }

        await Task.WhenAll(tasks);
        return results;
    }

    private static async Task<string> DownloadOne(string bucket, RemoteFile file, string folder, CancellationToken token)
    {
        const int maxRetries = 3;
        Directory.CreateDirectory(folder);
        var destination = Path.Combine(folder, Path.GetFileNameWithoutExtension(file.Key) + FileExtension);
        var url = $"{bucket}/{file.Key}";

        for (var attempt = 1; ; attempt++)
        {
            // Download+decompress into a temp file and move into place only on success, so a
            // failed download never truncates a previously valid data file.
            var temp = destination + $".{Guid.NewGuid():N}.tmp";
            try
            {
                await using (var fileStream = new FileStream(temp, FileMode.CreateNew))
                {
                    using var response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
                    response.EnsureSuccessStatusCode();

                    using var memoryStream = new MemoryStream();
                    await response.Content.CopyToAsync(memoryStream, token);
                    token.ThrowIfCancellationRequested();
                    memoryStream.Position = 0;
                    BZip2.Decompress(memoryStream, fileStream, true);
                }

                File.Move(temp, destination, overwrite: true);
                return destination;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // A genuine cancel — abort immediately, don't retry.
                TryDelete(temp);
                throw;
            }
            catch (Exception ex) when (attempt < maxRetries && ex is HttpRequestException or TaskCanceledException or IOException)
            {
                TryDelete(temp);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), token);
            }
            catch
            {
                TryDelete(temp);
                throw;
            }
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup of a temp file.
        }
    }

    private static async Task<RemoteFile[]> List(string bucket, string countryCode)
    {
        var listing = bucket.Contains("updates", StringComparison.InvariantCultureIgnoreCase)
            ? "list-country-updates"
            : "list-countries";
        return await Client.GetFromJsonAsync<RemoteFile[]>($"{listing}/{countryCode}") ?? [];
    }

    private static RemoteFile[] FilesToDownload(RemoteFile[] remote, Metadata metadata, bool force) =>
        remote.Where(r2File =>
        {
            var localFile = metadata.Files.FirstOrDefault(file =>
                Path.GetFileNameWithoutExtension(file.Name) == Path.GetFileNameWithoutExtension(r2File.Key));
            return localFile == null || localFile.LastWriteTimeUtc < r2File.Uploaded || force;
        }).ToArray();

    private static async Task SaveDataMetadata(string countryCode, RemoteFile[] downloaded)
    {
        if (downloaded.Length == 0)
        {
            return;
        }

        var metadataPath = MetadataPath(countryCode);
        var metadata = Extensions.TryJsonDeserializeFromFile(metadataPath, new Metadata());
        var newFiles = downloaded
            .Select(f => new MetadataFileEntry { Name = Path.GetFileNameWithoutExtension(f.Key), LastWriteTimeUtc = f.Uploaded })
            .Concat(metadata.Files.Where(f =>
                downloaded.All(x => Path.GetFileNameWithoutExtension(x.Key) != Path.GetFileNameWithoutExtension(RemoveDatePrefix(f.Name)))))
            .DistinctBy(f => f.Name)
            .ToArray();
        await Extensions.PrettyJsonSerializeToFile(metadataPath, metadata with { Files = newFiles });
    }

    private static async Task SaveUpdatesMetadata(string countryCode, IReadOnlyCollection<RemoteFile> applied)
    {
        if (applied.Count == 0)
        {
            return;
        }

        var metadataPath = MetadataPath(countryCode);
        var metadata = Extensions.TryJsonDeserializeFromFile(metadataPath, new Metadata());
        var newFiles = applied
            .Select(f => new MetadataFileEntry { Name = Path.GetFileNameWithoutExtension(f.Key), LastWriteTimeUtc = f.Uploaded })
            .Concat(metadata.Files)
            .DistinctBy(f => f.Name)
            .ToArray();
        await Extensions.PrettyJsonSerializeToFile(metadataPath, metadata with { Files = newFiles });
    }

    private static async Task EnsureMetadataFileExists(string countryCode)
    {
        var countryFolder = DataDownloadService.CountryFolder(countryCode, RunMode.Default);
        if (!Directory.Exists(countryFolder))
        {
            return;
        }

        var metadataPath = MetadataPath(countryCode);
        if (File.Exists(metadataPath))
        {
            return;
        }

        var files = Directory.GetFiles(countryFolder, $"*{FileExtension}").Select(f => new FileInfo(f));
        var metadata = new Metadata
        {
            Files = files.Select(f => new MetadataFileEntry
            {
                Name = Path.GetFileNameWithoutExtension(f.Name),
                LastWriteTimeUtc = f.LastWriteTimeUtc.AddTicks(-(f.LastWriteTimeUtc.Ticks % TimeSpan.TicksPerSecond))
            }).ToArray()
        };
        await Extensions.PrettyJsonSerializeToFile(metadataPath, metadata);
    }

    private static Metadata ReadMetadata(string countryCode) =>
        Extensions.TryJsonDeserializeFromFile(MetadataPath(countryCode), new Metadata());

    private static string MetadataPath(string countryCode) =>
        Path.Combine(DataDownloadService.CountryFolder(countryCode, RunMode.Default), "downloads.json");

    private static string RemoveDatePrefix(string filename) =>
        new(filename.SkipWhile(c => char.IsNumber(c) || c == '-').ToArray());

    private static bool IsWritable(string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);
            var probe = Path.Combine(folder, $".vali-studio-write-probe-{Guid.NewGuid():N}");
            File.WriteAllText(probe, "");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task ValiInfo(Request request)
    {
        string? latest = null;
        try
        {
            var index = await NugetClient.GetFromJsonAsync<NugetIndex>("v3-flatcontainer/vali/index.json");
            var versions = index?.Versions ?? [];
            latest = versions.LastOrDefault(v => !v.Contains('-')) ?? versions.LastOrDefault();
        }
        catch
        {
            // Offline or nuget unreachable — latestValiVersion stays null.
        }

        writer.Respond(request.Id, new
        {
            studioEngineVersion = "0.1.0",
            latestValiVersion = latest,
            nugetUrl = "https://www.nuget.org/packages/vali"
        });
    }

    private static readonly HttpClient NugetClient = new()
    {
        BaseAddress = new Uri("https://api.nuget.org/"),
        Timeout = TimeSpan.FromSeconds(10)
    };

    private sealed record NugetIndex(string[] Versions);
}
