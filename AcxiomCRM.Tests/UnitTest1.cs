using AcxiomCRM.Web.Models;

namespace AcxiomCRM.Tests;

public class BusinessValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Opportunity_WithNonPositiveAmount_IsInvalid(decimal amount)
    {
        var opportunity = new Opportunity
        {
            OpportunityName = "Test",
            Amount = amount,
            Probability = 50,
            ExpectedCloseDate = DateTime.Today.AddDays(5),
            Stage = "Qualification",
            Status = "Open",
            CustomerId = 1,
            AssignedTo = "user"
        };

        var errors = BusinessValidator.ValidateOpportunity(opportunity);

        Assert.Contains(errors, error => error.Contains("greater than 0"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Opportunity_WithInvalidProbability_IsInvalid(int probability)
    {
        var opportunity = new Opportunity
        {
            OpportunityName = "Test",
            Amount = 1000,
            Probability = probability,
            ExpectedCloseDate = DateTime.Today.AddDays(5),
            Stage = "Qualification",
            Status = "Open",
            CustomerId = 1,
            AssignedTo = "user"
        };

        var errors = BusinessValidator.ValidateOpportunity(opportunity);

        Assert.Contains(errors, error => error.Contains("between 0 and 100"));
    }

    [Fact]
    public void ActiveOpportunity_WithPastCloseDate_IsInvalid()
    {
        var opportunity = new Opportunity
        {
            OpportunityName = "Test",
            Amount = 1000,
            Probability = 20,
            ExpectedCloseDate = DateTime.Today.AddDays(-1),
            Stage = "Qualification",
            Status = "Open",
            CustomerId = 1,
            AssignedTo = "user"
        };

        var errors = BusinessValidator.ValidateOpportunity(opportunity);

        Assert.Contains(errors, error => error.Contains("cannot be in the past"));
    }

    [Fact]
    public void PlannedFollowUp_WithPastDate_IsInvalid()
    {
        var followUp = new FollowUp
        {
            Subject = "Call",
            FollowUpDate = DateTime.Today.AddDays(-1),
            Status = "Planned",
            AssignedTo = "user"
        };

        var errors = BusinessValidator.ValidateFollowUp(followUp);

        Assert.Contains(errors, error => error.Contains("cannot be earlier than today"));
    }
}