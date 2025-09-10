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

            // Seed Roles
            await SeedRolesAsync();
            
            // Seed Admin User
            await SeedAdminUserAsync();
            
            // Seed Modules
            await SeedModulesAsync();
            
            // Seed Letter Types
            await SeedLetterTypesAsync();

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
        var roles = new[]
        {
            new Role { Name = "Admin", Description = "System Administrator", Status = "Active" },
            new Role { Name = "User", Description = "Regular User", Status = "Active" },
            new Role { Name = "ER_Manager", Description = "ER Module Manager", Status = "Active" },
            new Role { Name = "Billing_Manager", Description = "Billing Module Manager", Status = "Active" },
            new Role { Name = "Timesheet_Manager", Description = "Timesheet Module Manager", Status = "Active" }
        };

        foreach (var role in roles)
        {
            if (!await _context.Roles.AnyAsync(r => r.Name == role.Name))
            {
                _context.Roles.Add(role);
            }
        }

        await _context.SaveChangesAsync();
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

    private async Task SeedModulesAsync()
    {
        var modules = new[]
        {
            new Module { Name = "ER", DisplayName = "Employee Relations", Description = "Employee Relations Module", IsActive = true },
            new Module { Name = "Billing", DisplayName = "Billing", Description = "Billing Module", IsActive = true },
            new Module { Name = "Timesheet", DisplayName = "Timesheet", Description = "Timesheet Module", IsActive = true }
        };

        foreach (var module in modules)
        {
            if (!await _context.Modules.AnyAsync(m => m.Name == module.Name))
            {
                _context.Modules.Add(module);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedLetterTypesAsync()
    {
        var erModule = await _context.Modules.FirstAsync(m => m.Name == "ER");
        
        var letterTypes = new[]
        {
            new LetterTypeDefinition
            {
                ModuleId = erModule.Id,
                TypeKey = "ER_Offer_Letter",
                DisplayName = "Offer Letter",
                Description = "Employee offer letter template",
                DataSourceType = "Database",
                FieldConfiguration = "{}",
                IsActive = true
            },
            new LetterTypeDefinition
            {
                ModuleId = erModule.Id,
                TypeKey = "ER_Appointment_Letter",
                DisplayName = "Appointment Letter",
                Description = "Employee appointment letter template",
                DataSourceType = "Database",
                FieldConfiguration = "{}",
                IsActive = true
            },
            new LetterTypeDefinition
            {
                ModuleId = erModule.Id,
                TypeKey = "ER_Experience_Certificate",
                DisplayName = "Experience Certificate",
                Description = "Employee experience certificate template",
                DataSourceType = "Database",
                FieldConfiguration = "{}",
                IsActive = true
            }
        };

        foreach (var letterType in letterTypes)
        {
            if (!await _context.LetterTypeDefinitions.AnyAsync(lt => lt.TypeKey == letterType.TypeKey))
            {
                _context.LetterTypeDefinitions.Add(letterType);
            }
        }

        await _context.SaveChangesAsync();
    }
}
