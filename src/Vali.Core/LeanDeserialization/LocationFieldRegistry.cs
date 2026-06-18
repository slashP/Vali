namespace Vali.Core.LeanDeserialization;

/// <summary>
/// Maps expression/tag property names to the proto fields that back them, and defines the
/// always-on baseline (fields the distributor, StoreMap projection, and default filters always read).
/// Field numbers mirror the [ProtoMember] attributes in Location.cs.
/// </summary>
public static class LocationFieldRegistry
{
    private static ProtoField Osm(int n, string m) => new(typeof(OsmData), n, m);
    private static ProtoField Google(int n, string m) => new(typeof(GoogleData), n, m);
    private static ProtoField Nominatim(int n, string m) => new(typeof(NominatimData), n, m);
    private static ProtoField Loc(int n, string m) => new(typeof(Location), n, m);

    public static IReadOnlySet<ProtoField> Baseline { get; } = new HashSet<ProtoField>
    {
        // Location: all scalars + all three sub-messages are always decoded.
        Loc(1, nameof(Location.NodeId)), Loc(2, nameof(Location.Lat)), Loc(3, nameof(Location.Lng)),
        Loc(4, nameof(Location.Google)), Loc(5, nameof(Location.Osm)), Loc(6, nameof(Location.Nominatim)),
        // GoogleData: the StoreMap output projection + default filters (everything except CountryCode(5) and CheckedAt(6)).
        Google(1, nameof(GoogleData.PanoId)), Google(2, nameof(GoogleData.Lat)), Google(3, nameof(GoogleData.Lng)),
        Google(4, nameof(GoogleData.DefaultHeading)), Google(7, nameof(GoogleData.Year)), Google(8, nameof(GoogleData.Month)),
        Google(9, nameof(GoogleData.DrivingDirectionAngle)), Google(10, nameof(GoogleData.ArrowCount)),
        Google(11, nameof(GoogleData.Elevation)), Google(12, nameof(GoogleData.DescriptionLength)),
        Google(13, nameof(GoogleData.IsScout)), Google(14, nameof(GoogleData.ResolutionHeight)),
        // OsmData: the default Tunnels filter (almost always active) reads Tunnels10.
        Osm(10, nameof(OsmData.Tunnels10)),
        // NominatimData: output + default filters.
        Nominatim(1, nameof(NominatimData.CountryCode)), Nominatim(2, nameof(NominatimData.SubdivisionCode)),
    };

    public static IReadOnlyDictionary<string, ProtoField[]> PropertyToFields { get; } = new Dictionary<string, ProtoField[]>
    {
        // Osm
        ["Surface"] = [Osm(13, nameof(OsmData.Surface))],
        ["Buildings10"] = [Osm(1, nameof(OsmData.Buildings10))],
        ["Buildings25"] = [Osm(2, nameof(OsmData.Buildings25))],
        ["Buildings100"] = [Osm(3, nameof(OsmData.Buildings100))],
        ["Buildings200"] = [Osm(4, nameof(OsmData.Buildings200))],
        ["Roads0"] = [Osm(14, nameof(OsmData.Roads0))],
        ["Roads10"] = [Osm(5, nameof(OsmData.Roads10))],
        ["Roads25"] = [Osm(6, nameof(OsmData.Roads25))],
        ["Roads50"] = [Osm(7, nameof(OsmData.Roads50))],
        ["Roads100"] = [Osm(8, nameof(OsmData.Roads100))],
        ["Roads200"] = [Osm(9, nameof(OsmData.Roads200))],
        ["Tunnels10"] = [Osm(10, nameof(OsmData.Tunnels10))],
        ["Tunnels200"] = [Osm(11, nameof(OsmData.Tunnels200))],
        ["IsResidential"] = [Osm(12, nameof(OsmData.IsResidential))],
        ["ClosestCoast"] = [Osm(15, nameof(OsmData.ClosestCoast))],
        ["ClosestLake"] = [Osm(17, nameof(OsmData.ClosestLake))],
        ["ClosestRiver"] = [Osm(18, nameof(OsmData.ClosestRiver))],
        ["ClosestRailway"] = [Osm(19, nameof(OsmData.ClosestRailway))],
        ["HighwayType"] = [Osm(16, nameof(OsmData.RoadType))],
        ["HighwayTypeCount"] = [Osm(16, nameof(OsmData.RoadType))],
        ["WayId"] = [Osm(20, nameof(OsmData.WayIds))],
        // Google
        ["Month"] = [Google(8, nameof(GoogleData.Month))],
        ["Year"] = [Google(7, nameof(GoogleData.Year))],
        ["Lat"] = [Google(2, nameof(GoogleData.Lat))],
        ["Lng"] = [Google(3, nameof(GoogleData.Lng))],
        ["Heading"] = [Google(4, nameof(GoogleData.DefaultHeading))],
        ["DrivingDirectionAngle"] = [Google(9, nameof(GoogleData.DrivingDirectionAngle))],
        ["ArrowCount"] = [Google(10, nameof(GoogleData.ArrowCount))],
        ["Elevation"] = [Google(11, nameof(GoogleData.Elevation))],
        ["DescriptionLength"] = [Google(12, nameof(GoogleData.DescriptionLength))],
        ["IsScout"] = [Google(13, nameof(GoogleData.IsScout))],
        ["ResolutionHeight"] = [Google(14, nameof(GoogleData.ResolutionHeight))],
        // Nominatim
        ["CountryCode"] = [Nominatim(1, nameof(NominatimData.CountryCode))],
        ["SubdivisionCode"] = [Nominatim(2, nameof(NominatimData.SubdivisionCode))],
        ["County"] = [Nominatim(3, nameof(NominatimData.County))],
    };
}
