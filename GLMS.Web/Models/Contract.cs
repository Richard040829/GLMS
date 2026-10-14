using System.ComponentModel.DataAnnotations;

namespace GLMS.Web.Models;

public enum ContractStatus { Draft = 0, Active = 1, Expired = 2, OnHold = 3 }

public class Contract : IValidatableObject
{
    public int Id { get; set; }

    [Required, Display(Name = "Client")]
    public int ClientId { get; set; }
    public Client? Client { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Start Date")]
    public DateTime StartDate { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "End Date")]
    public DateTime EndDate { get; set; }

    [Required]
    public ContractStatus Status { get; set; } = ContractStatus.Draft;

    [Required, StringLength(100), Display(Name = "Service Level")]
    public string ServiceLevel { get; set; } = string.Empty;

    [StringLength(300)]
    public string? SignedAgreementPath { get; set; }

    public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();

    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        if (EndDate <= StartDate)
            yield return new ValidationResult("End date must be after start date.", new[] { nameof(EndDate) });
    }
}
