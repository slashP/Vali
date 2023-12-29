using Shouldly;
using Vali.Core.Validation;
using Xunit;

namespace Vali.Core.Tests;

public class GenerateFileValidatorTests
{
    [Fact]
    public void CountryCodes()
    {
        var json = """
                   {"countryCodes": ""}
                   """;
        var valid = """
                   {"countryCodes": ["europe"]}
                   """;
        GenerateFileValidator.HumanReadableError(json).ShouldBe(@"countryCodes must be an array. F.ex. [""IT""]");
        GenerateFileValidator.HumanReadableError(valid).ShouldBeNull();
    }

    [Fact]
    public void SubdivisionInclusions()
    {
        var json1 = """
                   {"subdivisionInclusions": ""}
                   """;
        var json2 = """
                    {"subdivisionInclusions": [{
                      "FR-11": ""
                    }]}
                    """;
        var valid1 = """
                    {"subdivisionInclusions": {
                      "FR": ["FR-11"]
                    }}
                    """;
        var valid2 = """
                    {"subdivisionInclusions": {}}
                    """;

        var errorMessage = """
                           subdivisionInclusions does not have the correct format. Correct example:
                           {
                             "FR": ["FR-11"]
                           }
                           """;
        GenerateFileValidator.HumanReadableError(json1).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json2).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(valid1).ShouldBeNull();
        GenerateFileValidator.HumanReadableError(valid2).ShouldBeNull();
    }

    [Fact]
    public void SubdivisionExclusions()
    {
        var json1 = """
                    {"subdivisionExclusions": ""}
                    """;
        var json2 = """
                    {"subdivisionExclusions": [{
                      "FR-11": ""
                    }]}
                    """;
        var valid1 = """
                    {"subdivisionExclusions": {
                      "FR": ["FR-11"]
                    }}
                    """;
        var valid2 = """
                    {"subdivisionExclusions": {}}
                    """;
        var errorMessage = """
                           subdivisionExclusions does not have the correct format. Correct example:
                           {
                             "FR": ["FR-11"]
                           }
                           """;
        GenerateFileValidator.HumanReadableError(json1).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json2).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(valid1).ShouldBeNull();
        GenerateFileValidator.HumanReadableError(valid2).ShouldBeNull();
    }

    [Fact]
    public void CountryDistribution()
    {
        var json1 = """
                    {"countryDistribution": ""}
                    """;
        var json2 = """
                    {"countryDistribution": []}
                    """;
        var json3 = """
                    {"countryDistribution": {
                      "FR": 12.2
                    }}
                    """;
        var valid1 = """
                    {"countryDistribution": {
                      "FR": 12
                    }}
                    """;
        var valid2 = """
                    {"countryDistribution": {}}
                    """;


        var errorMessage = """
                           countryDistribution does not have the correct format. NB: Only integers. Correct example:
                           {
                             "FR": 12,
                             "IT": 10
                           }
                           """;
        GenerateFileValidator.HumanReadableError(json1).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json2).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json3).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(valid1).ShouldBeNull();
        GenerateFileValidator.HumanReadableError(valid2).ShouldBeNull();
    }

    [Fact]
    public void SubdivisionDistribution()
    {
        var json1 = """
                    {"subdivisionDistribution": ""}
                    """;
        var json2 = """
                    {"subdivisionDistribution": []}
                    """;
        var json3 = """
                    {"subdivisionDistribution": {
                      "FR": 12.2
                    }}
                    """;
        var valid1 = """
                    {"subdivisionDistribution": { }}
                    """;
        var valid2 = """
                    {"subdivisionDistribution": {
                      "FR": {
                        "FR-11": 25
                      }
                    }}
                    """;

        var errorMessage = """
                           subdivisionDistribution does not have the correct format. NB: Only integers. Correct example:
                           {
                             "FR": {
                               "FR-11": 12,
                               "FR-28": 25
                             }
                           }
                           """;
        GenerateFileValidator.HumanReadableError(json1).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json2).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json3).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(valid1).ShouldBeNull();
        GenerateFileValidator.HumanReadableError(valid2).ShouldBeNull();
    }

    [Fact]
    public void DistributionStrategy()
    {
        var json1 = """
                    {"distributionStrategy": ""}
                    """;
        var json2 = """
                    {"distributionStrategy": []}
                    """;
        var errorMessage = """
                           distributionStrategy does not have the correct format. Correct example:
                           {
                             "key": "FixedCountByMaxMinDistance",
                             "locationCountGoal": 10000,
                             "minMinDistance": 25
                           }
                           """;
        GenerateFileValidator.HumanReadableError(json1).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json2).ShouldBe(errorMessage);
    }

    [Fact]
    public void DistributionStrategy_LocationCountGoal()
    {
        var json1 = """
                    {"distributionStrategy": {
                      "locationCountGoal": ""
                    }}
                    """;
        var json2 = """
                    {"distributionStrategy": {
                      "locationCountGoal": {}
                    }}
                    """;
        var json3 = """
                    {"distributionStrategy": {
                      "locationCountGoal": []
                    }}
                    """;
        var json4 = """
                    {"distributionStrategy": {
                      "locationCountGoal": 23.2
                    }}
                    """;
        var valid = """
                    {"distributionStrategy": {
                      "locationCountGoal": 12
                    }}
                    """;
        var errorMessage = """
                           distributionStrategy.locationCountGoal must be an integer. Correct example:
                           {
                             "key": "FixedCountByMaxMinDistance",
                             "locationCountGoal": 10000,
                             "minMinDistance": 25
                           }
                           """;
        GenerateFileValidator.HumanReadableError(json1).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json2).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json3).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json4).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(valid).ShouldBeNull();
    }

    [Fact]
    public void DistributionStrategy_MinMinDistance()
    {
        var json1 = """
                    {"distributionStrategy": {
                      "minMinDistance": ""
                    }}
                    """;
        var json2 = """
                    {"distributionStrategy": {
                      "minMinDistance": {}
                    }}
                    """;
        var json3 = """
                    {"distributionStrategy": {
                      "minMinDistance": []
                    }}
                    """;
        var json4 = """
                    {"distributionStrategy": {
                      "minMinDistance": 23.2
                    }}
                    """;
        var valid = """
                    {"distributionStrategy": {
                      "minMinDistance": 12
                    }}
                    """;
        var errorMessage = """
                           distributionStrategy.minMinDistance must be an integer. Correct example:
                           {
                             "key": "FixedCountByMaxMinDistance",
                             "locationCountGoal": 10000,
                             "minMinDistance": 25
                           }
                           """;
        GenerateFileValidator.HumanReadableError(json1).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json2).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json3).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json4).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(valid).ShouldBeNull();
    }

    [Fact]
    public void DistributionStrategy_FixedMinDistance()
    {
        var json1 = """
                    {"distributionStrategy": {
                      "fixedMinDistance": ""
                    }}
                    """;
        var json2 = """
                    {"distributionStrategy": {
                      "fixedMinDistance": {}
                    }}
                    """;
        var json3 = """
                    {"distributionStrategy": {
                      "fixedMinDistance": []
                    }}
                    """;
        var json4 = """
                    {"distributionStrategy": {
                      "fixedMinDistance": 23.2
                    }}
                    """;
        var valid = """
                    {"distributionStrategy": {
                      "fixedMinDistance": 12
                    }}
                    """;
        var errorMessage = """
                           distributionStrategy.fixedMinDistance must be an integer. Correct example:
                           {
                             "key": "MaxCountByFixedMinDistance",
                             "locationCountGoal": 10000,
                             "fixedMinDistance": 25
                           }
                           """;
        GenerateFileValidator.HumanReadableError(json1).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json2).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json3).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json4).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(valid).ShouldBeNull();
    }

    [Fact]
    public void DistributionStrategy_TreatCountriesAsSingleSubdivision()
    {
        var json1 = """
                    {"distributionStrategy": {
                      "treatCountriesAsSingleSubdivision": ""
                    }}
                    """;
        var json2 = """
                    {"distributionStrategy": {
                      "treatCountriesAsSingleSubdivision": {}
                    }}
                    """;
        var json3 = """
                    {"distributionStrategy": {
                      "treatCountriesAsSingleSubdivision": [12]
                    }}
                    """;
        var json4 = """
                    {"distributionStrategy": {
                      "treatCountriesAsSingleSubdivision": 23.2
                    }}
                    """;
        var valid1 = """
                    {"distributionStrategy": {
                      "treatCountriesAsSingleSubdivision": []
                    }}
                    """;
        var valid2 = """
                     {"distributionStrategy": {
                       "treatCountriesAsSingleSubdivision": ["AL"]
                     }}
                     """;

        var errorMessage = """
                           distributionStrategy.treatCountriesAsSingleSubdivision must be an array of strings. Correct example:
                           {
                             "key": "FixedCountByMaxMinDistance",
                             "locationCountGoal": 10000,
                             "minMinDistance": 25,
                             "treatCountriesAsSingleSubdivision": ["GI", "JE"]
                           }
                           """;
        GenerateFileValidator.HumanReadableError(json1).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json2).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json3).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json4).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(valid1).ShouldBeNull();
        GenerateFileValidator.HumanReadableError(valid2).ShouldBeNull();
    }


    [Fact]
    public void DistributionStrategy_DefaultDistribution()
    {
        var json1 = """
                    {"distributionStrategy": {
                      "countryDistributionFromMap": 12
                    }}
                    """;
        var json2 = """
                    {"distributionStrategy": {
                      "countryDistributionFromMap": {}
                    }}
                    """;
        var json3 = """
                    {"distributionStrategy": {
                      "countryDistributionFromMap": []
                    }}
                    """;
        var json4 = """
                    {"distributionStrategy": {
                      "countryDistributionFromMap": 23.2
                    }}
                    """;
        var valid1 = """
                    {"distributionStrategy": {
                      "countryDistributionFromMap": "acw"
                    }}
                    """;
        var valid2 = """
                     {"distributionStrategy": {
                       "countryDistributionFromMap": null
                     }}
                     """;
        var errorMessage = """
                           distributionStrategy.countryDistributionFromMap must be a string. Correct example:
                           {
                             "key": "FixedCountByMaxMinDistance",
                             "locationCountGoal": 10000,
                             "minMinDistance": 25,
                             "countryDistributionFromMap": "acw"
                           }
                           """;
        GenerateFileValidator.HumanReadableError(json1).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json2).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json3).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json4).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(valid1).ShouldBeNull();
        GenerateFileValidator.HumanReadableError(valid2).ShouldBeNull();
    }

    [Fact]
    public void DistributionStrategy_GlobalLocationFilter()
    {
        var json1 = """
                    {"globalLocationFilter": 12}
                    """;
        var json2 = """
                    {"globalLocationFilter": {}}
                    """;
        var json3 = """
                    {"globalLocationFilter": []}
                    """;
        var json4 = """
                    {"globalLocationFilter": 23.2}
                    """;
        var valid1 = """
                     {"globalLocationFilter": ""}
                     """;
        var valid2 = """
                     {"globalLocationFilter": null}
                     """;
        var errorMessage = """
                           globalLocationFilter must be a string. Correct example:
                           {
                             "globalLocationFilter": "ClosestCoast lt 100"
                           }
                           """;
        GenerateFileValidator.HumanReadableError(json1).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json2).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json3).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json4).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(valid1).ShouldBeNull();
        GenerateFileValidator.HumanReadableError(valid2).ShouldBeNull();
    }

    [Fact]
    public void DistributionStrategy_CountryLocationFilters()
    {
        var json1 = """
                    {"countryLocationFilters": 12}
                    """;
        var json2 = """
                    {"countryLocationFilters": {
                      "TR": ["ClosestCoast lt 100"]
                    }}
                    """;
        var json3 = """
                    {"countryLocationFilters": []}
                    """;
        var json4 = """
                    {"countryLocationFilters": 23.2}
                    """;
        var valid1 = """
                     {"countryLocationFilters": {}}
                     """;
        var valid2 = """
                     {"countryLocationFilters": null}
                     """;
        var valid3 = """
                     {"countryLocationFilters": {
                       "TR": "ClosestCoast lt 100"
                     }}
                     """;
        var errorMessage = """
                           countryLocationFilters does not have the correct format. Correct example:
                           {
                             "FR": "ClosestCoast lt 100"
                           }
                           """;
        GenerateFileValidator.HumanReadableError(json1).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json2).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json3).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json4).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(valid1).ShouldBeNull();
        GenerateFileValidator.HumanReadableError(valid2).ShouldBeNull();
        GenerateFileValidator.HumanReadableError(valid3).ShouldBeNull();
    }

    [Fact]
    public void DistributionStrategy_SubdivisionLocationFilters()
    {
        var json1 = """
                    {"subdivisionLocationFilters": 12}
                    """;
        var json2 = """
                    {"subdivisionLocationFilters": {
                      "TR": ["ClosestCoast lt 100"]
                    }}
                    """;
        var json3 = """
                    {"subdivisionLocationFilters": {
                      "TR": []
                    }}
                    """;
        var json4 = """
                    {"subdivisionLocationFilters": 23.2}
                    """;
        var valid1 = """
                     {"subdivisionLocationFilters": {}}
                     """;
        var valid2 = """
                     {"subdivisionLocationFilters": null}
                     """;
        var valid3 = """
                     {"subdivisionLocationFilters": {
                       "TR": {
                         "TR-23": "ClosestCoast lt 100"
                       }
                     }}
                     """;
        var errorMessage = """
                           subdivisionLocationFilters does not have the correct format. Correct example:
                           {
                             "TR": {
                               "TR-22": "ClosestCoast lt 100"
                             }
                           }
                           """;
        GenerateFileValidator.HumanReadableError(json1).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json2).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json3).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json4).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(valid1).ShouldBeNull();
        GenerateFileValidator.HumanReadableError(valid2).ShouldBeNull();
        GenerateFileValidator.HumanReadableError(valid3).ShouldBeNull();
    }
}