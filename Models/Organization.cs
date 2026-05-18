using System.ComponentModel.DataAnnotations;

namespace TracKeee.Models
{
    public class Organization
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Organization name is required")]
        [StringLength(200)]
        [Display(Name = "Organization Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Created")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
    }

    public class OrganizationMember
    {
        public int Id { get; set; }

        [Required]
        public int OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public OrganizationRole Role { get; set; } = OrganizationRole.Employee;

        [Display(Name = "Joined")]
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }

    public class ProjectAssignment
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }
        public Project? Project { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Display(Name = "Assigned")]
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }

    public enum OrganizationRole
    {
        Owner,
        Admin,
        Accountant,
        Employee
    }
}