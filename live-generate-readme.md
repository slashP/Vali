## Live generate
`vali live-generate --file "C:\dev\priv\vali-maps\live.json"`

### Properties
| Property              | Description
|-----------------|-----------------------------------------------
| lat                   | Latitude
| lng                   | Longitude
| countryCode           | Two character ISO 3166 country code.
| arrowCount            | The number of arrows, mostly corresponds to the possible number of directions you can go in. Integer.
| descriptionLength     | The length of the "description" field in Google's API. Used to distinguish (estimate) trekker coverage.
| drivingDirectionAngle | The direction of the front of the Google car. Integer between 0 and 359.
| heading               | The default heading of the location. Corresponds to one of the "arrows". Number.
| month                 | The month of the coverage. Integer.
| year                  | The year of the coverage. Integer.
| isScout               | Whether the location is marked as "scout" in the Google API. This corresponds to meaning "gen3 trekker" if that term makes more sense to you.
| resolutionHeight      | Number of vertical pixels in the panorama. 8192: gen4, 6656: gen3/gen2/badcam.
| subdivision           | The name of the subdivision according to Google's API.
| panoramaCount         | The number of images available at this location.

### Structure
```
{
  "countries": {
   "US": 2000
  },
  "locationTags": ["YearMonth", "Elevation"], // output tags
  "fromDate": "2024-10",
  "toDate": "2025-05",
  "headingMode": "", // empty or "Random"
  "headingDelta": 180, // number to add to default heading
  "pitchMode": "", // empty or "Random"
  "pitch": 0, // default pitch -90 to 90.
  "randomPitchMin": 0, // if "pitchMode" is Random, specify min value.
  "randomPitchMax": 0, // if "pitchMode" is Random, specify max value.
  "zoomMode": "", // empty or "Random"
  "zoom": 0, // default zoom 0.435 to 3.36.
  "randomZoomMin": 0, // if "zoomMode" is Random, specify min value.
  "randomZoomMax": 0, // if "zoomMode" is Random, specify max value.
  "parallelRequests": 200, // number of parallel requests. Defaults to 100.
  "boxPrecision": 4, // number from 0-9 specifying how wide search should be from known points on roads in the world. Higher number is wider. Defaults to 4.
  "radius": 200, // radius in meters to search for panoramas within. Defaults to 100.
  "locationFilter": "resolutionHeight eq 8192" // vali expression to filter locations. See available 'properties' above and syntax/operators on the main vali documentation page.
  "panoSelectionStrategy": "", // empty or "Newest" / "Random" / "RandomNotNewest" / "RandomAvoidNewest" / "RandomNotOldest" / "RandomAvoidOldest" / "SecondNewest" / "Oldest" / "SecondOldest" / "YearMonthPeriod200901201012"
  "badCamStrategy": "", // empty or "DisallowInCountriesWithDecentOtherCoverage" / "AllowForAll"
  "acceptedCoverage": "", // empty or "Official" / "Unofficial" / "All"
  "rejectLocationsWithoutDescription": true, // whether to reject locations with no description. Defaults to true.
  "batchSize": 10000, // number of requests done between each filtering/updating of progress bar. Defaults to 10000.
  "geoJsonFiles": [], // list of files specifying area to search within. example: ["c:\\priv\\vali-maps\\florida.geojson"]
  "distribution": { // will generate two files, one with all locations and one with a distributed subset of locations.
    "minMinDistance": 5000, // minimum distance between locations in meters.
    "overshootFactor": 4 // multiplies location goal count with this number as total location count before doing location distribution
  }
}
```

### Examples

Basic Norway.
```json
{
  "countries": {
   "NO": 2000
  }
}
```
Tags.
```json
{
  "countries": {
   "IT": 2000
  },
  "locationTags": ["YearMonth", "Elevation-500", "ResolutionHeight", "Season"]
}
```
High elevation 2024.
```json
{
  "countries": {
   "FR": 2000
  },
  "locationFilter": "elevation gt 1500 and year gte 2024"
}
```
New roads Colombia.
```json
{
  "countries": {
   "CO": 2000
  },
  "fromDate": "2024-08",
  "locationFilter": "panoramaCount eq 1"
}
```
India gen4.
```json
{
  "countries": {
   "IN": 2000
  },
  "locationFilter": "resolutionHeight eq 8192"
}
```
A random pan and zoom Netherlands.
```json
{
  "countries": {
   "NL": 2000
  },
  "headingMode": "Random",
  "pitchMode": "Random",
  "zoomMode": "Random",
  "parallelRequests": 200
}
```
Terminus South Africa.
```json
{
  "countries": {
   "ZA": 2000
  },
  "locationFilter": "arrowCount eq 1",
  "headingDelta": 180,
  "parallelRequests": 200
}
```
Gen2 Sweden.
```json
{
  "countries": {
   "SE": 2000
  },
  "panoSelectionStrategy": "Oldest",
  "toDate": "2010-11"
}
```
Unofficial Namibia.
```json
{
  "countries": {
   "NA": 2000
  },
  "acceptedCoverage": "Unofficial",
  "rejectLocationsWithoutDescription": false
}
```
Well distributed map.
```json
{
  "countries": {
   "BE": 2000
  },
  "distribution": {
    "minMinDistance": 5000,
    "overshootFactor": 4
  }
}
```
GeoTime Nordics.
```json
{
  "countries": {
   "NO": 2000,
   "SE": 2000,
   "DK": 1500,
   "IS": 1000,
   "FI": 2000,
   "FO": 500
  },
  "distribution": {
    "minMinDistance": 10000,
    "overshootFactor": 4
  }
}
```
Gen2 Brazil.
```json
{
  "countries": {
    "BR": 1000
  },
  "parallelRequests": 600,
  "panoSelectionStrategy": "YearMonth200801201009"
}
```
Exhaustive Oman.
```json
{
  "countries": {
    "OM": 10000000
  },
  "parallelRequests": 600,
  "checkLinkedPanoramas": true
}
```