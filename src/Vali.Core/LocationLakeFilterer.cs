using System.Collections.Concurrent;
using System.Collections.Frozen;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using Vali.Core.Expressions;
using Vali.Core.Google;
using Loc = Vali.Core.Location;

namespace Vali.Core;

public static class LocationLakeFilterer
{
    public static readonly FrozenSet<string> CountryCodesAcceptableWithoutDescription = new[]
    {
        "CX", "CC", "MP", "GU", "EG", "ML", "MG", "PN", "GL", "MN", "KR", "FO", "UG", "KG", "RW", "LB", "RE",
        "MQ", "NP", "PK", "BY", "UM"
    }.ToFrozenSet();

    public static readonly FrozenSet<string> SubdivisionCodesAcceptableWithoutDescription = new[]
    {
        "NO-21", "CA-NU", "US-AK", "BR-PE"
    }.ToFrozenSet();

    public static Loc[] Filter(
        IReadOnlyCollection<Loc> locationsFromFile,
        Dictionary<ulong, List<Loc>> neighborLocationBuckets,
        Dictionary<ulong, List<ILatLng>> proximityLocationBuckets,
        string? locationFilterExpression,
        ProximityFilter proximityFilter,
        (GeometryFilter filter, Geometry[] geometries)[] geometryFilters,
        NeighborFilter[] neighborFilters,
        MapDefinition mapDefinition)
    {
        List<Func<Loc, bool>> defaultFilterSelectors = mapDefinition.EnableDefaultLocationFilters
            ?
            [
                x => x.Nominatim.CountryCode switch
                {
                    "FI" => x.Google.ResolutionHeight >= Resolution.Gen4 || x.Google.Year < 2022,
                    "EC" => x.Google.ResolutionHeight >= Resolution.Gen4 || x.Google.Year < 2021,
                    "NG" => x.Google.ResolutionHeight >= Resolution.Gen4 || x.Google.Year < 2021,
                    _ => true
                }
            ]
            : [];

        if (string.IsNullOrEmpty(locationFilterExpression) || !locationFilterExpression.Contains("Tunnels"))
        {
            defaultFilterSelectors.Add(x => x.Osm.Tunnels10 == 0);
        }

        if (string.IsNullOrEmpty(locationFilterExpression) || (!locationFilterExpression.Contains(nameof(Loc.Google.DescriptionLength)) && !locationFilterExpression.Contains(nameof(Loc.Google.IsScout))))
        {
            defaultFilterSelectors.Add(x => (x.Google.DescriptionLength is null or > 0 &&
                                            x.Google.IsScout == false) ||
                                            CountryCodesAcceptableWithoutDescription.Contains(x.Nominatim.CountryCode) ||
                                            SubdivisionCodesAcceptableWithoutDescription.Contains(x.Nominatim.SubdivisionCode));
        }

        var locations = defaultFilterSelectors.Aggregate(locationsFromFile.AsEnumerable(), (current, defaultFilterSelector) => current.Where(defaultFilterSelector));

        if (!string.IsNullOrEmpty(locationFilterExpression))
        {
            var typedExpression = CompileBoolLocationExpression(locationFilterExpression);
            locations = locations.Where(typedExpression);
        }

        if (proximityFilter.Radius > 0 && File.Exists(proximityFilter.LocationsPath))
        {
            locations = FilterByProximity(locations, proximityFilter, proximityLocationBuckets);
        }

        if (geometryFilters.Length > 0)
        {
            locations = FilterByGeometries(locations, geometryFilters);
        }

        foreach (var neighborFilter in neighborFilters)
        {
            locations = NeighborFilterer.FilterByNeighbors(locations, neighborLocationBuckets, neighborFilter, mapDefinition);
        }

        return locations.DistinctBy(l => l.NodeId).ToArray();
    }

    private static readonly ConcurrentDictionary<string, Func<Loc, int>> _cacheInt = new();
    private static readonly ConcurrentDictionary<string, Func<Loc, bool>> _cacheBool = new();
    private static readonly ConcurrentDictionary<string, Func<Loc, Loc, bool>> _cacheParentBool = new();
    private static readonly ConcurrentDictionary<string, Func<MapCheckrLocation, bool>> _cacheMapCheckrBool = new();

    public static Func<Loc, int> CompileIntLocationExpression(string initialExpression)
    {
        if (_cacheInt.TryGetValue(initialExpression, out var selector))
        {
            return selector;
        }

        var func = CompileExpression<Loc, int>(initialExpression, 0);
        _cacheInt.TryAdd(initialExpression, func);
        return func;
    }

    public static Func<Loc, bool> CompileBoolLocationExpression(string initialExpression)
    {
        if (_cacheBool.TryGetValue(initialExpression, out var selector))
        {
            return selector;
        }

        var func = CompileExpression<Loc, bool>(initialExpression, true);
        _cacheBool.TryAdd(initialExpression, func);
        return func;
    }

    public static Func<Loc, Loc, bool> CompileParentBoolLocationExpression(string initialExpression)
    {
        if (_cacheParentBool.TryGetValue(initialExpression, out var selector))
        {
            return selector;
        }

        var func = CompileExpressionWithParent<Loc, bool>(initialExpression, true);
        _cacheParentBool.TryAdd(initialExpression, func);
        return func;
    }

    public static Func<MapCheckrLocation, bool> CompileBoolMapCheckrExpression(string initialExpression, bool fallback)
    {
        if (_cacheMapCheckrBool.TryGetValue(initialExpression, out var selector))
        {
            return selector;
        }

        var func = CompileExpression<MapCheckrLocation, bool>(initialExpression, fallback);
        _cacheMapCheckrBool.TryAdd(initialExpression, func);
        return func;
    }

    public static Func<TLoc, T> CompileExpression<TLoc, T>(string initialExpression, T fallback)
    {
        var resolver = typeof(TLoc).Name switch
        {
            nameof(Location) => PropertyResolver.ForLocation(),
            nameof(MapCheckrLocation) => PropertyResolver.ForMapCheckrLocation(),
            _ => throw new ArgumentOutOfRangeException()
        };
        return ExpressionCompiler.Compile<TLoc, T>(initialExpression, fallback, resolver);
    }

    public static Func<TLoc, TLoc, T> CompileExpressionWithParent<TLoc, T>(string initialExpression, T fallback)
    {
        var resolver = typeof(TLoc).Name switch
        {
            nameof(Location) => PropertyResolver.ForLocation(),
            nameof(MapCheckrLocation) => PropertyResolver.ForMapCheckrLocation(),
            _ => throw new ArgumentOutOfRangeException()
        };
        return ExpressionCompiler.CompileWithParent<TLoc, T>(initialExpression, fallback, resolver);
    }

    private static readonly IReadOnlyCollection<string> _validProperties =
        PropertyResolver.ForLocation().ValidPropertyNames;

    public static IReadOnlyCollection<string> ValidProperties() => _validProperties;

    private static readonly IReadOnlyCollection<string> _validMapCheckrLocationProperties =
        PropertyResolver.ForMapCheckrLocation().ValidPropertyNames;

    public static IReadOnlyCollection<string> ValidMapCheckrLocationProperties() => _validMapCheckrLocationProperties;

    private static readonly string[] _validOperators =
    [
        "eq",
        "neq",
        "lt",
        "lte",
        "gt",
        "gte",
        "and",
        "or",
        "+",
        "-",
        "/",
        "*",
        "modulo",
        "in"
    ];

    public static IEnumerable<string?> ValidOperators() => _validOperators;

    private static IEnumerable<Loc> FilterByProximity(
        IEnumerable<Loc> locations,
        ProximityFilter proximityFilter,
        Dictionary<ulong, List<ILatLng>> proximityLocationBuckets)
    {
        var precision = proximityFilter.HashPrecisionFromProximityFilter()!.Value;
        var proximityFilterRadiusSquared = (double)proximityFilter.Radius * proximityFilter.Radius;
        return locations.Where(l =>
        {
            var hash = Hasher.Encode(l.Lat, l.Lng, precision);
            return LocationLookupService.GetNearbyLocations(proximityLocationBuckets, hash).Any(x => Extensions.PointsAreCloserThan(l.Lat, l.Lng, x.Lat, x.Lng, proximityFilterRadiusSquared));
        });
    }

    private static IEnumerable<Loc> FilterByGeometries(IEnumerable<Loc> locations, (GeometryFilter filter, Geometry[] geometries)[] geometryFilters)
    {
        var combinationMode = geometryFilters.First().filter.CombinationMode;
        var preparedFilters = geometryFilters
            .Select(gf => (gf.filter, geometries: gf.geometries.Select(PreparedGeometryFactory.Prepare).ToArray()))
            .ToArray();
        var globalEnvelope = new Envelope();
        foreach (var gf in geometryFilters)
            foreach (var g in gf.geometries)
                globalEnvelope.ExpandToInclude(g.EnvelopeInternal);

        return combinationMode switch
        {
            "union" => locations
                .Where(l =>
                {
                    if (!globalEnvelope.Covers(l.Lng, l.Lat))
                    {
                        return preparedFilters.Any(gf => !gf.filter.LocationsInside);
                    }

                    var point = new Point(l.Lng, l.Lat);
                    return preparedFilters
                        .Any(gf => gf.geometries
                            .Any(g => g.Covers(point)) == gf.filter.LocationsInside);
                }),
            "intersection" => locations
                .Where(l =>
                {
                    if (!globalEnvelope.Covers(l.Lng, l.Lat))
                    {
                        return preparedFilters.All(gf => !gf.filter.LocationsInside);
                    }

                    var point = new Point(l.Lng, l.Lat);
                    return preparedFilters
                        .All(gf => gf.geometries
                            .Any(g => g.Covers(point)) == gf.filter.LocationsInside);
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(combinationMode), "Only union/intersection acceptable values.")
        };
    }
}
