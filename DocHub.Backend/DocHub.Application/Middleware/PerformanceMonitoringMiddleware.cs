using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DocHub.Application.Middleware;

public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;

    public PerformanceMonitoringMiddleware(RequestDelegate next, ILogger<PerformanceMonitoringMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value?.ToLowerInvariant();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;

            // Log slow requests (over 2 seconds)
            if (elapsed > 2000)
            {
                _logger.LogWarning("🐌 [PERFORMANCE] Slow request detected: {Path} took {ElapsedMs}ms", path, elapsed);
            }
            // Log very slow requests (over 5 seconds)
            else if (elapsed > 5000)
            {
                _logger.LogError("🚨 [PERFORMANCE] Very slow request detected: {Path} took {ElapsedMs}ms", path, elapsed);
            }
            // Log dashboard requests for monitoring
            else if (path?.Contains("/dashboard") == true)
            {
                _logger.LogInformation("📊 [PERFORMANCE] Dashboard request: {Path} took {ElapsedMs}ms", path, elapsed);
            }

            // Add performance headers only if response hasn't started
            if (!context.Response.HasStarted)
            {
                context.Response.Headers.Add("X-Response-Time", $"{elapsed}ms");
            }
        }
    }
}
