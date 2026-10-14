namespace GLMS.Web.Services;

/// <summary>Pure currency arithmetic, separated from HTTP/API concerns for reliable unit testing.</summary>
public interface ICurrencyCalculator
{
    decimal ConvertUsdToZar(decimal usdAmount, decimal rate);
}

public sealed class CurrencyCalculator : ICurrencyCalculator
{
    public decimal ConvertUsdToZar(decimal usdAmount, decimal rate)
    {
        if (usdAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(usdAmount), "Amount cannot be negative.");
        if (rate <= 0)
            throw new ArgumentOutOfRangeException(nameof(rate), "Exchange rate must be greater than zero.");

        return Math.Round(usdAmount * rate, 2, MidpointRounding.AwayFromZero);
    }
}
