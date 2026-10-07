using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Web.Models;

public class Customer
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string CustomerCode { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(150)]
    public string? CompanyName { get; set; }

    [StringLength(250)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [Required]
    public string Status { get; set; } = "Active";

    public string? AssignedTo { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsDeleted { get; set; }

    public ICollection<Lead>? Leads { get; set; }
    public ICollection<Opportunity>? Opportunities { get; set; }
    public ICollection<FollowUp>? FollowUps { get; set; }
    public ICollection<Activity>? Activities { get; set; }
}
