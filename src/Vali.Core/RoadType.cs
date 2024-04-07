using ProtoBuf;

namespace Vali.Core;

[ProtoContract]
[Flags]
public enum RoadType
{
    Unknown = 0,
    Motorway = 1,
    Trunk = 2,
    Primary = 4,
    Secondary = 8,
    Tertiary = 16,
    Motorway_link = 32,
    Trunk_link = 64,
    Primary_link = 128,
    Secondary_link = 256,
    Tertiary_link = 512,
    Unclassified = 1024,
    Residential = 2048,
    Living_street = 4096,
    Service = 8192,
    Track = 16384,
    Road = 32768,
}
