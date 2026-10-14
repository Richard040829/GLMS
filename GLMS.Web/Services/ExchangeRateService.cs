using System.Text.Json;
using Microsoft.Extensions.Options;

namespace GLMS.Web.Services;

public class ExchangeRateOptions
{
    public string BaseUrl { get; set; } = "https://open.er-api.com/v6/latest/USD";
    public decimal FallbackUsdToZarRate { get; set; } = 18.50m;
}

public record ExchangeRateResult(decimal Rate, bool IsFallback);

public interface IExchangeRateService
{
    Task<ExchangeRateResult> GetUsdToZarRateAsync(CancellationToken ct = default);
    decimal ConvertUsdToZar(decimal usdAmount, decimal rate);
}

public class ExchangeRateService : IExchangeRateService
{
    private readonly HttpClient _http;
    private readonly ExchangeRateOptions _options;
    private readonly ILogger<ExchangeRateService> _logger;
    private readonly ICurrencyCalculator _calculator;

    public ExchangeRateService(HttpClient http, IOptions<ExchangeRateOptions> options, ILogger<ExchangeRateService> logger, ICurrencyCalculator? calculator = null)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _calculator = calculator ?? new CurrencyCalculator();
    }

    public async Task<ExchangeRateResult> GetUsdToZarRateAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.GetAsync(_options.BaseUrl, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var rate = doc.RootElement.GetProperty("rates").GetProperty("ZAR").GetDecimal();
            if (rate <= 0) throw new InvalidOperationException("API returned a non-positive rate.");

            return new ExchangeRateResult(rate, false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                      or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Exchange rate API failed. Using fallback rate {Rate}.", _options.FallbackUsdToZarRate);
            return new ExchangeRateResult(_options.FallbackUsdToZarRate, true);
        }
    }

    public decimal ConvertUsdToZar(decimal usdAmount, decimal rate) =>
        _calculator.ConvertUsdToZar(usdAmount, rate);
}
