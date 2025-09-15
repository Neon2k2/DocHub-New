using DocHub.Shared.DTOs.Common;
using DocHub.Shared.DTOs.Users;

namespace DocHub.Core.Interfaces;

public interface IRoleManagementService
{
    // Role Management
    Task<PaginatedResponse<RoleDto>> GetRolesAsync(GetRolesRequest request);
    Task<RoleDto?> GetRoleByIdAsync(Guid roleId);
    Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, Guid currentUserId);
    Task<RoleDto> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request, Guid currentUserId);
    Task<bool> DeleteRoleAsync(Guid roleId, Guid currentUserId);
    Task<bool> ToggleRoleStatusAsync(Guid roleId, bool isActive, Guid currentUserId);
    Task<RoleStatsDto> GetRoleStatsAsync();

    // Permission Management
    Task<List<PermissionDto>> GetAllPermissionsAsync();
    Task<List<PermissionDto>> GetPermissionsByCategoryAsync(string category);
    Task<PermissionDto?> GetPermissionByIdAsync(Guid permissionId);
    Task<PermissionDto> CreatePermissionAsync(CreatePermissionRequest request);
    Task<PermissionDto> UpdatePermissionAsync(Guid permissionId, UpdatePermissionRequest request);
    Task<bool> DeletePermissionAsync(Guid permissionId);

    // User Role Assignment
    Task<bool> AssignRoleToUserAsync(AssignRoleToUserRequest request, Guid currentUserId);
    Task<bool> RemoveRoleFromUserAsync(RemoveRoleFromUserRequest request, Guid currentUserId);
    Task<bool> BulkAssignRolesAsync(BulkAssignRolesRequest request, Guid currentUserId);
    Task<bool> BulkRemoveRolesAsync(BulkRemoveRolesRequest request, Guid currentUserId);
    Task<List<UserRoleAssignmentDto>> GetUserRolesAsync(Guid userId);
    Task<List<UserRoleAssignmentDto>> GetRoleUsersAsync(Guid roleId);

    // Role Permission Management
    Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);
    Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId);
    Task<List<PermissionDto>> GetRolePermissionsAsync(Guid roleId);
    Task<bool> BulkAssignPermissionsToRoleAsync(Guid roleId, List<Guid> permissionIds);
    Task<bool> BulkRemovePermissionsFromRoleAsync(Guid roleId, List<Guid> permissionIds);

    // User Permission Checking
    Task<bool> UserHasPermissionAsync(Guid userId, string permissionName);
    Task<bool> UserHasRoleAsync(Guid userId, string roleName);
    Task<List<string>> GetUserPermissionsAsync(Guid userId);
    Task<List<string>> GetUserRoleNamesAsync(Guid userId);

    // System Role Management
    Task<bool> CreateSystemRolesAsync();
    Task<bool> CreateDefaultPermissionsAsync();
    Task<bool> AssignDefaultPermissionsToRolesAsync();
}

// Additional request DTOs
public class CreatePermissionRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class UpdatePermissionRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool? IsActive { get; set; }
}
