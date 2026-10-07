using AcxiomCRM.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Customer>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Phone).IsUnique();
            entity.HasIndex(e => e.CustomerCode).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<Lead>(entity =>
        {
            entity.Property(e => e.ExpectedValue).HasPrecision(18, 2);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Phone).IsUnique();
            entity.HasIndex(e => e.LeadCode).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<Opportunity>(entity =>
        {
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<FollowUp>(entity =>
        {
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<Activity>(entity =>
        {
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }
}

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? CurrentRole { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public DateTime? LockoutEndDate { get; set; }
}
