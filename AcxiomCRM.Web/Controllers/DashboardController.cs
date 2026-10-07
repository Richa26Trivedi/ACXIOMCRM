using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? range = "This Month")
    {
        var user = await _userManager.GetUserAsync(User);
        var userRoles = await _userManager.GetRolesAsync(user!);
        var scope = BuildScope(user!, userRoles);

        var customers = await _context.Customers
            .Where(scope.Customers)
            .ToListAsync();

        var leads = await _context.Leads
            .Where(scope.Leads)
            .ToListAsync();

        var opportunities = await _context.Opportunities
            .Where(scope.Opportunities)
            .ToListAsync();

        var followUps = await _context.FollowUps
            .Where(scope.FollowUps)
            .ToListAsync();

        var dashboard = new DashboardViewModel
        {
            TotalCustomers = customers.Count,
            TotalLeads = leads.Count,
            OpenLeads = leads.Count(x => x.Status != "Closed" && x.Status != "Lost" && x.Status != "Converted"),
            TotalOpportunities = opportunities.Count,
            OpenOpportunities = opportunities.Count(x => x.Status == "Open"),
            WonOpportunities = opportunities.Count(x => x.Status == "Won"),
            LostOpportunities = opportunities.Count(x => x.Status == "Lost"),
            TotalPipeline = opportunities.Where(x => x.Status == "Open").Sum(x => x.Amount),
            LeadStatusChart = leads.GroupBy(x => x.Status).Select(x => new ChartPoint(x.Key, x.Count())).OrderBy(x => x.Label).ToList(),
            PipelineChart = opportunities.GroupBy(x => x.Stage).Select(x => new ChartPoint(x.Key, x.Sum(y => y.Amount))).OrderBy(x => x.Label).ToList(),
            MonthlySalesChart = GetMonthlySales(opportunities),
            PendingFollowUps = followUps.Count(x => x.Status == "Planned" && x.FollowUpDate >= DateTime.Today)
        };

        return View(dashboard);
    }

    private static ScopeFilter BuildScope(ApplicationUser user, IList<string> roles)
    {
        var userName = user.UserName ?? string.Empty;
        var isAdmin = roles.Contains("Admin");
        var isManager = roles.Contains("Manager");
        var isSales = roles.Contains("SalesExecutive");

        return new ScopeFilter
        {
            Customers = e => isAdmin || isManager || (isSales && e.AssignedTo == userName),
            Leads = e => isAdmin || isManager || (isSales && e.AssignedTo == userName),
            Opportunities = e => isAdmin || isManager || (isSales && e.AssignedTo == userName),
            FollowUps = e => isAdmin || isManager || (isSales && e.AssignedTo == userName)
        };
    }

    private static List<ChartPoint> GetMonthlySales(IEnumerable<Opportunity> opportunities)
    {
        var start = DateTime.Today.AddMonths(-5).Date;
        return Enumerable.Range(0, 6)
            .Select(month =>
            {
                var date = new DateTime(start.Year, start.Month, 1).AddMonths(month);
                var value = opportunities
                    .Where(x => x.ModifiedDate?.Month == date.Month && x.ModifiedDate?.Year == date.Year && x.Status == "Won")
                    .Sum(x => x.Amount);
                return new ChartPoint(date.ToString("MMM"), value);
            })
            .ToList();
    }
}

public sealed class DashboardViewModel
{
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal TotalPipeline { get; set; }
    public int PendingFollowUps { get; set; }
    public List<ChartPoint> LeadStatusChart { get; set; } = new();
    public List<ChartPoint> PipelineChart { get; set; } = new();
    public List<ChartPoint> MonthlySalesChart { get; set; } = new();
}

public sealed class ChartPoint
{
    public ChartPoint(string label, decimal value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; init; }
    public decimal Value { get; init; }
}

public sealed class ScopeFilter
{
    public System.Linq.Expressions.Expression<Func<Customer, bool>> Customers { get; set; } = _ => true;
    public System.Linq.Expressions.Expression<Func<Lead, bool>> Leads { get; set; } = _ => true;
    public System.Linq.Expressions.Expression<Func<Opportunity, bool>> Opportunities { get; set; } = _ => true;
    public System.Linq.Expressions.Expression<Func<FollowUp, bool>> FollowUps { get; set; } = _ => true;
}
