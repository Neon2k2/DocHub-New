using System.ComponentModel.DataAnnotations;
using DocHub.API.Extensions;

namespace DocHub.API.DTOs;

public class BulkEmailRequest
{
    [Required]
    public List<Guid> EmployeeIds { get; set; } = new();

    [Required]
    public Guid DocumentId { get; set; }

    public Guid? EmailTemplateId { get; set; }

    [Required]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public List<EmailAttachment>? Attachments { get; set; }

    [Required]
    public string SentBy { get; set; } = string.Empty;
}

public class SingleEmailRequest
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    public Guid DocumentId { get; set; }

    public Guid? EmailTemplateId { get; set; }

    [Required]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public List<EmailAttachment>? Attachments { get; set; }

    [Required]
    public string SentBy { get; set; } = string.Empty;
}

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}

public class BulkEmailResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid JobId { get; set; }
    public int TotalEmails { get; set; }
    public int SentEmails { get; set; }
    public int FailedEmails { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class EmailResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid JobId { get; set; }
}

public class EmailJobStatus
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string TrackingId { get; set; } = string.Empty;
}

public class EmailJobSummary
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
}

public class EmailTemplateSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateEmailTemplateRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [Required]
    public string CreatedBy { get; set; } = string.Empty;
}

public class UpdateEmailTemplateRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

