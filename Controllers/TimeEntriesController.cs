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

        public async Task<IActionResult> Index()
        {
            var orgId = await _orgService.GetCurrentOrganizationId();
            var role = await _orgService.GetCurrentRole();
            var userId = await _orgService.GetCurrentUserId();

            List<TimeEntry> entries;

            if (_orgService.HasPermission(role, "ViewAllTimeEntries"))
            {
                entries = await _context.TimeEntries
                    .Include(t => t.Project)
                        .ThenInclude(p => p!.Client)
                    .Where(t => t.OrganizationId == orgId)
                    .OrderByDescending(t => t.Date)
                    .ThenByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            else
            {
                entries = await _context.TimeEntries
                    .Include(t => t.Project)
                        .ThenInclude(p => p!.Client)
                    .Where(t => t.OrganizationId == orgId && t.UserId == userId)
                    .OrderByDescending(t => t.Date)
                    .ThenByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }

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
    }
}