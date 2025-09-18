using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace DocHub.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DocHubDbContext>
{
    public DocHubDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection not configured for design-time DbContext creation.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<DocHubDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.CommandTimeout(60);
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
            sqlOptions.MigrationsAssembly(typeof(DocHubDbContext).GetTypeInfo().Assembly.GetName().Name);
        });

        return new DocHubDbContext(optionsBuilder.Options);
    }

    private static IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder();

        // Try to load settings from the startup API project if available
        var currentDir = Directory.GetCurrentDirectory();
        var candidatePaths = new[]
        {
            Path.Combine(currentDir, "DocHub.API"),
            Path.Combine(currentDir, "..", "DocHub.API"),
            Path.Combine(currentDir, "../DocHub.API"),
        };

        string? basePath = null;
        foreach (var p in candidatePaths)
        {
            if (Directory.Exists(p) && File.Exists(Path.Combine(p, "appsettings.json")))
            {
                basePath = p;
                break;
            }
        }

        if (basePath == null)
        {
            basePath = currentDir;
        }

        builder.SetBasePath(basePath)
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
               .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false);

        return builder.Build();
    }
}


