using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using System.Text.Json.Nodes;

namespace Vali.Core;

public class GeoJsonSerialization
{
    public static async Task<Geometry[]> DeserializeFromFile(string path)
    {
        var jsonText = await File.ReadAllTextAsync(path);
        var serializer = GeoJsonSerializer.Create();
        using var stringReader = new StringReader(jsonText);
        using var jsonReader = new JsonTextReader(stringReader);

        var type = JsonNode.Parse(jsonText)?["type"]?.GetValue<string>();
        var geometries = type switch
        {
            "Point" or "MultiPoint" or "LineString" or "MultiLineString" or "Polygon" or "MultiPolygon" or "GeometryCollection" => [serializer.Deserialize<Geometry>(jsonReader)!],
            "Feature" => [serializer.Deserialize<Feature>(jsonReader)!.Geometry],
            "FeatureCollection" => serializer.Deserialize<FeatureCollection>(jsonReader)!.Select(f => f.Geometry).ToArray(),
            _ => throw new ArgumentOutOfRangeException($"GeoJSON file {path} does not contain a 'type' or type '{type}' is invalid.")
        };
        return geometries;
    }
}