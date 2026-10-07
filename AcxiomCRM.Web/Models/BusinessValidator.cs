using System.Text.RegularExpressions;

namespace AcxiomCRM.Web.Models;

public static partial class BusinessValidator
{
    public static List<string> ValidateCustomer(Customer customer)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(customer.CustomerName))
            errors.Add("Customer Name is required.");

        if (string.IsNullOrWhiteSpace(customer.Email) || !EmailRegex().IsMatch(customer.Email))
            errors.Add("Enter a valid email address.");

        if (string.IsNullOrWhiteSpace(customer.Phone) || !PhoneRegex().IsMatch(customer.Phone))
            errors.Add("Enter a valid phone number.");

        if (customer.CustomerName.Length > 150)
            errors.Add("Customer Name must not exceed 150 characters.");

        return errors;
    }

    public static List<string> ValidateLead(Lead lead)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(lead.LeadName))
            errors.Add("Lead Name is required.");

        if (lead.ExpectedValue < 0 || lead.ExpectedValue > 1000000000)
            errors.Add("Expected Value must be within the configured range.");

        if (string.IsNullOrWhiteSpace(lead.Status))
            errors.Add("Lead Status is required.");

        if (!string.IsNullOrWhiteSpace(lead.Email) && !EmailRegex().IsMatch(lead.Email))
            errors.Add("Enter a valid email address.");

        if (!string.IsNullOrWhiteSpace(lead.Phone) && !PhoneRegex().IsMatch(lead.Phone))
            errors.Add("Enter a valid phone number.");

        return errors;
    }

    public static List<string> ValidateOpportunity(Opportunity opportunity)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(opportunity.OpportunityName))
            errors.Add("Opportunity Name is required.");

        if (opportunity.Amount <= 0)
            errors.Add("Opportunity Amount must be greater than 0.");

        if (opportunity.Probability is < 0 or > 100)
            errors.Add("Probability must be between 0 and 100.");

        if (opportunity.ExpectedCloseDate.HasValue &&
            opportunity.ExpectedCloseDate.Value.Date < DateTime.Today &&
            opportunity.Status.Equals("Open", StringComparison.OrdinalIgnoreCase))
            errors.Add("Expected Close Date cannot be in the past.");

        return errors;
    }

    public static List<string> ValidateFollowUp(FollowUp followUp)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(followUp.Subject))
            errors.Add("Subject is required.");

        if (followUp.FollowUpDate.Date < DateTime.Today &&
            followUp.Status.Equals("Planned", StringComparison.OrdinalIgnoreCase))
            errors.Add("Follow-up date cannot be earlier than today.");

        return errors;
    }

    [GeneratedRegex("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex("^[0-9+()\\-\\s]{8,30}$")]
    private static partial Regex PhoneRegex();
}
