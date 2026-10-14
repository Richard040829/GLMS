using GLMS.Web.Data;
using GLMS.Web.Exceptions;
using GLMS.Web.Models;
using GLMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Web.Controllers;

public class ContractsController : Controller
{
    private readonly IContractService _contracts;
    private readonly IFileStorageService _files;
    private readonly ApplicationDbContext _db;

    public ContractsController(IContractService contracts, IFileStorageService files, ApplicationDbContext db)
    {
        _contracts = contracts;
        _files = files;
        _db = db;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to, ContractStatus? status)
    {
        await _contracts.ExpireOverdueContractsAsync();

        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewBag.StatusList = new SelectList(Enum.GetValues<ContractStatus>(), status);

        return View(await _contracts.SearchAsync(from, to, status));
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadClientsAsync();
        return View(new Contract { StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("ClientId,StartDate,EndDate,Status,ServiceLevel")] Contract contract, IFormFile? agreement)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _contracts.CreateAsync(contract, agreement);
                TempData["Success"] = "Contract created.";
                return RedirectToAction(nameof(Index));
            }
            catch (FileValidationException ex)
            {
                ModelState.AddModelError("agreement", ex.Message);
            }
        }
        await LoadClientsAsync(contract.ClientId);
        return View(contract);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAgreement(int id, IFormFile? agreement)
    {
        try
        {
            if (agreement is null) throw new FileValidationException("Please choose a PDF file.");
            await _contracts.AttachAgreementAsync(id, agreement);
            TempData["Success"] = "Signed agreement uploaded.";
        }
        catch (DomainValidationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Download(int id)
    {
        try
        {
            var contract = await _contracts.GetByIdAsync(id);
            if (string.IsNullOrEmpty(contract.SignedAgreementPath)) return NotFound();

            var (path, name) = _files.ResolveFile(contract.SignedAgreementPath);
            return PhysicalFile(path, "application/pdf", name);
        }
        catch (EntityNotFoundException)
        {
            return NotFound();
        }
    }

    private async Task LoadClientsAsync(int? selected = null) =>
        ViewBag.Clients = new SelectList(await _db.Clients.AsNoTracking().OrderBy(c => c.Name).ToListAsync(),
                                         "Id", "Name", selected);
}
