using Vali.Core;
using Vali.Core.Data;

namespace Vali.Host;

/// <summary>
/// Decides Vali's download folder for Studio without overriding the user's CLI
/// choice. The engine's <see cref="DataDownloadService.DownloadFolder"/> precedence is
/// env(VALI_DOWNLOAD_FOLDER) → applicationSettings.DownloadDirectory → default
/// (CommonApplicationData/Vali). Studio must never set VALI_DOWNLOAD_FOLDER
/// blindly — that shadows a <c>vali set-download-folder</c> choice and silently
/// splits Studio's data from the CLI's. Instead we set it only as a last resort:
/// when the user has configured nothing and the default isn't writable
/// (macOS/Linux, where it resolves under /usr/share), pointing at the OS-native
/// per-user dir the shell passed in VALI_STUDIO_DATA_DIR.
/// </summary>
public static class DownloadFolderResolver
{
    public static void EnsureUsableDownloadFolder()
    {
        try
        {
            // An explicit env var (set by the user, not the shell) already wins.
            if (!string.IsNullOrWhiteSpace(ApplicationSettingsService.ReadDownloadFolderFromEnvironmentVariable()))
            {
                return;
            }

            // A folder configured via `vali set-download-folder` is the user's
            // choice and keeps Studio + CLI on one data set — leave it alone.
            var settings = ApplicationSettingsService.ReadApplicationSettings();
            if (!string.IsNullOrWhiteSpace(settings.DownloadDirectory))
            {
                return;
            }

            // Nothing configured: the engine default is fine when writable (always
            // on Windows: C:\ProgramData\Vali). When it isn't (macOS/Linux:
            // /usr/share), fall back to the shell-provided per-user dir so the
            // engine never blocks on its interactive "pick a folder" prompt.
            var defaultFolder = DataDownloadService.DownloadFolder(RunMode.Default);
            if (IsWritable(defaultFolder))
            {
                return;
            }

            var hint = Environment.GetEnvironmentVariable("VALI_STUDIO_DATA_DIR");
            var fallbackBase = string.IsNullOrWhiteSpace(hint)
                ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                : hint;
            if (string.IsNullOrWhiteSpace(fallbackBase))
            {
                return;
            }

            // DownloadFolder() appends "Vali" to whatever base it resolves, so set
            // the *base* here (matches the shell's historical env value).
            Environment.SetEnvironmentVariable(
                ApplicationSettingsService.DownloadFolderEnvironmentVariableName, fallbackBase);
        }
        catch
        {
            // A corrupt settings file (or any other read failure) must not take
            // the host down at startup — leave the env untouched and let the
            // engine's own default resolution and error reporting handle it.
        }
    }

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
}
