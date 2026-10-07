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
public class OpportunitiesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OpportunitiesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OpportunityDto>>> GetOpportunities([FromQuery] string? search = null)
    {
        var query = _context.Opportunities.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.OpportunityName.Contains(search) || x.Customer!.CustomerName.Contains(search));

        var result = await query
            .OrderBy(x => x.OpportunityName)
            .Select(x => new OpportunityDto
            {
                Id = x.Id,
                OpportunityName = x.OpportunityName,
                CustomerId = x.CustomerId,
                LeadId = x.LeadId,
                Amount = x.Amount,
                Stage = x.Stage,
                Probability = x.Probability,
                ExpectedCloseDate = x.ExpectedCloseDate,
                Status = x.Status,
                AssignedTo = x.AssignedTo,
                Source = x.Source,
                Notes = x.Notes,
                CreatedDate = x.CreatedDate
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OpportunityDto>> GetOpportunity(int id)
    {
        var opportunity = await _context.Opportunities.FindAsync(id);
        if (opportunity is null)
            return NotFound();

        return Ok(MapToDto(opportunity));
    }

    [HttpPost]
    public async Task<ActionResult<OpportunityDto>> CreateOpportunity([FromBody] OpportunityCreateRequest request)
    {
        if (request is null)
            return BadRequest("Request body is required.");

        var opportunity = new Opportunity
        {
            OpportunityName = request.OpportunityName,
            CustomerId = request.CustomerId,
            LeadId = request.LeadId,
            Amount = request.Amount,
            Stage = request.Stage,
            Probability = request.Probability,
            ExpectedCloseDate = request.ExpectedCloseDate,
            Status = request.Status,
            Source = request.Source,
            Notes = request.Notes,
            AssignedTo = User.Identity?.Name ?? "api-user"
        };

        var errors = BusinessValidator.ValidateOpportunity(opportunity);
        if (errors.Count > 0)
            return ValidationProblem(new ValidationProblemDetails(errors.ToDictionary(error => error, error => new[] { error })));

        if (!await _context.Customers.AnyAsync(x => x.Id == request.CustomerId))
            return BadRequest("Customer does not exist.");

        if (request.LeadId is not null && !await _context.Leads.AnyAsync(x => x.Id == request.LeadId.Value))
            return BadRequest("Lead does not exist.");

        _context.Opportunities.Add(opportunity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOpportunity), new { id = opportunity.Id }, MapToDto(opportunity));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateOpportunity(int id, [FromBody] OpportunityCreateRequest request)
    {
        if (request is null)
            return BadRequest("Request body is required.");

        var opportunity = await _context.Opportunities.FindAsync(id);
        if (opportunity is null)
            return NotFound();

        var currentUser = User.Identity?.Name;
        var isManagerOrAdmin = User.IsInRole("Admin") || User.IsInRole("Manager");
        if (!isManagerOrAdmin && !string.Equals(opportunity.AssignedTo, currentUser, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        opportunity.OpportunityName = request.OpportunityName;
        opportunity.CustomerId = request.CustomerId;
        opportunity.LeadId = request.LeadId;
        opportunity.Amount = request.Amount;
        opportunity.Stage = request.Stage;
        opportunity.Probability = request.Probability;
        opportunity.ExpectedCloseDate = request.ExpectedCloseDate;
        opportunity.Status = request.Status;
        opportunity.Source = request.Source;
        opportunity.Notes = request.Notes;
        opportunity.ModifiedDate = DateTime.UtcNow;

        var errors = BusinessValidator.ValidateOpportunity(opportunity);
        if (errors.Count > 0)
            return ValidationProblem(new ValidationProblemDetails(errors.ToDictionary(error => error, error => new[] { error })));

        if (!await _context.Customers.AnyAsync(x => x.Id == request.CustomerId))
            return BadRequest("Customer does not exist.");

        if (request.LeadId is not null && !await _context.Leads.AnyAsync(x => x.Id == request.LeadId.Value))
            return BadRequest("Lead does not exist.");

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteOpportunity(int id)
    {
        var opportunity = await _context.Opportunities.FindAsync(id);
        if (opportunity is null)
            return NotFound();

        var currentUser = User.Identity?.Name;
        var isManagerOrAdmin = User.IsInRole("Admin") || User.IsInRole("Manager");
        if (!isManagerOrAdmin && !string.Equals(opportunity.AssignedTo, currentUser, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        opportunity.IsDeleted = true;
        opportunity.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static OpportunityDto MapToDto(Opportunity opportunity) => new()
    {
        Id = opportunity.Id,
        OpportunityName = opportunity.OpportunityName,
        CustomerId = opportunity.CustomerId,
        LeadId = opportunity.LeadId,
        Amount = opportunity.Amount,
        Stage = opportunity.Stage,
        Probability = opportunity.Probability,
        ExpectedCloseDate = opportunity.ExpectedCloseDate,
        Status = opportunity.Status,
        AssignedTo = opportunity.AssignedTo,
        Source = opportunity.Source,
        Notes = opportunity.Notes,
        CreatedDate = opportunity.CreatedDate
    };
}
