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
    public class InvoicesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly OrganizationService _orgService;
        private readonly InvoicePdfService _pdfService;
        private readonly YocoPaymentService _yocoService;

        public InvoicesController(ApplicationDbContext context, OrganizationService orgService, InvoicePdfService pdfService, YocoPaymentService yocoService)
        {
            _context = context;
            _orgService = orgService;
            _pdfService = pdfService;
            _yocoService = yocoService;
        }

        public async Task<IActionResult> Index()
        {
            var orgId = await _orgService.GetCurrentOrganizationId();
            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "ManageInvoices"))
                return Forbid();

            var invoices = await _context.Invoices
                .Include(i => i.Client)
                .Where(i => i.OrganizationId == orgId)
                .OrderByDescending(i => i.IssueDate)
                .ToListAsync();
            return View(invoices);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var orgId = await _orgService.GetCurrentOrganizationId();

            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.TimeEntries)
                    .ThenInclude(t => t.Project)
                .FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == orgId);
            if (invoice == null) return NotFound();

            ViewBag.BusinessProfile = await _context.BusinessProfiles
                .FirstOrDefaultAsync(p => p.OrganizationId == orgId);

            return View(invoice);
        }

        public async Task<IActionResult> Generate()
        {
            var orgId = await _orgService.GetCurrentOrganizationId();
            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "ManageInvoices"))
                return Forbid();

            var clientsWithTime = await _context.TimeEntries
                .Include(t => t.Project)
                    .ThenInclude(p => p!.Client)
                .Where(t => t.OrganizationId == orgId && !t.IsInvoiced)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(int ClientId)
        {
            var orgId = await _orgService.GetCurrentOrganizationId();
            var userId = await _orgService.GetCurrentUserId();

            var timeEntries = await _context.TimeEntries
                .Include(t => t.Project)
                .Where(t => t.OrganizationId == orgId
                    && !t.IsInvoiced
                    && t.Project!.ClientId == ClientId)
                .ToListAsync();

            if (!timeEntries.Any())
            {
                TempData["Message"] = "No uninvoiced time entries found for this client.";
                return RedirectToAction(nameof(Generate));
            }

            var subtotal = timeEntries.Sum(t => t.Hours * (t.Project?.HourlyRate ?? 0));
            var vatRate = 15m;
            var vatAmount = subtotal * vatRate / 100;
            var total = subtotal + vatAmount;

            var invoiceCount = await _context.Invoices
                .Where(i => i.OrganizationId == orgId)
                .CountAsync();
            var invoiceNumber = $"INV-{DateTime.Now:yyyyMM}-{(invoiceCount + 1):D4}";

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
                OrganizationId = orgId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            foreach (var entry in timeEntries)
            {
                entry.InvoiceId = invoice.Id;
                entry.IsInvoiced = true;
            }
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = invoice.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsSent(int id)
        {
            var orgId = await _orgService.GetCurrentOrganizationId();
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == orgId);
            if (invoice == null) return NotFound();

            invoice.Status = InvoiceStatus.Sent;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var orgId = await _orgService.GetCurrentOrganizationId();
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == orgId);
            if (invoice == null) return NotFound();

            invoice.Status = InvoiceStatus.Paid;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "Delete"))
                return Forbid();

            var orgId = await _orgService.GetCurrentOrganizationId();
            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == orgId);
            if (invoice == null) return NotFound();
            return View(invoice);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "Delete"))
                return Forbid();

            var orgId = await _orgService.GetCurrentOrganizationId();
            var invoice = await _context.Invoices
                .Include(i => i.TimeEntries)
                .FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == orgId);

            if (invoice != null)
            {
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

        public async Task<IActionResult> DownloadPdf(int? id)
        {
            if (id == null) return NotFound();
            var orgId = await _orgService.GetCurrentOrganizationId();

            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.TimeEntries)
                    .ThenInclude(t => t.Project)
                .FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == orgId);
            if (invoice == null) return NotFound();

            var profile = await _context.BusinessProfiles
                .FirstOrDefaultAsync(p => p.OrganizationId == orgId);
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var paymentUrl = $"{baseUrl}/Invoices/PayInvoice/{invoice.Id}";
            var pdfBytes = _pdfService.GenerateInvoicePdf(invoice, profile, paymentUrl);
            return File(pdfBytes, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int id)
        {
            var orgId = await _orgService.GetCurrentOrganizationId();
            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == orgId);
            if (invoice == null) return NotFound();

            var profile = await _context.BusinessProfiles
                .FirstOrDefaultAsync(p => p.OrganizationId == orgId);

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
                return Redirect(checkout.RedirectUrl);

            TempData["Message"] = "Unable to create payment. Please try again later.";
            return RedirectToAction(nameof(Details), new { id });
        }

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

        [AllowAnonymous]
        public async Task<IActionResult> PaymentCancel(int invoiceId)
        {
            TempData["Message"] = "Payment was cancelled.";
            return RedirectToAction(nameof(PayInvoice), new { id = invoiceId });
        }

        [AllowAnonymous]
        public async Task<IActionResult> PaymentFailed(int invoiceId)
        {
            TempData["Message"] = "Payment failed. Please try again.";
            return RedirectToAction(nameof(PayInvoice), new { id = invoiceId });
        }

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
                TempData["Message"] = "This invoice has already been paid.";

            ViewBag.BusinessProfile = await _context.BusinessProfiles
                .FirstOrDefaultAsync(p => p.OrganizationId == invoice.OrganizationId);

            return View(invoice);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null) return NotFound();

            var profile = await _context.BusinessProfiles
                .FirstOrDefaultAsync(p => p.OrganizationId == invoice.OrganizationId);

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
                return Redirect(checkout.RedirectUrl);

            TempData["Message"] = "Unable to create payment. Please try again later.";
            return RedirectToAction(nameof(PayInvoice), new { id });
        }

        [AllowAnonymous]
        public async Task<IActionResult> BusinessLogo(string userId)
        {
            // Find org by any member's userId
            var member = await _context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.UserId == userId);
            if (member == null) return NotFound();

            var profile = await _context.BusinessProfiles
                .FirstOrDefaultAsync(p => p.OrganizationId == member.OrganizationId);

            if (profile?.LogoData != null && profile.LogoContentType != null)
                return File(profile.LogoData, profile.LogoContentType);

            return NotFound();
        }
    }
}