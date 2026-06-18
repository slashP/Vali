using Shouldly;
using Xunit;

namespace Vali.Core.Tests;

public class LocationExternalDataTests
{
    private static Location NewLocation() => new()
    {
        Google = new GoogleData { PanoId = "", CountryCode = "NO" },
        Osm = new OsmData(),
        Nominatim = new NominatimData { CountryCode = "NO", SubdivisionCode = "NO-03" },
    };

    [Fact]
    public void ExternalData_is_not_allocated_until_accessed()
    {
        var location = NewLocation();
        location.HasExternalData.ShouldBeFalse();
    }

    [Fact]
    public void ExternalData_getter_lazily_creates_and_persists()
    {
        var location = NewLocation();
        location.ExternalData["k"] = "v";
        location.HasExternalData.ShouldBeTrue();
        location.ExternalData["k"].ShouldBe("v");
    }

    [Fact]
    public void ExternalNumber_returns_NaN_when_no_external_data()
    {
        var location = NewLocation();
        double.IsNaN(location.ExternalNumber("missing")).ShouldBeTrue();
        location.HasExternalData.ShouldBeFalse(); // reading a number must not allocate
    }
}
