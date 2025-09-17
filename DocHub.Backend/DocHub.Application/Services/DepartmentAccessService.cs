using DocHub.Core.Entities;
using DocHub.Core.Interfaces;
using DocHub.Core.Interfaces.Repositories;
using DocHub.Shared.DTOs.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocHub.Application.Services;

public class DepartmentAccessService : IDepartmentAccessService
{
    private readonly IDbContext _dbContext;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<LetterTypeDefinition> _letterTypeRepository;
    private readonly ILogger<DepartmentAccessService> _logger;

    public DepartmentAccessService(
        IDbContext dbContext,
        IRepository<User> userRepository,
        IRepository<LetterTypeDefinition> letterTypeRepository,
        ILogger<DepartmentAccessService> logger)
    {
        _dbContext = dbContext;
        _userRepository = userRepository;
        _letterTypeRepository = letterTypeRepository;
        _logger = logger;
    }

    public async Task<bool> UserCanAccessDepartment(Guid userId, string department)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            // SuperAdmin and Admin or users with no department restriction can access all departments
            if (await UserHasRoleAsync(userId, "SuperAdmin") || await UserHasRoleAsync(userId, "Admin") || string.IsNullOrEmpty(user.Department))
            {
                return true;
            }

            // Regular users can only access their own department
            return user.Department.ToLower() == department.ToLower();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking department access for user {UserId}, department {Department}", userId, department);
            return false;
        }
    }

    public async Task<bool> UserCanAccessTab(Guid userId, Guid tabId)
    {
        try
        {
            _logger.LogInformation("🔍 [DEPT-ACCESS] Checking tab access for user {UserId}, tab {TabId}", userId, tabId);
            
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) 
            {
                _logger.LogWarning("❌ [DEPT-ACCESS] User not found: {UserId}", userId);
                return false;
            }

            _logger.LogInformation("👤 [DEPT-ACCESS] User department: {UserDepartment}", user.Department);

            // SuperAdmin and Admin or users with no department restriction can access all tabs
            var isAdmin = await UserHasRoleAsync(userId, "SuperAdmin") || await UserHasRoleAsync(userId, "Admin") || string.IsNullOrEmpty(user.Department);
            _logger.LogInformation("🔐 [DEPT-ACCESS] User is admin: {IsAdmin}", isAdmin);
            
            if (isAdmin)
            {
                _logger.LogInformation("✅ [DEPT-ACCESS] Admin access granted");
                return true;
            }

            // Get the tab's department
            var tab = await _letterTypeRepository.GetByIdAsync(tabId);
            if (tab == null) 
            {
                _logger.LogWarning("❌ [DEPT-ACCESS] Tab not found: {TabId}", tabId);
                return false;
            }

            _logger.LogInformation("📋 [DEPT-ACCESS] Tab department: {TabDepartment}", tab.Department);

            // Check if user's department matches tab's department
            var canAccess = user.Department.ToLower() == tab.Department.ToLower();
            _logger.LogInformation("🔍 [DEPT-ACCESS] Department match: {CanAccess}", canAccess);
            
            return canAccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking tab access for user {UserId}, tab {TabId}", userId, tabId);
            return false;
        }
    }

    public async Task<List<Guid>> GetAccessibleTabIds(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return new List<Guid>();

            // SuperAdmin and Admin can access all tabs
            if (await UserHasRoleAsync(userId, "SuperAdmin") || await UserHasRoleAsync(userId, "Admin"))
            {
                return await _dbContext.LetterTypeDefinitions
                    .Where(lt => lt.IsActive)
                    .Select(lt => lt.Id)
                    .ToListAsync();
            }

            // Regular users can only access tabs from their department (or tabs without a department)
            return await _dbContext.LetterTypeDefinitions
                .Where(lt => lt.IsActive && (string.IsNullOrEmpty(lt.Department) || lt.Department.ToLower() == user.Department.ToLower()))
                .Select(lt => lt.Id)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accessible tab IDs for user {UserId}", userId);
            return new List<Guid>();
        }
    }

    public async Task<List<string>> GetUserDepartments(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return new List<string>();

            // SuperAdmin and Admin or users with no department restriction can access all departments
            if (await UserHasRoleAsync(userId, "SuperAdmin") || await UserHasRoleAsync(userId, "Admin") || string.IsNullOrEmpty(user.Department))
            {
                return new List<string> { "ER", "Billing" };
            }

            // Regular users can only access their own department
            return new List<string> { user.Department };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user departments for user {UserId}", userId);
            return new List<string>();
        }
    }

    public async Task<bool> IsUserInDepartment(Guid userId, string department)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            // SuperAdmin and Admin or users with no department restriction are considered to be in all departments
            if (await UserHasRoleAsync(userId, "SuperAdmin") || await UserHasRoleAsync(userId, "Admin") || string.IsNullOrEmpty(user.Department))
            {
                return true;
            }

            return user.Department.ToLower() == department.ToLower();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserId} is in department {Department}", userId, department);
            return false;
        }
    }

    public async Task<List<TabAccessDto>> GetUserTabAccess(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return new List<TabAccessDto>();

            var isAdmin = await UserHasRoleAsync(userId, "SuperAdmin") || await UserHasRoleAsync(userId, "Admin");
            var hasManagePermission = await UserHasPermissionAsync(userId, "tabs.manage");

            var tabs = await _dbContext.LetterTypeDefinitions
                .Where(lt => lt.IsActive && (isAdmin || lt.Department.ToLower() == user.Department.ToLower()))
                .Select(lt => new TabAccessDto
                {
                    TabId = lt.Id,
                    TabName = lt.DisplayName,
                    Department = lt.Department,
                    CanView = true, // If they can see the tab, they can view it
                    CanEdit = isAdmin || hasManagePermission,
                    CanDelete = isAdmin || hasManagePermission
                })
                .ToListAsync();

            return tabs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user tab access for user {UserId}", userId);
            return new List<TabAccessDto>();
        }
    }

    private async Task<bool> UserHasRoleAsync(Guid userId, string roleName)
    {
        try
        {
            return await _dbContext.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId && (ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow))
                .AnyAsync(ur => ur.Role.Name == roleName && ur.Role.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user role: {UserId}, {RoleName}", userId, roleName);
            return false;
        }
    }

    private async Task<bool> UserHasPermissionAsync(Guid userId, string permissionName)
    {
        try
        {
            return await _dbContext.UserRoles
                .Include(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Where(ur => ur.UserId == userId && !ur.IsExpired)
                .AnyAsync(ur => ur.Role.RolePermissions.Any(rp => rp.Permission.Name == permissionName && rp.Permission.IsActive));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user permission: {UserId}, {PermissionName}", userId, permissionName);
            return false;
        }
    }
}
