using System.ComponentModel.DataAnnotations;
using DocHub.API.Extensions;

namespace DocHub.API.DTOs;

public class SendGridWebhookEvent
{
    public string Email { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public int Attempt { get; set; }
    public string SgEventId { get; set; } = string.Empty;
    public string SgMessageId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Category { get; set; } = string.Empty;
    public string UniqueArgs { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
}
