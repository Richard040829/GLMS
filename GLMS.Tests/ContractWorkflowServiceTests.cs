using GLMS.Web.Exceptions;
using GLMS.Web.Models;
using GLMS.Web.Services;

namespace GLMS.Tests;

public class ContractWorkflowServiceTests
{
    private readonly ContractWorkflowService _workflow = new();
    private static readonly DateTime Today = new(2026, 10, 14);

    private static Contract Contract(
        ContractStatus status = ContractStatus.Active,
        DateTime? start = null,
        DateTime? end = null) => new()
    {
        Id = 1,
        ClientId = 1,
        Status = status,
        ServiceLevel = "Gold",
        StartDate = start ?? Today.AddDays(-10),
        EndDate = end ?? Today.AddDays(10)
    };

    [Fact]
    public void ActiveContractWithinDates_IsAllowed()
    {
        var exception = Record.Exception(() =>
            _workflow.EnsureCanCreateServiceRequest(Contract(), Today));
        Assert.Null(exception);
    }

    [Fact]
    public void ContractStartingInFuture_IsBlocked()
    {
        var ex = Assert.Throws<ContractWorkflowException>(() =>
            _workflow.EnsureCanCreateServiceRequest(
                Contract(start: Today.AddDays(1)), Today));
        Assert.Contains("before the contract start date", ex.Message);
    }

    [Fact]
    public void ContractExpiredByDate_IsBlockedEvenIfStatusSaysActive()
    {
        var ex = Assert.Throws<ContractWorkflowException>(() =>
            _workflow.EnsureCanCreateServiceRequest(
                Contract(end: Today.AddDays(-1)), Today));
        Assert.Contains("expired", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OnHoldContract_IsBlocked()
    {
        Assert.Throws<ContractWorkflowException>(() =>
            _workflow.EnsureCanCreateServiceRequest(
                Contract(ContractStatus.OnHold), Today));
    }

    [Fact]
    public void NullContract_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _workflow.EnsureCanCreateServiceRequest(null!, Today));
    }
}
