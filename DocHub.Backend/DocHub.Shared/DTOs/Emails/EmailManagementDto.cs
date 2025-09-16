using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DocHub.Shared.DTOs.Emails;

public class SendEmailRequest
{
    [Required]
    public Guid LetterTypeDefinitionId { get; set; }

    public Guid? ExcelUploadId { get; set; }

    public Guid? DocumentId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string ToEmail { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ToName { get; set; }

    public string? Attachments { get; set; } // JSON
    public string? TrackingOptions { get; set; } // JSON
}

public class SendBulkEmailsRequest
{
    [Required]
    public Guid LetterTypeDefinitionId { get; set; }

    [Required]
    public List<Guid> ExcelUploadIds { get; set; } = new List<Guid>();


    [Required]
    [MaxLength(255)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public string? Attachments { get; set; } // JSON
    public string? TrackingOptions { get; set; } // JSON
}


public class GetEmailJobsRequest
{
    public Guid? LetterTypeDefinitionId { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class EmailJobDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("letterTypeDefinitionId")]
    public Guid LetterTypeDefinitionId { get; set; }

    [JsonPropertyName("letterTypeName")]
    public string LetterTypeName { get; set; } = string.Empty;

    [JsonPropertyName("tabDataRecordId")]
    public Guid? ExcelUploadId { get; set; }

    [JsonPropertyName("documentId")]
    public Guid? DocumentId { get; set; }

    [JsonPropertyName("documentName")]
    public string? DocumentName { get; set; }


    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("attachments")]
    public string? Attachments { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("sentBy")]
    public Guid SentBy { get; set; }

    [JsonPropertyName("sentByName")]
    public string SentByName { get; set; } = string.Empty;

    // Employee information extracted from TabDataRecord
    [JsonPropertyName("employeeId")]
    public string EmployeeId { get; set; } = string.Empty;

    [JsonPropertyName("employeeName")]
    public string EmployeeName { get; set; } = string.Empty;

    [JsonPropertyName("employeeEmail")]
    public string EmployeeEmail { get; set; } = string.Empty;

    [JsonPropertyName("recipientEmail")]
    public string? RecipientEmail { get; set; }

    [JsonPropertyName("recipientName")]
    public string? RecipientName { get; set; }

    [JsonPropertyName("processedAt")]
    public DateTime? ProcessedAt { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedAt { get; set; }
    public DateTime? BouncedAt { get; set; }
    public DateTime? DroppedAt { get; set; }
    public DateTime? SpamReportedAt { get; set; }
    public DateTime? UnsubscribedAt { get; set; }
    public string? BounceReason { get; set; }
    public string? DropReason { get; set; }
    public string? TrackingId { get; set; }
    public string? SendGridMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime? LastRetryAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}


public class EmailStatusUpdate
{
    public Guid EmailJobId { get; set; }
    public Guid LetterTypeDefinitionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Reason { get; set; }
    public string? SendGridMessageId { get; set; }
    public string? EmployeeName { get; set; }
    public string? EmployeeEmail { get; set; }
}

public class ValidateEmailRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

