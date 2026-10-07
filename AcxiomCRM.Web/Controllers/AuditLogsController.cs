using AcxiomCRM.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Controllers;

[Authorize(Policy = "AuditView")]
public class AuditLogsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AuditLogsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? user = null, string? action = null, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(user))
            query = query.Where(x => x.UserId.Contains(user));
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(x => x.Action.Contains(action));
        if (from.HasValue)
            query = query.Where(x => x.CreatedDate >= from.Value);
        if (to.HasValue)
            query = query.Where(x => x.CreatedDate <= to.Value);

        var auditLogs = await query.OrderByDescending(x => x.CreatedDate).Take(500).ToListAsync();
        return View(auditLogs);
    }
}
