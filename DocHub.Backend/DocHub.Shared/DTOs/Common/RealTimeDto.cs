using System.Text.Json.Serialization;

namespace DocHub.Shared.DTOs.Common;

public class SystemAlert
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "error", "warning", "info", "success"

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("requiresAction")]
    public bool RequiresAction { get; set; }

    [JsonPropertyName("actionUrl")]
    public string? ActionUrl { get; set; }
}

public class ExcelProcessingUpdate
{
    [JsonPropertyName("uploadId")]
    public string UploadId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("processedRows")]
    public int ProcessedRows { get; set; }

    [JsonPropertyName("totalRows")]
    public int TotalRows { get; set; }

    [JsonPropertyName("completedAt")]
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new List<string>();
}

public class TabDataUpdate
{
    [JsonPropertyName("tabId")]
    public string TabId { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty; // "add", "update", "delete", "bulk_import"

    [JsonPropertyName("recordCount")]
    public int RecordCount { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }
}
