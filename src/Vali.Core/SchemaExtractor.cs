using System.Reflection;

namespace Vali.Core;

public static class SchemaExtractor
{
    public static string? GetEmbeddedSchema(string schemaFileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Vali.Schemas.{schemaFileName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static bool TryExtractSchemaToFile(string schemaFileName, string outputPath)
    {
        var schema = GetEmbeddedSchema(schemaFileName);
        if (schema == null)
        {
            return false;
        }

        File.WriteAllText(outputPath, schema);
        return true;
    }
}
