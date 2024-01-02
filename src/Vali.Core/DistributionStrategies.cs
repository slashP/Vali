﻿using Geohash;
using Spectre.Console;
using Vali.Core.Hash;
using Loc = Vali.Core.Location;

namespace Vali.Core;

public static class DistributionStrategies
{
    public const string FixedCountByMaxMinDistance = "FixedCountByMaxMinDistance";
    public const string MaxCountByFixedMinDistance = "MaxCountByFixedMinDistance";

    public static (IList<Loc> locations, int regionGoalCount, int minDistance) SubdivisionByMaxMinDistance(
        string countryCode,
        string file,
        int goalCount,
        string[] availableSubdivisions,
        MapDefinition mapDefinition)
    {
        var deserializeFromFile = Extensions.ProtoDeserializeFromFile<Loc[]>(file);
        var subdivision = deserializeFromFile.FirstOrDefault()?.Nominatim.SubdivisionCode ?? "";
        if (!availableSubdivisions.Contains(subdivision))
        {
            return (Array.Empty<Loc>(), 0, 0);
        }

        var filteredLocations = FilteredLocations(countryCode, mapDefinition, subdivision, deserializeFromFile);
        var regionGoalCount = mapDefinition.SubdivisionDistribution.TryGetValue(countryCode, out var subdivisionWeights) ?
            ((decimal)(subdivisionWeights.GetValueOrDefault(subdivision, 0)) / subdivisionWeights.Sum(x => x.Value) * goalCount).RoundToInt() :
            SubdivisionWeights.GoalForSubdivision(countryCode, subdivision, goalCount, availableSubdivisions);
        if (regionGoalCount == 0)
        {
            return (Array.Empty<Loc>(), 0, 0);
        }

        var regionBasisCount = Math.Min(filteredLocations.Count, 30_000);
        var chunkSize = 10_000;
        var chunks = (decimal)filteredLocations.Count / chunkSize;
        if (chunks == 0)
        {
            return (Array.Empty<Loc>(), 0, 0);
        }

        var perChunkRegionGoalCount = (regionBasisCount / chunks).RoundToInt();
        var minDistance = mapDefinition.DistributionStrategy.MinMinDistance;
        filteredLocations = filteredLocations.Count < regionBasisCount
            ? filteredLocations
            : filteredLocations
                .OrderBy(x => x.Lat * x.Lng)
                .Chunk(chunkSize)
                .AsParallel()
                .SelectMany(x => LocationDistributor.GetSome(x.ToArray(), perChunkRegionGoalCount, minDistance / 2))
                .ToArray();

        if (!filteredLocations.Any())
        {
            AnsiConsole.MarkupLine($"[lightseagreen]{0,6:N0} locations in {subdivision,7}. [/][olive]{regionGoalCount,4} locations short.[/]");
            return (Array.Empty<Loc>(), 0, 0);
        }

        var tuple = ByMaxMinDistance(filteredLocations, regionGoalCount, minDistance, mapDefinition, countryCode, subdivision);
        var diff = regionGoalCount - tuple.locations.Count;
        var notEnoughLocationsMessage = diff > 0 ? $"[olive]{diff,4} locations short.[/]" : "";
        AnsiConsole.MarkupLine($"[lightseagreen]{tuple.locations.Count,6:N0} locations in {subdivision,7}. At least {tuple.minDistance,7:N0}m. between each location.[/]{notEnoughLocationsMessage}");
        return (tuple.locations, regionGoalCount, tuple.minDistance);
    }

    private static IList<Location> FilteredLocations(
        string countryCode,
        MapDefinition mapDefinition,
        string subdivision,
        IEnumerable<Location> deserializeFromFile)
    {
        var locationFilters = LocationFilter(countryCode, mapDefinition, subdivision);
        var filteredLocations = LocationLakeFilterer.Filter(deserializeFromFile, locationFilters);
        return filteredLocations;
    }

    public static (IList<Loc> locations, int regionGoalCount, int minDistance)[] CountryByMaxMinDistance(
        string countryCode,
        string[] files,
        int goalCount,
        MapDefinition mapDefinition)
    {
        var locations = new List<Loc>();
        foreach (var file in files)
        {
            var fromFile = Extensions.ProtoDeserializeFromFile<Loc[]>(file);
            locations.AddRange(FilteredLocations(countryCode, mapDefinition, "", fromFile));
        }

        if (locations.Count == 0 || goalCount == 0)
        {
            return [(Array.Empty<Loc>(), 0, 0)];
        }

        var minDistance = mapDefinition.DistributionStrategy.MinMinDistance;
        var geoHasher = new Geohasher();
        var filteredLocations = locations
            .GroupBy(x => Hasher.Encode(x.Lat, x.Lng, HashPrecision.Size_km_1x1))
            .AsParallel()
            .SelectMany(x => LocationDistributor.GetSome(x.ToArray(), (120_000m/files.Length).RoundToInt(), minDistance / 2))
            .ToArray();
        var tuple = ByMaxMinDistance(filteredLocations, goalCount, minDistance, mapDefinition, countryCode, "");
        var diff = goalCount - tuple.locations.Count;
        var notEnoughLocationsMessage = diff > 0 ? $"[olive]{diff,4} locations short.[/]" : "";
        AnsiConsole.MarkupLine($"[lightseagreen]Generated {tuple.locations.Count,6:N0} locations in {countryCode}. At least {tuple.minDistance,7:N0}m. between each location.[/]{notEnoughLocationsMessage}");
        return new[] { (tuple.locations, goalCount, tuple.minDistance) };
    }

    public static (IList<Loc> locations, int regionGoalCount, int minDistance)[] MaxLocationsInSubdivisionsByFixedMinDistance(
        string countryCode,
        string[] files,
        string[] availableSubdivisions,
        MapDefinition mapDefinition)
    {
        var allAvailableLocations = new Dictionary<string, IList<Loc>>();
        foreach (var file in files)
        {
            var deserializeFromFile = Extensions.ProtoDeserializeFromFile<Loc[]>(file);
            var subdivision = deserializeFromFile.First().Nominatim.SubdivisionCode;
            if (string.IsNullOrEmpty(subdivision))
            {
                continue;
            }

            var locationFilter = LocationFilter(countryCode, mapDefinition, subdivision);

            allAvailableLocations[subdivision] = availableSubdivisions.Contains(subdivision)
                    ? LocationLakeFilterer.Filter(deserializeFromFile, locationFilter)
                    : Array.Empty<Loc>();
        }

        var fixedMinDistance = mapDefinition.DistributionStrategy.FixedMinDistance;
        var initialGoalCount = 110_000;
        var totalGoalCount = initialGoalCount;
        var triedGoalCounts = new List<(int count, Status status)>();
        var locations = allAvailableLocations.ToDictionary(x => x.Key, _ => (ICollection<Loc>)Array.Empty<Loc>());
        while (triedGoalCounts.Count < 10)
        {
            var subdivisionGoalCounts = locations.ToDictionary(x => x.Key, x =>
                mapDefinition.SubdivisionDistribution.TryGetValue(countryCode, out var subdivisionWeights) ?
                    ((decimal)(subdivisionWeights.GetValueOrDefault(x.Key, 0)) / subdivisionWeights.Sum(y => y.Value) * totalGoalCount).RoundToInt() :
                SubdivisionWeights.GoalForSubdivision(countryCode, x.Key, totalGoalCount, availableSubdivisions));
            foreach (var (subdivision, _) in locations)
            {
                var subdivisionGoalCount = subdivisionGoalCounts[subdivision];
                var subdivisionAvailableLocations = allAvailableLocations[subdivision];
                if (subdivisionAvailableLocations.Count < subdivisionGoalCount)
                {
                    triedGoalCounts.Add((totalGoalCount, Status.Fail));
                    break;
                }

                var subdivisionLocations = LocationDistributor.GetSome(subdivisionAvailableLocations, subdivisionGoalCount, fixedMinDistance);
                if (subdivisionLocations.Count < subdivisionGoalCount)
                {
                    triedGoalCounts.Add((totalGoalCount, Status.Fail));
                    break;
                }

                locations[subdivision] = subdivisionLocations;
            }

            if (locations.All(x => x.Value.Count >= subdivisionGoalCounts[x.Key]))
            {
                triedGoalCounts.Add((totalGoalCount, Status.Success));
            }

            if (triedGoalCounts.Count == 1 && triedGoalCounts.Single().status == Status.Success)
            {
                break;
            }

            Console.WriteLine($"Old goal count: {totalGoalCount,7}");
            Console.WriteLine(triedGoalCounts.Select(x => $"{x.status,8} | {x.count,7}").Merge(Environment.NewLine));

            var newGoalCount = triedGoalCounts.Any(x => x.status == Status.Success)
                ? LargestSuccessCount() + (triedGoalCounts.Where(x => x.status == Status.Fail && x.count > LargestSuccessCount()).MinBy(x => x.count).count - LargestSuccessCount()) / 2
                : triedGoalCounts.MinBy(x => x.count).count / 2;
            Console.WriteLine($"New goal count: {newGoalCount,7}");

            totalGoalCount = newGoalCount;
        }

        var maxLocationCountSatisfyingFixedMinDistance = triedGoalCounts
            .OrderByDescending(x => x.count)
            .Where(x => x.status == Status.Success)
            .Select(x => x.count == 0 ? null : (int?)x.count)
            .FirstOrDefault() ?? triedGoalCounts.MinBy(x => x.count).count;
        var subdivisionGoalCountsSatisfyingFixedMinDistance = locations.ToDictionary(
            x => x.Key,
            x => mapDefinition.SubdivisionDistribution.TryGetValue(countryCode, out var subdivisionWeights)
                ? ((decimal)(subdivisionWeights.GetValueOrDefault(x.Key, 0)) /
                    subdivisionWeights.Sum(y => y.Value) * totalGoalCount).RoundToInt()
                : SubdivisionWeights.GoalForSubdivision(countryCode, x.Key, maxLocationCountSatisfyingFixedMinDistance,
                    availableSubdivisions));
        return locations.Select(x =>
            (ByMaxMinDistance(allAvailableLocations[x.Key], subdivisionGoalCountsSatisfyingFixedMinDistance[x.Key], subdivisionGoalCountsSatisfyingFixedMinDistance[x.Key], mapDefinition, countryCode, x.Key).locations,
                subdivisionGoalCountsSatisfyingFixedMinDistance[x.Key], fixedMinDistance)).ToArray();

        int LargestSuccessCount()
        {
            return triedGoalCounts.Where(x => x.status == Status.Success).MaxBy(x => x.count).count;
        }
    }

    private static (IList<Loc> locations, int minDistance) ByMaxMinDistance(
        IList<Loc> filteredLocations,
        int regionGoalCount,
        int minDistance,
        MapDefinition mapDefinition,
        string countryCode,
        string subdivision)
    {
        var preferenceFilters = mapDefinition switch
        {
            _ when
                mapDefinition.SubdivisionLocationPreferenceFilters.TryGetValue(countryCode, out var subdivisionPreferences) &&
                subdivisionPreferences.TryGetValue(subdivision, out var subdivisionFilters) => subdivisionFilters,
            _ when mapDefinition.CountryLocationPreferenceFilters.TryGetValue(countryCode, out var countryPreferences) => countryPreferences,
            _ => mapDefinition.GlobalLocationPreferenceFilters
        };
        if (preferenceFilters.Any())
        {
            var locations = new List<Loc>();
            var lastMinMinDistance = minDistance;
            foreach (var locationPreferenceFilter in preferenceFilters)
            {
                var filtered = locationPreferenceFilter.Expression == "*" ?
                    filteredLocations :
                    LocationLakeFilterer.Filter(filteredLocations, locationPreferenceFilter.Expression);
                var goalCount = locationPreferenceFilter.Fill || locationPreferenceFilter.Percentage is null
                    ? regionGoalCount - locations.Count
                    : (regionGoalCount * locationPreferenceFilter.Percentage / 100m).Value.RoundToInt();
                var minMinDistance = locationPreferenceFilter.MinMinDistance ?? minDistance;
                var withMaxMinDistance = LocationDistributor.WithMaxMinDistance(filtered, goalCount, minMinDistance: minMinDistance, locationsAlreadyInMap: locations);
                lastMinMinDistance = withMaxMinDistance.minDistance;
                IEnumerable<Loc> locationsFromPreference = withMaxMinDistance.locations;
                if (!string.IsNullOrEmpty(locationPreferenceFilter.LocationTag))
                {
                    locationsFromPreference = locationsFromPreference.Select(l => l with
                    {
                        Tag = locationPreferenceFilter.LocationTag
                    });
                }
                locations.AddRange(locationsFromPreference);
            }

            return (locations, lastMinMinDistance);
        }

        return LocationDistributor.WithMaxMinDistance(filteredLocations, regionGoalCount, minMinDistance: minDistance);
    }

    private static string? LocationFilter(string countryCode, MapDefinition mapDefinition, string subdivision)
    {
        var locationFilter = mapDefinition switch
        {
            _ when
                mapDefinition.SubdivisionLocationFilters.TryGetValue(countryCode, out var countrySubdivisionFilters) &&
                countrySubdivisionFilters.TryGetValue(subdivision, out var subdivisionFilter) => subdivisionFilter,
            _ when mapDefinition.CountryLocationFilters.TryGetValue(countryCode, out var countryFilter) => countryFilter,
            _ => mapDefinition.GlobalLocationFilter
        };
        return locationFilter;
    }

    private enum Status
    {
        Success,
        Fail
    }
}