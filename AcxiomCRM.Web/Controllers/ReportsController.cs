using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var roles = await _userManager.GetRolesAsync(user!);
        var isManagerOrAdmin = roles.Contains("Admin") || roles.Contains("Manager");
        var userName = user?.UserName;

        var customerReport = await _context.Customers
            .Where(x => isManagerOrAdmin || x.AssignedTo == userName)
            .Select(x => new { x.CustomerName, x.Status, x.AssignedTo, x.CreatedDate })
            .ToListAsync();
        var leadReport = await _context.Leads
            .Where(x => isManagerOrAdmin || x.AssignedTo == userName)
            .Select(x => new { x.LeadName, x.Source, x.Status, x.AssignedTo, x.CustomerId })
            .ToListAsync();
        var opportunityReport = await _context.Opportunities
            .Where(x => isManagerOrAdmin || x.AssignedTo == userName)
            .Select(x => new { x.OpportunityName, x.Stage, x.Amount, x.Probability, x.ExpectedCloseDate, x.AssignedTo })
            .ToListAsync();
        var followUpReport = await _context.FollowUps
            .Where(x => isManagerOrAdmin || x.AssignedTo == userName)
            .Select(x => new { x.Subject, x.Status, x.AssignedTo, x.FollowUpDate })
            .ToListAsync();

        var pipelineReport = await _context.Opportunities
            .Where(x => isManagerOrAdmin || x.AssignedTo == userName)
            .GroupBy(x => new { x.Stage, x.AssignedTo })
            .Select(x => new { x.Key.Stage, x.Key.AssignedTo, Amount = x.Sum(y => y.Amount), WeightedPipeline = x.Sum(y => y.Amount * y.Probability / 100m) })
            .ToListAsync();

        return View(new ReportsViewModel
        {
            Customers = customerReport,
            Leads = leadReport,
            Opportunities = opportunityReport,
            FollowUps = followUpReport,
            Pipeline = pipelineReport
        });
    }

    [HttpGet("[action]")]
    public async Task<IActionResult> Data([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string type = "sales")
    {
        var user = await _userManager.GetUserAsync(User);
        var roles = await _userManager.GetRolesAsync(user!);
        var isManagerOrAdmin = roles.Contains("Admin") || roles.Contains("Manager");
        var userName = user?.UserName;

        var query = _context.Opportunities
            .Where(x => isManagerOrAdmin || x.AssignedTo == userName);

        if (from.HasValue)
            query = query.Where(x => x.CreatedDate >= from.Value.Date);

        if (to.HasValue)
            query = query.Where(x => x.CreatedDate <= to.Value.Date.AddDays(1));

        var data = type switch
        {
            "pipeline" => await query
                .GroupBy(x => x.Stage)
                .Select(x => new ReportChartItem { Label = x.Key, Value = x.Sum(y => y.Amount) })
                .OrderBy(x => x.Label)
                .ToListAsync(),
            "activities" => await query
                .GroupBy(x => x.Stage)
                .Select(x => new ReportChartItem { Label = x.Key, Value = x.Count() })
                .OrderBy(x => x.Label)
                .ToListAsync(),
            _ => await query
                .GroupBy(x => x.Stage)
                .Select(x => new ReportChartItem { Label = x.Key, Value = x.Sum(y => y.Amount) })
                .OrderBy(x => x.Label)
                .ToListAsync()
        };

        return Json(new
        {
            labels = data.Select(x => x.Label).ToArray(),
            values = data.Select(x => Convert.ToDouble(x.Value)).ToArray()
        });
    }
}

public class ReportChartItem
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class ReportsViewModel
{
    public object Customers { get; set; } = new();
    public object Leads { get; set; } = new();
    public object Opportunities { get; set; } = new();
    public object FollowUps { get; set; } = new();
    public object Pipeline { get; set; } = new();
}
