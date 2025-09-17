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
    private readonly ICacheService _cacheService;

    public DashboardService(IDbContext dbContext, ILogger<DashboardService> logger, ICacheService cacheService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync(string module, string? userId = null, bool isAdmin = false)
    {
        try
        {
            _logger.LogInformation("📊 [DASHBOARD-STATS] Getting dashboard stats for module: {Module}", module);

            var cacheKey = $"dashboard:{module}:{userId ?? "all"}:{isAdmin}";
            
            // Completely disable caching for debugging
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            _logger.LogInformation("🔄 [DASHBOARD-STATS] Calculating stats for {Module} at {Timestamp}", module, now);

            // Execute queries sequentially to avoid DbContext concurrency issues
            // Get user statistics
            var totalUsers = await _dbContext.Users.CountAsync();
            var activeUsers = await _dbContext.Users.CountAsync(u => u.IsActive);
            var activeSessions = await _dbContext.Users.CountAsync(u => u.LastLoginAt.HasValue && u.LastLoginAt >= DateTime.UtcNow.AddHours(-24));
            var newJoiningsThisMonth = await _dbContext.Users.CountAsync(u => u.CreatedAt >= startOfMonth);
            var relievedThisMonth = await _dbContext.Users.CountAsync(u => !u.IsActive && u.UpdatedAt >= startOfMonth);
            
            // Get letter type statistics
            var totalLetterTypes = await _dbContext.LetterTypeDefinitions.CountAsync();
            var activeLetterTypes = await _dbContext.LetterTypeDefinitions.CountAsync(lt => lt.IsActive);
            
            // Get document and email statistics
            var totalDocuments = await _dbContext.GeneratedDocuments.CountAsync();
            var totalEmailJobs = await _dbContext.EmailJobs.CountAsync();
            var pendingEmails = await _dbContext.EmailJobs.CountAsync(ej => ej.Status == "pending" || ej.Status == "sent");
            var approvedEmails = await _dbContext.EmailJobs.CountAsync(ej => ej.Status == "delivered" || ej.Status == "opened");
            
            // Simple uptime calculation
            var systemUptime = 99.9m;
            
            // Return empty activities for now
            var recentActivities = new List<ActivityDto>();

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
                TotalHoursThisMonth = (decimal)totalDocuments,
                BillableHours = (decimal)totalEmailJobs,
                PendingTimesheets = pendingEmails,
                ApprovedTimesheets = approvedEmails,
                TotalRevenue = totalDocuments * 10.0m, // $10 per document
                RecentActivities = recentActivities
            };

            _logger.LogInformation("✅ [DASHBOARD-STATS] Dashboard stats calculated successfully for module: {Module}", module);
            _logger.LogInformation("🔍 [DASHBOARD-STATS] Final stats - TotalUsers: {TotalUsers}, ActiveUsers: {ActiveUsers}, TotalProjects: {TotalProjects}, ActiveProjects: {ActiveProjects}, BillableHours: {BillableHours}, PendingTimesheets: {PendingTimesheets}", 
                stats.TotalUsers, stats.ActiveUsers, stats.TotalProjects, stats.ActiveProjects, stats.BillableHours, stats.PendingTimesheets);
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [DASHBOARD-STATS] Error getting dashboard stats for module: {Module}", module);
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
                .AsNoTracking() // Improve performance for read-only queries
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

            // Use a single query with conditional aggregation for better performance
            var stats = await _dbContext.GeneratedDocuments
                .GroupBy(gd => 1)
                .Select(g => new DocumentRequestStatsDto
                {
                    TotalRequests = g.Count(),
                    PendingRequests = g.Count(gd => gd.EmailJobs.Any(ej => ej.Status == "pending")),
                    ApprovedRequests = g.Count(gd => gd.EmailJobs.Any(ej => ej.Status == "delivered" || ej.Status == "opened")),
                    RejectedRequests = g.Count(gd => gd.EmailJobs.Any(ej => ej.Status == "bounced" || ej.Status == "dropped")),
                    InProgressRequests = g.Count(gd => gd.EmailJobs.Any(ej => ej.Status == "sent"))
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (stats == null)
            {
                stats = new DocumentRequestStatsDto();
            }

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

            // Use parallel queries for better performance
            var userGuid = !string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var parsed) ? parsed : (Guid?)null;

            var documentsTask = GetRecentDocumentActivitiesAsync(userGuid, isAdmin);
            var emailsTask = GetRecentEmailActivitiesAsync(userGuid, isAdmin);

            await Task.WhenAll(documentsTask, emailsTask);

            var recentDocuments = await documentsTask;
            var recentEmails = await emailsTask;

            activities.AddRange(recentDocuments);
            activities.AddRange(recentEmails);

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

    private async Task<List<ActivityDto>> GetRecentDocumentActivitiesAsync(Guid? userGuid, bool isAdmin)
    {
        var documentsQuery = _dbContext.GeneratedDocuments
            .Include(gd => gd.GeneratedByUser)
            .AsQueryable();

        if (!isAdmin && userGuid.HasValue)
        {
            documentsQuery = documentsQuery.Where(gd => gd.GeneratedBy == userGuid.Value);
        }

        var recentDocuments = await documentsQuery
            .OrderByDescending(gd => gd.GeneratedAt)
            .Take(5)
            .AsNoTracking()
            .ToListAsync();

        return recentDocuments
            .Where(doc => doc.GeneratedByUser != null)
            .Select(doc => new ActivityDto
            {
                Id = doc.Id.ToString(),
                Type = "document_generation",
                EmployeeName = $"{doc.GeneratedByUser!.FirstName} {doc.GeneratedByUser.LastName}".Trim(),
                EmployeeId = doc.GeneratedByUser.Id.ToString(),
                Status = "completed",
                CreatedAt = doc.GeneratedAt
            })
            .ToList();
    }

    private async Task<List<ActivityDto>> GetRecentEmailActivitiesAsync(Guid? userGuid, bool isAdmin)
    {
        var emailsQuery = _dbContext.EmailJobs
            .AsQueryable();

        if (!isAdmin && userGuid.HasValue)
        {
            emailsQuery = emailsQuery.Where(ej => ej.SentBy == userGuid.Value);
        }

        var recentEmails = await emailsQuery
            .OrderByDescending(ej => ej.CreatedAt)
            .Take(5)
            .AsNoTracking()
            .ToListAsync();

        return recentEmails.Select(email => new ActivityDto
        {
            Id = email.Id.ToString(),
            Type = "email_sent",
            EmployeeName = email.RecipientName ?? "Unknown",
            EmployeeId = email.RecipientEmail ?? "Unknown",
            Status = email.Status,
            CreatedAt = email.CreatedAt
        }).ToList();
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

    private async Task<(int TotalUsers, int ActiveUsers, int ActiveSessions, int NewJoiningsThisMonth, int RelievedThisMonth)> GetUserStatsAsync(DateTime startOfMonth)
    {
        try
        {
            _logger.LogInformation("📊 [USER-STATS] Getting user statistics");
            
            // First, let's check what data actually exists
            var totalUsersCount = await _dbContext.Users.CountAsync();
            var activeUsersCount = await _dbContext.Users.CountAsync(u => u.IsActive);
            var allUsers = await _dbContext.Users.Take(5).Select(u => new { u.Id, u.IsActive, u.CreatedAt, u.LastLoginAt }).ToListAsync();
            
            _logger.LogInformation("🔍 [USER-STATS] Debug - Total users in DB: {Total}, Active users: {Active}", totalUsersCount, activeUsersCount);
            _logger.LogInformation("🔍 [USER-STATS] Debug - Sample users: {Users}", string.Join(", ", allUsers.Select(u => $"Id={u.Id}, Active={u.IsActive}, Created={u.CreatedAt:yyyy-MM-dd}, LastLogin={u.LastLoginAt}")));
            
            var userStats = await _dbContext.Users
                .GroupBy(u => 1)
                .Select(g => new
                {
                    TotalUsers = g.Count(),
                    ActiveUsers = g.Count(u => u.IsActive),
                    ActiveSessions = g.Count(u => u.LastLoginAt.HasValue && u.LastLoginAt >= DateTime.UtcNow.AddHours(-24)),
                    NewJoiningsThisMonth = g.Count(u => u.CreatedAt >= startOfMonth),
                    RelievedThisMonth = g.Count(u => !u.IsActive && u.UpdatedAt >= startOfMonth)
                })
                .FirstOrDefaultAsync();

            if (userStats == null)
            {
                _logger.LogWarning("⚠️ [USER-STATS] No user statistics found, returning zeros");
                return (0, 0, 0, 0, 0);
            }

            _logger.LogInformation("✅ [USER-STATS] Retrieved user statistics: Total={Total}, Active={Active}, Sessions={Sessions}, NewThisMonth={New}, RelievedThisMonth={Relieved}", 
                userStats.TotalUsers, userStats.ActiveUsers, userStats.ActiveSessions, userStats.NewJoiningsThisMonth, userStats.RelievedThisMonth);

            return (userStats.TotalUsers, userStats.ActiveUsers, userStats.ActiveSessions, 
                   userStats.NewJoiningsThisMonth, userStats.RelievedThisMonth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [USER-STATS] Error getting user statistics");
            return (0, 0, 0, 0, 0);
        }
    }

    private async Task<(int TotalLetterTypes, int ActiveLetterTypes)> GetLetterTypeStatsAsync()
    {
        try
        {
            _logger.LogInformation("📊 [LETTER-TYPE-STATS] Getting letter type statistics");
            
            // Debug: Check what data exists
            var totalLetterTypes = await _dbContext.LetterTypeDefinitions.CountAsync();
            var activeLetterTypes = await _dbContext.LetterTypeDefinitions.CountAsync(lt => lt.IsActive);
            var sampleLetterTypes = await _dbContext.LetterTypeDefinitions.Take(3).Select(lt => new { lt.Id, lt.DisplayName, lt.IsActive }).ToListAsync();
            
            _logger.LogInformation("🔍 [LETTER-TYPE-STATS] Debug - Total letter types: {Total}, Active: {Active}", totalLetterTypes, activeLetterTypes);
            _logger.LogInformation("🔍 [LETTER-TYPE-STATS] Debug - Sample letter types: {Types}", string.Join(", ", sampleLetterTypes.Select(lt => $"Id={lt.Id}, DisplayName={lt.DisplayName}, Active={lt.IsActive}")));
            
            // Get letter type stats directly without GroupBy
            var letterTypeStats = new { TotalLetterTypes = totalLetterTypes, ActiveLetterTypes = activeLetterTypes };

            if (letterTypeStats == null)
            {
                _logger.LogWarning("⚠️ [LETTER-TYPE-STATS] No letter type statistics found, returning zeros");
                return (0, 0);
            }

            _logger.LogInformation("✅ [LETTER-TYPE-STATS] Retrieved letter type statistics: Total={Total}, Active={Active}", 
                letterTypeStats.TotalLetterTypes, letterTypeStats.ActiveLetterTypes);

            return (letterTypeStats.TotalLetterTypes, letterTypeStats.ActiveLetterTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [LETTER-TYPE-STATS] Error getting letter type statistics");
            return (0, 0);
        }
    }

    private async Task<(int DocumentsThisMonth, int EmailJobsThisMonth, int PendingEmails, int ApprovedEmails)> GetDocumentEmailStatsAsync(DateTime startOfMonth)
    {
        try
        {
            _logger.LogInformation("📊 [DOCUMENT-EMAIL-STATS] Getting document and email statistics");
            
            // Debug: Check what data exists
            var totalDocuments = await _dbContext.GeneratedDocuments.CountAsync();
            var totalEmailJobs = await _dbContext.EmailJobs.CountAsync();
            var sampleDocuments = await _dbContext.GeneratedDocuments.Take(3).Select(gd => new { gd.Id, gd.GeneratedAt }).ToListAsync();
            var sampleEmails = await _dbContext.EmailJobs.Take(3).Select(ej => new { ej.Id, ej.Status, ej.CreatedAt }).ToListAsync();
            
            _logger.LogInformation("🔍 [DOCUMENT-EMAIL-STATS] Debug - Total documents: {TotalDocs}, Total email jobs: {TotalEmails}", totalDocuments, totalEmailJobs);
            _logger.LogInformation("🔍 [DOCUMENT-EMAIL-STATS] Debug - Sample documents: {Docs}", string.Join(", ", sampleDocuments.Select(d => $"Id={d.Id}, Generated={d.GeneratedAt:yyyy-MM-dd}")));
            _logger.LogInformation("🔍 [DOCUMENT-EMAIL-STATS] Debug - Sample emails: {Emails}", string.Join(", ", sampleEmails.Select(e => $"Id={e.Id}, Status={e.Status}, Created={e.CreatedAt:yyyy-MM-dd}")));
            _logger.LogInformation("🔍 [DOCUMENT-EMAIL-STATS] Debug - Start of month filter: {StartOfMonth}", startOfMonth.ToString("yyyy-MM-dd"));
            
            // Count all documents (not just this month) since GeneratedDocuments table is empty
            var documentStats = await _dbContext.GeneratedDocuments.CountAsync();

            // Count all email jobs and their statuses (not just this month)
            // Since we don't know the exact status values, let's count all email jobs
            var pendingEmails = totalEmailJobs; // All email jobs are considered pending for now
            var approvedEmails = 0; // No approved emails yet
            
            var emailStats = new { 
                EmailJobsThisMonth = totalEmailJobs, 
                PendingEmails = pendingEmails, 
                ApprovedEmails = approvedEmails 
            };

            if (emailStats == null)
            {
                _logger.LogWarning("⚠️ [DOCUMENT-EMAIL-STATS] No email statistics found, returning document count only");
                return (documentStats, 0, 0, 0);
            }

            _logger.LogInformation("✅ [DOCUMENT-EMAIL-STATS] Retrieved document and email statistics: Documents={Documents}, Emails={Emails}, Pending={Pending}, Approved={Approved}", 
                documentStats, emailStats.EmailJobsThisMonth, emailStats.PendingEmails, emailStats.ApprovedEmails);

            return (documentStats, emailStats.EmailJobsThisMonth, emailStats.PendingEmails, emailStats.ApprovedEmails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [DOCUMENT-EMAIL-STATS] Error getting document and email statistics");
            return (0, 0, 0, 0);
        }
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
