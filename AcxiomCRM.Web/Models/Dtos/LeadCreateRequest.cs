using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Web.Models.Dtos;

public class LeadCreateRequest
{
    [Required]
    [StringLength(150)]
    public string LeadName { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? Phone { get; set; }

    [StringLength(150)]
    public string? CompanyName { get; set; }

    [Required]
    public string Source { get; set; } = "Website";

    [Required]
    public string Status { get; set; } = "New";

    public string? Priority { get; set; }

    [Range(0, 1000000000)]
    public decimal ExpectedValue { get; set; }
}
