using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Web.Data;

public static class InitialDataSeeder
{
    public static async Task SeedAsync(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
    {
        var roleNames = new[] { "Admin", "Manager", "SalesExecutive" };

        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        var adminEmail = "admin@acxiomcrm.local";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                IsActive = true,
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(admin, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        var manager = await userManager.FindByEmailAsync("manager@acxiomcrm.local");
        if (manager is null)
        {
            manager = new ApplicationUser
            {
                UserName = "manager@acxiomcrm.local",
                Email = "manager@acxiomcrm.local",
                FullName = "Sales Manager",
                IsActive = true,
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(manager, "Manager@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(manager, "Manager");
            }
        }

        var sales = await userManager.FindByEmailAsync("sales@acxiomcrm.local");
        if (sales is null)
        {
            sales = new ApplicationUser
            {
                UserName = "sales@acxiomcrm.local",
                Email = "sales@acxiomcrm.local",
                FullName = "Sales Executive",
                IsActive = true,
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(sales, "Sales@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(sales, "SalesExecutive");
            }
        }
    }
}
