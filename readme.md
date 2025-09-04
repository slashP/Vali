# Vali - create GeoGuessr locations like a pro
The creator of *An Arbitrary World*,  *An Arbitrary Rural World*, *Dirty World*, *IntersectionGuessr* ++ brings you Vali - the next evolution in computer generating GeoGuessr maps. With this tool **you** can create "An Arbitrary Rural Southern Europe", "Coastal Sri Lanka", "Skewed Africa" or something brand new. More than 100 million possible locations are available - be creative.

# What is Vali?
Vali uses a massive pool of pre-generated Google street view locations combined with specific data from OpenStreetMap and Nominatim to provide a tool that can generate locations based on your preferences. The creation and update of the locations happens outside of Vali and is not part of this repository. You provide a specification of what type of locations you want in the form of a JSON file, and Vali will give you locations you can upload to [map-making.app](https://map-making.app/) or GeoGuessr.

# First time setup
* Install [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
* Install [Windows Terminal](https://learn.microsoft.com/en-us/windows/terminal/install#install) (or use another terminal).
* Install [Visual Studio Code](https://code.visualstudio.com/download) or use another text editor suitable for editing JSON files.
* Open Windows Terminal and run `dotnet tool install -g vali`
* To update to the latest version, if you already have it installed - run `dotnet tool update -g vali`

# Getting started
* `vali create-file` - Create a JSON file to start off.
* Open the JSON file that was just created in Visual Studio Code. Edit it if you want.
* `vali generate --file "NO.json"` - Generate locations based on the JSON file specified.
* You can also watch a video showing Vali in use: https://youtu.be/pG10DSVLVKY

# Commands
* `vali generate --file norway.json` - Generate locations based on specification in `norway.json`.
* `vali subdivisions --country "ES"` - Export default subdivision distribution data for Spain as JSON.
* `vali subdivisions --country "ES" --text` - Export default subdivision distribution data for Spain as text.
* `vali countries "ES,FR,IT"` - Export default country distribution data for Spain as JSON.
* `vali countries "ES,FR,IT" --distribution "abw"` - Export country distribution data for Spain, France and Italy as specified by "A Balanced World" as JSON.
* `vali countries "*" --distribution "aiw" --text` - Export country distribution data for all countries as specified by "An Improved World" as text.
* `vali report --country "NO" --prop "County"` - Export counties/municipalities data for Norway.
* `vali report --country "BE" --prop "Year"` - Export coverage year data for Belgium.
* `vali download` - Download/update data. You will be asked which countries you want to download data for.
* `vali set-download-folder "D:\vali-data"` - Change folder where data is downloaded to. Default location is C:\ProgramData. On Mac? Set environment variable `VALI_DOWNLOAD_FOLDER` to the folder you want data downloaded to.
* `vali unset-download-folder` - Reset download folder to default.
* `vali application-settings` - Read application settings.
* `vali distribute-from-file --file ".\large-ES.json" --distance 250 --outputPath ".\large-ES-locations.json"` - Use vali's distribution algorithm to distribute locations from a file with lots of locations.

# Buliding blocks
Go directly to [full examples](#full-examples) or [properties](#properties) if you prefer not to read.

## Selecting countries
You need to specify which countries you want to include in the `countryCodes` array. Example:
```json
{
  "countryCodes": ["EC", "CO", "PE", "BO"]
}
```
## Distribution strategy
You can select between different strategies for distributing locations. Option 1 is `FixedCountByMaxMinDistance` where you specify how many locations you want in total and each subdivision will get their locations spread as far away from each other as possible. You can set a minimum minimum distance to set a lower limit on how close each location can be to each other. This is the recommended approach for most. Example:
```json
{
  "distributionStrategy": {
    "key": "FixedCountByMaxMinDistance",
    "locationCountGoal": 45000,
    "minMinDistance": 200
  }
}
```

Option 2 is `MaxCountByFixedMinDistance` where you specify how far you want each location to be from each other in `FixedMinDistance` and it finds the maximum amount of locations *still upholding the subdivision distribution*. Example:
```json
{
  "distributionStrategy": {
    "key": "MaxCountByFixedMinDistance",
    "FixedMinDistance": 200
  }
}
```
## Specific country distribution
You can specify a country distribution of your choice. All numbers are relative (not representing location counts) and must be integers. Example:
```json
{
  "countryDistribution": {
    "NO": 200,
    "SE": 220,
    "FI": 200,
    "DK": 140
  }
}
```
Use `vali countries --country "NO,SE,FI,DK"` to output the default distribution, so you can copy it and make adjustments suitable to your map.

## Specific subdivision distribution(s)
You can specify one or more subdivision distribution to override the default one(s). All numbers are relative (not representing location counts) and must be integers. You can output the default subdivision distribution as JSON with the command `vali subdivisions --country "NO"` (or `vali subdivisions --country "NO" --text` to get a textual representation) Example:
```json
{
  "subdivisionDistribution": {
    "NO": {
      "NO-03": 200,
      "NO-15": 1500
    },
    "SE": {
      "SE-AB": 1000,
      "SE-AC": 1500,
      "SE-BD": 2000
    }
  }
}
```
Use `vali subdivisions --country "NO"` to output the default distribution, so you can copy it and make adjustments suitable to your map.

## Include or exclude subdivisions
Sometimes it can be handy to include or excludle a few subdivisions. F.ex. include only African Spain or exclude European Türkiye. Examples:
```json
{
  "subdivisionInclusions": {
    "ES": [
      "ES-CN",
      "ES-CE",
      "ES-ML"
    ]
  }
}
```
```json
{
  "subdivisionExclusions": {
    "TR": [
      "TR-22",
      "TR-39",
      "TR-59",
      "TR-34"
    ]
  }
}
```
## Location filtering
Locations can be filtered globally, per country or per subdivision.

## Properties
| Property              | Description
|-----------------|-----------------------------------------------
| Surface               | [OSM] Surface on the road within a 3m radius.
| Buildings10           | [OSM] Number of buildings within a 10m radius.
| Buildings25           | [OSM] Number of buildings within a 25m radius.
| Buildings100          | [OSM] Number of buildings within a 100m radius.
| Buildings200          | [OSM] Number of buildings within a 200m radius.
| Roads0                | [OSM] Number of roads connecting this point. Meant for selecting intersection locations.
| Roads10               | [OSM] Number of roads within a 10m radius.
| Roads25               | [OSM] Number of roads within a 25m radius.
| Roads50               | [OSM] Number of roads within a 50m radius.
| Roads100              | [OSM] Number of roads within a 100m radius.
| Roads200              | [OSM] Number of roads within a 200m radius.
| Tunnels10             | [OSM] Number of tunnels within a 10m radius. Locations are filtered on 0 tunnels by default.
| Tunnels200            | [OSM] Number of tunnels within a 200m radius.
| IsResidential         | [OSM] Whether any road is marked with `landuse="residential"` within a 100m radius.
| ClosestCoast          | [OSM] Distance to closest coastline in meters. Only works up to ~10 000 meters. Can be not set (null)
| ClosestLake           | [OSM] Distance to closest lake in meters. Only works up to ~10 000 meters. Can be not set (null).
| ClosestRiver          | [OSM] Distance to closest river in meters. Only works up to ~10 000 meters. Can be not set (null).
| ClosestRailway        | [OSM] Distance to closest railway in meters. Only works up to ~10 000 meters. Can be not set (null).
| HighwayType           | [OSM] Text representing the [highway type](https://wiki.openstreetmap.org/wiki/Key:highway#Highway). See road types below for possible values. If Roads0 is larger than 1 (location is at an intersection) HighwayType can have multiple values, aka `HighwayType eq 'Living_street' and HighwayType eq 'Residential'` is valid.
| WayId                 | [OSM] Text representing the way id(s) of the location combined with `|`. So a location with two ways might have `368544076|1030813440`. Useful when comparing locations in neighbor filters.
| Month                 | [Google] The month of the coverage. Integer.
| Year                  | [Google] The year of the coverage. Integer.
| Lat                   | [Google] The latitude of the location. Number.
| Lng                   | [Google] The longitude of the location. Number.
| Heading               | [Google] The default heading of the location. Corresponds to one of the "arrows". Number.
| DrivingDirectionAngle | [Google] The direction of the front of the Google car. Integer between 0 and 359.
| ArrowCount            | [Google] The number of arrows, mostly corresponds to the possible number of directions you can go in. Integer.
| Elevation             | [Google] Meters above sea level.
| DescriptionLength     | [Google] The length of the "description" field in Google's API. Used to distinguish (estimate) trekker coverage. If you want to include all coverage, add `DescriptionLength gt -1` to your respective location filter.
| IsScout               | [Google] Whether the location is marked as "scout" in the Google API. This corresponds to meaning "gen3 trekker" if that term makes more sense to you.
| CountryCode           | [Nominatim] Two character ISO 3166 country code.
| SubdivisionCode       | [Nominatim] ISO 3166-2 code for the subdivision. Mostly corresponding to data available at [ISO_3166-2](https://en.wikipedia.org/wiki/ISO_3166-2) (but with some exceptions.)
| County                | [Nominatim] Municipality/county name where available.

## Operators
| Operator        | Description            
|-----------------|------------------------
| eq              | Equal to
| neq             | Not equal to
| lt              | Less than
| lte             | Less than or equal
| gt              | Greater than
| gte             | Greater than or equal
| and             | Logical AND
| or              | Logical OR
| -               | Minus
| +               | Plus
| /               | Divide
| *               | Multiply
| modulo          | Modulo

## Road types
* Motorway
* Trunk
* Primary
* Secondary
* Tertiary
* Motorway_link
* Trunk_link
* Primary_link
* Secondary_link
* Tertiary_link
* Unclassified
* Residential
* Living_street
* Service
* Track
* Road

With these building blocks we can write queries/expressions to filter out certain locations. Some examples:

| Query                                             | Description              
|---------------------------------------------------|--------------------------
| `Lat gt 66.6`                                     | Above the polar circle.
| `Month gte 9 and Month lte 11`                    | Autumn coverage.
| `Year lte 2010`                                   | Gen2.
| `Roads200 eq 1 and Buildings200 eq 0`             | Rural coverage.
| `ClosestCoast lt 100`                             | Coastal coverage.
| `Buildings25 gte 3 and Buildings100 gte 6`        | Urban-ish coverage?
| `Surface eq 'gravel' or Surface eq 'fine_gravel'` | Gravel roads?

## Filter locations globally
```json
{
  "globalLocationFilter": "ClosestCoast lt 100 and Buildings100 gte 4"
}
```

## Filter locations per country
```json
{
  "countryLocationFilters": {
    "GR": "ClosestCoast lt 100 and Buildings100 gte 4"
  }
}
```

## Filter locations per subdivision
```json
{
  "subdivisionLocationFilters": {
    "GR": {
      "GR-M": "ClosestCoast lt 50",
      "GR-L": "ClosestCoast lt 100"
    }
  }
}
```

## Location preference filtering
Sometimes you want a certain percentage of your map to contain one type of locations. Vali can help you try and achieve that. It's called preference filters and can be applied globally/per country or per subdivision. The filtering is applied *after* any location filtering as described above. Example showing how to achieve 25 % locations on unpaved roads and filling in the rest with any location:
```json
{
  "globalLocationPreferenceFilters": [
    {
      "expression": "Surface neq 'paved' and Surface neq 'asphalt'",
      "percentage": 25,
      "fill": false
    },
    {
      "expression": "*",
      "percentage": null,
      "fill": true
    }
  ]
}
```
```json
{
  "countryLocationPreferenceFilters": {
    "ES": [
      {
        "expression": "Surface neq 'paved' and Surface neq 'asphalt'",
        "percentage": 25,
        "fill": false
      },
      {
        "expression": "*",
        "percentage": null,
        "fill": true
      }
    ]
  }
}
```

```json
{
  "subdivisionLocationPreferenceFilters": {
    "ES": {
      "ES-AN": [
        {
          "expression": "Surface neq 'paved' and Surface neq 'asphalt'",
          "percentage": 25,
          "fill": false
        },
        {
          "expression": "*",
          "percentage": null,
          "fill": true
        }
      ]
    }
  }
}
```
## Named expressions
Sometimes filter expressions can get complicated and it would be nice to have a way to split and be able to reuse them. Named expressions must start with `$$` and act as "variables" that can be referenced later in places where an expression is expected. Both inside other named expressions or in `globalLocationFilter` f.ex.
```json
{
  "namedExpressions": {
    "$$generallyRural": "Buildings200 eq 0 and Roads100 eq 1",
    "$$mainRoad": "HighwayType eq 'Motorway' or HighwayType eq 'Trunk' or HighwayType eq 'Primary'",
    "$$ruralSmallerRoad": "$$generallyRural and $$mainRoad eq false",
    "$$residential": "IsResidential or Buildings100 gt 4",
    "$$dirty": "Surface eq 'dirt' or Surface eq 'gravel' or Surface eq 'unpaved' or Surface eq 'ground' or Surface eq 'sand'"
  }
}
```
And later use the expressions:
```json
{
  "globalLocationFilter": "$$ruralSmallerRoad or ($$residential and $$dirty)"
}
```
## Proximity filters
If you have a `.csv` or `.json` file with locations (standard formats, f.ex. export from map-making.app), you can let vali generate locations in a radius around *any* of the locations given in the file.
```json
{
  "proximityFilter": {
    "radius": 4000,
    "locationsPath": "c:\\priv\\vali-maps\\city-centers.json"
  }
}
```
```json
{
  "countryProximityFilters": {
    "AR": {
      "radius": 2000,
      "locationsPath": "c:\\priv\\vali-maps\\argentina-city-centers.json"
    }
  }
}
```
```json
{
  "subdivisionProximityFilters": {
    "AR": {
      "AR-B": {
        "radius": 500,
        "locationsPath": "c:\\priv\\vali-maps\\buenos-aires.json"
      }
    }
  }
}
```
## Polygon filters
Vali can filter locations that are in *any* polygon inside a GeoJSON file (e.g. created using [geojson.io](https://geojson.io) or the [Map Polygon Tool](https://www.keene.edu/campus/maps/tool/)).
```json
{
  "polygonFilters": [
    {
      "polygonPath": "c:\\priv\\vali-maps\\home-city.json"
    }
  ]
}
```
```json
{
  "countryPolygonFilters": {
    "AR": [
      {
        "polygonPath": "c:\\priv\\vali-maps\\argentina-custom-area.json"
      }
    ]
  }
}
```
```json
{
  "subdivisionPolygonFilters": {
    "AR": {
      "AR-B": [
        {
          "polygonPath": "c:\\priv\\vali-maps\\buenos-aires-inner-city.json"
        }
      ]
    }
  }
}
```
It can also be specified, that the locations must *not* be in *any* polygon in the file:
```json
{
  "polygonFilters": [
    {
      "polygonPath": "c:\\priv\\vali-maps\\home-city.json",
      "insidePolygon": false
    }
  ]
}
```
Multiple filters can be added, which must all be met for a location to be selected. For example to generate locations that are in your home county but not your home city:
```json
{
  "polygonFilters": [
    {
      "polygonPath": "c:\\priv\\vali-maps\\home-county.json",
      "insidePolygon": true
    }
    {
      "polygonPath": "c:\\priv\\vali-maps\\home-city.json",
      "insidePolygon": false
    }
  ]
}
```
You can also use polygon filters in preference filters. Example where 40% of the locaitons are in your home city:
```json
{
  "expression": "*",
  "percentage": 40,
  "fill": false,
  "polygonFilters": [
    {
      "polygonPath": "c:\\priv\\vali-maps\\home-city.json"
    }
  ]
},
```
## Neighbor filters
Vali can filter locations based on number or percentage of locations nearby (neighbors) that satisfy given requirements. Examples:
* Must have at least 5 locations with surface 'gravel' within 500 meters.
* Must have no locations with highway type 'Living_street' within 1000 meters.
* Must have no locations either north/east/south/west within 300 meters.
* At most 10 percent of locations within 2000 meters have more than 5 buildings within 200m.

This can be practical for "amplifying" filter expressions by requiring the location itself to meet a certain criteria, but also all neighbors to fulfill the same criteria. Neighbor filters are also valid on location preference filters.

NB: this feature is quite resource intensive and may take a long time depending on the location filtering you apply and which countries the map generation includes.

### Neighbor filter properties/attributes
There are five properties you can set: Radius / Expression / Bound / Limit / CheckEachCardinalDirectionSeparately

* Radius: the number of meters the neighbor filter applies to.
* Expression: an expression describing what the criterias each location requires. It is mostly the same as "regular" Vali expressions, but in addition you can access properties from the current location through prefixing it with `current:` i.e. `current:Buildings200`.
* Bound: in combination with `Limit` describes the number of locations required. Valid values: `gte` (greater than or equal), `lte` (less than or equal), `all`, `none`, `some`, `percentage-gte`, `percentage-lte`. `all`, `none`, `some` cannot be used togheter with `Limit`. `gte`, `lte` requires `Limit` and the limit denotes the absolute number of locations required. `percentage-gte`, `percentage-lte` requires `Limit` and the limit denotes the percentage of locations required, with valid values ranging between 0 and 100.
* Limit: not always required. Absolute number or percentage of locations, depending on `Bound`.

### Neighbor filter examples
"Some residential locations nearby":
```json
{
  "neighborFilters": [{
    "radius": 500,
    "expression": "Buildings100 gt 6 or Buildings25 gte 2 or IsResidential",
    "limit": 10,
    "bound": "gte"
  }]
}
```
"Only rural locations nearby":
```json
{
  "neighborFilters": [{
    "radius": 400,
    "expression": "Buildings100 eq 0",
    "bound": "all"
  }]
}
```
"Terminus in one of four directions"
```json
{
  "neighborFilters": [{
    "radius": 300,
    "expression": "",
    "limit": 0,
    "bound": "none",
    "checkEachCardinalDirectionSeparately": true
  }]
}
```
"High number of locations nearby"
```json
{
  "neighborFilters": [{
    "radius": 500,
    "expression": "",
    "limit": 120,
    "bound": "gte"
  }]
}
```
"Top of the hill"
```json
{
  "neighborFilters": [{
    "radius": 500,
    "expression": "current:Elevation gt Elevation",
    "bound": "all"
  }]
}
```
"Close to county borders"
```json
{
  "neighborFilters": [{
    "radius": 150,
    "expression": "(current:County neq County)",
    "bound": "some"
  }]
}
```
"Unusual road type" Less than 5 percent of locations in a 1000m radius has the same HighwayType as this location.
```json
{
  "neighborFilters": [{
    "radius": 1000,
    "expression": "current:HighwayType eq HighwayType",
    "limit": 5,
    "bound": "percentage-lte"
  }]
}
```
## Location output adjustments
You can adjust the zoom on locations with `globalZoom` (range 0-3.6) and set the pitch with `globalPitch` (range -90 to 90). Heading can be set with `globalHeadingExpression` or `countryHeadingExpressions`. Examples:
```json
{
  "output": {
    "globalHeadingExpression": "DrivingDirectionAngle + 90"
  }
}
```
```json
{
  "output": {
    "countryHeadingExpressions": {
      "FR": "DrivingDirectionAngle + 90",
      "GB": "DrivingDirectionAngle + 270"
    }
  }
}
```
```json
{
  "output": {
    "globalHeadingExpression": "",
    "globalPitch": -30,
    "globalZoom": 2.4
  }
}
```

## Pano verification and selection
Vali offers functionality for verifying each location against Google streetview APIs to ensure no unofficial coverage and/or to select specific/non-default panorama ids. When `panoVerificationStrategy` is not empty, each location is checked and the specified strategy for pano selection is applied. The resulting map will then have locations with `panoId` set. Example:
```json
{
  "countryCodes": [
    "LU"
  ],
  "distributionStrategy": {
    "key": "FixedCountByMaxMinDistance",
    "locationCountGoal": 15000,
    "minMinDistance": 50,
    "treatCountriesAsSingleSubdivision": []
  },
  "globalLocationFilter": "",
  "output": {
    "panoVerificationStrategy": "Newest"
  }
}
```
Available strategies: Newest, Random, RandomNotNewest, RandomAvoidNewest, RandomNotOldest, RandomAvoidOldest, SecondNewest, Oldest, SecondOldest

## Country code expansions
Most places that accept country code, can also accept special keywords that will expand into multiple country codes. Possible values:
* \* -> All countries.
* europe -> Countries in Europe. If you only specify europe, inclusions and exclusions will also be filled when generating maps.
* asia -> Countries in Asia. If you only specify asia, inclusions and exclusions will also be filled when generating maps.
* africa -> Countries in Africa. If you only specify africa, inclusions and exclusions will also be filled when generating maps.
* southamerica -> Countries in South America.
* northamerica -> Countries in North America. If you only specify northamerica, inclusions and exclusions will also be filled when generating maps.
* oceania -> Countries in oceania. If you only specify oceania, inclusions and exclusions will also be filled when generating maps.
* lefthandtraffic -> Countries that drive on the left side of the road.
* righthandtraffic -> Countries that drive on the right side of the road.

## Country distributions from famous maps
Default is acw. Possible values for distributionStrategy->countryDistributionFromMap:
* aarw
* aaw
* acw
* abw
* aiw
* proworld
* aow
* rainboltworld
* geotime
* lerg

## Tagging locations
You can tag locations in `output.locationTags`. Available tags:
* CountryCode
* SubdivisionCode
* County
* Surface
* Year
* Month
* YearMonth
* Elevation
* ArrowCount
* DescriptionLength
* IsScout
* Season
* HighwayType
* WayId
* IsResidential
* Elevation500 - elevation in buckets of 500 meters. Elevation1000/Elevation10 etc.
* Buildings10 - exact number of buildings within 10 meters.
* Buildings25 - exact number of buildings within 25 meters.
* Buildings100 - exact number of buildings within 100 meters.
* Buildings200 - exact number of buildings within 200 meters.
* Roads0 - exact number of roads within 0 meters.
* Roads10 - exact number of roads within 10 meters.
* Roads25 - exact number of roads within 25 meters.
* Roads50 - exact number of roads within 50 meters.
* Roads100 - exact number of roads within 100 meters.
* Roads200 - exact number of roads within 200 meters.
* Buildings10-5 - number of buildings within 10 meters in buckets of 5, i.e. 0-4 buildings, 5-9 buildings etc.
* Buildings25-5 - number of buildings within 25 meters in buckets of 5, i.e. 0-4 buildings, 5-9 buildings etc.
* Buildings100-10 - number of buildings within 100 meters in buckets of 10, i.e. 0-9 buildings, 10-19 buildings etc.
* Buildings200-20 - number of buildings within 200 meters in buckets of 20, i.e. 0-19 buildings, 20-39 buildings etc.
* Roads0-3 - number of roads within 0 meters in buckets of 3, i.e. 0-2 roads, 3-5 roads etc.
* Roads10-5 - number of roads within 10 meters in buckets of 5, i.e. 0-4 roads, 5-9 roads etc.
* Roads25-5 - number of roads within 25 meters in buckets of 5, i.e. 0-4 roads, 5-9 roads etc.
* Roads50-10 - number of roads within 50 meters in buckets of 10, i.e. 0-9 roads, 10-19 roads etc.
* Roads100-10 - number of roads within 100 meters in buckets of 10, i.e. 0-9 roads, 10-19 roads etc.
* Roads200-20 - number of roads within 200 meters in buckets of 10, i.e. 0-19 roads, 20-39 roads etc.
* ClosestCoast-100 - distance to coast in buckets of 100 meters, i.e. 0-99 meters, 100-199 meters etc.
* ClosestLake-500 - distance to lake in buckets of 500 meters, i.e. 0-499 meters, 500-999 meters etc.
* ClosestRiver-2000 - distance to river in buckets of 2000 meters, i.e. 0-1999 meters, 2000-3999 meters etc.
* ClosestRailway-25 - distance to railway in buckets of 25 meters, i.e. 0-24 meters, 25-49 meters etc.
* DrivingDirectionAngle-45 - angle of driving direction in buckets of 45 degrees, i.e. 0-44 degrees, 45-89 degrees etc.

# Full examples
With all the building blocks described above we can start creating real, serious maps.

### A very skewed Europe
```json
{
  "countryCodes": [
    "europe"
  ],
  "distributionStrategy": {
    "key": "FixedCountByMaxMinDistance",
    "locationCountGoal": 25000,
    "minMinDistance": 200,
    "countryDistributionFromMap": "acw"
  },
  "output": {
    "countryHeadingExpressions": {
      "lefthandtraffic": "DrivingDirectionAngle + 270",
      "righthandtraffic": "DrivingDirectionAngle + 90"
    }
  }
}
```
### A coastal Asia
```json
{
  "countryCodes": [
    "asia"
  ],
  "distributionStrategy": {
    "key": "FixedCountByMaxMinDistance",
    "locationCountGoal": 25000,
    "minMinDistance": 200,
    "countryDistributionFromMap": "acw"
  },
  "globalLocationFilter": "ClosestCoast lt 100"
}
```
### An Arbitrary Rural World
```json
{
  "countryCodes": [
    "*"
  ],
  "distributionStrategy": {
    "key": "FixedCountByMaxMinDistance",
    "locationCountGoal": 111000,
    "minMinDistance": 50,
    "countryDistributionFromMap": "aarw",
    "treatCountriesAsSingleSubdivision": ["IL,UG,AE,QA,TN,AD,CW,DO,GH,GL,JO,KG,LA,MK,MT,SN,SG,TW"]
  },
  "globalLocationFilter": "Buildings200 eq 0 and Roads200 eq 1",
  "globalLocationPreferenceFilters": [
    {
      "expression": "Surface eq 'dirt' or Surface eq 'earth' or Surface eq 'fine_gravel' or Surface eq 'grass' or Surface eq 'gravel' or Surface eq 'ground' or Surface eq 'sand'",
      "percentage": 30,
      "locationTag": "dirty",
      "minMinDistance": 500
    },
    {
      "expression": "*",
      "percentage": null,
      "fill": true,
      "locationTag": "fill"
    }
  ],
  "panoIdCountryCodes": [],
  "output": {
    "locationTags": [
      "Year",
      "Month",
      "Season",
      "Elevation500"
    ]
  }
}
```
### An Antenna Focused Gen3 Bulgaria
```json
{
  "countryCodes": [
    "BG"
  ],
  "distributionStrategy": {
    "key": "FixedCountByMaxMinDistance",
    "locationCountGoal": 12000,
    "minMinDistance": 200
  },
  "globalLocationFilter": "ArrowCount eq 2 and Year gt 2011 and Year lt 2019 and DrivingDirectionAngle neq 0",
  "output": {
    "globalHeadingExpression": "DrivingDirectionAngle + 180",
    "globalPitch": -30
  }
}
```
### A Mostly Non-urban Terminus Hungary
```json
{
  "countryCodes": [
    "HU"
  ],
  "distributionStrategy": {
    "key": "FixedCountByMaxMinDistance",
    "locationCountGoal": 5000,
    "minMinDistance": 200
  },
  "globalLocationFilter": "ArrowCount eq 1 and Buildings100 eq 0",
  "neighborFilters": [{
    "radius": 200,
    "expression": "",
    "bound": "none",
    "checkEachCardinalDirectionSeparately": true
  }],
  "output": {
    "panoVerificationStrategy": "Newest",
    "panoVerificationExpression": "ArrowCount eq 1",
    "globalHeadingExpression": "Heading + 180"
  }
}
```
### A Degenerated Oceania
```json
{
  "countryCodes": [
    "oceania"
  ],
  "distributionStrategy": {
    "key": "FixedCountByMaxMinDistance",
    "locationCountGoal": 10000,
    "minMinDistance": 25
  },
  "countryDistribution": {
    "AS": 10,
    "AU": 10,
    "GU": 10,
    "MP": 10,
    "NZ": 10,
    "PN": 10,
    "US": 10,
    "UM": 10
  }
}
```
### An Off-grid US
```json
{
  "countryCodes": [
    "US"
  ],
  "distributionStrategy": {
    "key": "FixedCountByMaxMinDistance",
    "locationCountGoal": 80000,
    "minMinDistance": 500
  },
  "globalLocationFilter": "(DrivingDirectionAngle gt 10 and Heading gt 10 and DrivingDirectionAngle lt 80 and Heading lt 80) or (DrivingDirectionAngle gt 100 and Heading gt 100 and DrivingDirectionAngle lt 170 and Heading lt 170) or (DrivingDirectionAngle gt 190 and Heading gt 190 and DrivingDirectionAngle lt 260 and Heading lt 260) or (DrivingDirectionAngle gt 280 and Heading gt 280 and DrivingDirectionAngle lt 350 and Heading lt 350)"
}
```

# Please remember
* Location count is not everything. The same goes for location spread and lack of clustering.
* Not all roads with coverage are included. It's ok. More will be added. And there are more than 100 million possible locations.
* Data quality can vary wildly, especially when it comes to OpenStreetMap. While that can be frustrating, it's just the way it is. Go contribute to make things better.
* Computer generating maps can be great, but it will not fully replace handpicked maps.
* This is a complex piece of software. Do not expect zero bugs.

# Potentially asked questions
> Can you turn it into a webpage?

I don't think so. Currently it's a very resource intensive program. Downloading/processing 15 GB of data is not something a web site is suitable for.

> Why is Vali a command line application?

Because I have very little interest in creating a user interface for it, especially before it gains any kind of popularity.

> Can you include more locations/roads in country X?

Maybe. The best chance for that is if you generate a JSON or csv with "Filter by minimum distance from locations" set to 1 km with as many locations as possible in country X and send it to me on discord. Only one country per file.

> Why are there (relatively) few locations on straight stretches of road?

Each location in the location pool corresponds to an OSM "node". Nodes exist to describe roads and other features, so naturally there will be more locations in places with curves, buildings, other roads etc.

# Run in Docker
Create a `Dockerfile` with the following content:

```Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0
ENV PATH="${PATH}:/root/.dotnet/tools"
```
then bulid the image.
```
docker build . -t vali-image
```
run it
```
docker run -it vali-image
```
and start using Vali as normal
```
dotnet tool install --global vali
...
```