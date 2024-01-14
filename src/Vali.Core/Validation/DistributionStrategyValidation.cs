namespace Vali.Core.Validation;

public static class DistributionStrategyValidation
{
    public static MapDefinition? ValidateDistributionStrategy(this MapDefinition definition)
    {
        var validStrategyKeys = DistributionStrategies.ValidStrategyKeys;
        var distributionStrategyKey = definition.DistributionStrategy.Key;
        if (!validStrategyKeys.Contains(distributionStrategyKey))
        {
            ConsoleLogger.Error($"{nameof(MapDefinition.DistributionStrategy)}.{nameof(MapDefinition.DistributionStrategy.Key)} must be one of {validStrategyKeys.Merge(", ")}.");
            return null;
        }

        if (definition.DistributionStrategy.LocationCountGoal <= 0 && distributionStrategyKey == DistributionStrategies.FixedCountByMaxMinDistance)
        {
            ConsoleLogger.Error($"{nameof(MapDefinition.DistributionStrategy.LocationCountGoal)} must be greater than zero.");
            return null;
        }

        const int maxLocationCount = 1_000_000;
        if (definition.DistributionStrategy.LocationCountGoal > maxLocationCount)
        {
            ConsoleLogger.Error($"{nameof(MapDefinition.DistributionStrategy.LocationCountGoal)} cannot be greater than {maxLocationCount:N0}.");
            return null;
        }

        var validMinDistances = LocationDistributor.Distances;
        if (distributionStrategyKey == DistributionStrategies.FixedCountByMaxMinDistance)
        {
            if (!validMinDistances.Contains(definition.DistributionStrategy.MinMinDistance))
            {
                ConsoleLogger.Error($"{nameof(MapDefinition.DistributionStrategy.MinMinDistance)} must be one of {validMinDistances.Select(x => x.ToString()).Merge(", ")}");
                return null;
            }
        }

        if (distributionStrategyKey == DistributionStrategies.MaxCountByFixedMinDistance || distributionStrategyKey == DistributionStrategies.EvenlyByDistanceWithinCountry)
        {
            if (!validMinDistances.Contains(definition.DistributionStrategy.FixedMinDistance))
            {
                ConsoleLogger.Error($"{nameof(MapDefinition.DistributionStrategy.FixedMinDistance)} must be one of {validMinDistances.Select(x => x.ToString()).Merge(", ")}");
                return null;
            }
        }

        return definition;
    }
}