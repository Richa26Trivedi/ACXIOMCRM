using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Web.Models;

public class AuditLog
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Action { get; set; } = string.Empty;

    [Required]
    public string EntityName { get; set; } = string.Empty;

    public string? RecordId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? Result { get; set; }
}
