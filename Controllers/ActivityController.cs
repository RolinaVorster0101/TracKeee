using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TracKeee.Areas.Identity.Data;
using TracKeee.Models;
using TracKeee.Services;

namespace TracKeee.Controllers
{
    [Authorize]
    public class ActivityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly OrganizationService _orgService;

        public ActivityController(ApplicationDbContext context, OrganizationService orgService)
        {
            _context = context;
            _orgService = orgService;
        }

        public async Task<IActionResult> Index(string? entityType, DateTime? dateFrom, DateTime? dateTo)
        {
            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "ViewAllTimeEntries"))
                return Forbid();

            var orgId = await _orgService.GetCurrentOrganizationId();

            var query = _context.ActivityLogs
                .Where(a => a.OrganizationId == orgId);

            if (!string.IsNullOrWhiteSpace(entityType))
                query = query.Where(a => a.EntityType == entityType);

            if (dateFrom.HasValue)
                query = query.Where(a => a.Timestamp >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(a => a.Timestamp <= dateTo.Value.AddDays(1));

            ViewBag.EntityType = entityType;
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");

            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Take(100)
                .ToListAsync();

            return View(logs);
        }
    }
}