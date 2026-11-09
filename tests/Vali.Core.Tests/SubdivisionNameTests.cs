using Shouldly;
using Xunit;

namespace Vali.Core.Tests;

public class SubdivisionNameTests
{
    [Fact]
    public void ReturnsNull_ForCountryInNeitherDictionary()
    {
        // TZ has no official coverage, so it is absent from both CountryToSubdivision and
        // NotQuiteThereYetCountryToSubdivision. Callers treat the return as "name unknown"
        // (`subdivisionName ?? "N/A"`), so an unlisted country must be null, not an exception.
        SubdivisionWeights.CountryToSubdivision.ContainsKey("TZ").ShouldBeFalse();
        SubdivisionWeights.NotQuiteThereYetCountryToSubdivision.ContainsKey("TZ").ShouldBeFalse();

        SubdivisionWeights.SubdivisionName("TZ", "TZ-02").ShouldBeNull();
    }

    [Fact]
    public void ReturnsNull_ForUnknownCodeInKnownCountry()
    {
        var cc = SubdivisionWeights.CountryToSubdivision.First().Key;

        SubdivisionWeights.SubdivisionName(cc, "ZZ-999").ShouldBeNull();
    }

    [Fact]
    public void ReturnsName_ForKnownCountryAndCode()
    {
        var (cc, subdivisions) = SubdivisionWeights.CountryToSubdivision.First();
        var (code, info) = subdivisions.First();

        SubdivisionWeights.SubdivisionName(cc, code).ShouldBe(info.subdivisionName);
    }

    [Fact]
    public void ReturnsName_ForNotQuiteThereYetCountry()
    {
        var (cc, subdivisions) = SubdivisionWeights.NotQuiteThereYetCountryToSubdivision.First();
        var (code, info) = subdivisions.First();

        SubdivisionWeights.SubdivisionName(cc, code).ShouldBe(info.subdivisionName);
    }
}
