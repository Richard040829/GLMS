using GLMS.Web.Data;
using GLMS.Web.Models;
using GLMS.Web.Services;
using GLMS.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IContractService _contracts;

    public HomeController(ApplicationDbContext db, IContractService contracts)
    {
        _db = db;
        _contracts = contracts;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        await _contracts.ExpireOverdueContractsAsync();

        var model = new HomeDashboardViewModel
        {
            TotalClients = await _db.Clients.CountAsync(cancellationToken),
            TotalContracts = await _db.Contracts.CountAsync(cancellationToken),
            ActiveContracts = await _db.Contracts.CountAsync(c => c.Status == ContractStatus.Active, cancellationToken),
            ExpiredContracts = await _db.Contracts.CountAsync(c => c.Status == ContractStatus.Expired, cancellationToken),
            TotalServiceRequests = await _db.ServiceRequests.CountAsync(cancellationToken),
            TotalServiceRequestValueZar = await _db.ServiceRequests.Select(s => (decimal?)s.CostInZAR).SumAsync(cancellationToken) ?? 0m,
            RecentRequests = await _db.ServiceRequests.AsNoTracking()
                .Include(s => s.Contract).ThenInclude(c => c!.Client)
                .OrderByDescending(s => s.Id).Take(5).ToListAsync(cancellationToken)
        };

        return View(model);
    }
}
