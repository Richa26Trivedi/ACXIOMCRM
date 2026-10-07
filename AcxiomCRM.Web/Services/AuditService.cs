using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Services;

public interface IAuditService
{
    Task LogAsync(string userId, string action, string entityName, string? recordId, object? oldValue, object? newValue, string? ipAddress = null, string result = "Success");
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string userId, string action, string entityName, string? recordId, object? oldValue, object? newValue, string? ipAddress = null, string result = "Success")
    {
        var audit = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            RecordId = recordId,
            OldValue = oldValue is null ? null : System.Text.Json.JsonSerializer.Serialize(oldValue),
            NewValue = newValue is null ? null : System.Text.Json.JsonSerializer.Serialize(newValue),
            IpAddress = ipAddress ?? _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            Result = result,
            CreatedDate = DateTime.UtcNow
        };

        _context.AuditLogs.Add(audit);
        await _context.SaveChangesAsync();
    }
}
