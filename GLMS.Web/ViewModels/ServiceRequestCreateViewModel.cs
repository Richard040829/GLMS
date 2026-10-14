using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GLMS.Web.ViewModels;

public class ServiceRequestCreateViewModel
{
    [Required, Display(Name = "Contract")]
    public int ContractId { get; set; }

    [Required, StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 100000000), Display(Name = "Cost (USD)")]
    public decimal CostInUSD { get; set; }

    public IEnumerable<SelectListItem> Contracts { get; set; } = Enumerable.Empty<SelectListItem>();
}
