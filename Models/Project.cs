using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TracKeee.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Project name is required")]
        [StringLength(200)]
        [Display(Name = "Project Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please select a client")]
        [Display(Name = "Client")]
        public int ClientId { get; set; }

        [ForeignKey("ClientId")]
        public Client? Client { get; set; }

        [Display(Name = "Hourly Rate (ZAR)")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 1000000, ErrorMessage = "Rate must be 0 or more")]
        public decimal HourlyRate { get; set; }

        [Required]
        [Display(Name = "Status")]
        public ProjectStatus Status { get; set; } = ProjectStatus.Active;

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Created")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string UserId { get; set; } = string.Empty;
    }

    public enum ProjectStatus
    {
        Active,
        OnHold,
        Completed,
        Cancelled
    }
}