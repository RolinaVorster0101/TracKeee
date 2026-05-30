using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TracKeee.Areas.Identity.Data;
using TracKeee.Models;
using TracKeee.Services;

namespace TracKeee.Controllers
{
    public class PortalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly InvoiceEmailService _emailService;

        public PortalController(ApplicationDbContext context, InvoiceEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: Portal/{token}
        [HttpGet("Portal/{token}")]
        public async Task<IActionResult> Index(string token)
        {
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.PortalToken == token);

            if (client == null) return NotFound();

            // Check if already verified in this session
            var verifiedClientId = HttpContext.Session.GetInt32("PortalClientId");
            if (verifiedClientId == client.Id)
                return RedirectToAction(nameof(Dashboard), new { token });

            ViewBag.Token = token;
            ViewBag.ClientName = client.Name;
            return View();
        }

        // POST: Portal/SendCode
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCode(string token, string email)
        {
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.PortalToken == token);

            if (client == null) return NotFound();

            if (string.IsNullOrEmpty(client.Email) || client.Email.ToLower() != email.Trim().ToLower())
            {
                TempData["Error"] = "The email address doesn't match our records for this client.";
                return RedirectToAction(nameof(Index), new { token });
            }

            // Generate 6-digit code
            var code = new Random().Next(100000, 999999).ToString();
            client.PortalVerificationCode = code;
            client.PortalCodeExpiry = DateTime.UtcNow.AddMinutes(10);
            await _context.SaveChangesAsync();

            // Send code via email
            var emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 400px;'>
                    <h2>Your verification code</h2>
                    <p>Use this code to access your client portal:</p>
                    <h1 style='font-size: 36px; letter-spacing: 8px; color: #0C447C; text-align: center; padding: 20px;'>{code}</h1>
                    <p style='color: #666;'>This code expires in 10 minutes.</p>
                    <p style='color: #999; font-size: 12px;'>If you didn't request this code, you can safely ignore this email.</p>
                </div>";

            var sent = await _emailService.SendInvoiceEmail(
                            client.Email,
                            client.ContactPerson ?? client.Name,
                            "Your TracKeee portal verification code",
                            emailBody,
                            Array.Empty<byte>(),
                            "",
                            "TracKeee");

            if (!sent)
            {
                TempData["Error"] = "Failed to send verification code. Please try again.";
                return RedirectToAction(nameof(Index), new { token });
            }

            TempData["CodeSent"] = true;
            ViewBag.Token = token;
            ViewBag.Email = email;
            return View("VerifyCode", new PortalVerifyModel { Token = token, Email = email });
        }

        // POST: Portal/VerifyCode
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCode(PortalVerifyModel model)
        {
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.PortalToken == model.Token);

            if (client == null) return NotFound();

            if (client.PortalVerificationCode != model.Code
                || client.PortalCodeExpiry == null
                || DateTime.UtcNow > client.PortalCodeExpiry)
            {
                TempData["Error"] = "Invalid or expired code. Please try again.";
                return View(model);
            }

            // Clear the code
            client.PortalVerificationCode = null;
            client.PortalCodeExpiry = null;
            await _context.SaveChangesAsync();

            // Set session
            HttpContext.Session.SetInt32("PortalClientId", client.Id);

            return RedirectToAction(nameof(Dashboard), new { token = model.Token });
        }

        // GET: Portal/{token}/Dashboard
        [HttpGet("Portal/{token}/Dashboard")]
        public async Task<IActionResult> Dashboard(string token)
        {
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.PortalToken == token);

            if (client == null) return NotFound();

            // Verify session
            var verifiedClientId = HttpContext.Session.GetInt32("PortalClientId");
            if (verifiedClientId != client.Id)
                return RedirectToAction(nameof(Index), new { token });

            var invoices = await _context.Invoices
                .Where(i => i.ClientId == client.Id
                    && i.Status != InvoiceStatus.Draft
                    && i.Status != InvoiceStatus.Cancelled)
                .OrderByDescending(i => i.IssueDate)
                .ToListAsync();

            ViewBag.Client = client;
            ViewBag.Token = token;
            ViewBag.TotalOutstanding = invoices
                .Where(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue)
                .Sum(i => i.Total);

            return View(invoices);
        }
    }

    public class PortalVerifyModel
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}