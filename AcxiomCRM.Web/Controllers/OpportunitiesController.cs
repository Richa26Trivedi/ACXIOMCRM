using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class OpportunitiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public OpportunitiesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? search = null, string? stage = null)
    {
        var user = await _userManager.GetUserAsync(User);
        var roles = await _userManager.GetRolesAsync(user!);
        var query = _context.Opportunities.Include(x => x.Customer).AsQueryable();

        if (!roles.Contains("Admin") && !roles.Contains("Manager") && user is not null)
            query = query.Where(x => x.AssignedTo == user.UserName);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.OpportunityName.Contains(search) || x.Customer!.CustomerName.Contains(search));
        if (!string.IsNullOrWhiteSpace(stage))
            query = query.Where(x => x.Stage == stage);

        var opportunities = await query.OrderByDescending(x => x.CreatedDate).ToListAsync();
        return View(opportunities);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Customers = await _context.Customers.OrderBy(x => x.CustomerName).ToListAsync();
        return View(new Opportunity());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Opportunity model)
    {
        var errors = BusinessValidator.ValidateOpportunity(model);
        foreach (var error in errors)
            ModelState.AddModelError(string.Empty, error);

        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.OrderBy(x => x.CustomerName).ToListAsync();
            return View(model);
        }

        model.AssignedTo = _userManager.GetUserName(User);
        _context.Opportunities.Add(model);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(_userManager.GetUserId(User)!, "Create", "Opportunity", model.Id.ToString(), null, model);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var opportunity = await _context.Opportunities.FirstOrDefaultAsync(x => x.Id == id);
        if (opportunity is null)
            return NotFound();

        ViewBag.Customers = await _context.Customers.OrderBy(x => x.CustomerName).ToListAsync();
        return View(opportunity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Opportunity model)
    {
        var existing = await _context.Opportunities.FirstOrDefaultAsync(x => x.Id == model.Id);
        if (existing is null)
            return NotFound();

        var errors = BusinessValidator.ValidateOpportunity(model);
        foreach (var error in errors)
            ModelState.AddModelError(string.Empty, error);

        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.OrderBy(x => x.CustomerName).ToListAsync();
            return View(model);
        }

        var oldValue = new { existing.OpportunityName, existing.Amount, existing.Probability, existing.Stage, existing.ExpectedCloseDate, existing.Status };
        existing.OpportunityName = model.OpportunityName;
        existing.CustomerId = model.CustomerId;
        existing.LeadId = model.LeadId;
        existing.Amount = model.Amount;
        existing.Stage = model.Stage;
        existing.Probability = model.Probability;
        existing.ExpectedCloseDate = model.ExpectedCloseDate;
        existing.Status = model.Status;
        existing.Source = model.Source;
        existing.Notes = model.Notes;
        existing.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(_userManager.GetUserId(User)!, "Update", "Opportunity", existing.Id.ToString(), oldValue, new { existing.OpportunityName, existing.Amount, existing.Probability, existing.Stage, existing.ExpectedCloseDate, existing.Status });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var opportunity = await _context.Opportunities.FindAsync(id);
        if (opportunity is null)
            return NotFound();

        opportunity.IsDeleted = true;
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(_userManager.GetUserId(User)!, "Delete", "Opportunity", opportunity.Id.ToString(), null, new { opportunity.OpportunityName, Deleted = true });
        return RedirectToAction(nameof(Index));
    }
}
