using DocHub.Core.Entities;
using DocHub.Core.Interfaces;
using DocHub.Core.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace DocHub.Application.Services;

public class DatabaseSeederService
{
    private readonly IDbContext _dbContext;
    private readonly IUserRepository _userRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<UserRole> _userRoleRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeederService> _logger;

    public DatabaseSeederService(
        IDbContext dbContext,
        IUserRepository userRepository,
        IRepository<Role> roleRepository,
        IRepository<UserRole> userRoleRepository,
        IConfiguration configuration,
        ILogger<DatabaseSeederService> logger)
    {
        _dbContext = dbContext;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting database seeding...");

            // Seed Roles
            await SeedRolesAsync();


            // Seed Admin User
            await SeedAdminUserAsync();

            // Note: Removed letter types seeding - users will create their own tabs

            _logger.LogInformation("Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during database seeding");
            throw;
        }
    }

    private async Task SeedRolesAsync()
    {
        try
        {
            var roles = new[]
            {
                new { Name = "Administrator", Description = "System Administrator with full access" },
                new { Name = "HR Manager", Description = "HR Manager with document management access" },
                new { Name = "Employee", Description = "Standard employee with limited access" },
                new { Name = "Supervisor", Description = "Supervisor with team management access" }
            };

            foreach (var roleData in roles)
            {
                var existingRole = (await _roleRepository.GetAllAsync())
                    .FirstOrDefault(r => r.Name == roleData.Name);

                if (existingRole == null)
                {
                    var role = new Role
                    {
                        Name = roleData.Name,
                        Description = roleData.Description,
                        IsActive = true
                    };

                    await _roleRepository.AddAsync(role);
                    _logger.LogInformation("Created role: {RoleName}", roleData.Name);
                }
            }

            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding roles");
            throw;
        }
    }


    private async Task SeedAdminUserAsync()
    {
        var adminConfig = _configuration.GetSection("AdminCredentials");
        var adminEmail = adminConfig["Email"] ?? "admin@collabera.com";
        var adminUsername = adminConfig["Username"] ?? "admin";
        var adminPassword = adminConfig["Password"] ?? "admin@123";
        var adminFirstName = adminConfig["FirstName"] ?? "System";
        var adminLastName = adminConfig["LastName"] ?? "Administrator";
        
        try
        {
            // Check if admin user already exists
            var existingAdmin = await _userRepository.GetByEmailAsync(adminEmail);
            if (existingAdmin != null)
            {
                _logger.LogInformation("Admin user already exists: {Email}", adminEmail);
                return;
            }

            // Also check by username
            existingAdmin = await _userRepository.GetByUsernameAsync(adminUsername);
            if (existingAdmin != null)
            {
                _logger.LogInformation("Admin user already exists with username: {Username}", adminUsername);
                return;
            }

            // Create admin user
            _logger.LogInformation("Creating admin user with email: {Email}, username: {Username}", adminEmail, adminUsername);
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
            _logger.LogInformation("Password hashed successfully");
            
            var adminUser = new User
            {
                Username = adminUsername,
                Email = adminEmail,
                PasswordHash = passwordHash,
                FirstName = adminFirstName,
                LastName = adminLastName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Adding admin user to repository...");
            await _userRepository.AddAsync(adminUser);
            _logger.LogInformation("Saving admin user to database...");
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Admin user saved successfully with ID: {UserId}", adminUser.Id);

            // Assign Administrator role
            var adminRole = (await _roleRepository.GetAllAsync())
                .FirstOrDefault(r => r.Name == "Administrator");

            if (adminRole != null)
            {
                var userRole = new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id,
                    AssignedAt = DateTime.UtcNow
                };

                await _userRoleRepository.AddAsync(userRole);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Created admin user: {Email} with Administrator role", adminEmail);
            }
            else
            {
                _logger.LogWarning("Administrator role not found, user created without role assignment");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating admin user: {Email}", adminEmail ?? "unknown");
            throw;
        }
    }

    public async Task<bool> IsDatabaseSeededAsync()
    {
        try
        {
            // Check if any users exist
            var users = await _userRepository.GetAllAsync();
            var userCount = users.Count();
            _logger.LogInformation("Database seeding check: Found {UserCount} users in database", userCount);
            
            // Check if any roles exist
            var roles = await _roleRepository.GetAllAsync();
            var roleCount = roles.Count();
            _logger.LogInformation("Database seeding check: Found {RoleCount} roles in database", roleCount);
            
            // Check if any modules exist
            
            if (userCount > 0)
            {
                var userEmails = users.Select(u => u.Email).ToList();
                _logger.LogInformation("Existing users: {UserEmails}", string.Join(", ", userEmails));
            }
            
            // Database is considered seeded if admin data exists (users, roles)
            var isSeeded = userCount > 0 && roleCount > 0;
            _logger.LogInformation("Database seeding status: Users={UserCount}, Roles={RoleCount}, IsSeeded={IsSeeded}", 
                userCount, roleCount, isSeeded);
            
            return isSeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if database is seeded, assuming it needs seeding");
            return false;
        }
    }


}
