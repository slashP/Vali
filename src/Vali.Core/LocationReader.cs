namespace Vali.Core;

public class LocationReader
{
    public static async Task<IList<LocationLakeMapGenerator.GeoMapLocation>> DeserializeLocationsFromFile(string path)
    {
        var firstChar = Extensions.ReadChars(path, 1);
        LocationLakeMapGenerator.GeoMapLocation[] mapLocations;
        if (firstChar[0] == '[')
        {
            mapLocations = await Extensions.DeserializeJsonFromFile<LocationLakeMapGenerator.GeoMapLocation[]>(path) ?? throw new InvalidOperationException("Invalid location json structure.");
        }
        else
        {
            var map = await Extensions.DeserializeJsonFromFile<MapWithDistributionLocations>(path) ?? throw new InvalidOperationException("Invalid map json structure.");
            mapLocations = map.customCoordinates;
        }

        return mapLocations;
    }

    record MapWithDistributionLocations
    {
        public LocationLakeMapGenerator.GeoMapLocation[] customCoordinates { get; set; } = [];
    }
}