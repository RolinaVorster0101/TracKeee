using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TracKeee.Areas.Identity.Data;
using TracKeee.Models;
using TracKeee.Services;
using TracKeee.ViewModels;

namespace TracKeee.Controllers
{
    [Authorize]
    public class OrganizationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly OrganizationService _orgService;
        private readonly ActivityLogService _activityLog;

        public OrganizationsController(ApplicationDbContext context, UserManager<IdentityUser> userManager, OrganizationService orgService, ActivityLogService activityLog)
        {
            _context = context;
            _userManager = userManager;
            _orgService = orgService;
            _activityLog = activityLog;
        }

        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);
            var existing = await _context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.UserId == userId);
            if (existing != null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string organizationName)
        {
            if (string.IsNullOrWhiteSpace(organizationName))
            {
                ModelState.AddModelError("", "Organization name is required.");
                return View();
            }

            var userId = _userManager.GetUserId(User);
            var existing = await _context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.UserId == userId);
            if (existing != null)
                return RedirectToAction("Index", "Home");

            await _orgService.CreateOrganizationForUser(userId!, organizationName);
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Team()
        {
            var role = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(role, "ManageTeam"))
                return Forbid();

            var orgId = await _orgService.GetCurrentOrganizationId();
            var org = await _context.Organizations
                .Include(o => o.Members)
                .FirstOrDefaultAsync(o => o.Id == orgId);
            if (org == null) return NotFound();

            var memberDetails = new List<TeamMemberViewModel>();
            foreach (var member in org.Members)
            {
                var user = await _userManager.FindByIdAsync(member.UserId);
                memberDetails.Add(new TeamMemberViewModel
                {
                    MemberId = member.Id,
                    Email = user?.Email ?? "Unknown",
                    Role = member.Role,
                    JoinedAt = member.JoinedAt
                });
            }

            ViewBag.OrganizationName = org.Name;
            return View(memberDetails);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Invite(string email, OrganizationRole role)
        {
            var currentRole = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(currentRole, "ManageTeam"))
                return Forbid();

            var orgId = await _orgService.GetCurrentOrganizationId();
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                TempData["Message"] = $"No registered user found with email {email}. They need to register first.";
                return RedirectToAction(nameof(Team));
            }

            var existingMember = await _context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == user.Id);
            if (existingMember != null)
            {
                TempData["Message"] = $"{email} is already a member of this organization.";
                return RedirectToAction(nameof(Team));
            }

            var otherMembership = await _context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.UserId == user.Id);
            if (otherMembership != null)
            {
                TempData["Message"] = $"{email} already belongs to another organization.";
                return RedirectToAction(nameof(Team));
            }

            var membership = new OrganizationMember
            {
                OrganizationId = orgId,
                UserId = user.Id,
                Role = role,
                JoinedAt = DateTime.UtcNow
            };

            _context.OrganizationMembers.Add(membership);
            await _context.SaveChangesAsync();
            await _activityLog.LogActivity("Invited", "Team", email, membership.Id, $"Added as {role}");

            TempData["Message"] = $"{email} has been added as {role}.";
            return RedirectToAction(nameof(Team));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int memberId, OrganizationRole newRole)
        {
            var currentRole = await _orgService.GetCurrentRole();
            if (!_orgService.HasPermission(currentRole, "ManageTeam"))
                return Forbid();

            var orgId = await _orgService.GetCurrentOrganizationId();
            var member = await _context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.Id == memberId && m.OrganizationId == orgId);
            if (member == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (member.UserId == userId)
            {
                TempData["Message"] = "You cannot change your own role.";
                return RedirectToAction(nameof(Team));
            }

            member.Role = newRole;
            await _context.SaveChangesAsync();
            var user = await _userManager.FindByIdAsync(member.UserId);
            await _activityLog.LogActivity("Changed Role", "Team", user?.Email, member.Id, $"Changed to {newRole}");

            TempData["Message"] = "Role updated successfully.";
            return RedirectToAction(nameof(Team));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int memberId)
        {
            var currentRole = await _orgService.GetCurrentRole();
            if (currentRole != OrganizationRole.Owner)
                return Forbid();

            var orgId = await _orgService.GetCurrentOrganizationId();
            var member = await _context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.Id == memberId && m.OrganizationId == orgId);
            if (member == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (member.UserId == userId)
            {
                TempData["Message"] = "You cannot remove yourself from the organization.";
                return RedirectToAction(nameof(Team));
            }

            _context.OrganizationMembers.Remove(member);
            await _context.SaveChangesAsync();
            var removedUser = await _userManager.FindByIdAsync(member.UserId);
            await _activityLog.LogActivity("Removed", "Team", removedUser?.Email, member.Id);

            TempData["Message"] = "Team member removed.";
            return RedirectToAction(nameof(Team));
        }

        public async Task<IActionResult> CheckInvite()
        {
            var userId = _userManager.GetUserId(User);
            var existing = await _context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (existing != null)
                return RedirectToAction("Index", "Home");

            TempData["InviteMessage"] = "No invite found yet. Ask your team admin to add your email address, then try again.";
            return RedirectToAction(nameof(Create));
        }
    }
}