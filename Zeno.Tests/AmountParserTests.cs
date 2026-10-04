using Zeno.Application.Services;

namespace Zeno.Tests;

public class AmountParserTests
{
    [Theory]
    [InlineData("R$ 25,90", 25.90)]
    [InlineData("25,90", 25.90)]
    [InlineData("25.90", 25.90)]
    [InlineData("25.9", 25.9)]
    [InlineData("25", 25)]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("1,234.56", 1234.56)]
    [InlineData("R$ 1.000", 1000)]
    [InlineData("R$ 1.234.567,89", 1234567.89)]
    [InlineData("$12.50", 12.50)]
    [InlineData("-12,50", -12.50)]
    [InlineData("(30,00)", -30)]
    [InlineData("  R$ 0,99 ", 0.99)]
    public void TryParse_ParsesCommonFormats(string text, double expected)
    {
        Assert.True(AmountParser.TryParse(text, out var value));
        Assert.Equal((decimal)expected, value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("R$")]
    [InlineData("1.2.3")]
    public void TryParse_RejectsInvalid(string? text)
    {
        Assert.False(AmountParser.TryParse(text, out _));
    }
}
