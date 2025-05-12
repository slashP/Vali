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
            static void DryRun(string expression)
            {
                var selector = LocationLakeFilterer.CompileIntLocationExpression(expression);
                var locations = FilterValidation.EmptyLocationArray().Select(selector).ToArray();
            }

            var validatedDefinition = definition.ValidateExpression(headingExpression!, DryRun, $"Invalid heading expression {headingExpression}", LocationLakeFilterer.ValidProperties());
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

        return definition;
    }
}