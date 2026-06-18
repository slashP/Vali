namespace Vali.Core.LeanDeserialization;

public static class LocationReadSetAnalyzer
{
    public static IReadOnlySet<ProtoField> Analyze(MapDefinition definition)
    {
        var sources = GatherExpressionSources(definition)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        var result = new HashSet<ProtoField>(LocationFieldRegistry.Baseline);
        foreach (var (propertyName, fields) in LocationFieldRegistry.PropertyToFields)
        {
            if (sources.Any(s => s!.Contains(propertyName, StringComparison.Ordinal)))
            {
                foreach (var field in fields)
                {
                    result.Add(field);
                }
            }
        }

        return result;
    }

    private static IEnumerable<string?> GatherExpressionSources(MapDefinition definition)
    {
        yield return definition.GlobalLocationFilter;

        foreach (var f in definition.CountryLocationFilters.Values) yield return f;
        foreach (var bySub in definition.SubdivisionLocationFilters.Values)
            foreach (var f in bySub.Values) yield return f;

        foreach (var n in definition.NeighborFilters) yield return n.Expression;

        foreach (var p in definition.GlobalLocationPreferenceFilters)
        {
            yield return p.Expression;
            foreach (var n in p.NeighborFilters) yield return n.Expression;
        }
        foreach (var arr in definition.CountryLocationPreferenceFilters.Values)
            foreach (var p in arr)
            {
                yield return p.Expression;
                foreach (var n in p.NeighborFilters) yield return n.Expression;
            }
        foreach (var bySub in definition.SubdivisionLocationPreferenceFilters.Values)
            foreach (var arr in bySub.Values)
                foreach (var p in arr)
                {
                    yield return p.Expression;
                    foreach (var n in p.NeighborFilters) yield return n.Expression;
                }

        yield return definition.Output.GlobalHeadingExpression;
        foreach (var h in definition.Output.CountryHeadingExpressions.Values) yield return h;
        foreach (var tag in definition.Output.LocationTags) yield return tag;
    }
}
