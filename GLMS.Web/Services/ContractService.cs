using GLMS.Web.Data;
using GLMS.Web.Exceptions;
using GLMS.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Web.Services;

public interface IContractService
{
    Task<List<Contract>> SearchAsync(DateTime? from, DateTime? to, ContractStatus? status);
    Task<Contract> GetByIdAsync(int id);
    Task<Contract> CreateAsync(Contract contract, IFormFile? agreement);
    Task AttachAgreementAsync(int contractId, IFormFile agreement);
    Task<int> ExpireOverdueContractsAsync();
}

public class ContractService : IContractService
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _files;

    public ContractService(ApplicationDbContext db, IFileStorageService files)
    {
        _db = db;
        _files = files;
    }

    public async Task<List<Contract>> SearchAsync(DateTime? from, DateTime? to, ContractStatus? status)
    {
        IQueryable<Contract> query = _db.Contracts.AsNoTracking().Include(c => c.Client);

        if (from.HasValue) query = query.Where(c => c.StartDate >= from.Value.Date);
        if (to.HasValue)   query = query.Where(c => c.EndDate <= to.Value.Date);
        if (status.HasValue) query = query.Where(c => c.Status == status.Value);

        return await query.OrderBy(c => c.StartDate).ToListAsync();
    }

    public async Task<Contract> GetByIdAsync(int id) =>
        await _db.Contracts.Include(c => c.Client).FirstOrDefaultAsync(c => c.Id == id)
        ?? throw new EntityNotFoundException($"Contract {id} was not found.");

    public async Task<Contract> CreateAsync(Contract contract, IFormFile? agreement)
    {
        // Validate and save the file first so a bad file never creates a contract
        if (agreement is not null && agreement.Length > 0)
            contract.SignedAgreementPath = await _files.SaveSignedAgreementAsync(agreement);

        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync();
        return contract;
    }

    public async Task AttachAgreementAsync(int contractId, IFormFile agreement)
    {
        var contract = await GetByIdAsync(contractId);
        contract.SignedAgreementPath = await _files.SaveSignedAgreementAsync(agreement);
        await _db.SaveChangesAsync();
    }

    /// <summary>Automated status tracking: flips Active contracts past their EndDate to Expired.</summary>
    public async Task<int> ExpireOverdueContractsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var overdue = await _db.Contracts
            .Where(c => c.Status == ContractStatus.Active && c.EndDate < today)
            .ToListAsync();

        overdue.ForEach(c => c.Status = ContractStatus.Expired);
        return await _db.SaveChangesAsync();
    }
}
