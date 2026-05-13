using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TracKeee.Areas.Identity.Data;
using TracKeee.Models;

namespace TracKeee.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProjectsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var projects = await _context.Projects
                .Include(p => p.Client)
                .Where(p => p.UserId == userId)
                .OrderBy(p => p.Name)
                .ToListAsync();
            return View(projects);
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var project = await _context.Projects
                .Include(p => p.Client)
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (project == null) return NotFound();
            return View(project);
        }

        // GET: Projects/Create
        public async Task<IActionResult> Create()
        {
            await PopulateClientsDropdown();
            return View();
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,ClientId,HourlyRate,Status,StartDate,DueDate")] Project project)
        {
            ModelState.Remove("UserId");
            ModelState.Remove("Client");

            if (ModelState.IsValid)
            {
                project.UserId = _userManager.GetUserId(User)!;
                project.CreatedAt = DateTime.UtcNow;
                _context.Add(project);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            await PopulateClientsDropdown(project.ClientId);
            return View(project);
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (project == null) return NotFound();
            await PopulateClientsDropdown(project.ClientId);
            return View(project);
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,ClientId,HourlyRate,Status,StartDate,DueDate")] Project project)
        {
            if (id != project.Id) return NotFound();

            ModelState.Remove("UserId");
            ModelState.Remove("Client");

            var userId = _userManager.GetUserId(User);
            var existing = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (existing == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    existing.Name = project.Name;
                    existing.Description = project.Description;
                    existing.ClientId = project.ClientId;
                    existing.HourlyRate = project.HourlyRate;
                    existing.Status = project.Status;
                    existing.StartDate = project.StartDate;
                    existing.DueDate = project.DueDate;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            await PopulateClientsDropdown(project.ClientId);
            return View(project);
        }

        // GET: Projects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var project = await _context.Projects
                .Include(p => p.Client)
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (project == null) return NotFound();
            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (project != null)
            {
                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProjectExists(int id)
        {
            var userId = _userManager.GetUserId(User);
            return _context.Projects.Any(e => e.Id == id && e.UserId == userId);
        }

        private async Task PopulateClientsDropdown(int? selectedClientId = null)
        {
            var userId = _userManager.GetUserId(User);
            var clients = await _context.Clients
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Name)
                .ToListAsync();
            ViewBag.ClientId = new SelectList(clients, "Id", "Name", selectedClientId);
        }
    }
}