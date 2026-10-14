using GLMS.Web.Exceptions;
using GLMS.Web.Models;

namespace GLMS.Web.Services;

/// <summary>Centralises contract lifecycle rules used by service-request workflows.</summary>
public class ContractWorkflowService
{
    public void EnsureCanCreateServiceRequest(Contract contract, DateTime? utcToday = null)
    {
        ArgumentNullException.ThrowIfNull(contract);
        var today = (utcToday ?? DateTime.UtcNow).Date;

        if (contract.Status == ContractStatus.Expired || contract.EndDate.Date < today)
            throw new ContractWorkflowException("Cannot raise a service request because this contract has expired.");

        if (contract.Status == ContractStatus.OnHold)
            throw new ContractWorkflowException("Cannot raise a service request because this contract is on hold.");

        if (contract.Status != ContractStatus.Active)
            throw new ContractWorkflowException("Service requests can only be raised against active contracts.");

        if (contract.StartDate.Date > today)
            throw new ContractWorkflowException("Cannot raise a service request before the contract start date.");
    }
}
