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
    public class TimeEntriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public TimeEntriesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: TimeEntries
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var entries = await _context.TimeEntries
                .Include(t => t.Project)
                    .ThenInclude(p => p!.Client)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();
            return View(entries);
        }

        // GET: TimeEntries/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var entry = await _context.TimeEntries
                .Include(t => t.Project)
                    .ThenInclude(p => p!.Client)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (entry == null) return NotFound();
            return View(entry);
        }

        // GET: TimeEntries/Create
        public async Task<IActionResult> Create()
        {
            await PopulateProjectsDropdown();
            return View();
        }

        // POST: TimeEntries/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProjectId,Date,Hours,Description")] TimeEntry entry)
        {
            ModelState.Remove("UserId");
            ModelState.Remove("Project");

            if (ModelState.IsValid)
            {
                entry.UserId = _userManager.GetUserId(User)!;
                entry.CreatedAt = DateTime.UtcNow;
                _context.Add(entry);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            await PopulateProjectsDropdown(entry.ProjectId);
            return View(entry);
        }

        // GET: TimeEntries/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var entry = await _context.TimeEntries
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (entry == null) return NotFound();
            await PopulateProjectsDropdown(entry.ProjectId);
            return View(entry);
        }

        // POST: TimeEntries/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ProjectId,Date,Hours,Description")] TimeEntry entry)
        {
            if (id != entry.Id) return NotFound();

            ModelState.Remove("UserId");
            ModelState.Remove("Project");

            var userId = _userManager.GetUserId(User);
            var existing = await _context.TimeEntries
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (existing == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    existing.ProjectId = entry.ProjectId;
                    existing.Date = entry.Date;
                    existing.Hours = entry.Hours;
                    existing.Description = entry.Description;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TimeEntryExists(entry.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            await PopulateProjectsDropdown(entry.ProjectId);
            return View(entry);
        }

        // GET: TimeEntries/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var entry = await _context.TimeEntries
                .Include(t => t.Project)
                    .ThenInclude(p => p!.Client)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (entry == null) return NotFound();
            return View(entry);
        }

        // POST: TimeEntries/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var entry = await _context.TimeEntries
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (entry != null)
            {
                _context.TimeEntries.Remove(entry);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TimeEntryExists(int id)
        {
            var userId = _userManager.GetUserId(User);
            return _context.TimeEntries.Any(e => e.Id == id && e.UserId == userId);
        }

        private async Task PopulateProjectsDropdown(int? selectedProjectId = null)
        {
            var userId = _userManager.GetUserId(User);
            var projects = await _context.Projects
                .Include(p => p.Client)
                .Where(p => p.UserId == userId && p.Status == ProjectStatus.Active)
                .OrderBy(p => p.Client!.Name)
                .ThenBy(p => p.Name)
                .Select(p => new { p.Id, DisplayName = p.Client!.Name + " — " + p.Name })
                .ToListAsync();
            ViewBag.ProjectId = new SelectList(projects, "Id", "DisplayName", selectedProjectId);
        }
    }
}