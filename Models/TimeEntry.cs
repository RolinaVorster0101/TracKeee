using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TracKeee.Models
{
    public class TimeEntry
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select a project")]
        [Display(Name = "Project")]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }

        [Required(ErrorMessage = "Date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Date")]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Hours are required")]
        [Display(Name = "Hours")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 24, ErrorMessage = "Hours must be between 0.01 and 24")]
        public decimal Hours { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Invoiced")]
        public bool IsInvoiced { get; set; } = false;

        [Display(Name = "Created")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string UserId { get; set; } = string.Empty;
    }
}