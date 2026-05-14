using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TracKeee.Areas.Identity.Data;
using TracKeee.Models;

namespace TracKeee.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var userId = _userManager.GetUserId(User);

            ViewBag.TotalClients = await _context.Clients
                .CountAsync(c => c.UserId == userId);

            ViewBag.ActiveProjects = await _context.Projects
                .CountAsync(p => p.UserId == userId && p.Status == ProjectStatus.Active);

            ViewBag.TotalProjects = await _context.Projects
                .CountAsync(p => p.UserId == userId);

            var thisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            ViewBag.HoursThisMonth = await _context.TimeEntries
                .Where(t => t.UserId == userId && t.Date >= thisMonth)
                .SumAsync(t => t.Hours);

            ViewBag.TotalHours = await _context.TimeEntries
                .Where(t => t.UserId == userId)
                .SumAsync(t => t.Hours);

            ViewBag.UninvoicedAmount = await _context.TimeEntries
                .Include(t => t.Project)
                .Where(t => t.UserId == userId && !t.IsInvoiced)
                .SumAsync(t => t.Hours * (t.Project!.HourlyRate));

            ViewBag.UnpaidInvoices = await _context.Invoices
                .Where(i => i.UserId == userId && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
                .SumAsync(i => i.Total);

            ViewBag.TotalInvoices = await _context.Invoices
                .CountAsync(i => i.UserId == userId);

            ViewBag.RecentTimeEntries = await _context.TimeEntries
                .Include(t => t.Project)
                    .ThenInclude(p => p!.Client)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentInvoices = await _context.Invoices
                .Include(i => i.Client)
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.IssueDate)
                .Take(5)
                .ToListAsync();
        }

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}