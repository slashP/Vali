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

        if (distributionStrategyKey == DistributionStrategies.FixedCountByCoverageDensity)
        {
            if (definition.DistributionStrategy.CoverageDensityTuningFactor is < 0.01 or > 1)
            {
                ConsoleLogger.Error($"{nameof(MapDefinition.DistributionStrategy.CoverageDensityTuningFactor)} must be between 0.01 and 1. 0.05 is close to \"no tuning\", 0.5 favors areas with less coverage. 1 completely swaps large and small. Default is 0.6");
                return null;
            }

            if (definition.DistributionStrategy.CoverageDensityClusterSize is < 1 or > 6)
            {
                ConsoleLogger.Error($"{nameof(MapDefinition.DistributionStrategy.CoverageDensityClusterSize)} must be between 1 and 6. 1 is roughly 5000x5000 km, 2 is roughly 1250x625 km, 3 is roughly 156x156 km, 4 is roughly 39x20 km, 5 is roughly 5x5 km, 6 is roughly 1x1 km. Default is 3.");
                return null;
            }
        }

        return definition;
    }
}