using NetTopologySuite.Geometries;

namespace Vali.Core;

public static class ContinentBoundaries
{
    private static readonly Lazy<Geometry[]> LazyEuropeanTurkey = new(() => LoadRegion("european-turkey.geojson"));
    private static readonly Lazy<Geometry[]> LazyEuropeanRussia = new(() => LoadRegion("european-russia.geojson"));
    private static readonly Lazy<Geometry[]> LazyEuropeanKazakhstan = new(() => LoadRegion("european-kazakhstan.geojson"));
    private static readonly Lazy<Geometry[]> LazyAfricanSpain = new(() => LoadRegion("african-spain.geojson"));
    private static readonly Lazy<Geometry[]> LazyHawaii = new(() => LoadRegion("hawaii.geojson"));

    public static Geometry[] EuropeanTurkey => LazyEuropeanTurkey.Value;
    public static Geometry[] EuropeanRussia => LazyEuropeanRussia.Value;
    public static Geometry[] EuropeanKazakhstan => LazyEuropeanKazakhstan.Value;
    public static Geometry[] AfricanSpain => LazyAfricanSpain.Value;
    public static Geometry[] Hawaii => LazyHawaii.Value;

    private static Geometry[] LoadRegion(string fileName)
    {
        var geoJson = Extensions.ReadManifestData(fileName);
        return GeoJsonSerialization.DeserializeFromString(geoJson, fileName);
    }
}
