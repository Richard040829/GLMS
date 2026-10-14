using GLMS.Tests.Helpers;
using GLMS.Web.Data;
using GLMS.Web.Exceptions;
using GLMS.Web.Models;
using GLMS.Web.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GLMS.Tests;

public class WorkflowEnforcementTests
{
    private static (ServiceRequestService svc, ApplicationDbContext db) Arrange(ContractStatus status, DateTime? end = null)
    {
        var db = TestHelpers.CreateDb();
        db.Clients.Add(new Client { Id = 1, Name = "Test", ContactDetails = "x", Region = "ZA" });
        db.Contracts.Add(new Contract
        {
            Id = 1, ClientId = 1, Status = status, ServiceLevel = "Gold",
            StartDate = DateTime.UtcNow.AddYears(-1), EndDate = end ?? DateTime.UtcNow.AddYears(1)
        });
        db.SaveChanges();

        var rates = new Mock<IExchangeRateService>();
        rates.Setup(r => r.GetUsdToZarRateAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(new ExchangeRateResult(18.50m, false));
        rates.Setup(r => r.ConvertUsdToZar(It.IsAny<decimal>(), It.IsAny<decimal>()))
             .Returns<decimal, decimal>((usd, rate) => Math.Round(usd * rate, 2));

        return (new ServiceRequestService(db, rates.Object), db);
    }

    [Theory]
    [InlineData(ContractStatus.Expired)]
    [InlineData(ContractStatus.OnHold)]
    [InlineData(ContractStatus.Draft)]
    public async Task Create_ContractNotActive_ThrowsAndSavesNothing(ContractStatus status)
    {
        var (svc, db) = Arrange(status);

        await Assert.ThrowsAsync<ContractWorkflowException>(() => svc.CreateAsync(1, "Shipment", 100m));
        Assert.Empty(await db.ServiceRequests.ToListAsync());
    }

    [Fact]
    public async Task Create_ActiveContractPastEndDate_IsBlocked()
    {
        var (svc, _) = Arrange(ContractStatus.Active, end: DateTime.UtcNow.AddDays(-1));
        await Assert.ThrowsAsync<ContractWorkflowException>(() => svc.CreateAsync(1, "Late", 50m));
    }

    [Fact]
    public async Task Create_ActiveContract_SavesWithCalculatedZar()
    {
        var (svc, db) = Arrange(ContractStatus.Active);

        var result = await svc.CreateAsync(1, "Container shipment", 100m);

        Assert.Equal(1850.00m, result.CostInZAR);
        Assert.Equal(1, await db.ServiceRequests.CountAsync());
    }

    [Fact]
    public async Task Create_UnknownContract_ThrowsNotFound()
    {
        var (svc, _) = Arrange(ContractStatus.Active);
        await Assert.ThrowsAsync<EntityNotFoundException>(() => svc.CreateAsync(999, "x", 10m));
    }
}
