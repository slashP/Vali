using Shouldly;
using Vali.Core.Expressions;
using Xunit;

namespace Vali.Core.Tests;

public class ExpressionParserTests
{
    private static ExpressionNode Parse(string expression) =>
        ExpressionParser.Parse(ExpressionLexer.Tokenize(expression), expression);

    [Fact]
    public void Should_parse_simple_comparison()
    {
        var node = Parse("Buildings100 eq 1");
        var binary = node.ShouldBeOfType<BinaryNode>();
        binary.Operator.Kind.ShouldBe(TokenKind.Eq);
        binary.Left.ShouldBeOfType<PropertyNode>().PropertyName.ShouldBe("Buildings100");
        binary.Right.ShouldBeOfType<LiteralNode>().Token.Value.ShouldBe("1");
    }

    [Fact]
    public void Should_parse_and_expression()
    {
        var node = Parse("Year gt 2020 and Month lt 6");
        var binary = node.ShouldBeOfType<BinaryNode>();
        binary.Operator.Kind.ShouldBe(TokenKind.And);
        binary.Left.ShouldBeOfType<BinaryNode>().Operator.Kind.ShouldBe(TokenKind.Gt);
        binary.Right.ShouldBeOfType<BinaryNode>().Operator.Kind.ShouldBe(TokenKind.Lt);
    }

    [Fact]
    public void Should_parse_or_expression()
    {
        var node = Parse("Year gt 2020 or Month lt 6");
        var binary = node.ShouldBeOfType<BinaryNode>();
        binary.Operator.Kind.ShouldBe(TokenKind.Or);
    }

    [Fact]
    public void Should_give_and_higher_precedence_than_or()
    {
        // A or B and C should parse as A or (B and C)
        var node = Parse("ArrowCount eq 1 or Year gt 2020 and Month lt 6");
        var or = node.ShouldBeOfType<BinaryNode>();
        or.Operator.Kind.ShouldBe(TokenKind.Or);
        or.Left.ShouldBeOfType<BinaryNode>().Operator.Kind.ShouldBe(TokenKind.Eq);
        var and = or.Right.ShouldBeOfType<BinaryNode>();
        and.Operator.Kind.ShouldBe(TokenKind.And);
    }

    [Fact]
    public void Should_parse_grouped_expression()
    {
        var node = Parse("(ArrowCount eq 1) and Year gt 2020");
        var binary = node.ShouldBeOfType<BinaryNode>();
        binary.Operator.Kind.ShouldBe(TokenKind.And);
        binary.Left.ShouldBeOfType<GroupNode>();
    }

    [Fact]
    public void Should_parse_arithmetic_in_comparison()
    {
        var node = Parse("Year * 100 + Month gt 202306");
        var cmp = node.ShouldBeOfType<BinaryNode>();
        cmp.Operator.Kind.ShouldBe(TokenKind.Gt);
        // left side: Year * 100 + Month — addition has lower precedence than multiplication
        var add = cmp.Left.ShouldBeOfType<BinaryNode>();
        add.Operator.Kind.ShouldBe(TokenKind.Plus);
        var mul = add.Left.ShouldBeOfType<BinaryNode>();
        mul.Operator.Kind.ShouldBe(TokenKind.Multiply);
    }

    [Fact]
    public void Should_parse_modulo()
    {
        var node = Parse("DrivingDirectionAngle modulo 90 eq 0");
        var eq = node.ShouldBeOfType<BinaryNode>();
        eq.Operator.Kind.ShouldBe(TokenKind.Eq);
        var mod = eq.Left.ShouldBeOfType<BinaryNode>();
        mod.Operator.Kind.ShouldBe(TokenKind.Modulo);
    }

    [Fact]
    public void Should_parse_division()
    {
        var node = Parse("Buildings100 / 2 gt 1");
        var gt = node.ShouldBeOfType<BinaryNode>();
        gt.Operator.Kind.ShouldBe(TokenKind.Gt);
        var div = gt.Left.ShouldBeOfType<BinaryNode>();
        div.Operator.Kind.ShouldBe(TokenKind.Divide);
    }

    [Fact]
    public void Should_parse_string_literal()
    {
        var node = Parse("Surface eq 'gravel'");
        var binary = node.ShouldBeOfType<BinaryNode>();
        var lit = binary.Right.ShouldBeOfType<LiteralNode>();
        lit.Token.Kind.ShouldBe(TokenKind.StringLiteral);
        lit.Token.Value.ShouldBe("gravel");
    }

    [Fact]
    public void Should_parse_null_comparison()
    {
        var node = Parse("DescriptionLength eq null");
        var binary = node.ShouldBeOfType<BinaryNode>();
        binary.Right.ShouldBeOfType<LiteralNode>().Token.Kind.ShouldBe(TokenKind.NullLiteral);
    }

    [Fact]
    public void Should_parse_boolean_literal()
    {
        var node = Parse("IsScout eq false");
        var binary = node.ShouldBeOfType<BinaryNode>();
        binary.Right.ShouldBeOfType<LiteralNode>().Token.Kind.ShouldBe(TokenKind.BooleanLiteral);
    }

    [Fact]
    public void Should_parse_external_property()
    {
        var node = Parse("external:HasPoles eq 'Yes'");
        var binary = node.ShouldBeOfType<BinaryNode>();
        var ext = binary.Left.ShouldBeOfType<ExternalPropertyNode>();
        ext.Key.ShouldBe("HasPoles");
    }

    [Fact]
    public void Should_parse_parent_property()
    {
        var node = Parse("current:Buildings100 gt Buildings100");
        var binary = node.ShouldBeOfType<BinaryNode>();
        binary.Left.ShouldBeOfType<ParentPropertyNode>().PropertyName.ShouldBe("Buildings100");
        binary.Right.ShouldBeOfType<PropertyNode>().PropertyName.ShouldBe("Buildings100");
    }

    [Fact]
    public void Should_parse_negative_number()
    {
        var node = Parse("Elevation gt -100");
        var binary = node.ShouldBeOfType<BinaryNode>();
        var lit = binary.Right.ShouldBeOfType<LiteralNode>();
        lit.Token.Value.ShouldBe("-100");
    }

    [Fact]
    public void Should_parse_subtraction()
    {
        var node = Parse("Roads100 - Roads10 gte 0");
        var cmp = node.ShouldBeOfType<BinaryNode>();
        cmp.Operator.Kind.ShouldBe(TokenKind.Gte);
        var sub = cmp.Left.ShouldBeOfType<BinaryNode>();
        sub.Operator.Kind.ShouldBe(TokenKind.Minus);
    }

    [Fact]
    public void Should_parse_wildcard()
    {
        var node = Parse("*");
        node.ShouldBeOfType<LiteralNode>().Token.Kind.ShouldBe(TokenKind.Wildcard);
    }

    [Fact]
    public void Should_throw_on_unmatched_open_paren()
    {
        var ex = Should.Throw<ExpressionCompilationException>(() => Parse("(Year gt 2020"));
        ex.Message.ShouldContain("Unmatched '('");
    }

    [Fact]
    public void Should_throw_on_unexpected_end()
    {
        var ex = Should.Throw<ExpressionCompilationException>(() => Parse("Year gt"));
        ex.Message.ShouldContain("Unexpected end of expression");
    }

    [Fact]
    public void Should_throw_on_unexpected_operator_as_operand()
    {
        var ex = Should.Throw<ExpressionCompilationException>(() => Parse("Year gt and"));
        ex.Message.ShouldContain("Expected operand");
    }

    [Fact]
    public void Should_throw_on_extra_tokens()
    {
        var ex = Should.Throw<ExpressionCompilationException>(() => Parse("Year gt 2020 2021"));
        ex.Message.ShouldContain("Unexpected token");
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
    [InlineData("external:HasPoles eq 'Yes'")]
    [InlineData("current:Buildings100 gt Buildings100")]
    [InlineData("(current:DrivingDirectionAngle + 360 - DrivingDirectionAngle) modulo 360 eq 0")]
    [InlineData("Elevation gt -100")]
    [InlineData("DescriptionLength eq null")]
    [InlineData("Buildings100 / 2 gt 1")]
    [InlineData("Surface in ['gravel', 'sand', 'dirt']")]
    [InlineData("ArrowCount in [1, 2]")]
    [InlineData("Surface in ['gravel', 'sand'] and Year gt 2020")]
    [InlineData("Elevation in [-100, 0, 100]")]
    public void Should_parse_all_existing_test_expressions(string expression)
    {
        Should.NotThrow(() => Parse(expression));
    }

    [Fact]
    public void Should_record_correct_text_spans()
    {
        var node = Parse("Year gt 2020");
        var binary = node.ShouldBeOfType<BinaryNode>();
        binary.Span.Start.ShouldBe(0);
        binary.Span.Length.ShouldBe(12);
    }

    [Fact]
    public void Should_parse_in_with_strings()
    {
        var node = Parse("Surface in ['gravel', 'sand', 'dirt']");
        var inNode = node.ShouldBeOfType<InNode>();
        inNode.Operand.ShouldBeOfType<PropertyNode>().PropertyName.ShouldBe("Surface");
        inNode.Values.Length.ShouldBe(3);
        inNode.Values[0].Token.Value.ShouldBe("gravel");
        inNode.Values[1].Token.Value.ShouldBe("sand");
        inNode.Values[2].Token.Value.ShouldBe("dirt");
    }

    [Fact]
    public void Should_parse_in_with_integers()
    {
        var node = Parse("ArrowCount in [1, 2]");
        var inNode = node.ShouldBeOfType<InNode>();
        inNode.Operand.ShouldBeOfType<PropertyNode>().PropertyName.ShouldBe("ArrowCount");
        inNode.Values.Length.ShouldBe(2);
        inNode.Values[0].Token.Value.ShouldBe("1");
        inNode.Values[1].Token.Value.ShouldBe("2");
    }

    [Fact]
    public void Should_parse_in_combined_with_and()
    {
        var node = Parse("Surface in ['gravel', 'sand'] and Year gt 2020");
        var binary = node.ShouldBeOfType<BinaryNode>();
        binary.Operator.Kind.ShouldBe(TokenKind.And);
        binary.Left.ShouldBeOfType<InNode>();
        binary.Right.ShouldBeOfType<BinaryNode>();
    }

    [Fact]
    public void Should_throw_on_in_without_bracket()
    {
        var ex = Should.Throw<ExpressionCompilationException>(() => Parse("Surface in 'gravel'"));
        ex.Message.ShouldContain("Expected '['");
    }

    [Fact]
    public void Should_throw_on_in_with_unclosed_bracket()
    {
        var ex = Should.Throw<ExpressionCompilationException>(() => Parse("Surface in ['gravel'"));
        ex.Message.ShouldContain("Expected ']'");
    }

    [Fact]
    public void Should_throw_on_in_with_non_literal()
    {
        var ex = Should.Throw<ExpressionCompilationException>(() => Parse("Surface in [Year]"));
        ex.Message.ShouldContain("Expected a literal value");
    }
}
