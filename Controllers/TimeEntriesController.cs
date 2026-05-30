using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TracKeee.Areas.Identity.Data;
using TracKeee.Models;
using TracKeee.Services;

namespace TracKeee.Controllers
{
    [Authorize]
    public class TimeEntriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly OrganizationService _orgService;

        public TimeEntriesController(ApplicationDbContext context, OrganizationService orgService)
        {
            _context = context;
            _orgService = orgService;
        }

        public async Task<IActionResult> Index(string? search, int? projectId, DateTime? dateFrom, DateTime? dateTo)
        {
            var orgId = await _orgService.GetCurrentOrganizationId();
            var role = await _orgService.GetCurrentRole();
            var userId = await _orgService.GetCurrentUserId();

            IQueryable<TimeEntry> query;

            if (_orgService.HasPermission(role, "ViewAllTimeEntries"))
            {
                query = _context.TimeEntries
                    .Include(t => t.Project)
                        .ThenInclude(p => p!.Client)
                    .Where(t => t.OrganizationId == orgId);
            }
            else
            {
                query = _context.TimeEntries
                    .Include(t => t.Project)
                        .ThenInclude(p => p!.Client)
                    .Where(t => t.OrganizationId == orgId && t.UserId == userId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(t => (t.Description != null && t.Description.ToLower().Contains(search))
                    || t.Project!.Name.ToLower().Contains(search)
                    || t.Project!.Client!.Name.ToLower().Contains(search));
            }

            if (projectId.HasValue)
            {
                query = query.Where(t => t.ProjectId == projectId.Value);
            }

            if (dateFrom.HasValue)
            {
                query = query.Where(t => t.Date >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(t => t.Date <= dateTo.Value);
            }

            ViewBag.Search = search;
            ViewBag.ProjectId = projectId;
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");

            // Populate project dropdown for filter
            var projectQuery = _context.Projects
                .Include(p => p.Client)
                .Where(p => p.OrganizationId == orgId);

            if (!_orgService.HasPermission(role, "ViewAllProjects"))
            {
                projectQuery = projectQuery.Where(p => _context.ProjectAssignments
                    .Any(a => a.ProjectId == p.Id && a.UserId == userId));
            }

            ViewBag.Projects = await projectQuery
                .OrderBy(p => p.Client!.Name)
                .ThenBy(p => p.Name)
                .Select(p => new { p.Id, Name = p.Client!.Name + " — " + p.Name })
                .ToListAsync();

            var entries = await query
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(entries);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var orgId = await _orgService.GetCurrentOrganizationId();
            var role = await _orgService.GetCurrentRole();
            var userId = await _orgService.GetCurrentUserId();

            var entry = await _context.TimeEntries
                .Include(t => t.Project)
                    .ThenInclude(p => p!.Client)
                .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == orgId);

            if (entry == null) return NotFound();
            if (!_orgService.HasPermission(role, "ViewAllTimeEntries") && entry.UserId != userId)
                return Forbid();

            return View(entry);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateProjectsDropdown();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProjectId,Date,Hours,Description")] TimeEntry entry)
        {
            ModelState.Remove("UserId");
            ModelState.Remove("OrganizationId");
            ModelState.Remove("Organization");
            ModelState.Remove("Project");

            if (ModelState.IsValid)
            {
                entry.OrganizationId = await _orgService.GetCurrentOrganizationId();
                entry.UserId = await _orgService.GetCurrentUserId();
                entry.CreatedAt = DateTime.UtcNow;
                _context.Add(entry);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            await PopulateProjectsDropdown(entry.ProjectId);
            return View(entry);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var orgId = await _orgService.GetCurrentOrganizationId();
            var userId = await _orgService.GetCurrentUserId();
            var role = await _orgService.GetCurrentRole();

            var entry = await _context.TimeEntries
                .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == orgId);
            if (entry == null) return NotFound();

            // Employees can only edit their own entries
            if (!_orgService.HasPermission(role, "ViewAllTimeEntries") && entry.UserId != userId)
                return Forbid();

            await PopulateProjectsDropdown(entry.ProjectId);
            return View(entry);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ProjectId,Date,Hours,Description")] TimeEntry entry)
        {
            if (id != entry.Id) return NotFound();

            ModelState.Remove("UserId");
            ModelState.Remove("OrganizationId");
            ModelState.Remove("Organization");
            ModelState.Remove("Project");

            var orgId = await _orgService.GetCurrentOrganizationId();
            var userId = await _orgService.GetCurrentUserId();
            var role = await _orgService.GetCurrentRole();

            var existing = await _context.TimeEntries
                .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == orgId);
            if (existing == null) return NotFound();

            if (!_orgService.HasPermission(role, "ViewAllTimeEntries") && existing.UserId != userId)
                return Forbid();

            if (ModelState.IsValid)
            {
                existing.ProjectId = entry.ProjectId;
                existing.Date = entry.Date;
                existing.Hours = entry.Hours;
                existing.Description = entry.Description;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            await PopulateProjectsDropdown(entry.ProjectId);
            return View(entry);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var orgId = await _orgService.GetCurrentOrganizationId();
            var userId = await _orgService.GetCurrentUserId();
            var role = await _orgService.GetCurrentRole();

            var entry = await _context.TimeEntries
                .Include(t => t.Project)
                    .ThenInclude(p => p!.Client)
                .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == orgId);
            if (entry == null) return NotFound();

            if (!_orgService.HasPermission(role, "ViewAllTimeEntries") && entry.UserId != userId)
                return Forbid();

            return View(entry);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var orgId = await _orgService.GetCurrentOrganizationId();
            var userId = await _orgService.GetCurrentUserId();
            var role = await _orgService.GetCurrentRole();

            var entry = await _context.TimeEntries
                .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == orgId);

            if (entry != null)
            {
                if (!_orgService.HasPermission(role, "ViewAllTimeEntries") && entry.UserId != userId)
                    return Forbid();

                _context.TimeEntries.Remove(entry);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateProjectsDropdown(int? selectedProjectId = null)
        {
            var orgId = await _orgService.GetCurrentOrganizationId();
            var role = await _orgService.GetCurrentRole();
            var userId = await _orgService.GetCurrentUserId();

            IQueryable<Project> query = _context.Projects
                .Include(p => p.Client)
                .Where(p => p.OrganizationId == orgId && p.Status == ProjectStatus.Active);

            // Employees only see assigned projects
            if (!_orgService.HasPermission(role, "ViewAllProjects"))
            {
                query = query.Where(p => _context.ProjectAssignments
                    .Any(a => a.ProjectId == p.Id && a.UserId == userId));
            }

            var projects = await query
                .OrderBy(p => p.Client!.Name)
                .ThenBy(p => p.Name)
                .Select(p => new { p.Id, DisplayName = p.Client!.Name + " — " + p.Name })
                .ToListAsync();

            ViewBag.ProjectId = new SelectList(projects, "Id", "DisplayName", selectedProjectId);
        }

        public async Task<IActionResult> Export(int? projectId, DateTime? dateFrom, DateTime? dateTo)
        {
            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "ExportData"))
                return Forbid();

            var orgId = await _orgService.GetCurrentOrganizationId();

            var query = _context.TimeEntries
                .Include(t => t.Project)
                    .ThenInclude(p => p!.Client)
                .Where(t => t.OrganizationId == orgId);

            if (projectId.HasValue)
                query = query.Where(t => t.ProjectId == projectId.Value);
            if (dateFrom.HasValue)
                query = query.Where(t => t.Date >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(t => t.Date <= dateTo.Value);

            var entries = await query.OrderByDescending(t => t.Date).ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Date,Client,Project,Description,Hours,Rate,Amount,Invoiced");
            foreach (var t in entries)
            {
                var rate = t.Project?.HourlyRate ?? 0;
                var amount = t.Hours * rate;
                csv.AppendLine($"{t.Date:yyyy-MM-dd},\"{t.Project?.Client?.Name}\",\"{t.Project?.Name}\",\"{t.Description?.Replace("\"", "\"\"")}\",{t.Hours:N2},{rate:N2},{amount:N2},{(t.IsInvoiced ? "Yes" : "No")}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"TimeEntries_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}