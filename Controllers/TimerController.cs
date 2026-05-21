using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TracKeee.Areas.Identity.Data;
using TracKeee.Models;
using TracKeee.Services;

namespace TracKeee.Controllers
{
    [Authorize]
    public class TimerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly OrganizationService _orgService;

        public TimerController(ApplicationDbContext context, OrganizationService orgService)
        {
            _context = context;
            _orgService = orgService;
        }

        // GET: Timer/Status — returns JSON with current timer state
        [HttpGet]
        public async Task<IActionResult> Status()
        {
            var orgId = await _orgService.GetCurrentOrganizationId();
            var userId = await _orgService.GetCurrentUserId();

            var timer = await _context.ActiveTimers
                .Include(t => t.Project)
                    .ThenInclude(p => p!.Client)
                .FirstOrDefaultAsync(t => t.OrganizationId == orgId && t.UserId == userId);

            if (timer == null)
                return Json(new { running = false });

            return Json(new
            {
                running = true,
                timerId = timer.Id,
                projectName = timer.Project?.Name,
                clientName = timer.Project?.Client?.Name,
                description = timer.Description,
                startedAt = timer.StartedAt.ToString("o"),
                elapsedSeconds = (int)(DateTime.UtcNow - timer.StartedAt).TotalSeconds
            });
        }

        // POST: Timer/Start
        [HttpPost]
        public async Task<IActionResult> Start([FromBody] StartTimerRequest request)
        {
            var orgId = await _orgService.GetCurrentOrganizationId();
            var userId = await _orgService.GetCurrentUserId();

            // Stop any existing timer first
            var existing = await _context.ActiveTimers
                .FirstOrDefaultAsync(t => t.OrganizationId == orgId && t.UserId == userId);

            if (existing != null)
                _context.ActiveTimers.Remove(existing);

            var timer = new ActiveTimer
            {
                OrganizationId = orgId,
                UserId = userId,
                ProjectId = request.ProjectId,
                Description = request.Description,
                StartedAt = DateTime.UtcNow
            };

            _context.ActiveTimers.Add(timer);
            await _context.SaveChangesAsync();

            return Json(new { success = true, timerId = timer.Id });
        }

        // POST: Timer/Stop
        [HttpPost]
        public async Task<IActionResult> Stop()
        {
            var orgId = await _orgService.GetCurrentOrganizationId();
            var userId = await _orgService.GetCurrentUserId();

            var timer = await _context.ActiveTimers
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.OrganizationId == orgId && t.UserId == userId);

            if (timer == null)
                return Json(new { success = false, message = "No timer running" });

            // Calculate hours
            var elapsed = DateTime.UtcNow - timer.StartedAt;
            var hours = Math.Round((decimal)elapsed.TotalHours, 2);

            if (hours < 0.01m)
                hours = 0.01m;

            // Create time entry
            var timeEntry = new TimeEntry
            {
                OrganizationId = orgId,
                UserId = userId,
                ProjectId = timer.ProjectId,
                Date = DateTime.Today,
                Hours = hours,
                Description = timer.Description,
                CreatedAt = DateTime.UtcNow
            };

            _context.TimeEntries.Add(timeEntry);
            _context.ActiveTimers.Remove(timer);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                hours = hours,
                projectName = timer.Project?.Name,
                timeEntryId = timeEntry.Id
            });
        }

        // GET: Timer/Projects — returns JSON list of projects for dropdown
        [HttpGet]
        public async Task<IActionResult> Projects()
        {
            var orgId = await _orgService.GetCurrentOrganizationId();
            var role = await _orgService.GetCurrentRole();
            var userId = await _orgService.GetCurrentUserId();

            IQueryable<Project> query = _context.Projects
                .Include(p => p.Client)
                .Where(p => p.OrganizationId == orgId && p.Status == ProjectStatus.Active);

            if (!_orgService.HasPermission(role, "ViewAllProjects"))
            {
                query = query.Where(p => _context.ProjectAssignments
                    .Any(a => a.ProjectId == p.Id && a.UserId == userId));
            }

            var projects = await query
                .OrderBy(p => p.Client!.Name)
                .ThenBy(p => p.Name)
                .Select(p => new { p.Id, name = p.Client!.Name + " — " + p.Name })
                .ToListAsync();

            return Json(projects);
        }
    }

    public class StartTimerRequest
    {
        public int ProjectId { get; set; }
        public string? Description { get; set; }
    }
}