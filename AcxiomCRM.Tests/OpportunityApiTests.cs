using AcxiomCRM.Web.Controllers.Api;
using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Models.Dtos;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Tests;

public class OpportunityApiTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static OpportunitiesController CreateController(ApplicationDbContext context)
    {
        var controller = new OpportunitiesController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, "api-user"),
                        new Claim(ClaimTypes.NameIdentifier, "test-user")
                    }, "TestAuth"))
                }
            }
        };

        return controller;
    }

    [Fact]
    public async Task CreateOpportunity_ReturnsCreatedOpportunity()
    {
        await using var context = CreateContext();
        context.Customers.Add(new Customer
        {
            CustomerCode = "CUST-1",
            CustomerName = "Acme",
            Email = "acme@example.com",
            Phone = "1234567890"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var result = await controller.CreateOpportunity(new OpportunityCreateRequest
        {
            OpportunityName = "Enterprise Renewal",
            CustomerId = 1,
            Amount = 5000m,
            Probability = 50,
            Stage = "Proposal",
            Status = "Open",
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
            Source = "Website"
        });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var value = Assert.IsType<OpportunityDto>(created.Value);
        Assert.Equal("Enterprise Renewal", value.OpportunityName);
        Assert.Equal(5000m, value.Amount);
        Assert.Equal("Proposal", value.Stage);
    }

    [Fact]
    public async Task CreateOpportunity_WithInvalidAmount_ReturnsValidationProblem()
    {
        await using var context = CreateContext();
        context.Customers.Add(new Customer
        {
            CustomerCode = "CUST-1",
            CustomerName = "Acme",
            Email = "acme@example.com",
            Phone = "1234567890"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var result = await controller.CreateOpportunity(new OpportunityCreateRequest
        {
            OpportunityName = "Invalid",
            CustomerId = 1,
            Amount = 0,
            Probability = 50,
            Stage = "Qualification",
            Status = "Open"
        });

        var response = Assert.IsAssignableFrom<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLead_WithNullRequest_ReturnsBadRequest()
    {
        await using var context = CreateContext();
        var controller = new LeadsController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, "api-user"),
                        new Claim(ClaimTypes.NameIdentifier, "test-user")
                    }, "TestAuth"))
                }
            }
        };

        var result = await controller.CreateLead(null!);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task DeleteOpportunity_SoftDeletesRecord()
    {
        await using var context = CreateContext();
        context.Customers.Add(new Customer
        {
            CustomerCode = "CUST-1",
            CustomerName = "Acme",
            Email = "acme@example.com",
            Phone = "1234567890"
        });
        context.Opportunities.Add(new Opportunity
        {
            OpportunityName = "Legacy",
            CustomerId = 1,
            Amount = 1000m,
            Probability = 25,
            Stage = "Qualification",
            Status = "Open",
            AssignedTo = "api-user"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var result = await controller.DeleteOpportunity(1);

        Assert.IsType<NoContentResult>(result);
        var opportunity = await context.Opportunities.IgnoreQueryFilters().SingleAsync(x => x.Id == 1);
        Assert.True(opportunity.IsDeleted);
    }

    [Fact]
    public async Task UpdateOpportunity_ByDifferentOwner_ReturnsForbidden()
    {
        await using var context = CreateContext();
        context.Customers.Add(new Customer
        {
            CustomerCode = "CUST-1",
            CustomerName = "Acme",
            Email = "acme@example.com",
            Phone = "1234567890"
        });
        context.Opportunities.Add(new Opportunity
        {
            OpportunityName = "Locked Deal",
            CustomerId = 1,
            Amount = 2500m,
            Probability = 40,
            Stage = "Qualification",
            Status = "Open",
            AssignedTo = "other-user"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "current-user"),
            new Claim(ClaimTypes.NameIdentifier, "current-user"),
            new Claim(ClaimTypes.Role, "SalesExecutive")
        }, "TestAuth"));

        var result = await controller.UpdateOpportunity(1, new OpportunityCreateRequest
        {
            OpportunityName = "Changed",
            CustomerId = 1,
            Amount = 3000m,
            Probability = 50,
            Stage = "Proposal",
            Status = "Open"
        });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteOpportunity_ByDifferentOwner_ReturnsForbidden()
    {
        await using var context = CreateContext();
        context.Customers.Add(new Customer
        {
            CustomerCode = "CUST-1",
            CustomerName = "Acme",
            Email = "acme@example.com",
            Phone = "1234567890"
        });
        context.Opportunities.Add(new Opportunity
        {
            OpportunityName = "Locked Deal",
            CustomerId = 1,
            Amount = 2500m,
            Probability = 40,
            Stage = "Qualification",
            Status = "Open",
            AssignedTo = "other-user"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "current-user"),
            new Claim(ClaimTypes.NameIdentifier, "current-user"),
            new Claim(ClaimTypes.Role, "SalesExecutive")
        }, "TestAuth"));

        var result = await controller.DeleteOpportunity(1);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task CreateOpportunity_WithNullRequest_ReturnsBadRequest()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.CreateOpportunity(null!);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CustomerUpdate_ByDifferentOwner_ReturnsForbidden()
    {
        await using var context = CreateContext();
        context.Customers.Add(new Customer
        {
            CustomerCode = "CUST-2",
            CustomerName = "Another Customer",
            Email = "another@example.com",
            Phone = "2234567890",
            AssignedTo = "other-user"
        });
        await context.SaveChangesAsync();

        var controller = new CustomersController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, "current-user"),
                        new Claim(ClaimTypes.Role, "SalesExecutive")
                    }, "TestAuth"))
                }
            }
        };

        var result = await controller.UpdateCustomer(1, new CustomerCreateRequest
        {
            CustomerName = "Updated Customer",
            Email = "updated@example.com",
            Phone = "3234567890",
            Status = "Active"
        });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task LeadGet_ReturnsOnlyAssignedRecordsForSalesUser()
    {
        await using var context = CreateContext();
        context.Leads.Add(new Lead
        {
            LeadCode = "LEAD-1",
            LeadName = "Assigned Lead",
            Email = "assigned@example.com",
            Phone = "1234567890",
            Source = "Website",
            Status = "New",
            AssignedTo = "api-user"
        });
        context.Leads.Add(new Lead
        {
            LeadCode = "LEAD-2",
            LeadName = "Other Lead",
            Email = "other@example.com",
            Phone = "2234567890",
            Source = "Referral",
            Status = "New",
            AssignedTo = "other-user"
        });
        await context.SaveChangesAsync();

        var controller = new LeadsController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, "api-user"),
                        new Claim(ClaimTypes.Role, "SalesExecutive")
                    }, "TestAuth"))
                }
            }
        };

        var result = await controller.GetLeads();
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var values = Assert.IsAssignableFrom<IEnumerable<LeadDto>>(okResult.Value);

        Assert.Single(values);
        Assert.Equal("Assigned Lead", values.Single().LeadName);
    }
}
