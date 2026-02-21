using Spectre.Console;
using Vali.Core.Hash;
using Loc = Vali.Core.Location;

namespace Vali.Core;

public static class DistributionStrategies
{
    public const string FixedCountByMaxMinDistance = "FixedCountByMaxMinDistance";
    public const string MaxCountByFixedMinDistance = "MaxCountByFixedMinDistance";
    public const string EvenlyByDistanceWithinCountry = "EvenlyByDistanceWithinCountry";
    public const string FixedCountByCoverageDensity = "FixedCountByCoverageDensity";

    public static readonly string[] ValidStrategyKeys =
    [
        FixedCountByMaxMinDistance,
        MaxCountByFixedMinDistance,
        EvenlyByDistanceWithinCountry,
        FixedCountByCoverageDensity
    ];

    public static (IList<Loc> locations, int regionGoalCount, int minDistance) SubdivisionByMaxMinDistance(
        string countryCode,
        string file,
        int goalCount,
        string[] availableSubdivisions,
        MapDefinition mapDefinition)
    {
        var deserializeFromFile = ReadFromFile(file, mapDefinition, countryCode);
        var subdivision = deserializeFromFile.FirstOrDefault()?.Nominatim.SubdivisionCode ?? "";
        if (!availableSubdivisions.Contains(subdivision))
        {
            return (Array.Empty<Loc>(), 0, 0);
        }

        var neighborLocationBuckets = LocationLookupService.Bucketize(deserializeFromFile, mapDefinition.HashPrecisionFromNeighborFiltersRadius());
        var proximityFilter = ProximityFilter(countryCode, mapDefinition, subdivision);
        var locationsFromFile = LocationReader.DeserializeLocationsFromFile(proximityFilter.LocationsPath);
        var proximityLocationBuckets = LocationLookupService.Bucketize<ILatLng>(locationsFromFile, proximityFilter.HashPrecisionFromProximityFilter());
        var filteredLocations = FilteredLocations(countryCode, mapDefinition, subdivision, deserializeFromFile, neighborLocationBuckets, proximityLocationBuckets, proximityFilter);
        var regionGoalCount = mapDefinition.SubdivisionDistribution.TryGetValue(countryCode, out var subdivisionWeights) ?
            ((decimal)(subdivisionWeights.GetValueOrDefault(subdivision, 0)) / subdivisionWeights.Sum(x => x.Value) * goalCount).RoundToInt() :
            SubdivisionWeights.GoalForSubdivision(countryCode, subdivision, goalCount, availableSubdivisions);
        if (regionGoalCount == 0)
        {
            return (Array.Empty<Loc>(), 0, 0);
        }

        if (filteredLocations.Length == 0)
        {
            return (Array.Empty<Loc>(), 0, 0);
        }

        var minDistance = mapDefinition.DistributionStrategy.MinMinDistance;

        var tuple = ByMaxMinDistance(filteredLocations, regionGoalCount, neighborLocationBuckets, minDistance, mapDefinition, countryCode, subdivision);
        var diff = regionGoalCount - tuple.locations.Count;
        var notEnoughLocationsMessage = diff > 0 ? $"[olive]{diff,4} locations short.[/]" : "";
        if (!ConsoleLogger.Silent)
        {
            AnsiConsole.MarkupLine($"[lightseagreen]{tuple.locations.Count,6:N0} locations in {subdivision,7}. At least {tuple.minDistance,7:N0}m. between each location.[/]{notEnoughLocationsMessage}");
        }

        return (tuple.locations, regionGoalCount, tuple.minDistance);
    }

    public static (IList<Loc> locations, int regionGoalCount, int minDistance) SubdivisionByCoverageDensity(
        string countryCode,
        string file,
        int goalCount,
        string[] availableSubdivisions,
        MapDefinition mapDefinition)
    {
        var deserializeFromFile = ReadFromFile(file, mapDefinition, countryCode);
        var subdivision = deserializeFromFile.FirstOrDefault()?.Nominatim.SubdivisionCode ?? "";
        if (!availableSubdivisions.Contains(subdivision))
        {
            return (Array.Empty<Loc>(), 0, 0);
        }

        var neighborLocationBuckets = LocationLookupService.Bucketize(deserializeFromFile, mapDefinition.HashPrecisionFromNeighborFiltersRadius());
        var proximityFilter = ProximityFilter(countryCode, mapDefinition, subdivision);
        var locationsFromFile = LocationReader.DeserializeLocationsFromFile(proximityFilter.LocationsPath);
        var proximityLocationBuckets = LocationLookupService.Bucketize<ILatLng>(locationsFromFile, proximityFilter.HashPrecisionFromProximityFilter());
        var filteredLocations = FilteredLocations(countryCode, mapDefinition, subdivision, deserializeFromFile, neighborLocationBuckets, proximityLocationBuckets, proximityFilter);
        var regionGoalCount = mapDefinition.SubdivisionDistribution.TryGetValue(countryCode, out var subdivisionWeights) ?
            ((decimal)(subdivisionWeights.GetValueOrDefault(subdivision, 0)) / subdivisionWeights.Sum(x => x.Value) * goalCount).RoundToInt() :
            SubdivisionWeights.GoalForSubdivision(countryCode, subdivision, goalCount, availableSubdivisions);
        if (regionGoalCount == 0)
        {
            return (Array.Empty<Loc>(), 0, 0);
        }

        if (filteredLocations.Length == 0)
        {
            return (Array.Empty<Loc>(), 0, 0);
        }

        var minDistance = mapDefinition.DistributionStrategy.MinMinDistance;

        return LocationsByCoverageDensity(countryCode, mapDefinition, filteredLocations, regionGoalCount, neighborLocationBuckets, minDistance, subdivision);
    }

    public static (IList<Loc> locations, int regionGoalCount, int minDistance)[] CountryByCoverageDensity(
        string countryCode,
        string[] files,
        int goalCount,
        MapDefinition mapDefinition)
    {
        var allLocations = new List<Loc>();
        foreach (var file in files)
        {
            var fromFile = ReadFromFile(file, mapDefinition, countryCode);
            allLocations.AddRange(fromFile);
        }

        if (allLocations.Count == 0 || goalCount == 0)
        {
            return [([], 0, 0)];
        }

        var neighborLocationBuckets = LocationLookupService.Bucketize(allLocations, mapDefinition.HashPrecisionFromNeighborFiltersRadius());
        var proximityFilter = ProximityFilter(countryCode, mapDefinition, "N/A");
        var locationsFromFile = LocationReader.DeserializeLocationsFromFile(proximityFilter.LocationsPath);
        var proximityLocationBuckets = LocationLookupService.Bucketize<ILatLng>(locationsFromFile, proximityFilter.HashPrecisionFromProximityFilter());
        var locations = FilteredLocations(countryCode, mapDefinition, "", allLocations, neighborLocationBuckets, proximityLocationBuckets, proximityFilter);
        if (locations.Length == 0)
        {
            return [([], 0, 0)];
        }

        var locationProbability = LocationProbability(countryCode, mapDefinition, "N/A");
        var minDistance = mapDefinition.DistributionStrategy.MinMinDistance;
        var filteredLocations = locations
            .GroupBy(x => Hasher.Encode(x.Lat, x.Lng, HashPrecision.Size_km_1x1))
            .AsParallel()
            .SelectMany(x => LocationDistributor.GetSome<Loc, long>([.. x], (120_000m / files.Length).RoundToInt(), minDistance / 2, locationProbability))
            .ToArray();
        return [LocationsByCoverageDensity(countryCode, mapDefinition, filteredLocations, goalCount, neighborLocationBuckets, minDistance, "N/A")];
    }

    private static (IList<Loc> locations, int regionGoalCount, int minDistance) LocationsByCoverageDensity(
        string countryCode,
        MapDefinition mapDefinition,
        Loc[] filteredLocations,
        int regionGoalCount,
        Dictionary<ulong, List<Loc>> neighborLocationBuckets,
        int minDistance,
        string subdivision)
    {
        var locationClusters = filteredLocations
            .GroupBy(x => Hasher.Encode(x.Lat, x.Lng, HashPrecision.Size_km_156x156))
            .Select(g => (Key: g.Key, Locations: g.ToArray()))
            .ToArray();

        var tuningFactor = mapDefinition.DistributionStrategy.CoverageDensityTuningFactor!.Value;
        var locationClusterWeights = locationClusters
            .Select(x => new
            {
                Weight = 1.0 / Math.Pow(x.Locations.Length, tuningFactor) * x.Locations.Length,
                x.Key
            })
            .ToArray();

        var weightSum = locationClusterWeights.Sum(x => x.Weight);
        var weights = locationClusterWeights.ToDictionary(x => x.Key, t => (t.Weight / weightSum));
        var resultLocations = new List<Loc>();
        var resultMinDistance = int.MaxValue;
        foreach (var locationCluster in locationClusters)
        {
            var count = (int)Math.Round(regionGoalCount * weights[locationCluster.Key], 0);
            var tuple = ByMaxMinDistance(locationCluster.Locations, count, neighborLocationBuckets, minDistance, mapDefinition, countryCode, subdivision);
            resultLocations.AddRange(tuple.locations);
            resultMinDistance = Math.Min(tuple.minDistance, resultMinDistance);
        }

        var diff = regionGoalCount - resultLocations.Count;
        var notEnoughLocationsMessage = diff > 0 ? $"[olive]{diff,4} locations short.[/]" : "";
        if (!ConsoleLogger.Silent)
        {
            AnsiConsole.MarkupLine($"[lightseagreen]{resultLocations.Count,6:N0} locations in {subdivision,7}. At least {resultMinDistance,7:N0}m. between each location.[/]{notEnoughLocationsMessage}");
        }

        return (resultLocations, regionGoalCount, resultMinDistance);
    }

    private static Loc[] FilteredLocations(
        string countryCode,
        MapDefinition mapDefinition,
        string subdivision,
        IReadOnlyCollection<Loc> deserializeFromFile,
        Dictionary<ulong, List<Loc>> neighborLocationBuckets,
        Dictionary<ulong, List<ILatLng>> proximityLocationBuckets,
        ProximityFilter proximityFilter)
    {
        var locationFilter = LocationFilter(countryCode, mapDefinition, subdivision);
        var geometryFilters = GeometryFilters(countryCode, mapDefinition, subdivision).GetApplicableGeometryFilters();
        var neighborFilters = mapDefinition.NeighborFilters;
        var filteredLocations = LocationLakeFilterer.Filter(deserializeFromFile, neighborLocationBuckets, proximityLocationBuckets, locationFilter, proximityFilter, geometryFilters, neighborFilters, mapDefinition);
        return filteredLocations;
    }

    public static (IList<Loc> locations, int regionGoalCount, int minDistance)[] CountryByMaxMinDistance(
        string countryCode,
        string[] files,
        int goalCount,
        MapDefinition mapDefinition)
    {
        var allLocations = new List<Loc>();
        foreach (var file in files)
        {
            var fromFile = ReadFromFile(file, mapDefinition, countryCode);
            allLocations.AddRange(fromFile);
        }

        if (allLocations.Count == 0 || goalCount == 0)
        {
            return [([], 0, 0)];
        }

        var neighborLocationBuckets = LocationLookupService.Bucketize(allLocations, mapDefinition.HashPrecisionFromNeighborFiltersRadius());
        var proximityFilter = ProximityFilter(countryCode, mapDefinition, "N/A");
        var locationsFromFile = LocationReader.DeserializeLocationsFromFile(proximityFilter.LocationsPath);
        var proximityLocationBuckets = LocationLookupService.Bucketize<ILatLng>(locationsFromFile, proximityFilter.HashPrecisionFromProximityFilter());
        var locations = FilteredLocations(countryCode, mapDefinition, "", allLocations, neighborLocationBuckets, proximityLocationBuckets, proximityFilter);
        if (locations.Length == 0)
        {
            return [([], 0, 0)];
        }

        var locationProbability = LocationProbability(countryCode, mapDefinition, "N/A");
        var minDistance = mapDefinition.DistributionStrategy.MinMinDistance;
        var filteredLocations = locations
            .GroupBy(x => Hasher.Encode(x.Lat, x.Lng, HashPrecision.Size_km_1x1))
            .AsParallel()
            .SelectMany(x => LocationDistributor.GetSome<Loc, long>([.. x], (120_000m/files.Length).RoundToInt(), minDistance / 2, locationProbability))
            .ToArray();
        var tuple = ByMaxMinDistance(filteredLocations, goalCount, neighborLocationBuckets, minDistance, mapDefinition, countryCode, "");
        var diff = goalCount - tuple.locations.Count;
        var notEnoughLocationsMessage = diff > 0 ? $"[olive]{diff,4} locations short.[/]" : "";
        if (!ConsoleLogger.Silent)
        {
            AnsiConsole.MarkupLine($"[lightseagreen]Generated {tuple.locations.Count,6:N0} locations in {countryCode}. At least {tuple.minDistance,7:N0}m. between each location.[/]{notEnoughLocationsMessage}");
        }

        return [(tuple.locations, goalCount, tuple.minDistance)];
    }

    public static (IList<Loc> locations, int regionGoalCount, int minDistance)[] MaxLocationsInSubdivisionsByFixedMinDistance(
        string countryCode,
        string[] files,
        string[] availableSubdivisions,
        MapDefinition mapDefinition)
    {
        var allAvailableLocations = new Dictionary<string, Loc[]>();
        Dictionary<string, Dictionary<ulong, List<Loc>>> neighborLocationsBuckets = new();
        Dictionary<string, Dictionary<ulong, List<ILatLng>>> proximityLocationBuckets = new();
        foreach (var file in files)
        {
            var deserializeFromFile = ReadFromFile(file, mapDefinition, countryCode);
            var subdivision = deserializeFromFile.First().Nominatim.SubdivisionCode;
            if (string.IsNullOrEmpty(subdivision))
            {
                continue;
            }

            var locationFilter = LocationFilter(countryCode, mapDefinition, subdivision);
            var proximityFilter = ProximityFilter(countryCode, mapDefinition, subdivision);

            neighborLocationsBuckets[subdivision] = LocationLookupService.Bucketize(deserializeFromFile, mapDefinition.HashPrecisionFromNeighborFiltersRadius());
            var locationsFromFile = LocationReader.DeserializeLocationsFromFile(proximityFilter.LocationsPath);
            var proximityLocationBucket = LocationLookupService.Bucketize<ILatLng>(locationsFromFile, proximityFilter.HashPrecisionFromProximityFilter());
            proximityLocationBuckets[subdivision] = proximityLocationBucket;
            var geometryFilters = GeometryFilters(countryCode, mapDefinition, subdivision).GetApplicableGeometryFilters();
            allAvailableLocations[subdivision] = availableSubdivisions.Contains(subdivision)
                    ? LocationLakeFilterer.Filter(deserializeFromFile, neighborLocationsBuckets[subdivision], proximityLocationBuckets[subdivision], locationFilter, proximityFilter, geometryFilters, mapDefinition.NeighborFilters, mapDefinition)
                    : [];
        }

        var fixedMinDistance = mapDefinition.DistributionStrategy.FixedMinDistance;
        var hasSubdivisionWeights = mapDefinition.SubdivisionDistribution.TryGetValue(countryCode, out var subdivisionWeightsForCountry);
        var subdivisionWeightSum = hasSubdivisionWeights ? subdivisionWeightsForCountry!.Sum(x => x.Value) : 0;
        var initialGoalCount = 110_000;
        var totalGoalCount = initialGoalCount;
        var triedGoalCounts = new List<(int count, Status status)>();
        var locations = allAvailableLocations.ToDictionary(x => x.Key, ICollection<Loc> (_) => Array.Empty<Loc>());
        while (triedGoalCounts.Count < 10)
        {
            var subdivisionGoalCounts = locations.ToDictionary(x => x.Key, x =>
                hasSubdivisionWeights ?
                    ((decimal)(subdivisionWeightsForCountry!.GetValueOrDefault(x.Key, 0)) / subdivisionWeightSum * totalGoalCount).RoundToInt() :
                SubdivisionWeights.GoalForSubdivision(countryCode, x.Key, totalGoalCount, availableSubdivisions));
            foreach (var (subdivision, _) in locations)
            {
                var subdivisionGoalCount = subdivisionGoalCounts[subdivision];
                var subdivisionAvailableLocations = allAvailableLocations[subdivision];
                if (subdivisionAvailableLocations.Length < subdivisionGoalCount)
                {
                    triedGoalCounts.Add((totalGoalCount, Status.Fail));
                    break;
                }

                var locationProbability = LocationProbability(countryCode, mapDefinition, subdivision);
                var subdivisionLocations = LocationDistributor.GetSome<Location, long>(subdivisionAvailableLocations, subdivisionGoalCount, fixedMinDistance, locationProbability);
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
            x => hasSubdivisionWeights
                ? ((decimal)(subdivisionWeightsForCountry!.GetValueOrDefault(x.Key, 0)) /
                    subdivisionWeightSum * totalGoalCount).RoundToInt()
                : SubdivisionWeights.GoalForSubdivision(countryCode, x.Key, maxLocationCountSatisfyingFixedMinDistance,
                    availableSubdivisions));
        return locations.Select(x =>
            (ByMaxMinDistance(allAvailableLocations[x.Key], subdivisionGoalCountsSatisfyingFixedMinDistance[x.Key], neighborLocationsBuckets[x.Key], subdivisionGoalCountsSatisfyingFixedMinDistance[x.Key], mapDefinition, countryCode, x.Key).locations,
                subdivisionGoalCountsSatisfyingFixedMinDistance[x.Key], fixedMinDistance)).ToArray();

        int LargestSuccessCount()
        {
            return triedGoalCounts.Where(x => x.status == Status.Success).MaxBy(x => x.count).count;
        }
    }

    public static (IList<Loc> locations, int regionGoalCount, int minDistance)[] EvenlyByDistanceInCountry(
        string countryCode,
        string[] files,
        string[] availableSubdivisions,
        MapDefinition mapDefinition)
    {
        var allAvailableLocations = new List<Loc>();
        foreach (var file in files)
        {
            var deserializeFromFile = ReadFromFile(file, mapDefinition, countryCode);
            var subdivision = deserializeFromFile.First().Nominatim.SubdivisionCode;
            if (string.IsNullOrEmpty(subdivision))
            {
                continue;
            }

            var locationFilter = LocationFilter(countryCode, mapDefinition, subdivision);
            var proximityFilter = ProximityFilter(countryCode, mapDefinition, subdivision);
            var locationsFromFile = LocationReader.DeserializeLocationsFromFile(proximityFilter.LocationsPath);
            var proximityLocationBuckets = LocationLookupService.Bucketize<ILatLng>(locationsFromFile, proximityFilter.HashPrecisionFromProximityFilter());

            var neighborLocationBuckets = LocationLookupService.Bucketize(deserializeFromFile, mapDefinition.HashPrecisionFromNeighborFiltersRadius());
            var geometryFilters = GeometryFilters(countryCode, mapDefinition, subdivision).GetApplicableGeometryFilters();
            allAvailableLocations.AddRange(availableSubdivisions.Contains(subdivision)
                ? LocationLakeFilterer.Filter(deserializeFromFile, neighborLocationBuckets, proximityLocationBuckets, locationFilter, proximityFilter, geometryFilters, mapDefinition.NeighborFilters, mapDefinition)
                : []);
        }

        var minDistanceBetweenLocations = mapDefinition.DistributionStrategy.FixedMinDistance;
        var locationProbability = LocationProbability(countryCode, mapDefinition, "N/A");
        var locations = LocationDistributor.DistributeEvenly<Loc, long>(allAvailableLocations, minDistanceBetweenLocations, locationProbability);
        return [(locations, -1, minDistanceBetweenLocations)];
    }

    private static (IList<Loc> locations, int minDistance) ByMaxMinDistance(
        Loc[] filteredLocations,
        int regionGoalCount,
        Dictionary<ulong, List<Loc>> neighborLocationBuckets,
        int minDistance,
        MapDefinition mapDefinition,
        string countryCode,
        string subdivision)
    {
        var usedLocations = mapDefinition.UsedLocationsPaths.Any()
            ? mapDefinition.UsedLocationsPaths
                .SelectMany(LocationReader.DeserializeLocationsFromFile)
                .Where(x => x.extra != null && x.extra.tags.Any(t => countryCode == t))
                .ToArray()
            : [];
        var preferenceFilters = mapDefinition switch
        {
            _ when
                mapDefinition.SubdivisionLocationPreferenceFilters.TryGetValue(countryCode, out var subdivisionPreferences) &&
                subdivisionPreferences.TryGetValue(subdivision, out var subdivisionFilters) => subdivisionFilters,
            _ when mapDefinition.CountryLocationPreferenceFilters.TryGetValue(countryCode, out var countryPreferences) => countryPreferences,
            _ => mapDefinition.GlobalLocationPreferenceFilters
        };
        var locationProbability = LocationProbability(countryCode, mapDefinition, subdivision);
        if (preferenceFilters.Any())
        {
            var locations = new List<Loc>();
            var lastMinMinDistance = minDistance;
            List<ILatLng>? combinedLocationsForMap = usedLocations.Length > 0 ? new List<ILatLng>(usedLocations) : null;
            foreach (var locationPreferenceFilter in preferenceFilters)
            {
                var proximityFilter = locationPreferenceFilter.ProximityFilter;
                var proximityLocationBuckets = LocationLookupService.Bucketize<ILatLng>(LocationReader.DeserializeLocationsFromFile(proximityFilter.LocationsPath), proximityFilter.HashPrecisionFromProximityFilter());
                var neighbourFilters = locationPreferenceFilter.NeighborFilters.Concat(mapDefinition.NeighborFilters).ToArray();
                var geometryFilters = locationPreferenceFilter.GeometryFilters.GetApplicableGeometryFilters();
                var filtered = LocationLakeFilterer.Filter(filteredLocations, neighborLocationBuckets, proximityLocationBuckets, locationPreferenceFilter.Expression, proximityFilter, geometryFilters, neighbourFilters, mapDefinition);
                var goalCount = locationPreferenceFilter.Fill || locationPreferenceFilter.Percentage is null
                    ? regionGoalCount - locations.Count
                    : (regionGoalCount * locationPreferenceFilter.Percentage / 100m).Value.RoundToInt();
                var minMinDistance = locationPreferenceFilter.MinMinDistance ?? minDistance;
                IReadOnlyCollection<ILatLng> locationsAlreadyInMap = combinedLocationsForMap ?? (IReadOnlyCollection<ILatLng>)locations;
                var withMaxMinDistance = LocationDistributor.WithMaxMinDistance<Loc, long>(filtered, goalCount, minMinDistance: minMinDistance, locationsAlreadyInMap: locationsAlreadyInMap, locationProbability: locationProbability);
                lastMinMinDistance = withMaxMinDistance.minDistance;
                IEnumerable<Loc> locationsFromPreference = withMaxMinDistance.locations;
                if (!string.IsNullOrEmpty(locationPreferenceFilter.LocationTag))
                {
                    locationsFromPreference = locationsFromPreference.Select(l => l with
                    {
                        Tag = locationPreferenceFilter.LocationTag
                    });
                }
                var prevCount = locations.Count;
                locations.AddRange(locationsFromPreference);
                if (combinedLocationsForMap != null)
                {
                    for (var i = prevCount; i < locations.Count; i++)
                        combinedLocationsForMap.Add(locations[i]);
                }
            }

            return (locations, lastMinMinDistance);
        }

        return LocationDistributor.WithMaxMinDistance<Loc, long>(filteredLocations, regionGoalCount, minMinDistance: minDistance, locationsAlreadyInMap: usedLocations, locationProbability: locationProbability);
    }

    private static string? LocationFilter(string countryCode, MapDefinition mapDefinition, string subdivision)
    {
        var subdivisionFilter =
            mapDefinition.SubdivisionLocationFilters.TryGetValue(countryCode, out var countrySubdivisionFilters) &&
            countrySubdivisionFilters.TryGetValue(subdivision, out var s)
                ? s
                : "";
        var countryFilter = mapDefinition.CountryLocationFilters.GetValueOrDefault(countryCode, "");
        var globalFilter = mapDefinition.GlobalLocationFilter;
        var locationFilter = new[] { globalFilter, countryFilter, subdivisionFilter }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => $"({x})")
            .Merge(" and ");
        return locationFilter;
    }

    private static Loc[] ReadFromFile(string file, MapDefinition mapDefinition, string countryCode)
    {
        var locations = Extensions.ProtoDeserializeFromFile<Loc[]>(file);
        if (mapDefinition.GlobalExternalDataFiles.Length > 0)
        {
            SetExternalData(locations, mapDefinition.GlobalExternalDataFiles);
        }

        if (mapDefinition.CountryExternalDataFiles.TryGetValue(countryCode, out var externalFiles) && externalFiles.Length > 0)
        {
            SetExternalData(locations, externalFiles);
        }
        
        if ((mapDefinition.GlobalExternalDataFiles.Length > 0 || mapDefinition.CountryExternalDataFiles.Count > 0) && (locations.Length > 0 && locations.First().ExternalData.Count == 0))
        {
            var fileWithDictionaryValues = mapDefinition.CountryExternalDataFiles
                .SelectMany(x => x.Value)
                .Select(f => (dictionary: Extensions.TryJsonDeserializeFromFile<Dictionary<string, Dictionary<string, string>>>(f, []), file: f))
                .First(x => x.dictionary.Values.Count > 0)
                .file;
            SetExternalData(locations, [fileWithDictionaryValues]);
        }

        return locations;

        static void SetExternalData(Loc[] locations, string[] externalFiles)
        {
            foreach (var externalDataFile in externalFiles)
            {
                var externalData = Extensions.TryJsonDeserializeFromFile<Dictionary<string, Dictionary<string, string>>>(externalDataFile, []);
                if (externalData.Count == 0)
                {
                    continue;
                }

                var defaultDictionary = externalData.First().Value.ToDictionary(x => x.Key, _ => "");
                foreach (var location in locations)
                {
                    location.ExternalData = externalData.GetValueOrDefault(location.LocationId.ToString(), defaultDictionary);
                }
            }
        }
    }

    private static ProximityFilter ProximityFilter(string countryCode, MapDefinition mapDefinition, string subdivision) =>
        mapDefinition switch
        {
            _ when
                mapDefinition.SubdivisionProximityFilters.TryGetValue(countryCode, out var countrySubdivisionProximityFilters) &&
                countrySubdivisionProximityFilters.TryGetValue(subdivision, out var subdivisionProximityFilter) => subdivisionProximityFilter,
            _ when mapDefinition.CountryProximityFilters.TryGetValue(countryCode, out var countryProximityFilter) => countryProximityFilter,
            _ => mapDefinition.ProximityFilter
        };

    private static GeometryFilter[] GeometryFilters(string countryCode, MapDefinition mapDefinition, string subdivision) =>
        mapDefinition switch
        {
            _ when
                mapDefinition.SubdivisionGeometryFilters.TryGetValue(countryCode, out var countrySubdivisionGeometryFilters) &&
                countrySubdivisionGeometryFilters.TryGetValue(subdivision, out var subdivisionGeometryFilters) => subdivisionGeometryFilters,
            _ when mapDefinition.CountryGeometryFilters.TryGetValue(countryCode, out var countryGeometryFilters) => countryGeometryFilters,
            _ => mapDefinition.GeometryFilters
        };

    private static LocationProbability LocationProbability(string countryCode, MapDefinition mapDefinition, string subdivision) =>
        mapDefinition switch
        {
            _ when
                mapDefinition.SubdivisionLocationProbabilities.TryGetValue(countryCode, out var countrySubdivisionLocationProbabilities) &&
                countrySubdivisionLocationProbabilities.TryGetValue(subdivision, out var subdivisionLocationProbability) => subdivisionLocationProbability,
            _ when mapDefinition.CountryLocationProbabilities.TryGetValue(countryCode, out var countryLocationProbability) => countryLocationProbability,
            _ => mapDefinition.GlobalLocationProbability
        };

    private enum Status
    {
        Success,
        Fail
    }
}