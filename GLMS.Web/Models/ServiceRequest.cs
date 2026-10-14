using System.ComponentModel.DataAnnotations;

namespace GLMS.Web.Models;

public class ServiceRequest
{
    public int Id { get; set; }

    [Required, Display(Name = "Contract")]
    public int ContractId { get; set; }
    public Contract? Contract { get; set; }

    [Required, StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 100000000), Display(Name = "Cost (USD)")]
    public decimal CostInUSD { get; set; }

    [Display(Name = "Cost (ZAR)")]
    public decimal CostInZAR { get; set; }

    [Required, StringLength(50)]
    public string Status { get; set; } = "Pending";
}
