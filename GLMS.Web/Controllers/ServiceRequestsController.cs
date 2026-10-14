using GLMS.Web.Data;
using GLMS.Web.Exceptions;
using GLMS.Web.Services;
using GLMS.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Web.Controllers;

public class ServiceRequestsController : Controller
{
    private readonly IServiceRequestService _requests;
    private readonly IExchangeRateService _rates;
    private readonly ApplicationDbContext _db;

    public ServiceRequestsController(IServiceRequestService requests, IExchangeRateService rates, ApplicationDbContext db)
    {
        _requests = requests;
        _rates = rates;
        _db = db;
    }

    public async Task<IActionResult> Index() => View(await _requests.GetAllAsync());

    [HttpGet]
    public async Task<IActionResult> Create() =>
        View(new ServiceRequestCreateViewModel { Contracts = await GetContractOptionsAsync() });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceRequestCreateViewModel vm)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _requests.CreateAsync(vm.ContractId, vm.Description, vm.CostInUSD);
                TempData["Success"] = "Service request created.";
                return RedirectToAction(nameof(Index));
            }
            catch (DomainValidationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }
        vm.Contracts = await GetContractOptionsAsync(vm.ContractId);
        return View(vm);
    }

    // AJAX endpoint: GET /ServiceRequests/Convert?usd=100
    [HttpGet]
    public async Task<IActionResult> Convert(decimal usd, CancellationToken ct)
    {
        if (usd < 0) return BadRequest(new { error = "Amount cannot be negative." });

        var result = await _rates.GetUsdToZarRateAsync(ct);
        return Json(new
        {
            rate = result.Rate,
            zar = _rates.ConvertUsdToZar(usd, result.Rate),
            isFallback = result.IsFallback
        });
    }

    private async Task<IEnumerable<SelectListItem>> GetContractOptionsAsync(int? selected = null) =>
        await _db.Contracts.AsNoTracking().Include(c => c.Client)
            .OrderBy(c => c.Id)
            .Select(c => new SelectListItem(
                $"#{c.Id} - {c.Client!.Name} ({c.Status})", c.Id.ToString(), c.Id == selected))
            .ToListAsync();
}
