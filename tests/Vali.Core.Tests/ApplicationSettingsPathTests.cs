using Shouldly;
using Xunit;

namespace Vali.Core.Tests;

[Collection("StaticState")]
public class ApplicationSettingsPathTests
{
    [Fact]
    public void ReadsSettingsFromEnvironmentPath()
    {
        var file = Path.GetTempFileName();
        var previous = Environment.GetEnvironmentVariable(ApplicationSettingsService.SettingsPathEnvironmentVariable);
        try
        {
            File.WriteAllText(file, """{"localhostDownloadDirectory":"/x/lake"}""");
            Environment.SetEnvironmentVariable(ApplicationSettingsService.SettingsPathEnvironmentVariable, file);

            ApplicationSettingsService.ReadApplicationSettings().LocalhostDownloadDirectory.ShouldBe("/x/lake");
        }
        finally
        {
            Environment.SetEnvironmentVariable(ApplicationSettingsService.SettingsPathEnvironmentVariable, previous);
            File.Delete(file);
        }
    }

    [Fact]
    public void MissingEnvironmentFileGivesDefaults()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"vali-settings-missing-{Guid.NewGuid():N}.json");
        var previous = Environment.GetEnvironmentVariable(ApplicationSettingsService.SettingsPathEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(ApplicationSettingsService.SettingsPathEnvironmentVariable, missing);

            ApplicationSettingsService.ReadApplicationSettings().ShouldBe(new ApplicationSettings());
        }
        finally
        {
            Environment.SetEnvironmentVariable(ApplicationSettingsService.SettingsPathEnvironmentVariable, previous);
        }
    }
}
