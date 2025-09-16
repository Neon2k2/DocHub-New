using DocHub.Core.Interfaces;
using DocHub.Core.Entities;
using DocHub.Shared.DTOs.Dashboard;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocHub.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(IDbContext dbContext, ILogger<DashboardService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync(string module, string? userId = null, bool isAdmin = false)
    {
        try
        {
            _logger.LogInformation("Getting dashboard stats for module: {Module}", module);

            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            // Get total and active users
            var totalUsers = await _dbContext.Users.CountAsync();
            var activeUsers = await _dbContext.Users.CountAsync(u => u.IsActive);

            // Calculate system uptime (simplified - based on application start time)
            var systemUptime = await CalculateSystemUptimeAsync();

            // Get active sessions (users who logged in within last 24 hours)
            var activeSessions = await _dbContext.Users
                .CountAsync(u => u.LastLoginAt.HasValue && u.LastLoginAt >= DateTime.UtcNow.AddHours(-24));

            // Get users created this month (new joinings)
            var newJoiningsThisMonth = await _dbContext.Users
                .CountAsync(u => u.CreatedAt >= startOfMonth);

            // Get users deactivated this month (relieved)
            var relievedThisMonth = await _dbContext.Users
                .CountAsync(u => !u.IsActive && u.UpdatedAt >= startOfMonth);

            // Get total and active letter types (projects/document types)
            var totalLetterTypes = await _dbContext.LetterTypeDefinitions.CountAsync();
            var activeLetterTypes = await _dbContext.LetterTypeDefinitions.CountAsync(lt => lt.IsActive);

            // Get generated documents this month (hours equivalent)
            var documentsThisMonth = await _dbContext.GeneratedDocuments
                .CountAsync(gd => gd.GeneratedAt >= startOfMonth);

            // Get email jobs this month (billable hours equivalent)
            var emailJobsThisMonth = await _dbContext.EmailJobs
                .CountAsync(ej => ej.CreatedAt >= startOfMonth);

            // Get pending and approved email jobs (timesheets equivalent)
            var pendingEmails = await _dbContext.EmailJobs
                .CountAsync(ej => ej.Status == "pending");
            var approvedEmails = await _dbContext.EmailJobs
                .CountAsync(ej => ej.Status == "delivered" || ej.Status == "opened");

            // Calculate revenue based on generated documents (simplified)
            var totalRevenue = documentsThisMonth * 10.0m; // $10 per document

            // Get recent activities (last 10 generated documents and email jobs)
            var recentActivities = await GetRecentActivitiesAsync(userId, isAdmin);

            var stats = new DashboardStatsDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                SystemUptime = systemUptime,
                ActiveSessions = activeSessions,
                NewJoiningsThisMonth = newJoiningsThisMonth,
                RelievedThisMonth = relievedThisMonth,
                TotalProjects = totalLetterTypes,
                ActiveProjects = activeLetterTypes,
                TotalHoursThisMonth = documentsThisMonth,
                BillableHours = emailJobsThisMonth,
                PendingTimesheets = pendingEmails,
                ApprovedTimesheets = approvedEmails,
                TotalRevenue = totalRevenue,
                RecentActivities = recentActivities
            };

            _logger.LogInformation("Dashboard stats calculated successfully for module: {Module}", module);
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats for module: {Module}", module);
            throw;
        }
    }

    public async Task<IEnumerable<DocumentRequestDto>> GetDocumentRequestsAsync(GetDocumentRequestsRequest request)
    {
        try
        {
            _logger.LogInformation("Getting document requests with filters: DocumentType={DocumentType}, Status={Status}, EmployeeId={EmployeeId}", 
                request.DocumentType, request.Status, request.EmployeeId);

            var query = _dbContext.GeneratedDocuments.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(request.DocumentType))
            {
                query = query.Where(gd => gd.LetterTypeDefinition.DisplayName.Contains(request.DocumentType));
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                // Map status to email job status
                var emailJobStatus = MapDocumentStatusToEmailStatus(request.Status);
                query = query.Where(gd => gd.EmailJobs.Any(ej => ej.Status == emailJobStatus));
            }

            if (!string.IsNullOrEmpty(request.EmployeeId))
            {
                // This would need to be implemented based on how employee data is stored
                // For now, we'll use the ExcelUploadId as a proxy
                if (Guid.TryParse(request.EmployeeId, out var employeeGuid))
                {
                    query = query.Where(gd => gd.ExcelUploadId == employeeGuid);
                }
            }

            var documents = await query
                .Include(gd => gd.LetterTypeDefinition)
                .Include(gd => gd.GeneratedByUser)
                .Include(gd => gd.EmailJobs)
                .OrderByDescending(gd => gd.GeneratedAt)
                .Skip((request.Page - 1) * request.Limit)
                .Take(request.Limit)
                .ToListAsync();

            var documentRequests = documents.Select(gd => new DocumentRequestDto
            {
                Id = gd.Id.ToString(),
                EmployeeId = gd.ExcelUploadId?.ToString() ?? string.Empty,
                EmployeeName = gd.GeneratedByUser.FirstName + " " + gd.GeneratedByUser.LastName,
                DocumentType = gd.LetterTypeDefinition.DisplayName,
                Status = GetDocumentStatus(gd),
                RequestedBy = gd.GeneratedByUser.FirstName + " " + gd.GeneratedByUser.LastName,
                CreatedAt = gd.GeneratedAt,
                ProcessedAt = gd.EmailJobs.FirstOrDefault()?.SentAt,
                Comments = gd.Metadata,
                Metadata = gd.Metadata
            }).ToList();

            _logger.LogInformation("Retrieved {Count} document requests", documentRequests.Count);
            return documentRequests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document requests");
            throw;
        }
    }

    public async Task<DocumentRequestStatsDto> GetDocumentRequestStatsAsync()
    {
        try
        {
            _logger.LogInformation("Getting document request statistics");

            var totalRequests = await _dbContext.GeneratedDocuments.CountAsync();
            
            var pendingRequests = await _dbContext.GeneratedDocuments
                .CountAsync(gd => gd.EmailJobs.Any(ej => ej.Status == "pending"));
            
            var approvedRequests = await _dbContext.GeneratedDocuments
                .CountAsync(gd => gd.EmailJobs.Any(ej => ej.Status == "delivered" || ej.Status == "opened"));
            
            var rejectedRequests = await _dbContext.GeneratedDocuments
                .CountAsync(gd => gd.EmailJobs.Any(ej => ej.Status == "bounced" || ej.Status == "dropped"));
            
            var inProgressRequests = await _dbContext.GeneratedDocuments
                .CountAsync(gd => gd.EmailJobs.Any(ej => ej.Status == "sent"));

            var stats = new DocumentRequestStatsDto
            {
                TotalRequests = totalRequests,
                PendingRequests = pendingRequests,
                ApprovedRequests = approvedRequests,
                RejectedRequests = rejectedRequests,
                InProgressRequests = inProgressRequests
            };

            _logger.LogInformation("Document request statistics calculated successfully");
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document request statistics");
            throw;
        }
    }

    private async Task<List<ActivityDto>> GetRecentActivitiesAsync(string? userId = null, bool isAdmin = false)
    {
        try
        {
            var activities = new List<ActivityDto>();

            // Get recent documents - filter by user if not admin
            var documentsQuery = _dbContext.GeneratedDocuments
                .Include(gd => gd.LetterTypeDefinition)
                .Include(gd => gd.GeneratedByUser)
                .AsQueryable();

            if (!isAdmin && !string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
            {
                documentsQuery = documentsQuery.Where(gd => gd.GeneratedBy == userGuid);
            }

            var recentDocuments = await documentsQuery
                .OrderByDescending(gd => gd.GeneratedAt)
                .Take(5)
                .ToListAsync();

            // Get recent emails - filter by user if not admin
            var emailsQuery = _dbContext.EmailJobs
                .Include(ej => ej.LetterTypeDefinition)
                .Include(ej => ej.SentByUser)
                .AsQueryable();

            if (!isAdmin && !string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid2))
            {
                emailsQuery = emailsQuery.Where(ej => ej.SentBy == userGuid2);
            }

            var recentEmails = await emailsQuery
                .OrderByDescending(ej => ej.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Add document generation activities
            foreach (var doc in recentDocuments)
            {
                if (doc.GeneratedByUser != null)
                {
                    activities.Add(new ActivityDto
                    {
                        Id = doc.Id.ToString(),
                        Type = "document_generation",
                        EmployeeName = $"{doc.GeneratedByUser.FirstName} {doc.GeneratedByUser.LastName}".Trim(),
                        EmployeeId = doc.GeneratedByUser.Id.ToString(),
                        Status = "completed",
                        CreatedAt = doc.GeneratedAt
                    });
                }
            }

            // Add email activities
            foreach (var email in recentEmails)
            {
                activities.Add(new ActivityDto
                {
                    Id = email.Id.ToString(),
                    Type = "email_sent",
                    EmployeeName = email.RecipientName ?? "Unknown",
                    EmployeeId = email.RecipientEmail ?? "Unknown",
                    Status = email.Status,
                    CreatedAt = email.CreatedAt
                });
            }

            return activities
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent activities");
            return new List<ActivityDto>();
        }
    }

    private string GetDocumentStatus(GeneratedDocument document)
    {
        var emailJob = document.EmailJobs.FirstOrDefault();
        if (emailJob == null)
            return "pending";

        return emailJob.Status switch
        {
            "pending" => "pending",
            "sent" => "in_progress",
            "delivered" or "opened" => "approved",
            "bounced" or "dropped" => "rejected",
            _ => "pending"
        };
    }

    private string MapDocumentStatusToEmailStatus(string documentStatus)
    {
        return documentStatus switch
        {
            "pending" => "pending",
            "in_progress" => "sent",
            "approved" => "delivered",
            "rejected" => "bounced",
            _ => "pending"
        };
    }

    private async Task<decimal> CalculateSystemUptimeAsync()
    {
        try
        {
            // For a real implementation, you would track system start time
            // For now, we'll calculate based on the oldest user creation date as a proxy
            var oldestUser = await _dbContext.Users
                .OrderBy(u => u.CreatedAt)
                .FirstOrDefaultAsync();

            if (oldestUser == null)
            {
                return 99.9m; // Default uptime if no users exist
            }

            var systemStartTime = oldestUser.CreatedAt;
            var totalUptime = DateTime.UtcNow - systemStartTime;
            var totalDays = (decimal)totalUptime.TotalDays;

            // Simulate some downtime (for demo purposes, assume 99.9% uptime)
            // In a real system, you'd track actual downtime events
            var downtimeHours = totalDays * 0.001m; // 0.1% downtime
            var uptimeHours = totalDays * 24 - downtimeHours;
            var uptimePercentage = (uptimeHours / (totalDays * 24)) * 100;

            return Math.Round(uptimePercentage, 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating system uptime");
            return 99.9m; // Default uptime on error
        }
    }
}
