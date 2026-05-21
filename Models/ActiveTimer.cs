using System.ComponentModel.DataAnnotations;

namespace TracKeee.Models
{
    public class ActiveTimer
    {
        public int Id { get; set; }

        [Required]
        public int OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int ProjectId { get; set; }
        public Project? Project { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    }
}