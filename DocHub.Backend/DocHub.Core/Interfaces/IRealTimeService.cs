using DocHub.Shared.DTOs.Emails;
using DocHub.Shared.DTOs.Common;

namespace DocHub.Core.Interfaces;

public interface IRealTimeService
{
    // Real-time Notifications
    Task NotifyEmailStatusUpdateAsync(string userId, EmailStatusUpdate update);
    Task NotifyDocumentGenerationCompleteAsync(string userId, DocumentGenerationUpdate update);
    Task NotifySystemAlertAsync(string userId, SystemAlert alert);
    Task NotifyBulkOperationProgressAsync(string userId, BulkOperationProgress progress);

    // Connection Management
    Task SubscribeToUpdatesAsync(string userId, string connectionId);
    Task UnsubscribeFromUpdatesAsync(string connectionId);
    Task JoinGroupAsync(string connectionId, string groupName);
    Task LeaveGroupAsync(string connectionId, string groupName);

    // Broadcasting
    Task BroadcastToGroupAsync(string groupName, object message);
    Task BroadcastToUserAsync(string userId, object message);
    Task BroadcastToAllAsync(object message);
}

public class DocumentGenerationUpdate
{
    public Guid DocumentId { get; set; }
    public string Status { get; set; } = string.Empty; // "completed", "failed", "processing"
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
    public string? DownloadUrl { get; set; }
}

public class BulkOperationProgress
{
    public Guid OperationId { get; set; }
    public string OperationType { get; set; } = string.Empty; // "document_generation", "email_sending", "excel_processing"
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int SuccessfulItems { get; set; }
    public int FailedItems { get; set; }
    public string Status { get; set; } = string.Empty; // "running", "completed", "failed", "cancelled"
    public string? CurrentItem { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Calculated property for percentage completion
    public decimal PercentageComplete => TotalItems > 0 ? (decimal)ProcessedItems / TotalItems * 100 : 0;
}
