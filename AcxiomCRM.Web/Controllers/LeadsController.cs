using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class LeadsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public LeadsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? search = null, string? status = null)
    {
        var user = await _userManager.GetUserAsync(User);
        var roles = await _userManager.GetRolesAsync(user!);
        var query = _context.Leads.AsQueryable();

        if (!roles.Contains("Admin") && !roles.Contains("Manager") && user is not null)
            query = query.Where(x => x.AssignedTo == user.UserName);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.LeadName.Contains(search) || x.CompanyName!.Contains(search) || x.Email!.Contains(search));
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);

        var leads = await query.OrderByDescending(x => x.CreatedDate).ToListAsync();
        return View(leads);
    }

    public IActionResult Create() => View(new Lead());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Lead model)
    {
        var errors = BusinessValidator.ValidateLead(model);
        foreach (var error in errors)
            ModelState.AddModelError(string.Empty, error);

        if (!ModelState.IsValid)
            return View(model);

        model.LeadCode = $"LEAD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8]}";
        model.AssignedTo = _userManager.GetUserName(User);
        _context.Leads.Add(model);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(_userManager.GetUserId(User)!, "Create", "Lead", model.Id.ToString(), null, model);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var lead = await _context.Leads.FirstOrDefaultAsync(x => x.Id == id);
        return lead is null ? NotFound() : View(lead);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Lead model)
    {
        var existing = await _context.Leads.FirstOrDefaultAsync(x => x.Id == model.Id);
        if (existing is null)
            return NotFound();

        var errors = BusinessValidator.ValidateLead(model);
        foreach (var error in errors)
            ModelState.AddModelError(string.Empty, error);

        if (!ModelState.IsValid)
            return View(model);

        var oldValue = new { existing.LeadName, existing.Status, existing.AssignedTo };
        existing.LeadName = model.LeadName;
        existing.Email = model.Email;
        existing.Phone = model.Phone;
        existing.CompanyName = model.CompanyName;
        existing.Source = model.Source;
        existing.Status = model.Status;
        existing.Priority = model.Priority;
        existing.ExpectedValue = model.ExpectedValue;
        existing.AssignedTo = model.AssignedTo;
        existing.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(_userManager.GetUserId(User)!, "Update", "Lead", existing.Id.ToString(), oldValue, new { existing.LeadName, existing.Status, existing.AssignedTo });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead is null)
            return NotFound();

        lead.IsDeleted = true;
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(_userManager.GetUserId(User)!, "Delete", "Lead", lead.Id.ToString(), null, new { lead.LeadName, Deleted = true });
        return RedirectToAction(nameof(Index));
    }
}
