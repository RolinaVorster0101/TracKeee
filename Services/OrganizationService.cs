using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TracKeee.Areas.Identity.Data;
using TracKeee.Models;

namespace TracKeee.Services
{
    public class OrganizationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrganizationService(ApplicationDbContext context, UserManager<IdentityUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<int> GetCurrentOrganizationId()
        {
            var userId = _userManager.GetUserId(_httpContextAccessor.HttpContext!.User);
            var member = await _context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (member == null)
                throw new InvalidOperationException("User is not a member of any organization.");

            return member.OrganizationId;
        }

        public async Task<OrganizationMember?> GetCurrentMembership()
        {
            var userId = _userManager.GetUserId(_httpContextAccessor.HttpContext!.User);
            return await _context.OrganizationMembers
                .Include(m => m.Organization)
                .FirstOrDefaultAsync(m => m.UserId == userId);
        }

        public async Task<OrganizationRole> GetCurrentRole()
        {
            var member = await GetCurrentMembership();
            return member?.Role ?? OrganizationRole.Employee;
        }

        public async Task<string> GetCurrentUserId()
        {
            return _userManager.GetUserId(_httpContextAccessor.HttpContext!.User)!;
        }

        public async Task<Organization> CreateOrganizationForUser(string userId, string organizationName)
        {
            var org = new Organization
            {
                Name = organizationName,
                CreatedAt = DateTime.UtcNow
            };

            _context.Organizations.Add(org);
            await _context.SaveChangesAsync();

            var membership = new OrganizationMember
            {
                OrganizationId = org.Id,
                UserId = userId,
                Role = OrganizationRole.Owner,
                JoinedAt = DateTime.UtcNow
            };

            _context.OrganizationMembers.Add(membership);
            await _context.SaveChangesAsync();

            return org;
        }

        public bool HasPermission(OrganizationRole userRole, string action)
        {
            return action switch
            {
                "ViewAllClients" => userRole != OrganizationRole.Employee,
                "ManageClients" => userRole == OrganizationRole.Owner || userRole == OrganizationRole.Admin,
                "ViewAllProjects" => userRole != OrganizationRole.Employee,
                "ManageProjects" => userRole == OrganizationRole.Owner || userRole == OrganizationRole.Admin,
                "AssignProjects" => userRole == OrganizationRole.Owner || userRole == OrganizationRole.Admin,
                "ViewAllTimeEntries" => userRole != OrganizationRole.Employee,
                "LogTime" => true,
                "ManageInvoices" => userRole != OrganizationRole.Employee,
                "ViewFinancials" => userRole != OrganizationRole.Employee,
                "ManageTeam" => userRole == OrganizationRole.Owner || userRole == OrganizationRole.Admin,
                "Delete" => userRole == OrganizationRole.Owner,
                "ManageSettings" => userRole == OrganizationRole.Owner,
                "ExportData" => userRole != OrganizationRole.Employee,
                _ => false
            };
        }
    }
}