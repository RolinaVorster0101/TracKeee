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
        private readonly TracKeee.Services.InvoicePdfService _pdfService;
        private readonly TracKeee.Services.YocoPaymentService _yocoService;

        public InvoicesController(ApplicationDbContext context, UserManager<IdentityUser> userManager, TracKeee.Services.InvoicePdfService pdfService, TracKeee.Services.YocoPaymentService yocoService)
        {
            _context = context;
            _userManager = userManager;
            _pdfService = pdfService;
            _yocoService = yocoService;
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

        // GET: Invoices/DownloadPdf/5
        public async Task<IActionResult> DownloadPdf(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.TimeEntries)
                    .ThenInclude(t => t.Project)
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

            if (invoice == null) return NotFound();

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var paymentUrl = $"{baseUrl}/Invoices/PayInvoice/{invoice.Id}";
            var profile = await _context.BusinessProfiles
    .FirstOrDefaultAsync(p => p.UserId == userId);
            var pdfBytes = _pdfService.GenerateInvoicePdf(invoice, profile, paymentUrl);
            return File(pdfBytes, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
        }

        // POST: Invoices/Pay/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int id)
        {
            var userId = _userManager.GetUserId(User);
            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

            if (invoice == null) return NotFound();

            var profile = await _context.BusinessProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile?.YocoSecretKey == null)
            {
                TempData["Message"] = "Please add your Yoco secret key in Settings first.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var checkout = await _yocoService.CreateCheckout(
                profile.YocoSecretKey,
                invoice.Total,
                invoice.InvoiceNumber,
                $"{baseUrl}/Invoices/PaymentSuccess?invoiceId={invoice.Id}",
                $"{baseUrl}/Invoices/PaymentCancel?invoiceId={invoice.Id}",
                $"{baseUrl}/Invoices/PaymentFailed?invoiceId={invoice.Id}"
            );

            if (checkout?.RedirectUrl != null)
            {
                return Redirect(checkout.RedirectUrl);
            }

            TempData["Message"] = "Unable to create payment. Please try again later.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Invoices/PaymentSuccess
        [AllowAnonymous]
        public async Task<IActionResult> PaymentSuccess(int invoiceId)
        {
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice != null)
            {
                invoice.Status = InvoiceStatus.Paid;
                await _context.SaveChangesAsync();
            }

            return View(invoice);
        }

        // GET: Invoices/PaymentCancel
        public async Task<IActionResult> PaymentCancel(int invoiceId)
        {
            TempData["Message"] = "Payment was cancelled.";
            return RedirectToAction(nameof(Details), new { id = invoiceId });
        }

        // GET: Invoices/PaymentFailed
        public async Task<IActionResult> PaymentFailed(int invoiceId)
        {
            TempData["Message"] = "Payment failed. Please try again.";
            return RedirectToAction(nameof(Details), new { id = invoiceId });
        }
        // GET: Invoices/PayInvoice/5 — PUBLIC page for clients to pay
        [AllowAnonymous]
        public async Task<IActionResult> PayInvoice(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.TimeEntries)
                    .ThenInclude(t => t.Project)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();
            if (invoice.Status == InvoiceStatus.Paid)
            {
                TempData["Message"] = "This invoice has already been paid.";
            }

            return View(invoice);
        }

        // POST: Invoices/ProcessPayment/5 — PUBLIC payment processing
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();

            // Get the freelancer's Yoco key from their Business Profile
            var profile = await _context.BusinessProfiles
                .FirstOrDefaultAsync(p => p.UserId == invoice.UserId);

            if (profile?.YocoSecretKey == null)
            {
                TempData["Message"] = "Payment is not configured for this invoice. Please contact the sender.";
                return RedirectToAction(nameof(PayInvoice), new { id });
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var checkout = await _yocoService.CreateCheckout(
                profile.YocoSecretKey,
                invoice.Total,
                invoice.InvoiceNumber,
                $"{baseUrl}/Invoices/PaymentSuccess?invoiceId={invoice.Id}",
                $"{baseUrl}/Invoices/PayInvoice/{invoice.Id}?cancelled=true",
                $"{baseUrl}/Invoices/PayInvoice/{invoice.Id}?failed=true"
            );

            if (checkout?.RedirectUrl != null)
            {
                return Redirect(checkout.RedirectUrl);
            }

            TempData["Message"] = "Unable to create payment. Please try again later.";
            return RedirectToAction(nameof(PayInvoice), new { id });
        }
    }
}