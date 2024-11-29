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

    public static string? ReadDownloadFolderFromEnvironmentVariable() =>
        Environment.GetEnvironmentVariable(DownloadFolderEnvironmentVariableName);

    public static void UnsetDownloadFolder()
    {
        var settings = ReadApplicationSettings();
        settings.DownloadDirectory = null;
        WriteApplicationSettings(settings);
    }

    private static void WriteApplicationSettings(ApplicationSettings settings)
    {
        var path = ApplicationSettingsPath();
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

    private static string ApplicationSettingsPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Vali", "application-settings.json");
}

public record ApplicationSettings
{
    public string? DownloadDirectory { get; set; }
    public string? LocalhostDownloadDirectory { get; set; }
}