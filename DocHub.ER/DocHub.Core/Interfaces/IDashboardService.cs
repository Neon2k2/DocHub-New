using DocHub.Shared.DTOs.Common;
using DocHub.Shared.DTOs.Dashboard;

namespace DocHub.Core.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync(string module, string? userId = null, bool isAdmin = false);
    Task<IEnumerable<DocumentRequestDto>> GetDocumentRequestsAsync(GetDocumentRequestsRequest request);
    Task<DocumentRequestStatsDto> GetDocumentRequestStatsAsync();
}
