using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TracKeee.Areas.Identity.Data;
using TracKeee.Models;
using TracKeee.Services;

namespace TracKeee.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly OrganizationService _orgService;

        public SettingsController(ApplicationDbContext context, OrganizationService orgService)
        {
            _context = context;
            _orgService = orgService;
        }

        public async Task<IActionResult> Index()
        {
            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "ManageSettings"))
                return Forbid();

            var orgId = await _orgService.GetCurrentOrganizationId();
            var profile = await _context.BusinessProfiles
                .FirstOrDefaultAsync(p => p.OrganizationId == orgId);

            if (profile == null)
            {
                profile = new BusinessProfile { OrganizationId = orgId };
            }

            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind("Id,BusinessName,ContactName,Email,Phone,Address,VatNumber,BankName,AccountNumber,BranchCode,AccountType,YocoSecretKey")] BusinessProfile profile, IFormFile? logo)
        {
            ModelState.Remove("OrganizationId");
            ModelState.Remove("Organization");

            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "ManageSettings"))
                return Forbid();

            var orgId = await _orgService.GetCurrentOrganizationId();
            var existing = await _context.BusinessProfiles
                .FirstOrDefaultAsync(p => p.OrganizationId == orgId);

            if (ModelState.IsValid)
            {
                if (existing == null)
                {
                    profile.OrganizationId = orgId;

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

        public async Task<IActionResult> Logo()
        {
            var orgId = await _orgService.GetCurrentOrganizationId();
            var profile = await _context.BusinessProfiles
                .FirstOrDefaultAsync(p => p.OrganizationId == orgId);

            if (profile?.LogoData != null && profile.LogoContentType != null)
                return File(profile.LogoData, profile.LogoContentType);

            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLogo()
        {
            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "ManageSettings"))
                return Forbid();

            var orgId = await _orgService.GetCurrentOrganizationId();
            var profile = await _context.BusinessProfiles
                .FirstOrDefaultAsync(p => p.OrganizationId == orgId);

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