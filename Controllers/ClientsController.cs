using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TracKeee.Areas.Identity.Data;
using TracKeee.Models;
using TracKeee.Services;

namespace TracKeee.Controllers
{
    [Authorize]
    public class ClientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly OrganizationService _orgService;
        private readonly ActivityLogService _activityLog;

        public ClientsController(ApplicationDbContext context, OrganizationService orgService, ActivityLogService activityLog)
        {
            _context = context;
            _orgService = orgService;
            _activityLog = activityLog;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var orgId = await _orgService.GetCurrentOrganizationId();
            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "ViewAllClients"))
                return Forbid();

            var query = _context.Clients
                .Where(c => c.OrganizationId == orgId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(search)
                    || (c.ContactPerson != null && c.ContactPerson.ToLower().Contains(search))
                    || (c.Email != null && c.Email.ToLower().Contains(search)));
            }

            ViewBag.Search = search;
            var clients = await query.OrderBy(c => c.Name).ToListAsync();
            return View(clients);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var orgId = await _orgService.GetCurrentOrganizationId();

            var client = await _context.Clients
                .FirstOrDefaultAsync(m => m.Id == id && m.OrganizationId == orgId);
            if (client == null) return NotFound();
            return View(client);
        }

        public async Task<IActionResult> Create()
        {
            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "ManageClients"))
                return Forbid();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,ContactPerson,Email,Phone,VatNumber,Address,Notes")] Client client)
        {
            ModelState.Remove("OrganizationId");
            ModelState.Remove("Organization");

            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "ManageClients"))
                return Forbid();

            if (ModelState.IsValid)
            {
                client.OrganizationId = await _orgService.GetCurrentOrganizationId();
                client.CreatedAt = DateTime.UtcNow;
                client.PortalToken = Guid.NewGuid().ToString("N");
                _context.Add(client);
                await _context.SaveChangesAsync();
                await _activityLog.LogActivity("Created", "Client", client.Name, client.Id);
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "ManageClients"))
                return Forbid();

            var orgId = await _orgService.GetCurrentOrganizationId();
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == orgId);
            if (client == null) return NotFound();
            return View(client);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,ContactPerson,Email,Phone,VatNumber,Address,Notes")] Client client)
        {
            if (id != client.Id) return NotFound();

            ModelState.Remove("OrganizationId");
            ModelState.Remove("Organization");

            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "ManageClients"))
                return Forbid();

            var orgId = await _orgService.GetCurrentOrganizationId();
            var existing = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == orgId);
            if (existing == null) return NotFound();

            if (ModelState.IsValid)
            {
                existing.Name = client.Name;
                existing.ContactPerson = client.ContactPerson;
                existing.Email = client.Email;
                existing.Phone = client.Phone;
                existing.VatNumber = client.VatNumber;
                existing.Address = client.Address;
                existing.Notes = client.Notes;
                await _context.SaveChangesAsync();
                await _activityLog.LogActivity("Updated", "Client", existing.Name, existing.Id);
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "Delete"))
                return Forbid();

            var orgId = await _orgService.GetCurrentOrganizationId();
            var client = await _context.Clients
                .FirstOrDefaultAsync(m => m.Id == id && m.OrganizationId == orgId);
            if (client == null) return NotFound();
            return View(client);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "Delete"))
                return Forbid();

            var orgId = await _orgService.GetCurrentOrganizationId();
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == orgId);

            if (client != null)
            {
                _context.Clients.Remove(client);
                await _context.SaveChangesAsync();
                await _activityLog.LogActivity("Deleted", "Client", client.Name, client.Id);
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Export()
        {
            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "ExportData"))
                return Forbid();

            var orgId = await _orgService.GetCurrentOrganizationId();
            var clients = await _context.Clients
                .Where(c => c.OrganizationId == orgId)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Name,Contact Person,Email,Phone,VAT Number,Address,Notes");
            foreach (var c in clients)
            {
                csv.AppendLine($"\"{c.Name}\",\"{c.ContactPerson}\",\"{c.Email}\",\"{c.Phone}\",\"{c.VatNumber}\",\"{c.Address?.Replace("\"", "\"\"")}\",\"{c.Notes?.Replace("\"", "\"\"")}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            await _activityLog.LogActivity("Exported", "Client", null, null, $"CSV export - {clients.Count} clients");
            return File(bytes, "text/csv", $"Clients_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}