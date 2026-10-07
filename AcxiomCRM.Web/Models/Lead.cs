using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Web.Models;

public class Lead
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string LeadCode { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string LeadName { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }

    [Phone]
    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(150)]
    public string? CompanyName { get; set; }

    [Required]
    public string Source { get; set; } = "Website";

    [Required]
    public string Status { get; set; } = "New";

    public string? Priority { get; set; }
    public decimal ExpectedValue { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }
    public bool IsDeleted { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public ICollection<Opportunity>? Opportunities { get; set; }
    public ICollection<FollowUp>? FollowUps { get; set; }
    public ICollection<Activity>? Activities { get; set; }
}
