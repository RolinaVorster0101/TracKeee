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
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly OrganizationService _orgService;

        public ProjectsController(ApplicationDbContext context, OrganizationService orgService)
        {
            _context = context;
            _orgService = orgService;
        }

        public async Task<IActionResult> Index(string? search, string? status)
        {
            var orgId = await _orgService.GetCurrentOrganizationId();
            var role = await _orgService.GetCurrentRole();
            var userId = await _orgService.GetCurrentUserId();

            IQueryable<Project> query;

            if (_orgService.HasPermission(role, "ViewAllProjects"))
            {
                query = _context.Projects
                    .Include(p => p.Client)
                    .Where(p => p.OrganizationId == orgId);
            }
            else
            {
                query = _context.Projects
                    .Include(p => p.Client)
                    .Where(p => p.OrganizationId == orgId
                        && _context.ProjectAssignments.Any(a => a.ProjectId == p.Id && a.UserId == userId));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(search)
                    || (p.Client != null && p.Client.Name.ToLower().Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ProjectStatus>(status, out var statusEnum))
            {
                query = query.Where(p => p.Status == statusEnum);
            }

            ViewBag.Search = search;
            ViewBag.Status = status;

            var projects = await query.OrderBy(p => p.Name).ToListAsync();
            return View(projects);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var orgId = await _orgService.GetCurrentOrganizationId();

            var project = await _context.Projects
                .Include(p => p.Client)
                .FirstOrDefaultAsync(m => m.Id == id && m.OrganizationId == orgId);
            if (project == null) return NotFound();
            return View(project);
        }

        public async Task<IActionResult> Create()
        {
            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "ManageProjects"))
                return Forbid();
            await PopulateClientsDropdown();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,ClientId,HourlyRate,Status,StartDate,DueDate")] Project project)
        {
            ModelState.Remove("OrganizationId");
            ModelState.Remove("Organization");
            ModelState.Remove("Client");

            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "ManageProjects"))
                return Forbid();

            if (ModelState.IsValid)
            {
                project.OrganizationId = await _orgService.GetCurrentOrganizationId();
                project.CreatedAt = DateTime.UtcNow;
                _context.Add(project);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            await PopulateClientsDropdown(project.ClientId);
            return View(project);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "ManageProjects"))
                return Forbid();

            var orgId = await _orgService.GetCurrentOrganizationId();
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == orgId);
            if (project == null) return NotFound();
            await PopulateClientsDropdown(project.ClientId);
            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,ClientId,HourlyRate,Status,StartDate,DueDate")] Project project)
        {
            if (id != project.Id) return NotFound();

            ModelState.Remove("OrganizationId");
            ModelState.Remove("Organization");
            ModelState.Remove("Client");

            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "ManageProjects"))
                return Forbid();

            var orgId = await _orgService.GetCurrentOrganizationId();
            var existing = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == orgId);
            if (existing == null) return NotFound();

            if (ModelState.IsValid)
            {
                existing.Name = project.Name;
                existing.Description = project.Description;
                existing.ClientId = project.ClientId;
                existing.HourlyRate = project.HourlyRate;
                existing.Status = project.Status;
                existing.StartDate = project.StartDate;
                existing.DueDate = project.DueDate;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            await PopulateClientsDropdown(project.ClientId);
            return View(project);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "Delete"))
                return Forbid();

            var orgId = await _orgService.GetCurrentOrganizationId();
            var project = await _context.Projects
                .Include(p => p.Client)
                .FirstOrDefaultAsync(m => m.Id == id && m.OrganizationId == orgId);
            if (project == null) return NotFound();
            return View(project);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "Delete"))
                return Forbid();

            var orgId = await _orgService.GetCurrentOrganizationId();
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == orgId);

            if (project != null)
            {
                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateClientsDropdown(int? selectedClientId = null)
        {
            var orgId = await _orgService.GetCurrentOrganizationId();
            var clients = await _context.Clients
                .Where(c => c.OrganizationId == orgId)
                .OrderBy(c => c.Name)
                .ToListAsync();
            ViewBag.ClientId = new SelectList(clients, "Id", "Name", selectedClientId);
        }
    }
}