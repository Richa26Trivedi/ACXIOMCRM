using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeadsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public LeadsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LeadDto>>> GetLeads([FromQuery] string? search = null)
    {
        var currentUser = User.Identity?.Name;
        var isManagerOrAdmin = User.IsInRole("Admin") || User.IsInRole("Manager");
        var query = _context.Leads.AsQueryable();

        if (!isManagerOrAdmin)
            query = query.Where(x => x.AssignedTo == currentUser);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.LeadName.Contains(search) || x.CompanyName!.Contains(search));

        var result = await query.OrderBy(x => x.LeadName).Select(x => new LeadDto
        {
            Id = x.Id,
            LeadCode = x.LeadCode,
            LeadName = x.LeadName,
            Email = x.Email,
            Phone = x.Phone,
            CompanyName = x.CompanyName,
            Source = x.Source,
            Status = x.Status,
            Priority = x.Priority,
            ExpectedValue = x.ExpectedValue,
            AssignedTo = x.AssignedTo
        }).ToListAsync();

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<LeadDto>> CreateLead([FromBody] LeadCreateRequest request)
    {
        if (request is null)
            return BadRequest("Request body is required.");

        var lead = new Lead
        {
            LeadName = request.LeadName,
            Email = request.Email,
            Phone = request.Phone,
            CompanyName = request.CompanyName,
            Source = request.Source,
            Status = request.Status,
            Priority = request.Priority,
            ExpectedValue = request.ExpectedValue,
            AssignedTo = User.Identity?.Name ?? "api-user"
        };

        var errors = BusinessValidator.ValidateLead(lead);
        if (errors.Count > 0)
            return ValidationProblem(new ValidationProblemDetails(errors.ToDictionary(error => error, error => new[] { error })));

        lead.LeadCode = $"L-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8]}";
        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetLeads), new { id = lead.Id }, new LeadDto { Id = lead.Id, LeadCode = lead.LeadCode, LeadName = lead.LeadName, Status = lead.Status, ExpectedValue = lead.ExpectedValue });
    }
}
