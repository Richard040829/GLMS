using GLMS.Web.Data;
using GLMS.Web.Exceptions;
using GLMS.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Web.Services;

public interface IServiceRequestService
{
    Task<List<ServiceRequest>> GetAllAsync();
    Task<ServiceRequest> CreateAsync(int contractId, string description, decimal costInUsd);
}

public class ServiceRequestService : IServiceRequestService
{
    private readonly ApplicationDbContext _db;
    private readonly IExchangeRateService _rates;
    private readonly ContractWorkflowService _workflow;

    public ServiceRequestService(ApplicationDbContext db, IExchangeRateService rates, ContractWorkflowService? workflow = null)
    {
        _db = db;
        _rates = rates;
        _workflow = workflow ?? new ContractWorkflowService();
    }

    public Task<List<ServiceRequest>> GetAllAsync() =>
        _db.ServiceRequests.AsNoTracking().Include(s => s.Contract).ThenInclude(c => c!.Client)
            .OrderByDescending(s => s.Id).ToListAsync();

    public async Task<ServiceRequest> CreateAsync(int contractId, string description, decimal costInUsd)
    {
        var contract = await _db.Contracts.FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new EntityNotFoundException($"Contract {contractId} was not found.");

        _workflow.EnsureCanCreateServiceRequest(contract);

        // The server recalculates the ZAR cost and never trusts the client-side figure
        var rate = await _rates.GetUsdToZarRateAsync();
        var zar = _rates.ConvertUsdToZar(costInUsd, rate.Rate);

        var request = new ServiceRequest
        {
            ContractId = contractId,
            Description = description,
            CostInUSD = costInUsd,
            CostInZAR = zar,
            Status = "Pending"
        };

        _db.ServiceRequests.Add(request);
        await _db.SaveChangesAsync();
        return request;
    }

    // Kept as a static compatibility helper for existing callers/tests.
    public static void EnsureContractAllowsRequests(Contract contract) =>
        new ContractWorkflowService().EnsureCanCreateServiceRequest(contract);
}
