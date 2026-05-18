using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TracKeee.Models;

namespace TracKeee.Areas.Identity.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Organization> Organizations { get; set; }
    public DbSet<OrganizationMember> OrganizationMembers { get; set; }
    public DbSet<ProjectAssignment> ProjectAssignments { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<TimeEntry> TimeEntries { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<BusinessProfile> BusinessProfiles { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Ensure one membership per user per organization
        builder.Entity<OrganizationMember>()
            .HasIndex(m => new { m.OrganizationId, m.UserId })
            .IsUnique();

        // Ensure one assignment per user per project
        builder.Entity<ProjectAssignment>()
            .HasIndex(a => new { a.ProjectId, a.UserId })
            .IsUnique();

        // Ensure one business profile per organization
        builder.Entity<BusinessProfile>()
            .HasIndex(b => b.OrganizationId)
            .IsUnique();

        // Prevent cascade delete cycles with Organization
        builder.Entity<Client>()
            .HasOne(c => c.Organization)
            .WithMany()
            .HasForeignKey(c => c.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Project>()
            .HasOne(p => p.Organization)
            .WithMany()
            .HasForeignKey(p => p.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TimeEntry>()
            .HasOne(t => t.Organization)
            .WithMany()
            .HasForeignKey(t => t.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Invoice>()
            .HasOne(i => i.Organization)
            .WithMany()
            .HasForeignKey(i => i.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<BusinessProfile>()
            .HasOne(b => b.Organization)
            .WithMany()
            .HasForeignKey(b => b.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}