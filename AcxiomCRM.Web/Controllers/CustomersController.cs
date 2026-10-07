using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public CustomersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? search = null)
    {
        var user = await _userManager.GetUserAsync(User);
        var roles = await _userManager.GetRolesAsync(user!);
        var query = _context.Customers.AsQueryable();

        if (!roles.Contains("Admin") && !roles.Contains("Manager") && user is not null)
            query = query.Where(x => x.AssignedTo == user.UserName);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.CustomerName.Contains(search) || x.Email.Contains(search) || x.Phone.Contains(search) || x.CompanyName!.Contains(search));

        var customers = await query.OrderByDescending(x => x.CreatedDate).ToListAsync();
        return View(customers);
    }

    public IActionResult Create() => View(new Customer());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer model)
    {
        var errors = BusinessValidator.ValidateCustomer(model);
        if (errors.Count > 0)
        {
            foreach (var error in errors)
                ModelState.AddModelError(string.Empty, error);
            return View(model);
        }

        if (await _context.Customers.AnyAsync(x => x.Email == model.Email))
            ModelState.AddModelError(nameof(Customer.Email), "A customer with this email already exists.");

        if (await _context.Customers.AnyAsync(x => x.Phone == model.Phone))
            ModelState.AddModelError(nameof(Customer.Phone), "A customer with this phone number already exists.");

        if (!ModelState.IsValid)
            return View(model);

        model.CustomerCode = $"CUST-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8]}";
        model.CreatedBy = _userManager.GetUserName(User);
        model.AssignedTo = _userManager.GetUserName(User);
        _context.Customers.Add(model);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(_userManager.GetUserId(User)!, "Create", "Customer", model.Id.ToString(), null, model);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(x => x.Id == id);
        return customer is null ? NotFound() : View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Customer model)
    {
        var existing = await _context.Customers.FirstOrDefaultAsync(x => x.Id == model.Id);
        if (existing is null)
            return NotFound();

        var errors = BusinessValidator.ValidateCustomer(model);
        foreach (var error in errors)
            ModelState.AddModelError(string.Empty, error);

        if (!ModelState.IsValid)
            return View(model);

        var oldValue = new { existing.CustomerName, existing.Email, existing.Phone, existing.Status, existing.AssignedTo };
        existing.CustomerName = model.CustomerName;
        existing.Email = model.Email;
        existing.Phone = model.Phone;
        existing.CompanyName = model.CompanyName;
        existing.Address = model.Address;
        existing.City = model.City;
        existing.State = model.State;
        existing.Status = model.Status;
        existing.ModifiedDate = DateTime.UtcNow;
        existing.AssignedTo = model.AssignedTo;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(_userManager.GetUserId(User)!, "Update", "Customer", existing.Id.ToString(), oldValue, new { existing.CustomerName, existing.Email, existing.Phone, existing.Status, existing.AssignedTo });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer is null)
            return NotFound();

        customer.IsDeleted = true;
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(_userManager.GetUserId(User)!, "Delete", "Customer", customer.Id.ToString(), null, new { customer.CustomerName, Deleted = true });
        return RedirectToAction(nameof(Index));
    }
}
