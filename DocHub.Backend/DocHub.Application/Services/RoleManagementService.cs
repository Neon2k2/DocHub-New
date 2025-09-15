using DocHub.Core.Entities;
using DocHub.Core.Interfaces;
using DocHub.Core.Interfaces.Repositories;
using DocHub.Shared.DTOs.Common;
using DocHub.Shared.DTOs.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocHub.Application.Services;

public class RoleManagementService : IRoleManagementService
{
    private readonly IDbContext _dbContext;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<Permission> _permissionRepository;
    private readonly IRepository<RolePermission> _rolePermissionRepository;
    private readonly IRepository<UserRole> _userRoleRepository;
    private readonly ILogger<RoleManagementService> _logger;

    public RoleManagementService(
        IDbContext dbContext,
        IRepository<Role> roleRepository,
        IRepository<Permission> permissionRepository,
        IRepository<RolePermission> rolePermissionRepository,
        IRepository<UserRole> userRoleRepository,
        ILogger<RoleManagementService> logger)
    {
        _dbContext = dbContext;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _userRoleRepository = userRoleRepository;
        _logger = logger;
    }

    public async Task<PaginatedResponse<RoleDto>> GetRolesAsync(GetRolesRequest request)
    {
        try
        {
            var query = _dbContext.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(r => r.Name.Contains(request.SearchTerm) || 
                                       (r.Description != null && r.Description.Contains(request.SearchTerm)));
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(r => r.IsActive == request.IsActive.Value);
            }

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(r => r.Name) : query.OrderBy(r => r.Name),
                "createdat" => request.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt),
                "updatedat" => request.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(r => r.UpdatedAt) : query.OrderBy(r => r.UpdatedAt),
                _ => query.OrderBy(r => r.Name)
            };

            var totalCount = await query.CountAsync();

            var roles = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    IsSystemRole = r.IsSystemRole,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    UserCount = r.UserRoles.Count(ur => !ur.IsExpired),
                    Permissions = r.RolePermissions.Select(rp => new PermissionDto
                    {
                        Id = rp.Permission.Id,
                        Name = rp.Permission.Name,
                        Description = rp.Permission.Description,
                        Category = rp.Permission.Category,
                        IsGranted = true
                    }).ToList()
                })
                .ToListAsync();

            return new PaginatedResponse<RoleDto>
            {
                Items = roles,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles");
            throw;
        }
    }

    public async Task<RoleDto?> GetRoleByIdAsync(Guid roleId)
    {
        try
        {
            var role = await _dbContext.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null) return null;

            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsActive = role.IsActive,
                IsSystemRole = role.IsSystemRole,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt,
                UserCount = role.UserRoles.Count(ur => !ur.IsExpired),
                Permissions = role.RolePermissions.Select(rp => new PermissionDto
                {
                    Id = rp.Permission.Id,
                    Name = rp.Permission.Name,
                    Description = rp.Permission.Description,
                    Category = rp.Permission.Category,
                    IsGranted = true
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role by ID: {RoleId}", roleId);
            throw;
        }
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, Guid currentUserId)
    {
        try
        {
            // Check if role name already exists
            if (await _roleRepository.FirstOrDefaultAsync(r => r.Name == request.Name) != null)
            {
                throw new ArgumentException("Role name already exists");
            }

            var role = new Role
            {
                Name = request.Name,
                Description = request.Description,
                IsActive = request.IsActive,
                IsSystemRole = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _roleRepository.AddAsync(role);
            await _dbContext.SaveChangesAsync();

            // Assign permissions if provided
            if (request.PermissionIds.Any())
            {
                await BulkAssignPermissionsToRoleAsync(role.Id, request.PermissionIds);
            }

            return await GetRoleByIdAsync(role.Id) ?? throw new InvalidOperationException("Failed to retrieve created role");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role");
            throw;
        }
    }

    public async Task<RoleDto> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request, Guid currentUserId)
    {
        try
        {
            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role == null)
            {
                throw new ArgumentException("Role not found");
            }

            if (role.IsSystemRole)
            {
                throw new InvalidOperationException("Cannot modify system roles");
            }

            // Check if new name already exists (if changing name)
            if (!string.IsNullOrEmpty(request.Name) && request.Name != role.Name)
            {
                if (await _roleRepository.FirstOrDefaultAsync(r => r.Name == request.Name && r.Id != roleId) != null)
                {
                    throw new ArgumentException("Role name already exists");
                }
                role.Name = request.Name;
            }

            if (request.Description != null)
                role.Description = request.Description;

            if (request.IsActive.HasValue)
                role.IsActive = request.IsActive.Value;

            role.UpdatedAt = DateTime.UtcNow;

            await _roleRepository.UpdateAsync(role);

            // Update permissions if provided
            if (request.PermissionIds != null)
            {
            // Remove existing permissions
            var existingPermissions = await _rolePermissionRepository.FindAsync(rp => rp.RoleId == roleId);
            foreach (var perm in existingPermissions)
            {
                await _rolePermissionRepository.DeleteAsync(perm);
            }

                // Add new permissions
                if (request.PermissionIds.Any())
                {
                    await BulkAssignPermissionsToRoleAsync(roleId, request.PermissionIds);
                }
            }

            await _dbContext.SaveChangesAsync();

            return await GetRoleByIdAsync(roleId) ?? throw new InvalidOperationException("Failed to retrieve updated role");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role: {RoleId}", roleId);
            throw;
        }
    }

    public async Task<bool> DeleteRoleAsync(Guid roleId, Guid currentUserId)
    {
        try
        {
            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role == null)
            {
                return false;
            }

            if (role.IsSystemRole)
            {
                throw new InvalidOperationException("Cannot delete system roles");
            }

            // Check if role is assigned to any users
            var userRoles = await _userRoleRepository.FindAsync(ur => ur.RoleId == roleId);
            if (userRoles.Any())
            {
                throw new InvalidOperationException("Cannot delete role that is assigned to users");
            }

            await _roleRepository.DeleteAsync(role);
            await _dbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role: {RoleId}", roleId);
            throw;
        }
    }

    public async Task<bool> ToggleRoleStatusAsync(Guid roleId, bool isActive, Guid currentUserId)
    {
        try
        {
            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role == null)
            {
                return false;
            }

            if (role.IsSystemRole)
            {
                throw new InvalidOperationException("Cannot modify system roles");
            }

            role.IsActive = isActive;
            role.UpdatedAt = DateTime.UtcNow;

            await _roleRepository.UpdateAsync(role);
            await _dbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling role status: {RoleId}", roleId);
            throw;
        }
    }

    public async Task<RoleStatsDto> GetRoleStatsAsync()
    {
        try
        {
            var totalRoles = await _dbContext.Roles.CountAsync();
            var activeRoles = await _dbContext.Roles.CountAsync(r => r.IsActive);
            var systemRoles = await _dbContext.Roles.CountAsync(r => r.IsSystemRole);
            var customRoles = totalRoles - systemRoles;

            var rolesByPermission = await _dbContext.RolePermissions
                .Include(rp => rp.Permission)
                .GroupBy(rp => rp.Permission.Category)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            return new RoleStatsDto
            {
                TotalRoles = totalRoles,
                ActiveRoles = activeRoles,
                SystemRoles = systemRoles,
                CustomRoles = customRoles,
                RolesByPermission = rolesByPermission
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role stats");
            throw;
        }
    }

    public async Task<List<PermissionDto>> GetAllPermissionsAsync()
    {
        try
        {
            return await _dbContext.Permissions
                .Where(p => p.IsActive)
                .Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Category = p.Category,
                    IsGranted = false
                })
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all permissions");
            throw;
        }
    }

    public async Task<List<PermissionDto>> GetPermissionsByCategoryAsync(string category)
    {
        try
        {
            return await _dbContext.Permissions
                .Where(p => p.IsActive && p.Category == category)
                .Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Category = p.Category,
                    IsGranted = false
                })
                .OrderBy(p => p.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions by category: {Category}", category);
            throw;
        }
    }

    public async Task<PermissionDto?> GetPermissionByIdAsync(Guid permissionId)
    {
        try
        {
            var permission = await _permissionRepository.GetByIdAsync(permissionId);
            if (permission == null) return null;

            return new PermissionDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description,
                Category = permission.Category,
                IsGranted = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission by ID: {PermissionId}", permissionId);
            throw;
        }
    }

    public async Task<PermissionDto> CreatePermissionAsync(CreatePermissionRequest request)
    {
        try
        {
            // Check if permission name already exists
            if (await _permissionRepository.FirstOrDefaultAsync(p => p.Name == request.Name) != null)
            {
                throw new ArgumentException("Permission name already exists");
            }

            var permission = new Permission
            {
                Name = request.Name,
                Description = request.Description,
                Category = request.Category,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _permissionRepository.AddAsync(permission);
            await _dbContext.SaveChangesAsync();

            return new PermissionDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description,
                Category = permission.Category,
                IsGranted = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission");
            throw;
        }
    }

    public async Task<PermissionDto> UpdatePermissionAsync(Guid permissionId, UpdatePermissionRequest request)
    {
        try
        {
            var permission = await _permissionRepository.GetByIdAsync(permissionId);
            if (permission == null)
            {
                throw new ArgumentException("Permission not found");
            }

            // Check if new name already exists (if changing name)
            if (!string.IsNullOrEmpty(request.Name) && request.Name != permission.Name)
            {
                if (await _permissionRepository.FirstOrDefaultAsync(p => p.Name == request.Name && p.Id != permissionId) != null)
                {
                    throw new ArgumentException("Permission name already exists");
                }
                permission.Name = request.Name;
            }

            if (request.Description != null)
                permission.Description = request.Description;

            if (request.Category != null)
                permission.Category = request.Category;

            if (request.IsActive.HasValue)
                permission.IsActive = request.IsActive.Value;

            permission.UpdatedAt = DateTime.UtcNow;

            await _permissionRepository.UpdateAsync(permission);
            await _dbContext.SaveChangesAsync();

            return new PermissionDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description,
                Category = permission.Category,
                IsGranted = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permission: {PermissionId}", permissionId);
            throw;
        }
    }

    public async Task<bool> DeletePermissionAsync(Guid permissionId)
    {
        try
        {
            var permission = await _permissionRepository.GetByIdAsync(permissionId);
            if (permission == null)
            {
                return false;
            }

            // Check if permission is assigned to any roles
            var rolePermissions = await _rolePermissionRepository.FindAsync(rp => rp.PermissionId == permissionId);
            if (rolePermissions.Any())
            {
                throw new InvalidOperationException("Cannot delete permission that is assigned to roles");
            }

            await _permissionRepository.DeleteAsync(permission);
            await _dbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting permission: {PermissionId}", permissionId);
            throw;
        }
    }

    public async Task<bool> AssignRoleToUserAsync(AssignRoleToUserRequest request, Guid currentUserId)
    {
        try
        {
            // Check if user already has this role
            var existingUserRole = await _userRoleRepository.FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == request.RoleId);
            if (existingUserRole != null)
            {
                if (existingUserRole.IsExpired)
                {
                    // Update existing expired role
                    existingUserRole.ExpiresAt = request.ExpiresAt;
                    existingUserRole.AssignedBy = currentUserId;
                    existingUserRole.AssignedAt = DateTime.UtcNow;
                    await _userRoleRepository.UpdateAsync(existingUserRole);
                }
                else
                {
                    throw new ArgumentException("User already has this role");
                }
            }
            else
            {
                var userRole = new UserRole
                {
                    UserId = request.UserId,
                    RoleId = request.RoleId,
                    ExpiresAt = request.ExpiresAt,
                    AssignedBy = currentUserId,
                    AssignedAt = DateTime.UtcNow
                };

                await _userRoleRepository.AddAsync(userRole);
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role to user");
            throw;
        }
    }

    public async Task<bool> RemoveRoleFromUserAsync(RemoveRoleFromUserRequest request, Guid currentUserId)
    {
        try
        {
            var userRole = await _userRoleRepository.FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == request.RoleId);
            if (userRole == null)
            {
                return false;
            }

            await _userRoleRepository.DeleteAsync(userRole);
            await _dbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role from user");
            throw;
        }
    }

    public async Task<bool> BulkAssignRolesAsync(BulkAssignRolesRequest request, Guid currentUserId)
    {
        try
        {
            foreach (var userId in request.UserIds)
            {
                foreach (var roleId in request.RoleIds)
                {
                    var assignRequest = new AssignRoleToUserRequest
                    {
                        UserId = userId,
                        RoleId = roleId,
                        ExpiresAt = request.ExpiresAt
                    };

                    try
                    {
                        await AssignRoleToUserAsync(assignRequest, currentUserId);
                    }
                    catch (ArgumentException)
                    {
                        // Skip if user already has role
                        continue;
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk assigning roles");
            throw;
        }
    }

    public async Task<bool> BulkRemoveRolesAsync(BulkRemoveRolesRequest request, Guid currentUserId)
    {
        try
        {
            foreach (var userId in request.UserIds)
            {
                foreach (var roleId in request.RoleIds)
                {
                    var removeRequest = new RemoveRoleFromUserRequest
                    {
                        UserId = userId,
                        RoleId = roleId
                    };

                    await RemoveRoleFromUserAsync(removeRequest, currentUserId);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk removing roles");
            throw;
        }
    }

    public async Task<List<UserRoleAssignmentDto>> GetUserRolesAsync(Guid userId)
    {
        try
        {
            return await _dbContext.UserRoles
                .Include(ur => ur.Role)
                .Include(ur => ur.AssignedByUser)
                .Where(ur => ur.UserId == userId)
                .Select(ur => new UserRoleAssignmentDto
                {
                    UserId = ur.UserId,
                    UserName = ur.User.Username,
                    UserEmail = ur.User.Email,
                    RoleId = ur.RoleId,
                    RoleName = ur.Role.Name,
                    AssignedAt = ur.AssignedAt,
                    AssignedBy = ur.AssignedByUser != null ? ur.AssignedByUser.Username : null,
                    ExpiresAt = ur.ExpiresAt,
                    IsExpired = ur.IsExpired
                })
                .OrderBy(ur => ur.RoleName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user roles: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<UserRoleAssignmentDto>> GetRoleUsersAsync(Guid roleId)
    {
        try
        {
            return await _dbContext.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.AssignedByUser)
                .Where(ur => ur.RoleId == roleId)
                .Select(ur => new UserRoleAssignmentDto
                {
                    UserId = ur.UserId,
                    UserName = ur.User.Username,
                    UserEmail = ur.User.Email,
                    RoleId = ur.RoleId,
                    RoleName = ur.Role.Name,
                    AssignedAt = ur.AssignedAt,
                    AssignedBy = ur.AssignedByUser != null ? ur.AssignedByUser.Username : null,
                    ExpiresAt = ur.ExpiresAt,
                    IsExpired = ur.IsExpired
                })
                .OrderBy(ur => ur.UserName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role users: {RoleId}", roleId);
            throw;
        }
    }

    public async Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        try
        {
            // Check if permission is already assigned
            var existingRolePermission = await _rolePermissionRepository.FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
            if (existingRolePermission != null)
            {
                return true; // Already assigned
            }

            var rolePermission = new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                CreatedAt = DateTime.UtcNow
            };

            await _rolePermissionRepository.AddAsync(rolePermission);
            await _dbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission to role");
            throw;
        }
    }

    public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        try
        {
            var rolePermission = await _rolePermissionRepository.FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
            if (rolePermission == null)
            {
                return false;
            }

            await _rolePermissionRepository.DeleteAsync(rolePermission);
            await _dbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permission from role");
            throw;
        }
    }

    public async Task<List<PermissionDto>> GetRolePermissionsAsync(Guid roleId)
    {
        try
        {
            return await _dbContext.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => new PermissionDto
                {
                    Id = rp.Permission.Id,
                    Name = rp.Permission.Name,
                    Description = rp.Permission.Description,
                    Category = rp.Permission.Category,
                    IsGranted = true
                })
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role permissions: {RoleId}", roleId);
            throw;
        }
    }

    public async Task<bool> BulkAssignPermissionsToRoleAsync(Guid roleId, List<Guid> permissionIds)
    {
        try
        {
            foreach (var permissionId in permissionIds)
            {
                await AssignPermissionToRoleAsync(roleId, permissionId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk assigning permissions to role: {RoleId}", roleId);
            throw;
        }
    }

    public async Task<bool> BulkRemovePermissionsFromRoleAsync(Guid roleId, List<Guid> permissionIds)
    {
        try
        {
            foreach (var permissionId in permissionIds)
            {
                await RemovePermissionFromRoleAsync(roleId, permissionId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk removing permissions from role: {RoleId}", roleId);
            throw;
        }
    }

    public async Task<bool> UserHasPermissionAsync(Guid userId, string permissionName)
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
            throw;
        }
    }

    public async Task<bool> UserHasRoleAsync(Guid userId, string roleName)
    {
        try
        {
            return await _dbContext.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId && !ur.IsExpired)
                .AnyAsync(ur => ur.Role.Name == roleName && ur.Role.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user role: {UserId}, {RoleName}", userId, roleName);
            throw;
        }
    }

    public async Task<List<string>> GetUserPermissionsAsync(Guid userId)
    {
        try
        {
            return await _dbContext.UserRoles
                .Include(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Where(ur => ur.UserId == userId && !ur.IsExpired)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user permissions: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<string>> GetUserRoleNamesAsync(Guid userId)
    {
        try
        {
            return await _dbContext.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId && !ur.IsExpired)
                .Select(ur => ur.Role.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user roles: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> CreateSystemRolesAsync()
    {
        try
        {
            var systemRoles = new[]
            {
                new Role { Name = "SuperAdmin", Description = "Full system access", IsSystemRole = true, IsActive = true },
                new Role { Name = "Admin", Description = "Administrative access", IsSystemRole = true, IsActive = true },
                new Role { Name = "Manager", Description = "Management access", IsSystemRole = true, IsActive = true },
                new Role { Name = "User", Description = "Standard user access", IsSystemRole = true, IsActive = true },
                new Role { Name = "Guest", Description = "Limited guest access", IsSystemRole = true, IsActive = true }
            };

            foreach (var role in systemRoles)
            {
                if (await _roleRepository.FirstOrDefaultAsync(r => r.Name == role.Name) == null)
                {
                    role.CreatedAt = DateTime.UtcNow;
                    role.UpdatedAt = DateTime.UtcNow;
                    await _roleRepository.AddAsync(role);
                }
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating system roles");
            throw;
        }
    }

    public async Task<bool> CreateDefaultPermissionsAsync()
    {
        try
        {
            var permissions = new[]
            {
                // User Management
                new Permission { Name = "users.view", Description = "View users", Category = "User Management", IsActive = true },
                new Permission { Name = "users.create", Description = "Create users", Category = "User Management", IsActive = true },
                new Permission { Name = "users.edit", Description = "Edit users", Category = "User Management", IsActive = true },
                new Permission { Name = "users.delete", Description = "Delete users", Category = "User Management", IsActive = true },
                new Permission { Name = "users.manage_roles", Description = "Manage user roles", Category = "User Management", IsActive = true },

                // Role Management
                new Permission { Name = "roles.view", Description = "View roles", Category = "Role Management", IsActive = true },
                new Permission { Name = "roles.create", Description = "Create roles", Category = "Role Management", IsActive = true },
                new Permission { Name = "roles.edit", Description = "Edit roles", Category = "Role Management", IsActive = true },
                new Permission { Name = "roles.delete", Description = "Delete roles", Category = "Role Management", IsActive = true },

                // Document Management
                new Permission { Name = "documents.view", Description = "View documents", Category = "Document Management", IsActive = true },
                new Permission { Name = "documents.create", Description = "Create documents", Category = "Document Management", IsActive = true },
                new Permission { Name = "documents.edit", Description = "Edit documents", Category = "Document Management", IsActive = true },
                new Permission { Name = "documents.delete", Description = "Delete documents", Category = "Document Management", IsActive = true },

                // Dashboard
                new Permission { Name = "dashboard.view", Description = "View dashboard", Category = "Dashboard", IsActive = true },
                new Permission { Name = "dashboard.admin", Description = "Admin dashboard access", Category = "Dashboard", IsActive = true },

                // System
                new Permission { Name = "system.settings", Description = "Manage system settings", Category = "System", IsActive = true },
                new Permission { Name = "system.audit", Description = "View audit logs", Category = "System", IsActive = true }
            };

            foreach (var permission in permissions)
            {
                if (await _permissionRepository.FirstOrDefaultAsync(p => p.Name == permission.Name) == null)
                {
                    permission.CreatedAt = DateTime.UtcNow;
                    permission.UpdatedAt = DateTime.UtcNow;
                    await _permissionRepository.AddAsync(permission);
                }
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating default permissions");
            throw;
        }
    }

    public async Task<bool> AssignDefaultPermissionsToRolesAsync()
    {
        try
        {
            // Get roles
            var superAdminRole = await _roleRepository.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
            var adminRole = await _roleRepository.FirstOrDefaultAsync(r => r.Name == "Admin");
            var managerRole = await _roleRepository.FirstOrDefaultAsync(r => r.Name == "Manager");
            var userRole = await _roleRepository.FirstOrDefaultAsync(r => r.Name == "User");

            if (superAdminRole == null || adminRole == null || managerRole == null || userRole == null)
            {
                throw new InvalidOperationException("System roles not found");
            }

            // Get all permissions
            var allPermissions = await _permissionRepository.GetAllAsync();

            // SuperAdmin gets all permissions
            foreach (var permission in allPermissions)
            {
                await AssignPermissionToRoleAsync(superAdminRole.Id, permission.Id);
            }

            // Admin gets most permissions except system settings
            var adminPermissions = allPermissions.Where(p => p.Name != "system.settings").ToList();
            foreach (var permission in adminPermissions)
            {
                await AssignPermissionToRoleAsync(adminRole.Id, permission.Id);
            }

            // Manager gets user and document management permissions
            var managerPermissions = allPermissions.Where(p => 
                p.Category == "User Management" || 
                p.Category == "Document Management" || 
                p.Name == "dashboard.view").ToList();
            foreach (var permission in managerPermissions)
            {
                await AssignPermissionToRoleAsync(managerRole.Id, permission.Id);
            }

            // User gets basic permissions
            var userPermissions = allPermissions.Where(p => 
                p.Name == "documents.view" || 
                p.Name == "documents.create" || 
                p.Name == "dashboard.view").ToList();
            foreach (var permission in userPermissions)
            {
                await AssignPermissionToRoleAsync(userRole.Id, permission.Id);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning default permissions to roles");
            throw;
        }
    }
}
