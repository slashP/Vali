using NetTopologySuite.Geometries;
using Shouldly;
using Xunit;

namespace Vali.Core.Tests;

public class ContinentBoundaryTests
{
    private static readonly GeometryFactory GeometryFactory = new();

    private static Point CreatePoint(double lng, double lat) =>
        GeometryFactory.CreatePoint(new Coordinate(lng, lat));

    private static bool IsInsideAny(Geometry[] geometries, double lng, double lat) =>
        geometries.Any(g => g.Contains(CreatePoint(lng, lat)));

    [Fact]
    public void EuropeanTurkey_ShouldLoadFromEmbeddedResource()
    {
        var geometries = ContinentBoundaries.EuropeanTurkey;
        geometries.ShouldNotBeNull();
        geometries.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void EuropeanRussia_ShouldLoadFromEmbeddedResource()
    {
        var geometries = ContinentBoundaries.EuropeanRussia;
        geometries.ShouldNotBeNull();
        geometries.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void EuropeanKazakhstan_ShouldLoadFromEmbeddedResource()
    {
        var geometries = ContinentBoundaries.EuropeanKazakhstan;
        geometries.ShouldNotBeNull();
        geometries.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void AfricanSpain_ShouldLoadFromEmbeddedResource()
    {
        var geometries = ContinentBoundaries.AfricanSpain;
        geometries.ShouldNotBeNull();
        geometries.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Hawaii_ShouldLoadFromEmbeddedResource()
    {
        var geometries = ContinentBoundaries.Hawaii;
        geometries.ShouldNotBeNull();
        geometries.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void EuropeanTurkey_IstanbulEuropeanSide_ShouldBeInside()
    {
        // Sultanahmet, Istanbul (European side)
        IsInsideAny(ContinentBoundaries.EuropeanTurkey, 28.977, 41.006).ShouldBeTrue();
    }

    [Fact]
    public void EuropeanTurkey_IstanbulAsianSide_ShouldBeOutside()
    {
        // Kadikoy, Istanbul (Asian side)
        IsInsideAny(ContinentBoundaries.EuropeanTurkey, 29.03, 40.99).ShouldBeFalse();
    }

    [Fact]
    public void EuropeanTurkey_Ankara_ShouldBeOutside()
    {
        // Ankara (Asian Turkey)
        IsInsideAny(ContinentBoundaries.EuropeanTurkey, 32.86, 39.93).ShouldBeFalse();
    }

    [Fact]
    public void EuropeanRussia_Moscow_ShouldBeInside()
    {
        IsInsideAny(ContinentBoundaries.EuropeanRussia, 37.62, 55.75).ShouldBeTrue();
    }

    [Fact]
    public void EuropeanRussia_Novosibirsk_ShouldBeOutside()
    {
        // Novosibirsk (Siberia)
        IsInsideAny(ContinentBoundaries.EuropeanRussia, 82.92, 55.01).ShouldBeFalse();
    }

    [Fact]
    public void EuropeanRussia_StPetersburg_ShouldBeInside()
    {
        IsInsideAny(ContinentBoundaries.EuropeanRussia, 30.32, 59.93).ShouldBeTrue();
    }

    [Fact]
    public void EuropeanKazakhstan_Atyrau_ShouldBeInside()
    {
        // Atyrau (west of Ural River, in European Kazakhstan)
        IsInsideAny(ContinentBoundaries.EuropeanKazakhstan, 51.88, 47.10).ShouldBeTrue();
    }

    [Fact]
    public void EuropeanKazakhstan_Astana_ShouldBeOutside()
    {
        // Astana (Asian Kazakhstan)
        IsInsideAny(ContinentBoundaries.EuropeanKazakhstan, 71.43, 51.13).ShouldBeFalse();
    }

    [Fact]
    public void AfricanSpain_LasPalmas_ShouldBeInside()
    {
        // Las Palmas, Canary Islands
        IsInsideAny(ContinentBoundaries.AfricanSpain, -15.42, 28.10).ShouldBeTrue();
    }

    [Fact]
    public void AfricanSpain_Ceuta_ShouldBeInside()
    {
        // Ceuta
        IsInsideAny(ContinentBoundaries.AfricanSpain, -5.32, 35.89).ShouldBeTrue();
    }

    [Fact]
    public void AfricanSpain_Melilla_ShouldBeInside()
    {
        // Melilla
        IsInsideAny(ContinentBoundaries.AfricanSpain, -2.95, 35.29).ShouldBeTrue();
    }

    [Fact]
    public void AfricanSpain_Madrid_ShouldBeOutside()
    {
        IsInsideAny(ContinentBoundaries.AfricanSpain, -3.70, 40.42).ShouldBeFalse();
    }

    [Fact]
    public void Hawaii_Honolulu_ShouldBeInside()
    {
        IsInsideAny(ContinentBoundaries.Hawaii, -157.86, 21.31).ShouldBeTrue();
    }

    [Fact]
    public void Hawaii_NewYork_ShouldBeOutside()
    {
        IsInsideAny(ContinentBoundaries.Hawaii, -74.01, 40.71).ShouldBeFalse();
    }

    [Fact]
    public void ApplyDefaults_Europe_ShouldProduceCountryGeometryFilters()
    {
        var definition = new MapDefinition { CountryCodes = ["europe"] };
        var result = definition.ApplyDefaults();

        result.CountryGeometryFilters.ShouldContainKey("TR");
        result.CountryGeometryFilters.ShouldContainKey("RU");
        result.CountryGeometryFilters.ShouldContainKey("KZ");
        result.CountryGeometryFilters.ShouldContainKey("ES");

        result.CountryGeometryFilters["TR"].ShouldContain(f => f.PreloadedGeometries != null && f.InclusionMode == "include");
        result.CountryGeometryFilters["RU"].ShouldContain(f => f.PreloadedGeometries != null && f.InclusionMode == "include");
        result.CountryGeometryFilters["KZ"].ShouldContain(f => f.PreloadedGeometries != null && f.InclusionMode == "include");
        result.CountryGeometryFilters["ES"].ShouldContain(f => f.PreloadedGeometries != null && f.InclusionMode == "exclude");
    }

    [Fact]
    public void ApplyDefaults_Asia_ShouldProduceExcludeGeometryFilters()
    {
        var definition = new MapDefinition { CountryCodes = ["asia"] };
        var result = definition.ApplyDefaults();

        result.CountryGeometryFilters["TR"].ShouldContain(f => f.PreloadedGeometries != null && f.InclusionMode == "exclude");
        result.CountryGeometryFilters["RU"].ShouldContain(f => f.PreloadedGeometries != null && f.InclusionMode == "exclude");
        result.CountryGeometryFilters["KZ"].ShouldContain(f => f.PreloadedGeometries != null && f.InclusionMode == "exclude");
    }

    [Fact]
    public void ApplyDefaults_Africa_ShouldIncludeAfricanSpain()
    {
        var definition = new MapDefinition { CountryCodes = ["africa"] };
        var result = definition.ApplyDefaults();

        result.CountryGeometryFilters["ES"].ShouldContain(f => f.PreloadedGeometries != null && f.InclusionMode == "include");
    }

    [Fact]
    public void ApplyDefaults_Oceania_ShouldIncludeHawaii()
    {
        var definition = new MapDefinition { CountryCodes = ["oceania"] };
        var result = definition.ApplyDefaults();

        result.CountryGeometryFilters["US"].ShouldContain(f => f.PreloadedGeometries != null && f.InclusionMode == "include");
    }

    [Fact]
    public void ApplyDefaults_NorthAmerica_ShouldExcludeHawaii()
    {
        var definition = new MapDefinition { CountryCodes = ["northamerica"] };
        var result = definition.ApplyDefaults();

        result.CountryGeometryFilters["US"].ShouldContain(f => f.PreloadedGeometries != null && f.InclusionMode == "exclude");
    }

    [Fact]
    public void ApplyDefaults_Europe_ShouldNotInjectSubdivisionInclusions()
    {
        var definition = new MapDefinition { CountryCodes = ["europe"] };
        var result = definition.ApplyDefaults();

        result.SubdivisionInclusions.ShouldBeEmpty();
        result.SubdivisionExclusions.ShouldBeEmpty();
    }

    [Fact]
    public void ApplyDefaults_Europe_ShouldNotInjectCountryLocationFilters()
    {
        var definition = new MapDefinition { CountryCodes = ["europe"] };
        var result = definition.ApplyDefaults();

        result.CountryLocationFilters.ShouldNotContainKey("TR");
        result.CountryLocationFilters.ShouldNotContainKey("RU");
        result.CountryLocationFilters.ShouldNotContainKey("KZ");
    }

    [Fact]
    public void ApplyDefaults_SingleCountry_ShouldNotInjectGeometryFilters()
    {
        var definition = new MapDefinition { CountryCodes = ["NO"] };
        var result = definition.ApplyDefaults();

        result.CountryGeometryFilters.ShouldBeEmpty();
    }
}
