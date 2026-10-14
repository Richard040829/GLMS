using System.ComponentModel.DataAnnotations;
using GLMS.Web.Models;

namespace GLMS.Tests;

public class ModelValidationTests
{
    [Fact]
    public void Client_WithRequiredFieldsMissing_FailsValidation()
    {
        var client = new Client();
        var results = Validate(client);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Client.Name)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Client.ContactDetails)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Client.Region)));
    }

    [Fact]
    public void Client_WithValidFields_PassesValidation()
    {
        var client = new Client
        {
            Name = "TechMove Logistics",
            ContactDetails = "operations@example.com",
            Region = "Southern Africa"
        };
        Assert.Empty(Validate(client));
    }

    [Fact]
    public void Contract_EndDateBeforeStartDate_FailsValidation()
    {
        var contract = new Contract
        {
            ClientId = 1,
            StartDate = new DateTime(2026, 10, 10),
            EndDate = new DateTime(2026, 10, 9),
            ServiceLevel = "Gold",
            Status = ContractStatus.Active
        };
        Assert.Contains(Validate(contract), r => r.MemberNames.Contains(nameof(Contract.EndDate)));
    }

    [Fact]
    public void Contract_EndDateEqualToStartDate_FailsValidation()
    {
        var date = new DateTime(2026, 10, 10);
        var contract = new Contract
        {
            ClientId = 1,
            StartDate = date,
            EndDate = date,
            ServiceLevel = "Gold",
            Status = ContractStatus.Active
        };
        Assert.Contains(Validate(contract), r => r.MemberNames.Contains(nameof(Contract.EndDate)));
    }

    [Fact]
    public void ServiceRequest_ZeroUsdCost_FailsRangeValidation()
    {
        var request = new ServiceRequest
        {
            ContractId = 1,
            Description = "Freight booking",
            CostInUSD = 0,
            CostInZAR = 0,
            Status = "Pending"
        };
        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(ServiceRequest.CostInUSD)));
    }

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        if (model is IValidatableObject custom)
            results.AddRange(custom.Validate(new ValidationContext(model)));
        return results;
    }
}
