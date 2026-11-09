using Shouldly;
using Vali.Core.Validation;
using Xunit;

namespace Vali.Core.Tests;

public class ExternalDataFileValidationTests : IDisposable
{
    private readonly string _existing = Path.GetTempFileName();
    private readonly string _missing = Path.Combine(Path.GetTempPath(), $"vali-missing-{Guid.NewGuid():N}.json");

    public void Dispose() => File.Delete(_existing);

    [Fact]
    public void No_external_files_is_accepted() =>
        new MapDefinition().ValidateExternalDataFiles().ShouldNotBeNull();

    [Fact]
    public void Existing_files_are_accepted() =>
        new MapDefinition
        {
            GlobalExternalDataFiles = [_existing],
            CountryExternalDataFiles = new() { ["NO"] = [_existing] },
            SubdivisionExternalDataFiles = new() { ["NO"] = new() { ["NO-03"] = [_existing] } }
        }.ValidateExternalDataFiles().ShouldNotBeNull();

    [Fact]
    public void Missing_global_file_is_rejected() =>
        new MapDefinition { GlobalExternalDataFiles = [_existing, _missing] }.ValidateExternalDataFiles().ShouldBeNull();

    [Fact]
    public void Missing_country_file_is_rejected() =>
        new MapDefinition { CountryExternalDataFiles = new() { ["NO"] = [_existing], ["SE"] = [_missing] } }
            .ValidateExternalDataFiles().ShouldBeNull();

    [Fact]
    public void Missing_subdivision_file_is_rejected() =>
        new MapDefinition { SubdivisionExternalDataFiles = new() { ["NO"] = new() { ["NO-03"] = [_missing] } } }
            .ValidateExternalDataFiles().ShouldBeNull();

    [Fact]
    public void Validate_runs_the_external_file_check()
    {
        MapDefinition WithFile(string path) =>
            new MapDefinition
            {
                CountryCodes = ["NO"],
                DistributionStrategy = new() { Key = DistributionStrategies.FixedCountByMaxMinDistance, LocationCountGoal = 100, MinMinDistance = 500 },
                CountryExternalDataFiles = new() { ["NO"] = [path] }
            }.ApplyDefaults();

        WithFile(_existing).Validate().ShouldNotBeNull();
        WithFile(_missing).Validate().ShouldBeNull();
    }
}
