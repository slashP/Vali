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
                   {"subdivisionInclusions": {}}
                   """;
        var json3 = """
                    {"subdivisionInclusions": [{
                      "FR-11": ""
                    }]}
                    """;
        var valid = """
                    {"subdivisionInclusions": {
                      "FR": ["FR-11"]
                    }}
                    """;

        var errorMessage = """
                           subdivisionInclusions does not have the correct format. Correct example:
                           {
                             "FR": ["FR-11"]
                           }
                           """;
        GenerateFileValidator.HumanReadableError(json1).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json2).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json3).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(valid).ShouldBeNull();
    }

    [Fact]
    public void SubdivisionExclusions()
    {
        var json1 = """
                    {"subdivisionExclusions": ""}
                    """;
        var json2 = """
                    {"subdivisionExclusions": {}}
                    """;
        var json3 = """
                    {"subdivisionExclusions": [{
                      "FR-11": ""
                    }]}
                    """;
        var valid = """
                    {"subdivisionExclusions": {
                      "FR": ["FR-11"]
                    }}
                    """;

        var errorMessage = """
                           subdivisionExclusions does not have the correct format. Correct example:
                           {
                             "FR": ["FR-11"]
                           }
                           """;
        GenerateFileValidator.HumanReadableError(json1).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json2).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(json3).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(valid).ShouldBeNull();
    }

    [Fact]
    public void CountryDistribution()
    {
        var json1 = """
                    {"countryDistribution": ""}
                    """;
        var json2 = """
                    {"countryDistribution": {}}
                    """;
        var json3 = """
                    {"countryDistribution": []}
                    """;
        var json4 = """
                    {"countryDistribution": {
                      "FR": 12.2
                    }}
                    """;
        var valid = """
                    {"countryDistribution": {
                      "FR": 12
                    }}
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
        GenerateFileValidator.HumanReadableError(json4).ShouldBe(errorMessage);
        GenerateFileValidator.HumanReadableError(valid).ShouldBeNull();
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
}