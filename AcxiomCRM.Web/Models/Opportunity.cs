using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Web.Models;

public class Opportunity
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string OpportunityName { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; }

    [Required]
    public string Stage { get; set; } = "Qualification";

    [Range(0, 100)]
    public int Probability { get; set; }

    public DateTime? ExpectedCloseDate { get; set; }

    [Required]
    public string Status { get; set; } = "Open";

    public string? AssignedTo { get; set; }
    public string? Source { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }
    public bool IsDeleted { get; set; }
    public ICollection<FollowUp>? FollowUps { get; set; }
}
