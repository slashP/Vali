namespace Vali.Core;

public static class ApplicationSettingsService
{
    public const string DownloadFolderEnvironmentVariableName = "VALI_DOWNLOAD_FOLDER";

    public static void SetDownloadFolder(string directory)
    {
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            ConsoleLogger.Error($"Directory {directory} does not exist.");
        }

        var settings = ReadApplicationSettings();
        settings.DownloadDirectory = string.IsNullOrEmpty(directory) ? null : directory;
        WriteApplicationSettings(settings);
    }

    public static void SetParallelism(int parallelism)
    {
        var settings = ReadApplicationSettings();
        settings.Parallelism = parallelism;
        WriteApplicationSettings(settings);
    }

    public static string? ReadDownloadFolderFromEnvironmentVariable() =>
        Environment.GetEnvironmentVariable(DownloadFolderEnvironmentVariableName);

    public static void UnsetDownloadFolder()
    {
        var settings = ReadApplicationSettings();
        settings.DownloadDirectory = null;
        WriteApplicationSettings(settings);
    }

    public static void UnsetParallelism()
    {
        var settings = ReadApplicationSettings();
        settings.Parallelism = null;
        WriteApplicationSettings(settings);
    }

    private static void WriteApplicationSettings(ApplicationSettings settings)
    {
        var path = ApplicationSettingsPath();
        var fullName = new DirectoryInfo(path).Parent!.FullName;
        Directory.CreateDirectory(fullName);
        var json = Serializer.PrettySerialize(settings);
        File.WriteAllText(path, json);
    }

    public static ApplicationSettings ReadApplicationSettings()
    {
        var path = ApplicationSettingsPath();
        if (!File.Exists(path))
        {
            return new ApplicationSettings();
        }

        var json = File.ReadAllText(path);
        return Serializer.Deserialize<ApplicationSettings>(json) ?? throw new InvalidOperationException($"Application setting file in {path} is incorrect. Try deleting it and retry.");
    }

    private static string ApplicationSettingsPath()
    {
        const string valiFolderName = "Vali";
        var applicationCommonDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), valiFolderName);
        const string applicationSettingsFilename = "application-settings.json";
        if (Directory.Exists(applicationCommonDataFolder))
        {
            return Path.Combine(applicationCommonDataFolder, applicationSettingsFilename);
        }

        return Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), valiFolderName), applicationSettingsFilename);
    }
}

public record ApplicationSettings
{
    public string? DownloadDirectory { get; set; }
    public string? LocalhostDownloadDirectory { get; set; }
    public int? Parallelism { get; set; }
}