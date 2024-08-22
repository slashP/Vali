using Vali.Core.Google;

namespace Vali.Core;

public class LocationReader
{
    public static LocationLakeMapGenerator.GeoMapLocation[] DeserializeLocationsFromFile(string path)
    {
        var extension = Path.GetExtension(path);
        if (extension == ".csv")
        {
            var lines = File.ReadAllLines(path);
            return lines.Where(x => x.Length > 2).Select(x =>
                    new LocationLakeMapGenerator.GeoMapLocation
                    {
                        lat = x.Split(',')[0].ParseAsDouble(),
                        lng = x.Split(',')[1].ParseAsDouble(),
                    })
                .ToArray();
        }

        var firstChar = Extensions.ReadChars(path, 1);
        LocationLakeMapGenerator.GeoMapLocation[] mapLocations;
        if (firstChar[0] == '[')
        {
            mapLocations = Extensions.DeserializeJsonFromFile<LocationLakeMapGenerator.GeoMapLocation[]>(path) ?? throw new InvalidOperationException("Invalid location json structure.");
        }
        else
        {
            var map = Extensions.DeserializeJsonFromFile<MapWithDistributionLocations>(path) ?? throw new InvalidOperationException("Invalid map json structure.");
            mapLocations = map.customCoordinates;
        }

        return mapLocations;
    }

    record MapWithDistributionLocations
    {
        public LocationLakeMapGenerator.GeoMapLocation[] customCoordinates { get; set; } = [];
    }
}