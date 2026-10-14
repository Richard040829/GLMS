using GLMS.Web.Services;

namespace GLMS.Tests;

public class CurrencyCalculatorTests
{
    private readonly CurrencyCalculator _calculator = new();

    [Theory]
    [InlineData(100, 18.5, 1850)]
    [InlineData(1, 18.2345, 18.23)]
    [InlineData(0.01, 18.5, 0.19)]
    [InlineData(250.75, 17.9, 4488.43)]
    [InlineData(0, 18.5, 0)]
    public void ConvertUsdToZar_UsesRateAndRoundsToTwoDecimals(double usd, double rate, double expected)
    {
        var actual = _calculator.ConvertUsdToZar((decimal)usd, (decimal)rate);
        Assert.Equal((decimal)expected, actual);
    }

    [Fact]
    public void ConvertUsdToZar_RejectsNegativeAmount() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.ConvertUsdToZar(-1m, 18.5m));

    [Fact]
    public void ConvertUsdToZar_RejectsZeroRate() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.ConvertUsdToZar(10m, 0m));
}
