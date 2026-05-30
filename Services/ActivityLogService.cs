using Microsoft.AspNetCore.Identity;
using TracKeee.Areas.Identity.Data;
using TracKeee.Models;

namespace TracKeee.Services
{
    public class ActivityLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly OrganizationService _orgService;

        public ActivityLogService(ApplicationDbContext context, OrganizationService orgService)
        {
            _context = context;
            _orgService = orgService;
        }

        public async Task LogActivity(string action, string entityType, string? entityName = null, int? entityId = null, string? details = null)
        {
            try
            {
                var orgId = await _orgService.GetCurrentOrganizationId();
                var userId = await _orgService.GetCurrentUserId();
                var membership = await _orgService.GetCurrentMembership();

                var log = new ActivityLog
                {
                    OrganizationId = orgId,
                    UserId = userId,
                    UserEmail = membership?.UserId ?? userId,
                    Action = action,
                    EntityType = entityType,
                    EntityName = entityName,
                    EntityId = entityId,
                    Details = details,
                    Timestamp = DateTime.UtcNow
                };

                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Don't let logging failures break the app
            }
        }
    }
}