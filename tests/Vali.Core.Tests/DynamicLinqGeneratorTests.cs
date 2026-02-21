using Shouldly;
using Vali.Core.Expressions;
using Xunit;

namespace Vali.Core.Tests;

public class DynamicLinqGeneratorTests
{
    private static string Generate(string expression, string? parentParam = null)
    {
        var tokens = ExpressionLexer.Tokenize(expression);
        var ast = ExpressionParser.Parse(tokens, expression);
        var resolver = PropertyResolver.ForLocation();
        var generator = new DynamicLinqGenerator(resolver, "x", parentParam, expression);
        return generator.Generate(ast);
    }

    private static string GenerateMapCheckr(string expression)
    {
        var tokens = ExpressionLexer.Tokenize(expression);
        var ast = ExpressionParser.Parse(tokens, expression);
        var resolver = PropertyResolver.ForMapCheckrLocation();
        var generator = new DynamicLinqGenerator(resolver, "x", null, expression);
        return generator.Generate(ast);
    }

    [Fact]
    public void Should_generate_simple_comparison()
    {
        var result = Generate("Buildings100 eq 1");
        result.ShouldBe("(x.Osm.Buildings100 == 1)");
    }

    [Fact]
    public void Should_generate_and_expression()
    {
        var result = Generate("Year gt 2020 and Month lt 6");
        result.ShouldBe("((x.Google.Year > 2020) && (x.Google.Month < 6))");
    }

    [Fact]
    public void Should_generate_or_expression()
    {
        var result = Generate("Year gt 2020 or Month lt 6");
        result.ShouldBe("((x.Google.Year > 2020) || (x.Google.Month < 6))");
    }

    [Fact]
    public void Should_generate_string_comparison()
    {
        var result = Generate("Surface eq 'gravel'");
        result.ShouldBe("(x.Osm.Surface == \"gravel\")");
    }

    [Fact]
    public void Should_generate_null_comparison()
    {
        var result = Generate("DescriptionLength eq null");
        result.ShouldBe("(x.Google.DescriptionLength == null)");
    }

    [Fact]
    public void Should_generate_boolean_comparison()
    {
        var result = Generate("IsScout eq false");
        result.ShouldBe("(x.Google.IsScout == false)");
    }

    [Fact]
    public void Should_generate_arithmetic()
    {
        var result = Generate("Year * 100 + Month gt 202306");
        result.ShouldBe("(((x.Google.Year * 100) + x.Google.Month) > 202306)");
    }

    [Fact]
    public void Should_generate_modulo()
    {
        var result = Generate("DrivingDirectionAngle modulo 90 eq 0");
        result.ShouldBe("((x.Google.DrivingDirectionAngle % 90) == 0)");
    }

    [Fact]
    public void Should_generate_division()
    {
        var result = Generate("Buildings100 / 2 gt 1");
        result.ShouldBe("((x.Osm.Buildings100 / 2) > 1)");
    }

    [Fact]
    public void Should_generate_subtraction()
    {
        var result = Generate("Roads100 - Roads10 gte 0");
        result.ShouldBe("((x.Osm.Roads100 - x.Osm.Roads10) >= 0)");
    }

    [Fact]
    public void Should_generate_external_property()
    {
        var result = Generate("external:HasPoles eq 'Yes'");
        result.ShouldBe("((x.ExternalData.ContainsKey(\"HasPoles\") ? x.ExternalData[\"HasPoles\"] : \"\") == \"Yes\")");
    }

    [Fact]
    public void Should_generate_parent_property()
    {
        var result = Generate("current:Buildings100 gt Buildings100", parentParam: "current");
        result.ShouldBe("(current.Osm.Buildings100 > x.Osm.Buildings100)");
    }

    [Fact]
    public void Should_generate_grouped_expression()
    {
        var result = Generate("(ArrowCount eq 1 and Buildings100 eq 0) or Year gt 2020");
        // GroupNode wraps with parens, then BinaryNode(Or) also wraps — extra parens are harmless
        result.ShouldBe("((((x.Google.ArrowCount == 1) && (x.Osm.Buildings100 == 0))) || (x.Google.Year > 2020))");
    }

    [Fact]
    public void Should_generate_nested_groups()
    {
        var result = Generate("((Buildings100 gt 5 and Roads100 gt 3) or (Buildings200 gt 20)) and Year gt 2018");
        result.ShouldContain("&&");
        result.ShouldContain("||");
    }

    [Fact]
    public void Should_generate_negative_number()
    {
        var result = Generate("Elevation gt -100");
        result.ShouldBe("(x.Google.Elevation > -100)");
    }

    [Fact]
    public void Should_generate_decimal_number()
    {
        var result = Generate("Heading lt 10.2");
        result.ShouldBe("(x.Google.Heading < 10.2)");
    }

    [Fact]
    public void Should_generate_mapcheckr_expression()
    {
        var result = GenerateMapCheckr("year eq 2023");
        result.ShouldBe("(x.year == 2023)");
    }

    [Fact]
    public void Should_generate_in_with_strings()
    {
        var result = Generate("Surface in ['gravel', 'sand', 'dirt']");
        result.ShouldBe("((x.Osm.Surface == \"gravel\") || (x.Osm.Surface == \"sand\") || (x.Osm.Surface == \"dirt\"))");
    }

    [Fact]
    public void Should_generate_in_with_integers()
    {
        var result = Generate("ArrowCount in [1, 2]");
        result.ShouldBe("((x.Google.ArrowCount == 1) || (x.Google.ArrowCount == 2))");
    }

    [Fact]
    public void Should_generate_in_single_value()
    {
        var result = Generate("Surface in ['gravel']");
        result.ShouldBe("((x.Osm.Surface == \"gravel\"))");
    }

    [Fact]
    public void Should_generate_in_combined_with_and()
    {
        var result = Generate("Surface in ['gravel', 'sand'] and Year gt 2020");
        result.ShouldBe("(((x.Osm.Surface == \"gravel\") || (x.Osm.Surface == \"sand\")) && (x.Google.Year > 2020))");
    }

    [Fact]
    public void Should_throw_on_unknown_property()
    {
        var ex = Should.Throw<ExpressionCompilationException>(() => Generate("Bildings100 eq 1"));
        ex.Message.ShouldContain("Unknown property 'Bildings100'");
        ex.Message.ShouldContain("Did you mean 'Buildings100'");
    }

    [Fact]
    public void Should_throw_on_parent_property_without_parent_context()
    {
        var ex = Should.Throw<ExpressionCompilationException>(() => Generate("current:Buildings100 gt 5"));
        ex.Message.ShouldContain("not allowed in this context");
    }

    [Theory]
    [InlineData("Buildings100 eq 1")]
    [InlineData("Buildings100 eq 1 and Buildings10 eq 0")]
    [InlineData("Buildings100 eq 1 and Buildings10 eq 0 or Roads10 lt 3")]
    [InlineData("Surface eq 'gravel' and Buildings200 eq 0")]
    [InlineData("County eq 'Buildings200 County' and Buildings200 gt 0")]
    [InlineData("ArrowCount gte 2")]
    [InlineData("Heading lt 10.2 or Lat gt 23 + 42")]
    [InlineData("ArrowCount eq 1 and Buildings100 eq 0")]
    [InlineData("(ArrowCount eq 1 and Buildings100 eq 0) or (DrivingDirectionAngle lt 10 and DrivingDirectionAngle gt 350)")]
    [InlineData("ArrowCount eq 2 and Year gt 2011 and Year lt 2019 and DrivingDirectionAngle neq 0")]
    [InlineData("Roads25 eq 1 and (HighwayType eq 'Residential' or HighwayType eq 'Service' or HighwayType eq 'Tertiary' or HighwayType eq 'Road' or HighwayType eq 'Track' or HighwayType eq 'Unclassified')")]
    [InlineData("Month gte 6 and Month lte 9")]
    [InlineData("Elevation gt 500")]
    [InlineData("Year * 100 + Month gt 202306")]
    [InlineData("DrivingDirectionAngle modulo 90 eq 0")]
    [InlineData("Buildings100 + Buildings200 gt 5")]
    [InlineData("Roads100 - Roads10 gte 0")]
    [InlineData("((Buildings100 gt 5 and Roads100 gt 3) or (Buildings200 gt 20 and Roads200 gt 5)) and Year gt 2018")]
    [InlineData("DescriptionLength eq null")]
    [InlineData("IsScout eq false")]
    [InlineData("IsResidential eq true")]
    [InlineData("Buildings100 / 2 gt 1")]
    [InlineData("Elevation gt -100")]
    [InlineData("Surface in ['gravel', 'sand', 'dirt']")]
    [InlineData("ArrowCount in [1, 2]")]
    [InlineData("Surface in ['gravel'] and Year gt 2020")]
    [InlineData("Elevation in [-100, 0, 100]")]
    public void Should_generate_all_existing_expressions(string expression)
    {
        Should.NotThrow(() => Generate(expression));
    }
}
