using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TracKeee.Areas.Identity.Data;
using TracKeee.Models;
using TracKeee.Services;

namespace TracKeee.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly OrganizationService _orgService;

        public ReportsController(ApplicationDbContext context, OrganizationService orgService)
        {
            _context = context;
            _orgService = orgService;
        }

        public async Task<IActionResult> Index(DateTime? dateFrom, DateTime? dateTo)
        {
            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "ViewFinancials"))
                return Forbid();

            var orgId = await _orgService.GetCurrentOrganizationId();

            // Default to current month
            dateFrom ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            dateTo ??= DateTime.Today;

            ViewBag.DateFrom = dateFrom.Value.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo.Value.ToString("yyyy-MM-dd");

            // Revenue summary
            var invoices = await _context.Invoices
                .Include(i => i.Client)
                .Where(i => i.OrganizationId == orgId
                    && i.IssueDate >= dateFrom && i.IssueDate <= dateTo)
                .ToListAsync();

            ViewBag.TotalInvoiced = invoices.Sum(i => i.Total);
            ViewBag.TotalPaid = invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Total);
            ViewBag.TotalUnpaid = invoices.Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled).Sum(i => i.Total);
            ViewBag.InvoiceCount = invoices.Count;

            // Hours summary
            var timeEntries = await _context.TimeEntries
                .Include(t => t.Project)
                    .ThenInclude(p => p!.Client)
                .Where(t => t.OrganizationId == orgId
                    && t.Date >= dateFrom && t.Date <= dateTo)
                .ToListAsync();

            ViewBag.TotalHours = timeEntries.Sum(t => t.Hours);
            ViewBag.TotalValue = timeEntries.Sum(t => t.Hours * (t.Project?.HourlyRate ?? 0));
            ViewBag.InvoicedHours = timeEntries.Where(t => t.IsInvoiced).Sum(t => t.Hours);
            ViewBag.UninvoicedHours = timeEntries.Where(t => !t.IsInvoiced).Sum(t => t.Hours);

            // Hours by client
            ViewBag.HoursByClient = timeEntries
                .GroupBy(t => t.Project?.Client?.Name ?? "Unknown")
                .Select(g => new
                {
                    Client = g.Key,
                    Hours = g.Sum(t => t.Hours),
                    Value = g.Sum(t => t.Hours * (t.Project?.HourlyRate ?? 0))
                })
                .OrderByDescending(g => g.Hours)
                .ToList();

            // Hours by project
            ViewBag.HoursByProject = timeEntries
                .GroupBy(t => new { Project = t.Project?.Name ?? "Unknown", Client = t.Project?.Client?.Name ?? "" })
                .Select(g => new
                {
                    Project = g.Key.Project,
                    Client = g.Key.Client,
                    Hours = g.Sum(t => t.Hours),
                    Value = g.Sum(t => t.Hours * (t.Project?.HourlyRate ?? 0))
                })
                .OrderByDescending(g => g.Hours)
                .ToList();

            // Monthly breakdown
            ViewBag.MonthlyBreakdown = timeEntries
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Hours = g.Sum(t => t.Hours),
                    Value = g.Sum(t => t.Hours * (t.Project?.HourlyRate ?? 0))
                })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToList();

            // Invoice breakdown
            ViewBag.InvoiceBreakdown = invoices
                .OrderByDescending(i => i.IssueDate)
                .ToList();

            return View();
        }
    }
}