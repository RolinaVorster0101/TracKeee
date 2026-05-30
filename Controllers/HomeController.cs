using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TracKeee.Areas.Identity.Data;
using TracKeee.Models;
using TracKeee.Services;

namespace TracKeee.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly OrganizationService _orgService;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, OrganizationService orgService)
    {
        _logger = logger;
        _context = context;
        _orgService = orgService;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            try
            {
                var orgId = await _orgService.GetCurrentOrganizationId();
                var role = await _orgService.GetCurrentRole();
                var userId = await _orgService.GetCurrentUserId();

                ViewBag.TotalClients = await _context.Clients
                    .CountAsync(c => c.OrganizationId == orgId);

                ViewBag.ActiveProjects = await _context.Projects
                    .CountAsync(p => p.OrganizationId == orgId && p.Status == ProjectStatus.Active);

                ViewBag.TotalProjects = await _context.Projects
                    .CountAsync(p => p.OrganizationId == orgId);

                var thisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

                if (_orgService.HasPermission(role, "ViewAllTimeEntries"))
                {
                    ViewBag.HoursThisMonth = await _context.TimeEntries
                        .Where(t => t.OrganizationId == orgId && t.Date >= thisMonth)
                        .SumAsync(t => t.Hours);

                    ViewBag.TotalHours = await _context.TimeEntries
                        .Where(t => t.OrganizationId == orgId)
                        .SumAsync(t => t.Hours);
                }
                else
                {
                    ViewBag.HoursThisMonth = await _context.TimeEntries
                        .Where(t => t.OrganizationId == orgId && t.UserId == userId && t.Date >= thisMonth)
                        .SumAsync(t => t.Hours);

                    ViewBag.TotalHours = await _context.TimeEntries
                        .Where(t => t.OrganizationId == orgId && t.UserId == userId)
                        .SumAsync(t => t.Hours);
                }

                ViewBag.UninvoicedAmount = await _context.TimeEntries
                    .Include(t => t.Project)
                    .Where(t => t.OrganizationId == orgId && !t.IsInvoiced)
                    .SumAsync(t => t.Hours * (t.Project!.HourlyRate));

                ViewBag.UnpaidInvoices = await _context.Invoices
                    .Where(i => i.OrganizationId == orgId && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
                    .SumAsync(i => i.Total);

                ViewBag.TotalInvoices = await _context.Invoices
                    .CountAsync(i => i.OrganizationId == orgId);

                if (_orgService.HasPermission(role, "ViewAllProjects"))
                {
                    ViewBag.RecentProjects = await _context.Projects
                        .Include(p => p.Client)
                        .Where(p => p.OrganizationId == orgId)
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(5)
                        .ToListAsync();
                }
                else
                {
                    ViewBag.RecentProjects = await _context.Projects
                        .Include(p => p.Client)
                        .Where(p => p.OrganizationId == orgId
                            && _context.ProjectAssignments.Any(a => a.ProjectId == p.Id && a.UserId == userId))
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(5)
                        .ToListAsync();
                }

                if (_orgService.HasPermission(role, "ViewAllTimeEntries"))
                {
                    ViewBag.RecentTimeEntries = await _context.TimeEntries
                        .Include(t => t.Project)
                            .ThenInclude(p => p!.Client)
                        .Where(t => t.OrganizationId == orgId)
                        .OrderByDescending(t => t.Date)
                        .Take(5)
                        .ToListAsync();
                }
                else
                {
                    ViewBag.RecentTimeEntries = await _context.TimeEntries
                        .Include(t => t.Project)
                            .ThenInclude(p => p!.Client)
                        .Where(t => t.OrganizationId == orgId && t.UserId == userId)
                        .OrderByDescending(t => t.Date)
                        .Take(5)
                        .ToListAsync();
                }

                ViewBag.RecentInvoices = await _context.Invoices
                    .Include(i => i.Client)
                    .Where(i => i.OrganizationId == orgId)
                    .OrderByDescending(i => i.IssueDate)
                    .Take(5)
                    .ToListAsync();

                ViewBag.UserRole = role;
            }
            catch (InvalidOperationException)
            {
                // User has no organization yet — redirect to create one
                return RedirectToAction("Create", "Organizations");
            }
        }

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Terms()
    {
        return View();
    }

    public IActionResult CookiePolicy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ChartHoursByMonth()
    {
        var orgId = await _orgService.GetCurrentOrganizationId();
        var sixMonthsAgo = DateTime.Today.AddMonths(-5);
        var startDate = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);

        var data = await _context.TimeEntries
            .Where(t => t.OrganizationId == orgId && t.Date >= startDate)
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new
            {
                year = g.Key.Year,
                month = g.Key.Month,
                hours = g.Sum(t => t.Hours)
            })
            .OrderBy(g => g.year).ThenBy(g => g.month)
            .ToListAsync();

        return Json(data);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ChartRevenueByMonth()
    {
        var orgId = await _orgService.GetCurrentOrganizationId();
        var sixMonthsAgo = DateTime.Today.AddMonths(-5);
        var startDate = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);

        var data = await _context.Invoices
            .Where(i => i.OrganizationId == orgId && i.IssueDate >= startDate)
            .GroupBy(i => new { i.IssueDate.Year, i.IssueDate.Month })
            .Select(g => new
            {
                year = g.Key.Year,
                month = g.Key.Month,
                total = g.Sum(i => i.Total)
            })
            .OrderBy(g => g.year).ThenBy(g => g.month)
            .ToListAsync();

        return Json(data);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ChartHoursByClient()
    {
        var orgId = await _orgService.GetCurrentOrganizationId();

        var data = await _context.TimeEntries
            .Include(t => t.Project)
                .ThenInclude(p => p!.Client)
            .Where(t => t.OrganizationId == orgId)
            .GroupBy(t => t.Project!.Client!.Name)
            .Select(g => new
            {
                client = g.Key,
                hours = g.Sum(t => t.Hours)
            })
            .OrderByDescending(g => g.hours)
            .Take(8)
            .ToListAsync();

        return Json(data);
    }

    [Route("Error/{code}")]
    public IActionResult HttpError(int code)
    {
        ViewBag.ErrorCode = code;
        ViewBag.ErrorMessage = code switch
        {
            404 => "Page not found",
            403 => "Access denied",
            500 => "Something went wrong",
            _ => "An error occurred"
        };
        return View("HttpError");
    }
}