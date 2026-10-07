namespace AcxiomCRM.Web.Models.Dtos;

public class LeadDto
{
    public int Id { get; set; }
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Priority { get; set; }
    public decimal ExpectedValue { get; set; }
    public string? AssignedTo { get; set; }
}
