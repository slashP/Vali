using System.Numerics;
using ProtoBuf;

namespace Vali.Core;

[ProtoContract]
public record Location : IDistributionLocation<long>
{
    [ProtoMember(1)]
    public long NodeId { get; set; }
    [ProtoMember(2)]
    public double Lat { get; set; }
    [ProtoMember(3)]
    public double Lng { get; set; }
    [ProtoMember(4)]
    public required GoogleData Google { get; set; }
    [ProtoMember(5)]
    public required OsmData Osm { get; set; }
    [ProtoMember(6)]
    public required NominatimData Nominatim { get; set; }
    public long LocationId => NodeId;
    public string? Tag { get; init; }
}

public interface IDistributionLocation<T> : ILatLng
{
    public T LocationId { get; }
}

[ProtoContract]
public record GoogleData
{
    [ProtoMember(1)]
    public required string PanoId { get; set; }
    [ProtoMember(2)]
    public double Lat { get; set; }
    [ProtoMember(3)]
    public double Lng { get; set; }
    [ProtoMember(4)]
    public double DefaultHeading { get; set; }
    public int Heading => DefaultHeading.RoundToInt();
    [ProtoMember(5)]
    public required string CountryCode { get; set; }
    [ProtoMember(6)]
    public DateTime CheckedAt { get; set; }
    [ProtoMember(7)]
    public int Year { get; set; }
    [ProtoMember(8)]
    public int Month { get; set; }
    [ProtoMember(9)]
    public int DrivingDirectionAngle { get; set; }
    [ProtoMember(10)]
    public int ArrowCount { get; set; }
    [ProtoMember(11)]
    public int? Elevation { get; set; }
    [ProtoMember(12)]
    public int? DescriptionLength { get; set; }
    [ProtoMember(13)]
    public bool IsScout { get; set; }
    [ProtoMember(14)]
    public int ResolutionHeight { get; set; }
}

[ProtoContract]
public record OsmData
{
    [ProtoMember(1)]
    public int Buildings10 { get; set; }
    [ProtoMember(2)]
    public int Buildings25 { get; set; }
    [ProtoMember(3)]
    public int Buildings100 { get; set; }
    [ProtoMember(4)]
    public int Buildings200 { get; set; }
    [ProtoMember(5)]
    public int Roads10 { get; set; }
    [ProtoMember(6)]
    public int Roads25 { get; set; }
    [ProtoMember(7)]
    public int Roads50 { get; set; }
    [ProtoMember(8)]
    public int Roads100 { get; set; }
    [ProtoMember(9)]
    public int Roads200 { get; set; }
    [ProtoMember(10)]
    public int Tunnels10 { get; set; }
    [ProtoMember(11)]
    public int Tunnels200 { get; set; }
    [ProtoMember(12)]
    public bool IsResidential { get; set; }
    [ProtoMember(13)]
    public string? Surface { get; set; }
    [ProtoMember(14)]
    public int Roads0 { get; set; }
    [ProtoMember(15)]
    public int? ClosestCoast { get; set; }
    [ProtoMember(16)]
    public RoadType RoadType { get; set; }
    public HighwayType HighwayType => new HighwayType(RoadType);
    public int HighwayTypeCount => BitOperations.PopCount((uint)RoadType);
    [ProtoMember(17)]
    public int? ClosestLake { get; set; }
    [ProtoMember(18)]
    public int? ClosestRiver { get; set; }
    [ProtoMember(19)]
    public int? ClosestRailway { get; set; }
    [ProtoMember(20)]
    public long[] WayIds { get; set; } = [];
    public string WayId => string.Join("|", WayIds.Select(w => w.ToString()));
}

[ProtoContract]
public record NominatimData
{
    [ProtoMember(1)]
    public required string CountryCode { get; set; }
    [ProtoMember(2)]
    public required string SubdivisionCode { get; set; }
    [ProtoMember(3)]
    public string? County { get; set; }
}

public interface ILatLng
{
    double Lat { get; }
    double Lng { get; }
}
