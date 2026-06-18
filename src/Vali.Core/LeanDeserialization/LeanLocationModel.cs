using ProtoBuf.Meta;

namespace Vali.Core.LeanDeserialization;

public sealed record ProtoField(Type DeclaringType, int FieldNumber, string MemberName);

public static class LeanLocationModel
{
    public static TypeModel Build(IReadOnlyCollection<ProtoField> fields)
    {
        var model = RuntimeTypeModel.Create();
        foreach (var group in fields.GroupBy(f => f.DeclaringType))
        {
            var meta = model.Add(group.Key, applyDefaultBehaviour: false);
            foreach (var field in group)
            {
                meta.Add(field.FieldNumber, field.MemberName);
            }
        }

        model.CompileInPlace();
        return model;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, TypeModel> Cache = new();

    public static TypeModel? For(MapDefinition definition)
    {
        if (Environment.GetEnvironmentVariable("VALI_LEAN_DESERIALIZE") == "0")
        {
            return null;
        }

        var readSet = LocationReadSetAnalyzer.Analyze(definition);
        var signature = string.Join(
            ",",
            readSet.OrderBy(f => f.DeclaringType.Name, StringComparer.Ordinal)
                   .ThenBy(f => f.FieldNumber)
                   .Select(f => $"{f.DeclaringType.Name}.{f.FieldNumber}"));
        return Cache.GetOrAdd(signature, _ => Build(readSet));
    }
}
