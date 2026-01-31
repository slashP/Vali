using Shouldly;
using Xunit;

namespace Vali.Core.Tests;

public class PointsAreCloserThanTests
{
    /// <summary>
    /// Reference implementation using ApproximateDistance with Math.Sqrt.
    /// Used to verify the optimized PointsAreCloserThan produces identical results.
    /// </summary>
    private static bool OriginalPointsAreCloserThan(double lat1, double lon1, double lat2, double lon2, int meters)
    {
        if (Math.Abs(lat1 - lat2) > meters / 110000.0)
            return false;
        return Extensions.ApproximateDistance(lat1, lon1, lat2, lon2) < meters;
    }

    [Theory]
    // Close points, various thresholds
    [InlineData(59.9, 10.7, 59.901, 10.701, 500)]
    [InlineData(59.9, 10.7, 59.901, 10.701, 100)]
    [InlineData(59.9, 10.7, 59.905, 10.705, 1000)]
    // Far apart points
    [InlineData(59.9, 10.7, 60.5, 11.2, 5000)]
    [InlineData(59.9, 10.7, 60.0, 10.8, 20000)]
    // Equator
    [InlineData(0.0, 0.0, 0.001, 0.001, 200)]
    [InlineData(0.0, 0.0, 0.01, 0.01, 2000)]
    // High latitude (Norway)
    [InlineData(70.0, 25.0, 70.001, 25.002, 200)]
    [InlineData(70.0, 25.0, 70.005, 25.010, 1000)]
    // Southern hemisphere
    [InlineData(-33.8, 151.2, -33.801, 151.201, 200)]
    // Large distance
    [InlineData(50.0, 10.0, 50.1, 10.1, 75000)]
    // Very close
    [InlineData(59.9, 10.7, 59.9001, 10.7001, 25)]
    // Same point
    [InlineData(59.9, 10.7, 59.9, 10.7, 100)]
    // Boundary: distance ~= meters
    [InlineData(59.9, 10.7, 59.9045, 10.7, 500)]
    public void PointsAreCloserThan_MatchesOriginalImplementation(
        double lat1, double lon1, double lat2, double lon2, int meters)
    {
        var expected = OriginalPointsAreCloserThan(lat1, lon1, lat2, lon2, meters);
        var actual = Extensions.PointsAreCloserThan(lat1, lon1, lat2, lon2, meters * meters);
        actual.ShouldBe(expected);
    }

    [Fact]
    public void PointsAreCloserThan_SamePoint_ReturnsTrue()
    {
        Extensions.PointsAreCloserThan(59.9, 10.7, 59.9, 10.7, 100 * 100).ShouldBeTrue();
    }

    [Fact]
    public void PointsAreCloserThan_FarApart_ReturnsFalse()
    {
        Extensions.PointsAreCloserThan(59.9, 10.7, 61.0, 12.0, 1000 * 1000).ShouldBeFalse();
    }

    [Fact]
    public void PointsAreCloserThan_ClosePoints_ReturnsTrue()
    {
        // ~157m apart at 60° latitude
        Extensions.PointsAreCloserThan(60.0, 10.0, 60.001, 10.001, 500 * 500).ShouldBeTrue();
    }

    [Fact]
    public void PointsAreCloserThan_LatitudeEarlyExit_ReturnsFalse()
    {
        // dlat = 0.01 degrees = ~1111m, meters = 500 -> early exit
        Extensions.PointsAreCloserThan(60.0, 10.0, 60.01, 10.0, 500 * 500).ShouldBeFalse();
    }
}
