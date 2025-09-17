namespace DocHub.Shared.DTOs.Emails;

public class EmailStatsDto
{
    public int TotalEmails { get; set; }
    public int DeliveredEmails { get; set; }
    public int PendingEmails { get; set; }
    public int FailedEmails { get; set; }
    public int BouncedEmails { get; set; }
    public int OpenedEmails { get; set; }
    public double SuccessRate { get; set; }
    public double AverageDeliveryTime { get; set; }
    public int EmailsToday { get; set; }
    public int EmailsThisWeek { get; set; }
    public int EmailsThisMonth { get; set; }
    public Dictionary<string, int> StatusDistribution { get; set; } = new();
    public Dictionary<string, int> HourlyDistribution { get; set; } = new();
    public Dictionary<string, int> DailyDistribution { get; set; } = new();
}
