using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CustomersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers([FromQuery] string? search = null)
    {
        var currentUser = User.Identity?.Name;
        var isManagerOrAdmin = User.IsInRole("Admin") || User.IsInRole("Manager");
        var query = _context.Customers.AsQueryable();

        if (!isManagerOrAdmin)
            query = query.Where(x => x.AssignedTo == currentUser);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.CustomerName.Contains(search) || x.Email.Contains(search));

        var result = await query.OrderBy(x => x.CustomerName).Select(x => new CustomerDto
        {
            Id = x.Id,
            CustomerCode = x.CustomerCode,
            CustomerName = x.CustomerName,
            Email = x.Email,
            Phone = x.Phone,
            CompanyName = x.CompanyName,
            Address = x.Address,
            City = x.City,
            State = x.State,
            Status = x.Status,
            AssignedTo = x.AssignedTo,
            CreatedDate = x.CreatedDate
        }).ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> GetCustomer(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer is null)
            return NotFound();

        var currentUser = User.Identity?.Name;
        var isManagerOrAdmin = User.IsInRole("Admin") || User.IsInRole("Manager");
        if (!isManagerOrAdmin && !string.Equals(customer.AssignedTo, currentUser, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        return Ok(new CustomerDto
        {
            Id = customer.Id,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            AssignedTo = customer.AssignedTo,
            CreatedDate = customer.CreatedDate
        });
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> CreateCustomer([FromBody] CustomerCreateRequest request)
    {
        if (request is null)
            return BadRequest("Request body is required.");

        var errors = BusinessValidator.ValidateCustomer(new Customer
        {
            CustomerName = request.CustomerName,
            Email = request.Email,
            Phone = request.Phone,
            CompanyName = request.CompanyName,
            Address = request.Address,
            City = request.City,
            State = request.State,
            Status = request.Status
        });

        if (errors.Count > 0)
            return ValidationProblem(new ValidationProblemDetails(errors.ToDictionary(error => error, error => new[] { error })));

        if (await _context.Customers.AnyAsync(x => x.Email == request.Email))
            return Conflict("Customer email already exists.");

        if (await _context.Customers.AnyAsync(x => x.Phone == request.Phone))
            return Conflict("Customer phone already exists.");

        var customer = new Customer
        {
            CustomerCode = $"API-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8]}",
            CustomerName = request.CustomerName,
            Email = request.Email,
            Phone = request.Phone,
            CompanyName = request.CompanyName,
            Address = request.Address,
            City = request.City,
            State = request.State,
            Status = request.Status,
            CreatedBy = "API",
            AssignedTo = "API"
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, new CustomerDto { Id = customer.Id, CustomerCode = customer.CustomerCode, CustomerName = customer.CustomerName, Email = customer.Email, Phone = customer.Phone, CompanyName = customer.CompanyName, Status = customer.Status });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerCreateRequest request)
    {
        if (request is null)
            return BadRequest("Request body is required.");

        var customer = await _context.Customers.FindAsync(id);
        if (customer is null)
            return NotFound();

        var currentUser = User.Identity?.Name;
        var isManagerOrAdmin = User.IsInRole("Admin") || User.IsInRole("Manager");
        if (!isManagerOrAdmin && !string.Equals(customer.AssignedTo, currentUser, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var errors = BusinessValidator.ValidateCustomer(new Customer
        {
            CustomerName = request.CustomerName,
            Email = request.Email,
            Phone = request.Phone,
            CompanyName = request.CompanyName,
            Address = request.Address,
            City = request.City,
            State = request.State,
            Status = request.Status
        });

        if (errors.Count > 0)
            return ValidationProblem(new ValidationProblemDetails(errors.ToDictionary(error => error, error => new[] { error })));

        customer.CustomerName = request.CustomerName;
        customer.Email = request.Email;
        customer.Phone = request.Phone;
        customer.CompanyName = request.CompanyName;
        customer.Address = request.Address;
        customer.City = request.City;
        customer.State = request.State;
        customer.Status = request.Status;
        customer.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer is null)
            return NotFound();

        var currentUser = User.Identity?.Name;
        var isManagerOrAdmin = User.IsInRole("Admin") || User.IsInRole("Manager");
        if (!isManagerOrAdmin && !string.Equals(customer.AssignedTo, currentUser, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        customer.IsDeleted = true;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
