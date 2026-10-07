using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Controllers;

[Authorize(Policy = "UserManagement")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditService _auditService;

    public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IAuditService auditService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? search = null)
    {
        var users = await _userManager.Users
            .Where(x => string.IsNullOrWhiteSpace(search) || x.FullName.Contains(search) || x.Email!.Contains(search))
            .OrderBy(x => x.FullName)
            .ToListAsync();

        var view = users.Select(async user => new UserAdminViewModel
        {
            User = user,
            Roles = await _userManager.GetRolesAsync(user)
        }).Select(x => x.Result).ToList();

        return View(view);
    }

    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        var roles = await _roleManager.Roles
            .Where(x => x.Name != null)
            .Select(x => x.Name!)
            .ToListAsync();
        return View(new UserEditViewModel { User = user, AvailableRoles = roles, SelectedRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserEditViewModel model)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        if (!string.IsNullOrWhiteSpace(model.SelectedRole) && !await _roleManager.RoleExistsAsync(model.SelectedRole))
            return BadRequest("Selected role does not exist.");

        if (roles.Count > 1)
            return BadRequest("Users must have exactly one primary role.");

        var currentRole = roles.FirstOrDefault();
        if (currentRole is not null && model.SelectedRole != currentRole)
        {
            await _userManager.RemoveFromRolesAsync(user, roles);
            if (!string.IsNullOrWhiteSpace(model.SelectedRole))
                await _userManager.AddToRoleAsync(user, model.SelectedRole);
        }

        user.IsActive = model.IsActive;
        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
            return View(model);

        await _auditService.LogAsync(_userManager.GetUserId(User)!, "Role Change", "User", user.Id, new { OldRole = currentRole }, new { NewRole = model.SelectedRole, Active = model.IsActive });
        return RedirectToAction(nameof(Index));
    }
}

public class UserAdminViewModel
{
    public ApplicationUser User { get; set; } = new();
    public IList<string> Roles { get; set; } = new List<string>();
}

public class UserEditViewModel
{
    public ApplicationUser User { get; set; } = new();
    public IList<string> AvailableRoles { get; set; } = new List<string>();
    public string? SelectedRole { get; set; }
    public bool IsActive { get; set; } = true;
}
