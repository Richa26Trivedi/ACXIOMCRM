using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class ActivitiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ActivitiesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? status = null)
    {
        var user = await _userManager.GetUserAsync(User);
        var roles = await _userManager.GetRolesAsync(user!);
        var query = _context.Activities.Include(x => x.Customer).Include(x => x.Lead).AsQueryable();

        if (!roles.Contains("Admin") && !roles.Contains("Manager") && user is not null)
            query = query.Where(x => x.AssignedTo == user.UserName);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);

        var activities = await query.OrderByDescending(x => x.ActivityDate).ToListAsync();
        return View(activities);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Customers = await _context.Customers.OrderBy(x => x.CustomerName).ToListAsync();
        ViewBag.Leads = await _context.Leads.OrderBy(x => x.LeadName).ToListAsync();
        return View(new Activity());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Activity model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.OrderBy(x => x.CustomerName).ToListAsync();
            ViewBag.Leads = await _context.Leads.OrderBy(x => x.LeadName).ToListAsync();
            return View(model);
        }

        model.AssignedTo = _userManager.GetUserName(User);
        _context.Activities.Add(model);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var activity = await _context.Activities.FindAsync(id);
        if (activity is null)
            return NotFound();

        activity.Status = "Completed";
        activity.Description ??= "Completed";
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
