using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TracKeee.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        [Display(Name = "Invoice Number")]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a client")]
        [Display(Name = "Client")]
        public int ClientId { get; set; }

        [ForeignKey("ClientId")]
        public Client? Client { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Issue Date")]
        public DateTime IssueDate { get; set; } = DateTime.Today;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);

        [Display(Name = "Subtotal")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Display(Name = "VAT Rate (%)")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal VatRate { get; set; } = 15m;

        [Display(Name = "VAT Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal VatAmount { get; set; }

        [Display(Name = "Total")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        [Required]
        [Display(Name = "Status")]
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        [StringLength(1000)]
        public string? Notes { get; set; }

        [Display(Name = "Created")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        // Navigation - time entries linked to this invoice
        public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
    }

    public enum InvoiceStatus
    {
        Draft,
        Sent,
        Paid,
        Overdue,
        Cancelled
    }
}