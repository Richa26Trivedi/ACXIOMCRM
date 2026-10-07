using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IAuditService auditService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _auditService = auditService;
    }

    [AllowAnonymous]
    public IActionResult Login() => View();

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null || !user.IsActive)
        {
            await _auditService.LogAsync("system", "Failed Login", "Authentication", null, new { model.Email }, null, result: "Failed");
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            await _auditService.LogAsync(user.Id, "Login", "Authentication", user.Id, null, new { user.Email }, result: "Success");
            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            await _auditService.LogAsync(user.Id, "Failed Login", "Authentication", user.Id, null, new { model.Email, Lockout = true }, result: "LockedOut");
            ModelState.AddModelError(string.Empty, "Your account is locked due to repeated unsuccessful login attempts.");
            return View(model);
        }

        await _auditService.LogAsync(user.Id, "Failed Login", "Authentication", user.Id, null, new { model.Email }, result: "Failed");
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [AllowAnonymous]
    public IActionResult Register() => View();

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            IsActive = true,
            EmailConfirmed = true,
            CreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "SalesExecutive");
            await _auditService.LogAsync(user.Id, "Create User", "Authentication", user.Id, null, new { user.Email }, result: "Success");
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Dashboard");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = _userManager.GetUserId(User);
        await _signInManager.SignOutAsync();
        await _auditService.LogAsync(userId ?? "system", "Logout", "Authentication", userId, null, new { LoggedOut = true }, result: "Success");
        return RedirectToAction("Login");
    }

    [Authorize]
    public IActionResult AccessDenied() => View();
}
