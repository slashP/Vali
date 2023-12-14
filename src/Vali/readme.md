### Vali - create GeoGuessr locations like a pro

* Install .NET 8 runtime.
* dotnet tool install -g vali

### Commands
* `vali download --country "FR"` - Download data for France.
* `vali download` - Download data for all countries.
* `vali generate --file norway.json` - Generate locations based on specification in `norway.json`.
* `vali subdivisions --country "ES"` - Export default subdivision distribution data for Spain as JSON.
* `vali subdivisions --country "ES" --text` - Export default subdivision distribution data for Spain as text.
* `vali counties --country "NO"` - Export counties/municipalities data for Norway.

### Selecting countries
You need to specify which countries you want to include in the `countryCodes` array. Example:
```json
{
  "countryCodes": ["EC", "CO", "PE", "BO"]
}
```
### Distribution strategy
You can select between different strategies for distributing locations. Option 1 is `FixedCountByMaxMinDistance` where you specify how many locations you want in total and each subdivision will get their locations spread as far away from each other as possible. You can set a minimum minimum distance to set a lower limit on how close each location can be to each other. Example:
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
### Specific country distribution
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
### Specific subdivision distribution(s)
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
### Include or exclude subdivisions
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
### Location filtering
Locations can be filtered globally, per country or per subdivision.

#### Properties
| Property        | Description
|-----------------|-----------------------------------------------
| Surface         | [OSM] Surface on the road within a 3m radius.
| Buildings10     | [OSM] Number of buildings within a 10m radius.
| Buildings25     | [OSM] Number of buildings within a 25m radius.
| Buildings100    | [OSM] Number of buildings within a 100m radius.
| Buildings200    | [OSM] Number of buildings within a 200m radius.
| Roads0          | [OSM] Number of roads at the location. (experimental)
| Roads10         | [OSM] Number of roads within a 10m radius.
| Roads25         | [OSM] Number of roads within a 25m radius.
| Roads50         | [OSM] Number of roads within a 50m radius.
| Roads100        | [OSM] Number of roads within a 100m radius.
| Roads200        | [OSM] Number of roads within a 200m radius.
| Tunnels10       | [OSM] Number of tunnels within a 10m radius. Locations are filtered on 0 tunnels by default.
| Tunnels200      | [OSM] Number of tunnels within a 200m radius. (experimental)
| IsResidential   | [OSM] Whether any road is marked with `landuse="residential"` within a 100m radius.
| ClosestCoast    | [OSM] Distance to closest coastline in meters. Only works up to ~10 000 meters. Can be not set (null)
| Month           | [Google] The month of the coverage. Integer.
| Year            | [Google] The year of the coverage. Integer.
| Lat             | [Google] The latitude of the location. Number.
| Lng             | [Google] The longitude of the location. Number.
| Heading         | [Google] The default heading of the location pointing towards the road.. Number.
| CountryCode     | [Nominatim] Two character ISO 3166 country code.
| SubdivisionCode | [Nominatim] ISO 3166-2 code for the subdivision. Mostly corresponding to data available at [ISO_3166-2](https://en.wikipedia.org/wiki/ISO_3166-2) (but with some exceptions.)
| County          | [Nominatim] Municipality/county name where available.

#### Operators
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

#### Filter locations globally
```json
{
  "globalLocationFilters": [
    "ClosestCoast lt 100 and Buildings100 gte 4"
  ]
}
```

#### Filter locations per country
```json
{
  "countryLocationFilters": {
    "GR": [
      "ClosestCoast lt 100 and Buildings100 gte 4"
    ]
  }
}
```

#### Filter locations per subdivision
```json
{
  "countryLocationFilters": {
    "GR": {
      "GR-M": [
        "ClosestCoast lt 50"
      ],
      "GR-L": [
        "ClosestCoast lt 100"
      ]
    }
  }
}
```

### Location preference filtering.
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

#### Full examples
With all the building blocks described above we can start creating real, serious maps.
