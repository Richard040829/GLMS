using GLMS.Web.Data;
using GLMS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Web.Controllers;

public class ClientsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(ApplicationDbContext db, ILogger<ClientsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GET: /Clients
    public async Task<IActionResult> Index(string? searchString, CancellationToken cancellationToken)
    {
        var query = _db.Clients.AsNoTracking().Include(c => c.Contracts).AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var term = searchString.Trim();
            query = query.Where(c => c.Name.Contains(term) ||
                                     c.ContactDetails.Contains(term) ||
                                     c.Region.Contains(term));
        }

        ViewData["SearchString"] = searchString;
        var clients = await query.OrderBy(c => c.Name).ToListAsync(cancellationToken);
        return View(clients);
    }

    // GET: /Clients/Details/5
    public async Task<IActionResult> Details(int? id, CancellationToken cancellationToken)
    {
        if (id is null) return NotFound();

        var client = await _db.Clients.AsNoTracking()
            .Include(c => c.Contracts)
            .ThenInclude(c => c.ServiceRequests)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return client is null ? NotFound() : View(client);
    }

    // GET: /Clients/Create
    public IActionResult Create() => View(new Client());

    // POST: /Clients/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,ContactDetails,Region")] Client client, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(client);

        client.Name = client.Name.Trim();
        client.ContactDetails = client.ContactDetails.Trim();
        client.Region = client.Region.Trim();

        try
        {
            _db.Clients.Add(client);
            await _db.SaveChangesAsync(cancellationToken);
            TempData["Success"] = $"Client '{client.Name}' was added successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Unable to create client {ClientName}", client.Name);
            ModelState.AddModelError(string.Empty, "The client could not be saved. Please check the information and try again.");
            return View(client);
        }
    }

    // GET: /Clients/Edit/5
    public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
    {
        if (id is null) return NotFound();
        var client = await _db.Clients.FindAsync(new object?[] { id.Value }, cancellationToken);
        return client is null ? NotFound() : View(client);
    }

    // POST: /Clients/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,ContactDetails,Region")] Client client, CancellationToken cancellationToken)
    {
        if (id != client.Id) return NotFound();
        if (!ModelState.IsValid) return View(client);

        client.Name = client.Name.Trim();
        client.ContactDetails = client.ContactDetails.Trim();
        client.Region = client.Region.Trim();

        try
        {
            _db.Update(client);
            await _db.SaveChangesAsync(cancellationToken);
            TempData["Success"] = $"Client '{client.Name}' was updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (!await _db.Clients.AnyAsync(c => c.Id == id, cancellationToken)) return NotFound();
            _logger.LogWarning(ex, "Concurrency issue updating client {ClientId}", id);
            ModelState.AddModelError(string.Empty, "This client was changed by another process. Refresh the page and try again.");
            return View(client);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Unable to update client {ClientId}", id);
            ModelState.AddModelError(string.Empty, "The client could not be updated. Please try again.");
            return View(client);
        }
    }

    // GET: /Clients/Delete/5
    public async Task<IActionResult> Delete(int? id, CancellationToken cancellationToken)
    {
        if (id is null) return NotFound();
        var client = await _db.Clients.AsNoTracking().Include(c => c.Contracts)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return client is null ? NotFound() : View(client);
    }

    // POST: /Clients/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var client = await _db.Clients.Include(c => c.Contracts)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (client is null) return NotFound();

        if (client.Contracts.Count > 0)
        {
            TempData["Error"] = $"'{client.Name}' cannot be deleted because it has {client.Contracts.Count} contract(s). Remove or reassign those contracts first.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            _db.Clients.Remove(client);
            await _db.SaveChangesAsync(cancellationToken);
            TempData["Success"] = $"Client '{client.Name}' was deleted.";
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Unable to delete client {ClientId}", id);
            TempData["Error"] = "This client could not be deleted because related records exist.";
        }

        return RedirectToAction(nameof(Index));
    }
}
