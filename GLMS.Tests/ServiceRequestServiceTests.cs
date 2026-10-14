using GLMS.Tests.Helpers;
using GLMS.Web.Exceptions;
using GLMS.Web.Models;
using GLMS.Web.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GLMS.Tests;

public class ServiceRequestServiceTests
{
    private static (ServiceRequestService Service, GLMS.Web.Data.ApplicationDbContext Db) Arrange()
    {
        var db = TestHelpers.CreateDb();
        db.Clients.Add(new Client
        {
            Id = 1, Name = "TechMove Logistics",
            ContactDetails = "ops@example.com", Region = "Africa"
        });
        db.Contracts.Add(new Contract
        {
            Id = 1, ClientId = 1, Status = ContractStatus.Active,
            ServiceLevel = "Gold", StartDate = DateTime.UtcNow.Date.AddDays(-5),
            EndDate = DateTime.UtcNow.Date.AddDays(30)
        });
        db.SaveChanges();

        var rates = new Mock<IExchangeRateService>();
        rates.Setup(x => x.GetUsdToZarRateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExchangeRateResult(18.5m, false));
        rates.Setup(x => x.ConvertUsdToZar(It.IsAny<decimal>(), It.IsAny<decimal>()))
            .Returns<decimal, decimal>((usd, rate) =>
                Math.Round(usd * rate, 2, MidpointRounding.AwayFromZero));

        return (new ServiceRequestService(db, rates.Object), db);
    }

    [Fact]
    public async Task CreateAsync_PersistsUsdZarAndPendingStatus()
    {
        var (service, db) = Arrange();

        var created = await service.CreateAsync(1, "International freight", 125m);
        var persisted = await db.ServiceRequests.SingleAsync();

        Assert.Equal(125m, persisted.CostInUSD);
        Assert.Equal(2312.50m, persisted.CostInZAR);
        Assert.Equal("Pending", persisted.Status);
        Assert.Equal(created.Id, persisted.Id);
    }

    [Fact]
    public async Task CreateAsync_NegativeUsdAmount_IsRejectedAndNotPersisted()
    {
        var (service, db) = Arrange();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.CreateAsync(1, "Invalid cost", -1m));

        Assert.Empty(await db.ServiceRequests.ToListAsync());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsRequestsWithContractAndClient()
    {
        var (service, db) = Arrange();
        db.ServiceRequests.Add(new ServiceRequest
        {
            ContractId = 1, Description = "Shipment", CostInUSD = 10m,
            CostInZAR = 185m, Status = "Pending"
        });
        await db.SaveChangesAsync();

        var results = await service.GetAllAsync();

        var item = Assert.Single(results);
        Assert.NotNull(item.Contract);
        Assert.NotNull(item.Contract!.Client);
        Assert.Equal("TechMove Logistics", item.Contract.Client!.Name);
    }

    [Fact]
    public async Task CreateAsync_UnknownContract_IsNotFound()
    {
        var (service, db) = Arrange();
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            service.CreateAsync(999, "Shipment", 10m));
        Assert.Empty(await db.ServiceRequests.ToListAsync());
    }
}
