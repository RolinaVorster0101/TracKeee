using TracKeee.Models;

namespace TracKeee.ViewModels
{
    public class TeamMemberViewModel
    {
        public int MemberId { get; set; }
        public string Email { get; set; } = string.Empty;
        public OrganizationRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}