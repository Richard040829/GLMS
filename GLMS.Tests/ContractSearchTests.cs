using GLMS.Tests.Helpers;
using GLMS.Web.Models;
using GLMS.Web.Services;
using Moq;

namespace GLMS.Tests;

public class ContractSearchTests
{
    private static ContractService Arrange()
    {
        var db = TestHelpers.CreateDb();
        db.Clients.Add(new Client { Id = 1, Name = "C", ContactDetails = "x", Region = "ZA" });
        db.Contracts.AddRange(
            new Contract { Id = 1, ClientId = 1, Status = ContractStatus.Active,  ServiceLevel = "A", StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31) },
            new Contract { Id = 2, ClientId = 1, Status = ContractStatus.Expired, ServiceLevel = "B", StartDate = new DateTime(2023, 1, 1), EndDate = new DateTime(2024, 12, 31) },
            new Contract { Id = 3, ClientId = 1, Status = ContractStatus.OnHold,  ServiceLevel = "C", StartDate = new DateTime(2026, 6, 1), EndDate = new DateTime(2027, 5, 31) });
        db.SaveChanges();
        return new ContractService(db, new Mock<IFileStorageService>().Object);
    }

    [Fact]
    public async Task Search_ByStatus_ReturnsOnlyMatching()
    {
        var result = await Arrange().SearchAsync(null, null, ContractStatus.Expired);
        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public async Task Search_ByDateRange_ReturnsContractsInsideRange()
    {
        var result = await Arrange().SearchAsync(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), null);
        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task Search_DateRangeAndStatus_Combined()
    {
        var result = await Arrange().SearchAsync(new DateTime(2026, 1, 1), new DateTime(2030, 1, 1), ContractStatus.OnHold);
        Assert.Single(result);
        Assert.Equal(3, result[0].Id);
    }

    [Fact]
    public async Task Search_NoFilters_ReturnsAll() =>
        Assert.Equal(3, (await Arrange().SearchAsync(null, null, null)).Count);

    [Fact]
    public async Task ExpireOverdue_FlipsActiveContractsPastEndDate()
    {
        var db = TestHelpers.CreateDb();
        db.Clients.Add(new Client { Id = 1, Name = "C", ContactDetails = "x", Region = "ZA" });
        db.Contracts.Add(new Contract { Id = 1, ClientId = 1, Status = ContractStatus.Active, ServiceLevel = "A",
            StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2021, 1, 1) });
        db.SaveChanges();

        var svc = new ContractService(db, new Mock<IFileStorageService>().Object);
        await svc.ExpireOverdueContractsAsync();

        Assert.Equal(ContractStatus.Expired, db.Contracts.Single().Status);
    }
}
