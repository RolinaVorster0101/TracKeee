using System.ComponentModel.DataAnnotations;

namespace TracKeee.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }

        [Required]
        public int OrganizationId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string UserEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string EntityType { get; set; } = string.Empty;

        [StringLength(200)]
        public string? EntityName { get; set; }

        public int? EntityId { get; set; }

        [StringLength(500)]
        public string? Details { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}