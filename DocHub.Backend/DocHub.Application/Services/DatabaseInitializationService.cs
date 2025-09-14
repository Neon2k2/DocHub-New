using DocHub.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Syncfusion.Licensing;
using Microsoft.Extensions.Configuration;

namespace DocHub.Application.Services;

public class DatabaseInitializationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializationService> _logger;
    private readonly IConfiguration _configuration;

    public DatabaseInitializationService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseInitializationService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting database initialization...");

            // Register Syncfusion license
            RegisterSyncfusionLicense();

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>() as DbContext;
            var seederService = scope.ServiceProvider.GetRequiredService<DatabaseSeederService>();

            // Apply pending migrations
            await ApplyMigrationsAsync(dbContext);

            // Seed database if needed
            await SeedDatabaseAsync(seederService);

            _logger.LogInformation("Database initialization completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during database initialization");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void RegisterSyncfusionLicense()
    {
        try
        {
            var syncfusionLicense = _configuration["SyncfusionLicense"];
            if (!string.IsNullOrEmpty(syncfusionLicense))
            {
                SyncfusionLicenseProvider.RegisterLicense(syncfusionLicense);
                _logger.LogInformation("Syncfusion license registered successfully");
            }
            else
            {
                _logger.LogWarning("Syncfusion license not found in configuration");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering Syncfusion license");
        }
    }

    private async Task ApplyMigrationsAsync(DbContext? dbContext)
    {
        try
        {
            if (dbContext == null)
            {
                _logger.LogError("DbContext is null, cannot apply migrations");
                return;
            }

            _logger.LogInformation("Checking for pending migrations...");

            try
            {
                var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    _logger.LogInformation("Found {Count} pending migrations. Applying...", pendingMigrations.Count());
                    
                    foreach (var migration in pendingMigrations)
                    {
                        _logger.LogInformation("Pending migration: {Migration}", migration);
                    }

                    await dbContext.Database.MigrateAsync();
                    _logger.LogInformation("All migrations applied successfully!");
                }
                else
                {
                    _logger.LogInformation("Database is up to date, no migrations to apply");
                }
            }
            catch (Exception migrationEx)
            {
                _logger.LogWarning(migrationEx, "Migration failed, attempting to ensure database creation...");
                
                // Fallback: Ensure database and tables are created
                await dbContext.Database.EnsureCreatedAsync();
                _logger.LogInformation("Database ensured using EnsureCreated as fallback");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying database migrations");
            throw;
        }
    }

    private async Task SeedDatabaseAsync(DatabaseSeederService seederService)
    {
        try
        {
            _logger.LogInformation("Checking if database seeding is required...");

            var isSeeded = await seederService.IsDatabaseSeededAsync();
            if (!isSeeded)
            {
                _logger.LogInformation("Database is empty, starting seeding process...");
                await seederService.SeedAsync();
                _logger.LogInformation("Database seeding completed successfully");
            }
            else
            {
                _logger.LogInformation("Database is already seeded, skipping seeding process");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database seeding");
            throw;
        }
    }
}
