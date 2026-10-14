using GLMS.Web.Models;

namespace GLMS.Web.ViewModels;

public class HomeDashboardViewModel
{
    public int TotalClients { get; init; }
    public int TotalContracts { get; init; }
    public int ActiveContracts { get; init; }
    public int ExpiredContracts { get; init; }
    public int TotalServiceRequests { get; init; }
    public decimal TotalServiceRequestValueZar { get; init; }
    public IReadOnlyList<ServiceRequest> RecentRequests { get; init; } = Array.Empty<ServiceRequest>();
}
