using PanBug.Domain.Common;
using Xunit;

namespace PanBug.Domain.Tests;

public class PolishFormatTests
{
    [Theory]
    [InlineData("1250", 1250)]
    [InlineData("1 250,50", 1250.50)]
    [InlineData("1.250,50", 1250.50)]
    [InlineData("1.250", 1250)]
    [InlineData("1.000.000", 1000000)]
    [InlineData("12.5", 12.5)]
    [InlineData("0,01", 0.01)]
    public void Amount_formats_are_read(string text, double expected)
    {
        Assert.True(PolishNumber.TryParseAmount(text, out var value));
        Assert.Equal((decimal)expected, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1,2,3")]
    [InlineData("1.25,50")]
    [InlineData("1,234")]
    [InlineData("-5")]
    [InlineData("١٢")] // cyfry arabskie
    [InlineData("abc")]
    public void Invalid_amounts_are_rejected(string text) =>
        Assert.False(PolishNumber.TryParseAmount(text, out _));

    [Theory]
    [InlineData("600000000", true)]
    [InlineData("+48 600 000 000", true)]
    [InlineData("0048 600-000-000", true)]
    [InlineData("+49600000000", false)]
    [InlineData("60000000", false)]
    [InlineData("６００000000", false)] // cyfry pełnej szerokości
    public void Phone_numbers_are_normalized(string text, bool valid)
    {
        Assert.Equal(valid, PhoneNumber.TryNormalize(text, out var digits));
        if (valid)
            Assert.Equal("600000000", digits);
    }

    [Fact]
    public void Money_uses_polish_format() =>
        Assert.Equal("1 250,50 zł", PolishNumber.Money(1250.5m).Replace(' ', ' '));
}
