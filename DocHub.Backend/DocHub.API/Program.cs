using DocHub.Core.Interfaces;
using DocHub.Core.Interfaces.Repositories;
using DocHub.Infrastructure.Data;
using DocHub.Infrastructure.Repositories;
using DocHub.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DocHub.Application.Hubs;
using DocHub.Application.Middleware;
using Syncfusion.Licensing;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add memory cache
builder.Services.AddMemoryCache();

// Database
builder.Services.AddDbContext<DocHubDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), 
        sqlOptions => 
        {
            sqlOptions.CommandTimeout(60);
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
        }));

// Repositories
builder.Services.AddScoped<IDbContext>(provider => provider.GetRequiredService<DocHubDbContext>());
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<TableSchemaRepository>();

// Services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IDocumentGenerationService, DocumentGenerationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFileManagementService, FileManagementService>();
builder.Services.AddScoped<ITabManagementService, TabManagementService>();
builder.Services.AddScoped<IExcelProcessingService, ExcelProcessingService>();
builder.Services.AddScoped<ISignatureProcessingService, SignatureProcessingService>();
builder.Services.AddScoped<IRealTimeService, SignalRService>();
builder.Services.AddScoped<IDynamicTableService, DynamicTableService>();
builder.Services.AddScoped<IDynamicLetterGenerationService, DynamicLetterGenerationService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<ISignatureCleanupService, SignatureCleanupService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<ISessionManagementService, SessionManagementService>();
builder.Services.AddScoped<IPasswordPolicyService, PasswordPolicyService>();
builder.Services.AddScoped<IRoleManagementService, RoleManagementService>();
builder.Services.AddScoped<IDepartmentAccessService, DepartmentAccessService>();

// Background Services
builder.Services.AddHostedService<EmailStatusPollingService>();
Console.WriteLine("🔧 [STARTUP] EmailStatusPollingService registered as background service");

// Database Initialization (Auto-migration and admin user creation)

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT Secret not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// SignalR
builder.Services.AddSignalR();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Logging
builder.Services.AddLogging();

var app = builder.Build();
Console.WriteLine("🚀 [STARTUP] Application built successfully");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    Console.WriteLine("🔧 [STARTUP] Development environment detected, enabling Swagger");
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Custom middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");

// Simple database initialization
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var configuration = app.Services.GetRequiredService<IConfiguration>();

logger.LogInformation("Starting database initialization...");

try
{
    // Register Syncfusion license
    var syncfusionLicense = configuration["SyncfusionLicense"];
    if (!string.IsNullOrEmpty(syncfusionLicense))
    {
        SyncfusionLicenseProvider.RegisterLicense(syncfusionLicense);
        logger.LogInformation("Syncfusion license registered successfully");
    }
    
    // Apply migrations and create admin user
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<DocHubDbContext>();
        
        // Apply migrations
        logger.LogInformation("🔄 [STARTUP] Applying database migrations...");
        await context.Database.MigrateAsync();
        logger.LogInformation("✅ [STARTUP] Database migrations completed.");
        
        // Create system roles if they don't exist
        logger.LogInformation("🔍 [STARTUP] Checking system roles...");
        var systemRoles = new[]
        {
            new DocHub.Core.Entities.Role { Name = "SuperAdmin", Description = "Full system access", IsSystemRole = true, IsActive = true },
            new DocHub.Core.Entities.Role { Name = "Admin", Description = "Administrative access", IsSystemRole = true, IsActive = true },
            new DocHub.Core.Entities.Role { Name = "Manager", Description = "Management access", IsSystemRole = true, IsActive = true },
            new DocHub.Core.Entities.Role { Name = "User", Description = "Standard user access", IsSystemRole = true, IsActive = true }
        };

        foreach (var role in systemRoles)
        {
            if (!await context.Roles.AnyAsync(r => r.Name == role.Name))
            {
                role.CreatedAt = DateTime.UtcNow;
                role.UpdatedAt = DateTime.UtcNow;
                context.Roles.Add(role);
                logger.LogInformation("✅ [STARTUP] Created role: {RoleName}", role.Name);
            }
        }
        await context.SaveChangesAsync();

        // Create admin user if it doesn't exist
        logger.LogInformation("🔍 [STARTUP] Checking if admin user exists...");
        var adminExists = await context.Users.AnyAsync(u => u.Email == "admin@collabera.com");
        logger.LogInformation("👤 [STARTUP] Admin user exists: {Exists}", adminExists);
        
        if (!adminExists)
        {
            logger.LogInformation("🔄 [STARTUP] Creating admin user...");
            var adminUser = new DocHub.Core.Entities.User
            {
                Username = "admin",
                Email = "admin@collabera.com",
                FirstName = "Admin",
                LastName = "User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                IsActive = true,
                Department = "ER", // Set department for admin
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
            logger.LogInformation("✅ [STARTUP] Admin user created successfully: admin@collabera.com / admin123");
            
            // Assign Admin role to admin user
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            if (adminRole != null)
            {
                var userRole = new DocHub.Core.Entities.UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id,
                    AssignedAt = DateTime.UtcNow
                };
                context.UserRoles.Add(userRole);
                await context.SaveChangesAsync();
                logger.LogInformation("✅ [STARTUP] Assigned Admin role to admin user");
            }
        }
        else
        {
            logger.LogInformation("ℹ️ [STARTUP] Admin user already exists");
            
            // Check if admin user has Admin role, if not assign it
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@collabera.com");
            if (adminUser != null)
            {
                var hasAdminRole = await context.UserRoles
                    .Include(ur => ur.Role)
                    .AnyAsync(ur => ur.UserId == adminUser.Id && ur.Role.Name == "Admin");
                
                if (!hasAdminRole)
                {
                    var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                    if (adminRole != null)
                    {
                        var userRole = new DocHub.Core.Entities.UserRole
                        {
                            UserId = adminUser.Id,
                            RoleId = adminRole.Id,
                            AssignedAt = DateTime.UtcNow
                        };
                        context.UserRoles.Add(userRole);
                        await context.SaveChangesAsync();
                        logger.LogInformation("✅ [STARTUP] Assigned Admin role to existing admin user");
                    }
                }
                
                // Update admin user department if it's empty
                if (string.IsNullOrEmpty(adminUser.Department))
                {
                    adminUser.Department = "ER";
                    await context.SaveChangesAsync();
                    logger.LogInformation("✅ [STARTUP] Updated admin user department to ER");
                }
            }
        }
    }
    
    logger.LogInformation("🎉 [STARTUP] Database initialization completed successfully!");
}
catch (Exception ex)
{
    logger.LogError(ex, "Error during database initialization");
    throw;
}

Console.WriteLine("🎯 [STARTUP] Starting application server...");
app.Run();