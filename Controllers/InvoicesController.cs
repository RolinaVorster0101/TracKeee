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
    public class InvoicesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public InvoicesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Invoices
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var invoices = await _context.Invoices
                .Include(i => i.Client)
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.IssueDate)
                .ToListAsync();
            return View(invoices);
        }

        // GET: Invoices/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.TimeEntries)
                    .ThenInclude(t => t.Project)
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

            if (invoice == null) return NotFound();
            return View(invoice);
        }

        // GET: Invoices/Generate
        public async Task<IActionResult> Generate()
        {
            var userId = _userManager.GetUserId(User);

            // Only show clients that have uninvoiced time entries
            var clientsWithTime = await _context.TimeEntries
                .Include(t => t.Project)
                    .ThenInclude(p => p!.Client)
                .Where(t => t.UserId == userId && !t.IsInvoiced)
                .Select(t => t.Project!.Client)
                .Distinct()
                .OrderBy(c => c!.Name)
                .ToListAsync();

            if (!clientsWithTime.Any())
            {
                TempData["Message"] = "No uninvoiced time entries found. Log some time first.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ClientId = new SelectList(clientsWithTime, "Id", "Name");
            return View();
        }

        // POST: Invoices/Generate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(int ClientId)
        {
            var userId = _userManager.GetUserId(User);

            // Get all uninvoiced time entries for this client
            var timeEntries = await _context.TimeEntries
                .Include(t => t.Project)
                .Where(t => t.UserId == userId
                    && !t.IsInvoiced
                    && t.Project!.ClientId == ClientId)
                .ToListAsync();

            if (!timeEntries.Any())
            {
                TempData["Message"] = "No uninvoiced time entries found for this client.";
                return RedirectToAction(nameof(Generate));
            }

            // Calculate totals
            var subtotal = timeEntries.Sum(t => t.Hours * (t.Project?.HourlyRate ?? 0));
            var vatRate = 15m;
            var vatAmount = subtotal * vatRate / 100;
            var total = subtotal + vatAmount;

            // Generate invoice number
            var invoiceCount = await _context.Invoices
                .Where(i => i.UserId == userId)
                .CountAsync();
            var invoiceNumber = $"INV-{DateTime.Now:yyyyMM}-{(invoiceCount + 1):D4}";

            // Create invoice
            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                ClientId = ClientId,
                IssueDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(30),
                Subtotal = subtotal,
                VatRate = vatRate,
                VatAmount = vatAmount,
                Total = total,
                Status = InvoiceStatus.Draft,
                UserId = userId!,
                CreatedAt = DateTime.UtcNow
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // Link time entries to invoice and mark as invoiced
            foreach (var entry in timeEntries)
            {
                entry.InvoiceId = invoice.Id;
                entry.IsInvoiced = true;
            }
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = invoice.Id });
        }

        // POST: Invoices/MarkAsSent/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsSent(int id)
        {
            var userId = _userManager.GetUserId(User);
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

            if (invoice == null) return NotFound();

            invoice.Status = InvoiceStatus.Sent;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Invoices/MarkAsPaid/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var userId = _userManager.GetUserId(User);
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

            if (invoice == null) return NotFound();

            invoice.Status = InvoiceStatus.Paid;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Invoices/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

            if (invoice == null) return NotFound();
            return View(invoice);
        }

        // POST: Invoices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var invoice = await _context.Invoices
                .Include(i => i.TimeEntries)
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

            if (invoice != null)
            {
                // Unlink time entries and mark as uninvoiced
                foreach (var entry in invoice.TimeEntries)
                {
                    entry.InvoiceId = null;
                    entry.IsInvoiced = false;
                }

                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}