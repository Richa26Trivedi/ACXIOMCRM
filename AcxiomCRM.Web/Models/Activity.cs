using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Web.Models;

public class Activity
{
    public int Id { get; set; }

    [Required]
    public string ActivityType { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Subject { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public DateTime ActivityDate { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }
    public string? AssignedTo { get; set; }
    public string Status { get; set; } = "Planned";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}
