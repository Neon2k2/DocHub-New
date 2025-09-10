using DocHub.API.Models;
using DocHub.API.Extensions;

namespace DocHub.API.Services.Interfaces;

public interface IAuditService
{
    Task LogAsync(string userId, string userName, string action, string entityType, string entityId, object? oldValues = null, object? newValues = null, string? ipAddress = null, string? userAgent = null);
    Task<IEnumerable<AuditLog>> GetLogsAsync(string? userId = null, string? action = null, string? entityType = null, string? entityId = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 50);
    Task<AuditLogStats> GetStatsAsync(DateTime? startDate = null, DateTime? endDate = null);
}

public class AuditLogStats
{
    public int TotalLogs { get; set; }
    public Dictionary<string, int> ByAction { get; set; } = new();
    public Dictionary<string, int> ByEntityType { get; set; } = new();
    public Dictionary<string, int> ByUser { get; set; } = new();
    public int RecentActivity { get; set; }
}
