using AcxiomCRM.Web.Controllers;
using AcxiomCRM.Web.Controllers.Api;
using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Security.Claims;

namespace AcxiomCRM.Tests;

public class AuthenticationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthenticationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor is not null)
                    services.Remove(descriptor);

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("AcxiomCRM-Auth-Tests"));

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                db.Database.EnsureCreated();
                Task.Run(async () => await InitialDataSeeder.SeedAsync(roleManager, userManager)).GetAwaiter().GetResult();
            });
        });
    }

    [Fact]
    public async Task ProtectedDashboard_RequiresAuthentication()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Dashboard");

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString);
        Assert.Contains("ReturnUrl", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Login_WithValidCredentials_RedirectsToDashboard()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var loginPage = await client.GetAsync("/Account/Login");
        var pageContent = await loginPage.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(pageContent);

        var form = new Dictionary<string, string>
        {
            ["Email"] = "admin@acxiomcrm.local",
            ["Password"] = "Admin@123",
            ["RememberMe"] = "false",
            ["__RequestVerificationToken"] = token
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/Account/Login")
        {
            Content = new FormUrlEncodedContent(form)
        };

        var loginResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        Assert.Equal("/Dashboard", loginResponse.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsValidationError()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var loginPage = await client.GetAsync("/Account/Login");
        var pageContent = await loginPage.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(pageContent);

        var response = await client.PostAsync("/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = "admin@acxiomcrm.local",
                ["Password"] = "WrongPassword!",
                ["RememberMe"] = "false",
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid login attempt", content);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthyAndSecurityHeaders()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
    }

    [Fact]
    public async Task AuthenticatedReportData_ReturnsFilteredChartData()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync("sales@acxiomcrm.local");
        db.Opportunities.AddRange(
            new Opportunity
            {
                OpportunityName = "Sales opportunity",
                Amount = 1000m,
                Probability = 50,
                Stage = "Qualification",
                Status = "Open",
                AssignedTo = user!.UserName,
                CustomerId = 1
            },
            new Opportunity
            {
                OpportunityName = "Other opportunity",
                Amount = 2000m,
                Probability = 25,
                Stage = "Proposal",
                Status = "Open",
                AssignedTo = "manager@acxiomcrm.local",
                CustomerId = 1
            });
        await db.SaveChangesAsync();

        var controller = new ReportsController(db, userManager)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, user.UserName!),
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim(ClaimTypes.Role, "SalesExecutive")
                    }, "TestAuth"))
                }
            }
        };

        var result = await controller.Data(null, null, "sales");
        var json = Assert.IsType<JsonResult>(result);
        var data = json.Value;

        Assert.NotNull(data);
        Assert.Contains("labels", data.ToString()!);
        Assert.Contains("values", data.ToString()!);
    }

    [Fact]
    public async Task SalesUser_CannotCompleteAnotherUsersFollowUp()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync("sales@acxiomcrm.local");
        var followUp = new FollowUp
        {
            Subject = "Other user's task",
            FollowUpDate = DateTime.Today.AddDays(1),
            Status = "Planned",
            AssignedTo = "manager@acxiomcrm.local"
        };
        db.FollowUps.Add(followUp);
        await db.SaveChangesAsync();

        var controller = new FollowUpsController(db, userManager, new AuditService(db, new HttpContextAccessor()));
        var identity = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, user!.UserName!),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Role, "SalesExecutive")
        }, "TestAuth"));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = identity } };

        var result = await controller.Complete(followUp.Id, "Completed");

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task SalesUser_ReportsOnlyAssignedRecords()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync("sales@acxiomcrm.local");
        db.Customers.Add(new Customer
        {
            CustomerCode = "CUST-SALES-1",
            CustomerName = "Assigned Customer",
            Email = "assigned@example.com",
            Phone = "1234567890",
            AssignedTo = user!.UserName
        });
        db.Customers.Add(new Customer
        {
            CustomerCode = "CUST-SALES-2",
            CustomerName = "Other Customer",
            Email = "other@example.com",
            Phone = "1234567891",
            AssignedTo = "manager@acxiomcrm.local"
        });
        await db.SaveChangesAsync();

        var controller = new ReportsController(db, userManager);
        var identity = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Role, "SalesExecutive")
        }, "TestAuth"));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = identity } };

        var result = await controller.Index();
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ReportsViewModel>(view.Model);
        var customers = Assert.IsAssignableFrom<IEnumerable<object>>(model.Customers).ToList();

        Assert.Single(customers);
        Assert.Contains("Assigned Customer", customers[0].ToString()!);
    }

    [Fact]
    public async Task Admin_RejectsInvalidOrMultiRoleAssignment()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var user = await userManager.FindByEmailAsync("sales@acxiomcrm.local");
        var controller = new UsersController(userManager, roleManager, new AuditService(db, new HttpContextAccessor()));
        var admin = await userManager.FindByEmailAsync("admin@acxiomcrm.local");
        var identity = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, admin!.UserName!),
            new Claim(ClaimTypes.NameIdentifier, admin.Id),
            new Claim(ClaimTypes.Role, "Admin")
        }, "TestAuth"));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = identity } };

        var invalidResult = await controller.Edit(user!.Id, new UserEditViewModel
        {
            SelectedRole = "InvalidRole",
            IsActive = true
        });

        await userManager.AddToRoleAsync(user, "Manager");
        var multiRoleResult = await controller.Edit(user.Id, new UserEditViewModel
        {
            SelectedRole = "Admin",
            IsActive = true
        });

        Assert.IsType<BadRequestObjectResult>(invalidResult);
        Assert.IsType<BadRequestObjectResult>(multiRoleResult);
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        var start = html.IndexOf("name=\"__RequestVerificationToken\"", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var valueStart = html.IndexOf("value=\"", start, StringComparison.Ordinal) + 7;
        var valueEnd = html.IndexOf("\"", valueStart, StringComparison.Ordinal);
        return html.Substring(valueStart, valueEnd - valueStart);
    }
}
