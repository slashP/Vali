using Shouldly;
using Vali.Core.Expressions;
using Xunit;

namespace Vali.Core.Tests;

public class ExpressionLexerTests
{
    [Fact]
    public void Should_tokenize_wildcard()
    {
        var tokens = ExpressionLexer.Tokenize("*");
        tokens.Length.ShouldBe(2);
        tokens[0].Kind.ShouldBe(TokenKind.Wildcard);
        tokens[1].Kind.ShouldBe(TokenKind.EndOfExpression);
    }

    [Theory]
    [InlineData("42", TokenKind.IntegerLiteral, "42")]
    [InlineData("10.5", TokenKind.DecimalLiteral, "10.5")]
    [InlineData("-100", TokenKind.IntegerLiteral, "-100")]
    [InlineData("-3.14", TokenKind.DecimalLiteral, "-3.14")]
    public void Should_tokenize_numbers(string input, TokenKind expectedKind, string expectedValue)
    {
        var tokens = ExpressionLexer.Tokenize(input);
        tokens[0].Kind.ShouldBe(expectedKind);
        tokens[0].Value.ShouldBe(expectedValue);
    }

    [Fact]
    public void Should_tokenize_string_literal()
    {
        var tokens = ExpressionLexer.Tokenize("'gravel'");
        tokens[0].Kind.ShouldBe(TokenKind.StringLiteral);
        tokens[0].Value.ShouldBe("gravel");
    }

    [Fact]
    public void Should_tokenize_string_with_spaces()
    {
        var tokens = ExpressionLexer.Tokenize("'New York'");
        tokens[0].Kind.ShouldBe(TokenKind.StringLiteral);
        tokens[0].Value.ShouldBe("New York");
    }

    [Fact]
    public void Should_tokenize_string_with_escaped_quotes()
    {
        var tokens = ExpressionLexer.Tokenize(@"'O\'Brien'");
        tokens[0].Kind.ShouldBe(TokenKind.StringLiteral);
        tokens[0].Value.ShouldBe("O'Brien");
    }

    [Fact]
    public void Should_tokenize_string_with_hyphens()
    {
        var tokens = ExpressionLexer.Tokenize("'Saint-Tropez'");
        tokens[0].Kind.ShouldBe(TokenKind.StringLiteral);
        tokens[0].Value.ShouldBe("Saint-Tropez");
    }

    [Fact]
    public void Should_throw_on_unterminated_string()
    {
        var ex = Should.Throw<ExpressionCompilationException>(() => ExpressionLexer.Tokenize("'unterminated"));
        ex.Position.ShouldBe(0);
        ex.Message.ShouldContain("Unterminated string");
    }

    [Theory]
    [InlineData("true", TokenKind.BooleanLiteral)]
    [InlineData("false", TokenKind.BooleanLiteral)]
    [InlineData("null", TokenKind.NullLiteral)]
    public void Should_tokenize_keyword_literals(string input, TokenKind expectedKind)
    {
        var tokens = ExpressionLexer.Tokenize(input);
        tokens[0].Kind.ShouldBe(expectedKind);
        tokens[0].Value.ShouldBe(input);
    }

    [Theory]
    [InlineData("eq", TokenKind.Eq)]
    [InlineData("neq", TokenKind.Neq)]
    [InlineData("lt", TokenKind.Lt)]
    [InlineData("lte", TokenKind.Lte)]
    [InlineData("gt", TokenKind.Gt)]
    [InlineData("gte", TokenKind.Gte)]
    [InlineData("and", TokenKind.And)]
    [InlineData("or", TokenKind.Or)]
    [InlineData("modulo", TokenKind.Modulo)]
    public void Should_tokenize_operators(string input, TokenKind expectedKind)
    {
        var tokens = ExpressionLexer.Tokenize(input);
        tokens[0].Kind.ShouldBe(expectedKind);
    }

    [Theory]
    [InlineData("+", TokenKind.Plus)]
    [InlineData("/", TokenKind.Divide)]
    [InlineData("(", TokenKind.OpenParen)]
    [InlineData(")", TokenKind.CloseParen)]
    public void Should_tokenize_symbols(string input, TokenKind expectedKind)
    {
        var tokens = ExpressionLexer.Tokenize(input);
        tokens[0].Kind.ShouldBe(expectedKind);
    }

    [Fact]
    public void Should_tokenize_multiply()
    {
        var tokens = ExpressionLexer.Tokenize("Year * 100");
        tokens[0].Kind.ShouldBe(TokenKind.Property);
        tokens[1].Kind.ShouldBe(TokenKind.Multiply);
        tokens[2].Kind.ShouldBe(TokenKind.IntegerLiteral);
    }

    [Fact]
    public void Should_tokenize_minus_as_binary_after_value()
    {
        var tokens = ExpressionLexer.Tokenize("Roads100 - Roads10");
        tokens[0].Kind.ShouldBe(TokenKind.Property);
        tokens[1].Kind.ShouldBe(TokenKind.Minus);
        tokens[2].Kind.ShouldBe(TokenKind.Property);
    }

    [Fact]
    public void Should_tokenize_negative_number_after_operator()
    {
        var tokens = ExpressionLexer.Tokenize("Elevation gt -100");
        tokens[0].Kind.ShouldBe(TokenKind.Property);
        tokens[1].Kind.ShouldBe(TokenKind.Gt);
        tokens[2].Kind.ShouldBe(TokenKind.IntegerLiteral);
        tokens[2].Value.ShouldBe("-100");
    }

    [Fact]
    public void Should_tokenize_property_names()
    {
        var tokens = ExpressionLexer.Tokenize("Buildings100");
        tokens[0].Kind.ShouldBe(TokenKind.Property);
        tokens[0].Value.ShouldBe("Buildings100");
    }

    [Fact]
    public void Should_tokenize_external_property()
    {
        var tokens = ExpressionLexer.Tokenize("external:HasPoles");
        tokens[0].Kind.ShouldBe(TokenKind.ExternalProperty);
        tokens[0].Value.ShouldBe("external:HasPoles");
    }

    [Fact]
    public void Should_tokenize_parent_property()
    {
        var tokens = ExpressionLexer.Tokenize("current:Buildings100");
        tokens[0].Kind.ShouldBe(TokenKind.ParentProperty);
        tokens[0].Value.ShouldBe("current:Buildings100");
    }

    [Fact]
    public void Should_track_positions()
    {
        var tokens = ExpressionLexer.Tokenize("Year gt 2020");
        tokens[0].Position.ShouldBe(0);
        tokens[0].Length.ShouldBe(4);
        tokens[1].Position.ShouldBe(5);
        tokens[1].Length.ShouldBe(2);
        tokens[2].Position.ShouldBe(8);
        tokens[2].Length.ShouldBe(4);
    }

    [Fact]
    public void Should_throw_on_unexpected_character()
    {
        var ex = Should.Throw<ExpressionCompilationException>(() => ExpressionLexer.Tokenize("Year @ 5"));
        ex.Position.ShouldBe(5);
    }

    [Theory]
    [InlineData("Buildings100 eq 1")]
    [InlineData("Buildings100 eq 1 and Buildings10 eq 0")]
    [InlineData("Surface eq 'gravel' and Buildings200 eq 0")]
    [InlineData("County eq 'Buildings200 County' and Buildings200 gt 0")]
    [InlineData("ArrowCount gte 2")]
    [InlineData("Heading lt 10.2 or Lat gt 23 + 42")]
    [InlineData("(ArrowCount eq 1 and Buildings100 eq 0) or (DrivingDirectionAngle lt 10 and DrivingDirectionAngle gt 350)")]
    [InlineData("Year * 100 + Month gt 202306")]
    [InlineData("DrivingDirectionAngle modulo 90 eq 0")]
    [InlineData("Buildings100 + Buildings200 gt 5")]
    [InlineData("Roads100 - Roads10 gte 0")]
    [InlineData("external:HasPoles eq 'Yes'")]
    [InlineData("current:Buildings100 gt Buildings100")]
    [InlineData("(current:DrivingDirectionAngle + 360 - DrivingDirectionAngle) modulo 360 eq 0")]
    [InlineData("Elevation gt -100")]
    [InlineData("DescriptionLength eq null")]
    [InlineData("IsScout eq false")]
    [InlineData("IsResidential eq true")]
    [InlineData("Buildings100 / 2 gt 1")]
    public void Should_tokenize_all_expression_forms(string expression)
    {
        var tokens = ExpressionLexer.Tokenize(expression);
        tokens.Length.ShouldBeGreaterThan(1);
        tokens[^1].Kind.ShouldBe(TokenKind.EndOfExpression);
    }

    [Fact]
    public void Should_end_with_end_of_expression()
    {
        var tokens = ExpressionLexer.Tokenize("Year gt 2020");
        tokens[^1].Kind.ShouldBe(TokenKind.EndOfExpression);
    }

    [Fact]
    public void Should_tokenize_complex_nested_expression()
    {
        var tokens = ExpressionLexer.Tokenize("((Buildings100 gt 5 and Roads100 gt 3) or (Buildings200 gt 20 and Roads200 gt 5)) and Year gt 2018");
        tokens[^1].Kind.ShouldBe(TokenKind.EndOfExpression);
        tokens.Count(t => t.Kind == TokenKind.OpenParen).ShouldBe(3);
        tokens.Count(t => t.Kind == TokenKind.CloseParen).ShouldBe(3);
    }

    [Fact]
    public void Should_tokenize_in_operator()
    {
        var tokens = ExpressionLexer.Tokenize("Surface in ['gravel', 'sand']");
        tokens[0].Kind.ShouldBe(TokenKind.Property);
        tokens[1].Kind.ShouldBe(TokenKind.In);
        tokens[2].Kind.ShouldBe(TokenKind.OpenBracket);
        tokens[3].Kind.ShouldBe(TokenKind.StringLiteral);
        tokens[4].Kind.ShouldBe(TokenKind.Comma);
        tokens[5].Kind.ShouldBe(TokenKind.StringLiteral);
        tokens[6].Kind.ShouldBe(TokenKind.CloseBracket);
    }

    [Fact]
    public void Should_tokenize_in_with_integers()
    {
        var tokens = ExpressionLexer.Tokenize("ArrowCount in [1, 2]");
        tokens[0].Kind.ShouldBe(TokenKind.Property);
        tokens[1].Kind.ShouldBe(TokenKind.In);
        tokens[2].Kind.ShouldBe(TokenKind.OpenBracket);
        tokens[3].Kind.ShouldBe(TokenKind.IntegerLiteral);
        tokens[4].Kind.ShouldBe(TokenKind.Comma);
        tokens[5].Kind.ShouldBe(TokenKind.IntegerLiteral);
        tokens[6].Kind.ShouldBe(TokenKind.CloseBracket);
    }

    [Fact]
    public void Should_tokenize_negative_number_in_bracket_list()
    {
        var tokens = ExpressionLexer.Tokenize("Elevation in [-100, 0, 100]");
        tokens[3].Kind.ShouldBe(TokenKind.IntegerLiteral);
        tokens[3].Value.ShouldBe("-100");
    }
}
