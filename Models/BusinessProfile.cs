using System.ComponentModel.DataAnnotations;

namespace TracKeee.Models
{
    public class BusinessProfile
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Business name is required")]
        [StringLength(200)]
        [Display(Name = "Business / Company Name")]
        public string BusinessName { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Contact Name")]
        public string? ContactName { get; set; }

        [EmailAddress]
        [StringLength(200)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Phone]
        [StringLength(50)]
        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [StringLength(500)]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [StringLength(50)]
        [Display(Name = "VAT Registration Number")]
        public string? VatNumber { get; set; }

        // Banking Details
        [StringLength(100)]
        [Display(Name = "Bank Name")]
        public string? BankName { get; set; }

        [StringLength(50)]
        [Display(Name = "Account Number")]
        public string? AccountNumber { get; set; }

        [StringLength(20)]
        [Display(Name = "Branch Code")]
        public string? BranchCode { get; set; }

        [StringLength(50)]
        [Display(Name = "Account Type")]
        public string? AccountType { get; set; }

        // Logo
        [Display(Name = "Logo")]
        public byte[]? LogoData { get; set; }

        [StringLength(50)]
        public string? LogoContentType { get; set; }

        // Yoco Payment Integration
        [StringLength(200)]
        [Display(Name = "Yoco Secret Key")]
        public string? YocoSecretKey { get; set; }

        [Required]
        public int OrganizationId { get; set; }
        public Organization? Organization { get; set; }
    }
}