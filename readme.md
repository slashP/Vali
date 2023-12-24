# Vali - create GeoGuessr locations like a pro
The creator of *An Arbitrary World*,  *An Arbitrary Rural World*, *Dirty World*, *IntersectionGuessr* ++ brings you Vali - the next evolution in computer generating GeoGuessr maps. With this tool **you** can create "An Arbitrary Rural Southern Europe", "Coastal Sri Lanka", "Skewed Africa" or something brand new. More than 100 million possible locations are awaiting - be creative.

# First time setup
* Install [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
* Install [Windows Terminal](https://learn.microsoft.com/en-us/windows/terminal/install#install) (or use another terminal).
* Install [Visual Studio Code](https://code.visualstudio.com/download) or use another text editor suitable for editing JSON files.
* Open Windows Terminal and run `dotnet tool install -g vali`

# Getting started
* `vali download` - Download data for one or more countries.
* `vali create-file` - Create a JSON file to start off.
* Edit JSON file.
* `vali generate --file "NO.json"` - Generate locations based on the JSON file specified.

# Commands
* `vali download` - Download data. You will get asked which countries you want to download data for.
* `vali generate --file norway.json` - Generate locations based on specification in `norway.json`.
* `vali subdivisions --country "ES"` - Export default subdivision distribution data for Spain as JSON.
* `vali subdivisions --country "ES" --text` - Export default subdivision distribution data for Spain as text.
* `vali countries --country "ES,FR,IT"` - Export default country distribution data for Spain as JSON.
* `vali countries --country "ES,FR,IT" --distribution "abw"` - Export country distribution data for Spain, France and Italy as specified by "A Balanced World" as JSON.
* `vali countries --country "*" --distribution "aiw" --text` - Export country distribution data for all countries as specified by "An Improved World" as text.
* `vali report --country "NO" --prop "County"` - Export counties/municipalities data for Norway.
* `vali report --country "BE" --prop "Year"` - Export coverage year data for Belgium.

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
| Roads10               | [OSM] Number of roads within a 10m radius.
| Roads25               | [OSM] Number of roads within a 25m radius.
| Roads50               | [OSM] Number of roads within a 50m radius.
| Roads100              | [OSM] Number of roads within a 100m radius.
| Roads200              | [OSM] Number of roads within a 200m radius.
| Tunnels10             | [OSM] Number of tunnels within a 10m radius. Locations are filtered on 0 tunnels by default.
| Tunnels200            | [OSM] Number of tunnels within a 200m radius.
| IsResidential         | [OSM] Whether any road is marked with `landuse="residential"` within a 100m radius.
| ClosestCoast          | [OSM] Distance to closest coastline in meters. Only works up to ~10 000 meters. Can be not set (null)
| Month                 | [Google] The month of the coverage. Integer.
| Year                  | [Google] The year of the coverage. Integer.
| Lat                   | [Google] The latitude of the location. Number.
| Lng                   | [Google] The longitude of the location. Number.
| Heading               | [Google] The default heading of the location. Corresponds to one of the "arrows". Number.
| DrivingDirectionAngle | [Google] The direction of the front of the Google car. Integer between 0 and 359.
| ArrowCount            | [Google] The number of arrows, mostly corresponds to the possible number of directions you can go in. Integer.
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
## Location output adjustments
You can adjust the zoom on locations with `globalZoom` (range 0-3.6) and set the pitch with `globalPitch` (range -90 to 90). Heading can be set with `globalHeadingExpression` or `countryHeadingExpressions`. Examples:
```json
{
  "globalHeadingExpression": "DrivingDirectionAngle + 90"
}
```
```json
{
  "countryHeadingExpressions": {
    "FR": "DrivingDirectionAngle + 90",
    "GB": "DrivingDirectionAngle + 270"
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
    "defaultDistribution": "acw"
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
    "defaultDistribution": "acw"
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
    "defaultDistribution": "aarw",
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
  "locationTags": [
    "Year",
    "Month",
    "Season"
  ]
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
  "globalLocationFilter": "ArrowCount eq 1 and Buildings100 eq 0"
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

I don't think so. Currently it's a very resource intensive program. Downloading and processing 15 GB of data is not something the web is suitable for.

> Why is Vali a command line application?

Because I have very little interest in creating a user interface for it, especially before it gains any kind of popularity.

> Can you include more locations/roads in country X?

Maybe. The best chance for that is if you generate a JSON or csv with "Filter by minimum distance from locations" set to 1 km with as many locations as possible in country X and send it to me on discord. Only one country per json.

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
vali download
...
```