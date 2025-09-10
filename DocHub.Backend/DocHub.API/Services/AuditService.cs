using DocHub.API.Data;
using DocHub.API.Models;
using DocHub.API.Services.Interfaces;
using DocHub.API.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DocHub.API.Services;

public class AuditService : IAuditService
{
    private readonly DocHubDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(DocHubDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(string userId, string userName, string action, string entityType, string entityId, object? oldValues = null, object? newValues = null, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserName = userName,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                IpAddress = ipAddress ?? "Unknown",
                UserAgent = userAgent ?? "Unknown",
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Audit log created: {Action} on {EntityType} by {UserName}", action, entityType, userName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for action: {Action} on {EntityType}", action, entityType);
            // Don't throw exception to avoid breaking the main operation
        }
    }

    public async Task<IEnumerable<AuditLog>> GetLogsAsync(string? userId = null, string? action = null, string? entityType = null, string? entityId = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 50)
    {
        try
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(a => a.UserId == userId);
            }

            if (!string.IsNullOrEmpty(action))
            {
                query = query.Where(a => a.Action == action);
            }

            if (!string.IsNullOrEmpty(entityType))
            {
                query = query.Where(a => a.EntityType == entityType);
            }

            if (!string.IsNullOrEmpty(entityId))
            {
                query = query.Where(a => a.EntityId == entityId);
            }

            if (startDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= endDate.Value);
            }

            return await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit logs");
            throw;
        }
    }

    public async Task<AuditLogStats> GetStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _context.AuditLogs.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= endDate.Value);
            }

            var logs = await query.ToListAsync();

            var stats = new AuditLogStats
            {
                TotalLogs = logs.Count,
                ByAction = logs.GroupBy(a => a.Action)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ByEntityType = logs.GroupBy(a => a.EntityType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ByUser = logs.GroupBy(a => a.UserId)
                    .ToDictionary(g => g.Key, g => g.Count()),
                RecentActivity = logs.Count(a => a.Timestamp >= DateTime.UtcNow.AddHours(-24))
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit log statistics");
            throw;
        }
    }
}
