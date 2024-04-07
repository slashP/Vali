namespace Vali.Core;

public readonly struct HighwayType(RoadType roadType)
{
    public bool Equals(HighwayType other)
    {
        return _roadType == other._roadType;
    }

    public override bool Equals(object? obj)
    {
        return obj is HighwayType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)_roadType;
    }

    private readonly RoadType _roadType = roadType;

    public static bool operator ==(HighwayType a, string b)
        => a._roadType.HasFlag(Enum.TryParse<RoadType>(b, true, out var val) ? val : RoadType.Unknown);

    public static bool operator !=(HighwayType a, string b) => !(a == b);
}
