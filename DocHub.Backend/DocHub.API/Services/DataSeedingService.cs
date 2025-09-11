using DocHub.API.Data;
using DocHub.API.Models;
using DocHub.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DocHub.API.Services;

public class DataSeedingService : IDataSeedingService
{
    private readonly DocHubDbContext _context;
    private readonly ILogger<DataSeedingService> _logger;

    public DataSeedingService(DocHubDbContext context, ILogger<DataSeedingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting data seeding...");

            // Seed Roles (only Admin role needed)
            await SeedRolesAsync();
            
            // Seed Admin User
            await SeedAdminUserAsync();
            
            // Clear existing tab employee data to start fresh
            await ClearTabEmployeeDataAsync();

            _logger.LogInformation("Data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data seeding");
            throw;
        }
    }

    private async Task SeedRolesAsync()
    {
        // Only seed the Admin role
        if (!await _context.Roles.AnyAsync(r => r.Name == "Admin"))
        {
            var adminRole = new Role { Name = "Admin", Description = "System Administrator", Status = "Active" };
            _context.Roles.Add(adminRole);
            await _context.SaveChangesAsync();
        }
    }

    private async Task SeedAdminUserAsync()
    {
        if (await _context.Users.AnyAsync(u => u.Email == "admin@collabera.com"))
            return;

        var adminUser = new User
        {
            Username = "admin",
            Email = "admin@collabera.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin@123"),
            FirstName = "System",
            LastName = "Administrator",
            Status = "Active",
            IsEmailVerified = true
        };

        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync();

        // Assign Admin role
        var adminRole = await _context.Roles.FirstAsync(r => r.Name == "Admin");
        var userRole = new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id,
            ModuleAccess = "Admin" // Admin has access to all modules
        };

        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin user created: admin@collabera.com");
    }

    private async Task ClearTabEmployeeDataAsync()
    {
        try
        {
            var existingData = await _context.TabEmployeeData.ToListAsync();
            if (existingData.Any())
            {
                _context.TabEmployeeData.RemoveRange(existingData);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cleared {Count} existing tab employee records", existingData.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing tab employee data");
            // Don't throw - this is not critical
        }
    }

}
