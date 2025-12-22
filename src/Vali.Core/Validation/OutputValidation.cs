using Vali.Core.Data;
using Vali.Core.Google;

namespace Vali.Core.Validation;

public static class OutputValidation
{
    public static MapDefinition? ValidateOutput(this MapDefinition definition)
    {
        foreach (var headingExpression in definition.Output.CountryHeadingExpressions.Select(x => x.Value)
                     .Concat(new[] { definition.Output.GlobalHeadingExpression })
                     .Where(x => !string.IsNullOrEmpty(x)))
        {
            static void DryRun(string expression, MapDefinition definition)
            {
                var selector = LocationLakeFilterer.CompileIntLocationExpression(expression);
                var locations = FilterValidation.EmptyLocationArray().Select(selector).ToArray();
            }

            var validatedDefinition = definition.ValidateExpression(
                headingExpression!,
                DryRun,
                $"Invalid heading expression {headingExpression}",
                LocationLakeFilterer.ValidProperties(),
                LocationLakeFilterer.ValidProperties());
            if (validatedDefinition == null)
            {
                return null;
            }
        }

        if (!string.IsNullOrEmpty(definition.Output.PanoVerificationStrategy))
        {
            if (!Enum.TryParse<GoogleApi.PanoStrategy>(definition.Output.PanoVerificationStrategy, out var strategy))
            {
                ConsoleLogger.Error($"{nameof(definition.Output.PanoVerificationStrategy)} must be empty or one of {Enum.GetValues<GoogleApi.PanoStrategy>().Select(x => x.ToString()).Merge(", ")}");
                return null;
            }
        }

        foreach (var countryPanning in definition.Output.CountryPanoVerificationPanning)
        {
            if (string.IsNullOrEmpty(countryPanning.Key) || !CountryCodes.Countries.ContainsKey(countryPanning.Key))
            {
                ConsoleLogger.Error($"{nameof(definition.Output.CountryPanoVerificationPanning)} keys must be valid country codes. |{countryPanning.Key}|");
                return null;
            }

            if (countryPanning.Value?.DefaultPanning is not ("awayfromdrivingdirection" or "indrivingdirection"))
            {
                ConsoleLogger.Error($"{nameof(countryPanning.Value.DefaultPanning)} must be either indrivingdirection or awayfromdrivingdirection.");
                return null;
            }

            static void DryRunMapCheckrExpression(string filter, MapDefinition definition)
            {
                var expression = LocationLakeFilterer.CompileExpression<MapCheckrLocation, bool>(filter, true);
                var locations = new[] { ExampleMapCheckrLocation() }.Where(expression).ToArray();
            }

            foreach (var panningExpression in countryPanning.Value?.PanningExpressions ?? [])
            {
                if (string.IsNullOrEmpty(panningExpression.Expression))
                {
                    ConsoleLogger.Error($"panningExpression.{nameof(panningExpression.Expression)} cannot be empty.");
                    return null;
                }

                var validProperties = new[]
                {
                    nameof(MapCheckrLocation.countryCode),
                    nameof(MapCheckrLocation.lat),
                    nameof(MapCheckrLocation.lng),
                    nameof(MapCheckrLocation.year),
                    nameof(MapCheckrLocation.month),
                    nameof(MapCheckrLocation.resolutionHeight),
                };
                var validatedDefinition = definition.ValidateExpression(
                    panningExpression.Expression,
                    DryRunMapCheckrExpression,
                    $"Invalid panningExpression expression {panningExpression.Expression}",
                    validProperties,
                    validProperties);
                if (validatedDefinition == null)
                {
                    return null;
                }

                if (panningExpression.Panning is not ("awayfromdrivingdirection" or "indrivingdirection"))
                {
                    ConsoleLogger.Error($"{nameof(panningExpression.Panning)} must be either indrivingdirection or awayfromdrivingdirection.");
                    return null;
                }
            }
        }

        return definition;
    }

    private static MapCheckrLocation ExampleMapCheckrLocation() =>
        new()
        {
            lat = 20,
            lng = 40,
            year = 2023,
            month = 7,
            resolutionHeight = 8192,
            countryCode = "US"
        };
}