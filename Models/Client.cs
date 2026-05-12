using System.ComponentModel.DataAnnotations;

namespace TracKeee.Models
{
    public class Client
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Client name is required")]
        [StringLength(200)]
        [Display(Name = "Client Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }

        [EmailAddress]
        [StringLength(200)]
        public string? Email { get; set; }

        [Phone]
        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(50)]
        [Display(Name = "VAT Number")]
        public string? VatNumber { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        [Display(Name = "Created")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Multi-tenant - which user owns this client
        [Required]
        public string UserId { get; set; } = string.Empty;
    }
}