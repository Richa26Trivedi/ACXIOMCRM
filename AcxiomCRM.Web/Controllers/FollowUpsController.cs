using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class FollowUpsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public FollowUpsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? status = null)
    {
        var user = await _userManager.GetUserAsync(User);
        var roles = await _userManager.GetRolesAsync(user!);
        var query = _context.FollowUps.Include(x => x.Customer).Include(x => x.Lead).Include(x => x.Opportunity).AsQueryable();

        if (!roles.Contains("Admin") && !roles.Contains("Manager") && user is not null)
            query = query.Where(x => x.AssignedTo == user.UserName);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);

        var followUps = await query.OrderBy(x => x.FollowUpDate).ToListAsync();
        return View(followUps);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Customers = await _context.Customers.OrderBy(x => x.CustomerName).ToListAsync();
        ViewBag.Leads = await _context.Leads.OrderBy(x => x.LeadName).ToListAsync();
        ViewBag.Opportunities = await _context.Opportunities.OrderBy(x => x.OpportunityName).ToListAsync();
        return View(new FollowUp());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUp model)
    {
        var errors = BusinessValidator.ValidateFollowUp(model);
        foreach (var error in errors)
            ModelState.AddModelError(string.Empty, error);

        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.OrderBy(x => x.CustomerName).ToListAsync();
            ViewBag.Leads = await _context.Leads.OrderBy(x => x.LeadName).ToListAsync();
            ViewBag.Opportunities = await _context.Opportunities.OrderBy(x => x.OpportunityName).ToListAsync();
            return View(model);
        }

        model.AssignedTo = _userManager.GetUserName(User);
        _context.FollowUps.Add(model);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(_userManager.GetUserId(User)!, "Create", "FollowUp", model.Id.ToString(), null, model);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string? notes = null)
    {
        var followUp = await _context.FollowUps.FindAsync(id);
        if (followUp is null)
            return NotFound();

        var currentUser = User.Identity?.Name;
        var isManagerOrAdmin = User.IsInRole("Admin") || User.IsInRole("Manager");
        if (!isManagerOrAdmin && !string.Equals(followUp.AssignedTo, currentUser, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        followUp.Status = "Completed";
        followUp.Notes = notes ?? followUp.Notes;
        followUp.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(_userManager.GetUserId(User)!, "Complete", "FollowUp", followUp.Id.ToString(), new { Status = "Planned" }, new { Status = "Completed" });
        return RedirectToAction(nameof(Index));
    }
}
