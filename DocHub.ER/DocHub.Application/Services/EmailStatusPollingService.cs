using DocHub.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace DocHub.Application.Services;

public class EmailStatusPollingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailStatusPollingService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _pollingInterval;

    public EmailStatusPollingService(
        IServiceProvider serviceProvider,
        ILogger<EmailStatusPollingService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        
        // Get polling interval from configuration (default: 10 seconds)
        var intervalSeconds = _configuration.GetSection("EmailPolling:IntervalSeconds").Value;
        if (string.IsNullOrEmpty(intervalSeconds) || !int.TryParse(intervalSeconds, out var parsedSeconds))
        {
            parsedSeconds = 10; // Default to 10 seconds
        }
        _pollingInterval = TimeSpan.FromSeconds(parsedSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 [EMAIL_POLLING] Service starting. Polling every {Interval} seconds", 
            _pollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("🔄 [EMAIL_POLLING] Starting polling cycle at {Timestamp}", DateTime.UtcNow);
                await PollEmailStatusesAsync();
                _logger.LogDebug("✅ [EMAIL_POLLING] Polling cycle completed at {Timestamp}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [EMAIL_POLLING] Error occurred while polling email statuses at {Timestamp}", DateTime.UtcNow);
            }

            _logger.LogDebug("⏰ [EMAIL_POLLING] Waiting {Interval} seconds until next poll", _pollingInterval.TotalSeconds);
            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("🛑 [EMAIL_POLLING] Service stopped at {Timestamp}", DateTime.UtcNow);
    }

    private async Task PollEmailStatusesAsync()
    {
        _logger.LogDebug("🔍 [EMAIL_POLLING] Creating service scope for polling");
        using var scope = _serviceProvider.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        try
        {
            _logger.LogDebug("📧 [EMAIL_POLLING] Calling EmailService.PollEmailStatusesAsync");
            await emailService.PollEmailStatusesAsync();
            _logger.LogDebug("✅ [EMAIL_POLLING] EmailService.PollEmailStatusesAsync completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [EMAIL_POLLING] Error in email status polling: {Message}", ex.Message);
        }
    }
}
