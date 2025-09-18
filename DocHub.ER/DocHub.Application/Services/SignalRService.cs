using DocHub.Core.Interfaces;
using DocHub.Application.Hubs;
using DocHub.Shared.DTOs.Emails;
using DocHub.Shared.DTOs.Common;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DocHub.Application.Services;

public class SignalRService : IRealTimeService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRService> _logger;

    public SignalRService(IHubContext<NotificationHub> hubContext, ILogger<SignalRService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyEmailStatusUpdateAsync(string userId, EmailStatusUpdate update)
    {
        try
        {
            _logger.LogDebug("📡 [SIGNALR] Starting email status notification for user {UserId}, job {JobId}, status {Status}", 
                userId, update.EmailJobId, update.Status);
            
            var groupName = $"user_{userId}";
            _logger.LogDebug("👥 [SIGNALR] Sending to SignalR group: {GroupName}", groupName);
            
            await _hubContext.Clients.Group(groupName)
                .SendAsync("EmailStatusUpdated", update);
            
            _logger.LogInformation("✅ [SIGNALR] Email status update sent to user {UserId}: {JobId} - {Status} (Employee: {EmployeeName})", 
                userId, update.EmailJobId, update.Status, update.EmployeeName ?? "Unknown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [SIGNALR] Error sending email status update to user {UserId}: {Message}", 
                userId, ex.Message);
        }
    }

    public async Task NotifyDocumentGenerationCompleteAsync(string userId, DocumentGenerationUpdate update)
    {
        try
        {
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("DocumentGenerationComplete", update);
            
            _logger.LogInformation("Document generation complete notification sent to user {UserId}: {DocumentId}", 
                userId, update.DocumentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending document generation notification to user {UserId}", userId);
        }
    }

    public async Task NotifySystemAlertAsync(string userId, SystemAlert alert)
    {
        try
        {
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("SystemAlert", alert);
            
            _logger.LogInformation("System alert sent to user {UserId}: {AlertType}", 
                userId, alert.Type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending system alert to user {UserId}", userId);
        }
    }

    public async Task NotifyExcelProcessingCompleteAsync(string userId, ExcelProcessingUpdate update)
    {
        try
        {
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("ExcelProcessingComplete", update);
            
            _logger.LogInformation("Excel processing complete notification sent to user {UserId}: {UploadId}", 
                userId, update.UploadId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Excel processing notification to user {UserId}", userId);
        }
    }

    public async Task NotifyBulkOperationProgressAsync(string userId, BulkOperationProgress progress)
    {
        try
        {
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("BulkOperationProgress", progress);
            
            _logger.LogDebug("Bulk operation progress sent to user {UserId}: {Percentage}%", 
                userId, progress.PercentageComplete);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk operation progress to user {UserId}", userId);
        }
    }

    public async Task NotifyTabDataUpdatedAsync(string tabId, TabDataUpdate update)
    {
        try
        {
            await _hubContext.Clients.Group($"tab_{tabId}")
                .SendAsync("TabDataUpdated", update);
            
            _logger.LogInformation("Tab data update sent for tab {TabId}: {RecordCount} records", 
                tabId, update.RecordCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending tab data update for tab {TabId}", tabId);
        }
    }

    public async Task SubscribeToUpdatesAsync(string userId, string connectionId)
    {
        try
        {
            await _hubContext.Groups.AddToGroupAsync(connectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} subscribed to updates with connection {ConnectionId}", userId, connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing user {UserId} to updates", userId);
        }
    }

    public Task UnsubscribeFromUpdatesAsync(string connectionId)
    {
        try
        {
            // SignalR automatically handles group cleanup when connections disconnect
            _logger.LogInformation("Connection {ConnectionId} unsubscribed from updates", connectionId);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing connection {ConnectionId} from updates", connectionId);
            return Task.CompletedTask;
        }
    }

    public async Task JoinGroupAsync(string connectionId, string groupName)
    {
        try
        {
            await _hubContext.Groups.AddToGroupAsync(connectionId, groupName);
            _logger.LogInformation("Connection {ConnectionId} joined group {GroupName}", connectionId, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding connection {ConnectionId} to group {GroupName}", connectionId, groupName);
        }
    }

    public async Task LeaveGroupAsync(string connectionId, string groupName)
    {
        try
        {
            await _hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName);
            _logger.LogInformation("Connection {ConnectionId} left group {GroupName}", connectionId, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing connection {ConnectionId} from group {GroupName}", connectionId, groupName);
        }
    }

    public async Task BroadcastToGroupAsync(string groupName, object message)
    {
        try
        {
            await _hubContext.Clients.Group(groupName).SendAsync("Broadcast", message);
            _logger.LogInformation("Message broadcasted to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting to group {GroupName}", groupName);
        }
    }

    public async Task BroadcastToUserAsync(string userId, object message)
    {
        try
        {
            await _hubContext.Clients.Group($"user_{userId}").SendAsync("Broadcast", message);
            _logger.LogInformation("Message broadcasted to user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting to user {UserId}", userId);
        }
    }

    public async Task BroadcastToAllAsync(object message)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("Broadcast", message);
            _logger.LogInformation("Message broadcasted to all clients");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting to all clients");
        }
    }
}
