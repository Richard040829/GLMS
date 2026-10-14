using System.Net;
using GLMS.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace GLMS.Tests;

public class CurrencyCalculationTests
{
    private static ExchangeRateService CreateService(HttpResponseMessage response, decimal fallback = 18.50m)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var opts = Options.Create(new ExchangeRateOptions { BaseUrl = "https://test.local/rates", FallbackUsdToZarRate = fallback });
        return new ExchangeRateService(new HttpClient(handler.Object), opts, NullLogger<ExchangeRateService>.Instance);
    }

    [Theory]
    [InlineData(100, 18.50, 1850.00)]
    [InlineData(1, 18.2345, 18.23)]
    [InlineData(0.01, 18.50, 0.19)]       // 0.185 rounds up (AwayFromZero)
    [InlineData(250.75, 17.9, 4488.43)]   // 4488.425 rounds up
    [InlineData(0, 18.50, 0)]
    public void ConvertUsdToZar_ReturnsCorrectAmount(double usd, double rate, double expected)
    {
        var svc = CreateService(new HttpResponseMessage(HttpStatusCode.OK));
        Assert.Equal((decimal)expected, svc.ConvertUsdToZar((decimal)usd, (decimal)rate));
    }

    [Fact]
    public void ConvertUsdToZar_NegativeAmount_Throws()
    {
        var svc = CreateService(new HttpResponseMessage(HttpStatusCode.OK));
        Assert.Throws<ArgumentOutOfRangeException>(() => svc.ConvertUsdToZar(-1m, 18m));
    }

    [Fact]
    public void ConvertUsdToZar_NonPositiveRate_Throws()
    {
        var svc = CreateService(new HttpResponseMessage(HttpStatusCode.OK));
        Assert.Throws<ArgumentOutOfRangeException>(() => svc.ConvertUsdToZar(10m, 0m));
    }

    [Fact]
    public async Task GetRate_ValidApiResponse_ReturnsLiveRate()
    {
        var json = "{\"result\":\"success\",\"rates\":{\"ZAR\":17.75}}";
        var svc = CreateService(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });

        var result = await svc.GetUsdToZarRateAsync();

        Assert.Equal(17.75m, result.Rate);
        Assert.False(result.IsFallback);
    }

    [Fact]
    public async Task GetRate_ApiFails_ReturnsFallbackRate()
    {
        var svc = CreateService(new HttpResponseMessage(HttpStatusCode.InternalServerError), fallback: 19.00m);

        var result = await svc.GetUsdToZarRateAsync();

        Assert.Equal(19.00m, result.Rate);
        Assert.True(result.IsFallback);
    }

    [Fact]
    public async Task GetRate_MalformedJson_ReturnsFallbackRate()
    {
        var svc = CreateService(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{ not json") });
        var result = await svc.GetUsdToZarRateAsync();
        Assert.True(result.IsFallback);
    }
}
