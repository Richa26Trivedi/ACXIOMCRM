namespace AcxiomCRM.Web.Models.Dtos;

public class OpportunityDto
{
    public int Id { get; set; }
    public string OpportunityName { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int? LeadId { get; set; }
    public decimal Amount { get; set; }
    public string Stage { get; set; } = string.Empty;
    public int Probability { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public string? Source { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class OpportunityCreateRequest
{
    public string OpportunityName { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int? LeadId { get; set; }
    public decimal Amount { get; set; }
    public string Stage { get; set; } = "Qualification";
    public int Probability { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public string Status { get; set; } = "Open";
    public string? Source { get; set; }
    public string? Notes { get; set; }
}
