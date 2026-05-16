using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TracKeee.Areas.Identity.Data;
using TracKeee.Models;

namespace TracKeee.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public SettingsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Settings
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var profile = await _context.BusinessProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                profile = new BusinessProfile { UserId = userId! };
            }

            return View(profile);
        }

        // POST: Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind("Id,BusinessName,ContactName,Email,Phone,Address,VatNumber,BankName,AccountNumber,BranchCode,AccountType,YocoSecretKey")] BusinessProfile profile, IFormFile? logo)
        {
            ModelState.Remove("UserId");

            var userId = _userManager.GetUserId(User);
            var existing = await _context.BusinessProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (ModelState.IsValid)
            {
                if (existing == null)
                {
                    profile.UserId = userId!;

                    if (logo != null && logo.Length > 0)
                    {
                        using var ms = new MemoryStream();
                        await logo.CopyToAsync(ms);
                        profile.LogoData = ms.ToArray();
                        profile.LogoContentType = logo.ContentType;
                    }

                    _context.BusinessProfiles.Add(profile);
                }
                else
                {
                    existing.BusinessName = profile.BusinessName;
                    existing.ContactName = profile.ContactName;
                    existing.Email = profile.Email;
                    existing.Phone = profile.Phone;
                    existing.Address = profile.Address;
                    existing.VatNumber = profile.VatNumber;
                    existing.BankName = profile.BankName;
                    existing.AccountNumber = profile.AccountNumber;
                    existing.BranchCode = profile.BranchCode;
                    existing.AccountType = profile.AccountType;
                    if (!string.IsNullOrEmpty(profile.YocoSecretKey))
                        existing.YocoSecretKey = profile.YocoSecretKey;

                    if (logo != null && logo.Length > 0)
                    {
                        using var ms = new MemoryStream();
                        await logo.CopyToAsync(ms);
                        existing.LogoData = ms.ToArray();
                        existing.LogoContentType = logo.ContentType;
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Message"] = "Business profile saved successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(profile);
        }

        // GET: Settings/Logo
        public async Task<IActionResult> Logo()
        {
            var userId = _userManager.GetUserId(User);
            var profile = await _context.BusinessProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile?.LogoData != null && profile.LogoContentType != null)
            {
                return File(profile.LogoData, profile.LogoContentType);
            }

            return NotFound();
        }

        // POST: Settings/RemoveLogo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLogo()
        {
            var userId = _userManager.GetUserId(User);
            var profile = await _context.BusinessProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile != null)
            {
                profile.LogoData = null;
                profile.LogoContentType = null;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}