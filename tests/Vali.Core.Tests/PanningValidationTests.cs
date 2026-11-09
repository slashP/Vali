using Shouldly;
using Vali.Core.Validation;
using Xunit;

namespace Vali.Core.Tests;

public class PanningValidationTests
{
    private static MapDefinition WithPanning(CountryPanning panning) =>
        new() { Output = new LocationOutput { CountryPanoVerificationPanning = new() { ["US"] = panning } } };

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Default_percentage_outside_0_to_100_is_rejected(int percentage) =>
        WithPanning(new CountryPanning { DefaultPanning = "indrivingdirection", Percentage = percentage })
            .ValidateOutput().ShouldBeNull();

    [Theory]
    [InlineData(0)]
    [InlineData(75)]
    [InlineData(100)]
    public void Default_percentage_inside_0_to_100_is_accepted(int percentage) =>
        WithPanning(new CountryPanning { DefaultPanning = "indrivingdirection", Percentage = percentage })
            .ValidateOutput().ShouldNotBeNull();

    [Fact]
    public void Missing_percentage_is_accepted() =>
        WithPanning(new CountryPanning { DefaultPanning = "indrivingdirection" }).ValidateOutput().ShouldNotBeNull();

    [Fact]
    public void Expression_percentage_outside_0_to_100_is_rejected() =>
        WithPanning(new CountryPanning
        {
            DefaultPanning = "indrivingdirection",
            PanningExpressions = [new PanningExpression { Expression = "resolutionHeight lt 8192", Panning = "awayfromdrivingdirection", Percentage = 101 }]
        }).ValidateOutput().ShouldBeNull();
}
