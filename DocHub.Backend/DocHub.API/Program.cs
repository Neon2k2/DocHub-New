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
builder.Services.AddSingleton<ICacheService, CacheService>();

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    // Core permissions
    options.AddPolicy("ViewDashboard", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("Admin") || 
            context.User.IsInRole("User")));
    
    options.AddPolicy("ViewAnalytics", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("Admin") || 
            context.User.IsInRole("User")));
    
    // Document Management permissions
    options.AddPolicy("ViewDocuments", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("Admin") || 
            context.User.IsInRole("User")));
    
    options.AddPolicy("CreateDocuments", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("Admin") || 
            context.User.IsInRole("User")));
    
    options.AddPolicy("EditDocuments", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("Admin") || 
            context.User.IsInRole("User")));
    
    options.AddPolicy("DeleteDocuments", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("Admin") || 
            context.User.IsInRole("User")));
    
    // File Management permissions
    options.AddPolicy("ViewFiles", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("Admin") || 
            context.User.IsInRole("User")));
    
    options.AddPolicy("UploadFiles", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("Admin") || 
            context.User.IsInRole("User")));
    
    options.AddPolicy("DownloadFiles", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("Admin") || 
            context.User.IsInRole("User")));
    
    options.AddPolicy("DeleteFiles", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("Admin") || 
            context.User.IsInRole("User")));
    
    // Email Management permissions
    options.AddPolicy("SendEmails", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("Admin") || 
            context.User.IsInRole("User")));
    
    options.AddPolicy("SendBulkEmails", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("Admin") || 
            context.User.IsInRole("User")));
    
    options.AddPolicy("ViewEmailHistory", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("Admin") || 
            context.User.IsInRole("User")));
    
    // Excel Processing permissions
    options.AddPolicy("UploadExcel", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("Admin") || 
            context.User.IsInRole("User")));
    
    options.AddPolicy("ProcessExcel", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("Admin") || 
            context.User.IsInRole("User")));
    
    // Signature Management permissions
    options.AddPolicy("UploadSignatures", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("Admin") || 
            context.User.IsInRole("User")));
    
    options.AddPolicy("ProcessSignatures", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("Admin") || 
            context.User.IsInRole("User")));
    
    // Tab Management permissions (Admin only)
    options.AddPolicy("ViewTabs", policy => 
        policy.RequireAssertion(context => 
            context.User.IsInRole("Admin") || 
            context.User.IsInRole("User")));
    
    options.AddPolicy("CreateTabs", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("EditTabs", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("DeleteTabs", policy => 
        policy.RequireRole("Admin"));
    
    // User Management permissions (Admin only)
    options.AddPolicy("ViewUsers", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("CreateUsers", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("EditUsers", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("DeleteUsers", policy => 
        policy.RequireRole("Admin"));
    
    // Role Management permissions (Admin only)
    options.AddPolicy("ViewRoles", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("CreateRoles", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("EditRoles", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("DeleteRoles", policy => 
        policy.RequireRole("Admin"));
    
    // Session Management permissions (Admin only)
    options.AddPolicy("ViewSessions", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("ManageSessions", policy => 
        policy.RequireRole("Admin"));
    
    // System Administration permissions (Admin only)
    options.AddPolicy("ManageSystemSettings", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("ViewAuditLogs", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("SystemMonitoring", policy => 
        policy.RequireRole("Admin"));
});

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
var secretKey = jwtSettings["Secret"] ?? jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT Secret not configured");

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

// HttpContext accessor (needed for session/user context in services)
builder.Services.AddHttpContextAccessor();

// SignalR
builder.Services.AddSignalR();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:5173", "http://127.0.0.1:3000", "http://127.0.0.1:3001", "http://127.0.0.1:5173")
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
app.UseMiddleware<PerformanceMonitoringMiddleware>();

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
        
        // Create system permissions if they don't exist
        logger.LogInformation("🔍 [STARTUP] Checking system permissions...");
        var systemPermissions = new[]
        {
            // Core Application Permissions
            new DocHub.Core.Entities.Permission { Name = "ViewDashboard", Description = "View dashboard", Category = "Core", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "ViewAnalytics", Description = "View analytics and reports", Category = "Core", IsActive = true },
            
            // Document Management Permissions
            new DocHub.Core.Entities.Permission { Name = "ViewDocuments", Description = "View documents", Category = "DocumentManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "CreateDocuments", Description = "Create new documents", Category = "DocumentManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "EditDocuments", Description = "Edit existing documents", Category = "DocumentManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "DeleteDocuments", Description = "Delete documents", Category = "DocumentManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "DownloadDocuments", Description = "Download documents", Category = "DocumentManagement", IsActive = true },
            
            // File Management Permissions
            new DocHub.Core.Entities.Permission { Name = "ViewFiles", Description = "View file list", Category = "FileManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "UploadFiles", Description = "Upload files", Category = "FileManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "DownloadFiles", Description = "Download files", Category = "FileManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "DeleteFiles", Description = "Delete files", Category = "FileManagement", IsActive = true },
            
            // Email Management Permissions
            new DocHub.Core.Entities.Permission { Name = "SendEmails", Description = "Send individual emails", Category = "EmailManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "SendBulkEmails", Description = "Send bulk emails", Category = "EmailManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "ViewEmailHistory", Description = "View email history", Category = "EmailManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "ViewEmailStatus", Description = "View email status", Category = "EmailManagement", IsActive = true },
            
            // Excel Processing Permissions
            new DocHub.Core.Entities.Permission { Name = "UploadExcel", Description = "Upload Excel files", Category = "ExcelProcessing", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "ProcessExcel", Description = "Process Excel data", Category = "ExcelProcessing", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "DownloadExcel", Description = "Download processed Excel", Category = "ExcelProcessing", IsActive = true },
            
            // Signature Management Permissions
            new DocHub.Core.Entities.Permission { Name = "UploadSignatures", Description = "Upload signature images", Category = "SignatureManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "ProcessSignatures", Description = "Process signature data", Category = "SignatureManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "InsertSignatures", Description = "Insert signatures into documents", Category = "SignatureManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "ViewSignatures", Description = "View signature list", Category = "SignatureManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "DeleteSignatures", Description = "Delete signatures", Category = "SignatureManagement", IsActive = true },
            
            // Tab Management Permissions (Dynamic Letters)
            new DocHub.Core.Entities.Permission { Name = "ViewTabs", Description = "View available tabs", Category = "TabManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "CreateTabs", Description = "Create new tabs", Category = "TabManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "EditTabs", Description = "Edit existing tabs", Category = "TabManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "DeleteTabs", Description = "Delete tabs", Category = "TabManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "ConfigureTabs", Description = "Configure tab settings", Category = "TabManagement", IsActive = true },
            
            // User Management Permissions (Admin Only)
            new DocHub.Core.Entities.Permission { Name = "ViewUsers", Description = "View user list", Category = "UserManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "CreateUsers", Description = "Create new users", Category = "UserManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "EditUsers", Description = "Edit existing users", Category = "UserManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "DeleteUsers", Description = "Delete users", Category = "UserManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "ManageUserRoles", Description = "Assign/remove user roles", Category = "UserManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "ResetUserPasswords", Description = "Reset user passwords", Category = "UserManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "ToggleUserStatus", Description = "Activate/deactivate users", Category = "UserManagement", IsActive = true },
            
            // Role Management Permissions (Admin Only)
            new DocHub.Core.Entities.Permission { Name = "ViewRoles", Description = "View role list", Category = "RoleManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "CreateRoles", Description = "Create new roles", Category = "RoleManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "EditRoles", Description = "Edit existing roles", Category = "RoleManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "DeleteRoles", Description = "Delete roles", Category = "RoleManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "ManageRolePermissions", Description = "Assign/remove role permissions", Category = "RoleManagement", IsActive = true },
            
            // Session Management Permissions (Admin Only)
            new DocHub.Core.Entities.Permission { Name = "ViewSessions", Description = "View active sessions", Category = "SessionManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "ManageSessions", Description = "Manage user sessions", Category = "SessionManagement", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "TerminateSessions", Description = "Terminate user sessions", Category = "SessionManagement", IsActive = true },
            
            // System Administration Permissions (Admin Only)
            new DocHub.Core.Entities.Permission { Name = "ManageSystemSettings", Description = "Manage system settings", Category = "SystemAdministration", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "ViewAuditLogs", Description = "View audit logs", Category = "SystemAdministration", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "ManageEmailTemplates", Description = "Manage email templates", Category = "SystemAdministration", IsActive = true },
            new DocHub.Core.Entities.Permission { Name = "SystemMonitoring", Description = "Monitor system health", Category = "SystemAdministration", IsActive = true }
        };

        foreach (var permission in systemPermissions)
        {
            if (!await context.Permissions.AnyAsync(p => p.Name == permission.Name))
            {
                permission.CreatedAt = DateTime.UtcNow;
                permission.UpdatedAt = DateTime.UtcNow;
                context.Permissions.Add(permission);
                logger.LogInformation("✅ [STARTUP] Created permission: {PermissionName}", permission.Name);
            }
        }
        await context.SaveChangesAsync();

        // Create system roles if they don't exist
        logger.LogInformation("🔍 [STARTUP] Checking system roles...");
        var systemRoles = new[]
        {
            new DocHub.Core.Entities.Role { Name = "Admin", Description = "Full system access - can manage everything", IsSystemRole = true, IsActive = true },
            new DocHub.Core.Entities.Role { Name = "User", Description = "Standard user access - department-based permissions", IsSystemRole = true, IsActive = true }
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

        // Assign permissions to roles
        logger.LogInformation("🔍 [STARTUP] Assigning permissions to roles...");
        await AssignPermissionsToRolesAsync(context, logger);

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
                Department = string.Empty, // No department restriction for admin
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
                
                // Ensure admin user has no department restriction
                if (!string.IsNullOrEmpty(adminUser.Department))
                {
                    adminUser.Department = string.Empty;
                    await context.SaveChangesAsync();
                    logger.LogInformation("✅ [STARTUP] Cleared admin user department to allow full access");
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

// Helper method to assign permissions to roles
static async Task AssignPermissionsToRolesAsync(DocHubDbContext context, ILogger logger)
{
    try
    {
        // Get all roles and permissions
        var roles = await context.Roles.ToListAsync();
        var permissions = await context.Permissions.ToListAsync();
        
        // Define role-permission mappings
        var rolePermissionMappings = new Dictionary<string, string[]>
        {
            ["Admin"] = permissions.Select(p => p.Name).ToArray(), // All permissions
            ["User"] = new[]
            {
                // Core Application
                "ViewDashboard", "ViewAnalytics",
                
                // Document Management
                "ViewDocuments", "CreateDocuments", "EditDocuments", "DeleteDocuments", "DownloadDocuments",
                
                // File Management
                "ViewFiles", "UploadFiles", "DownloadFiles", "DeleteFiles",
                
                // Email Management
                "SendEmails", "SendBulkEmails", "ViewEmailHistory", "ViewEmailStatus",
                
                // Excel Processing
                "UploadExcel", "ProcessExcel", "DownloadExcel",
                
                // Signature Management
                "UploadSignatures", "ProcessSignatures", "InsertSignatures", "ViewSignatures", "DeleteSignatures",
                
                // Tab Management (View only - can use existing tabs)
                "ViewTabs"
            }
        };

        foreach (var roleMapping in rolePermissionMappings)
        {
            var role = roles.FirstOrDefault(r => r.Name == roleMapping.Key);
            if (role == null) continue;

            foreach (var permissionName in roleMapping.Value)
            {
                var permission = permissions.FirstOrDefault(p => p.Name == permissionName);
                if (permission == null) continue;

                // Check if role-permission relationship already exists
                var existingRolePermission = await context.RolePermissions
                    .FirstOrDefaultAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id);

                if (existingRolePermission == null)
                {
                    var rolePermission = new DocHub.Core.Entities.RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permission.Id,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.RolePermissions.Add(rolePermission);
                    logger.LogInformation("✅ [STARTUP] Assigned permission '{PermissionName}' to role '{RoleName}'", 
                        permissionName, role.Name);
                }
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation("✅ [STARTUP] All role-permission assignments completed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ [STARTUP] Error assigning permissions to roles");
        throw;
    }
}