using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DocHub.Shared.DTOs.Users;

// Role Management DTOs
public class RoleDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("isSystemRole")]
    public bool IsSystemRole { get; set; }

    [JsonPropertyName("permissions")]
    public List<PermissionDto> Permissions { get; set; } = new();

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("userCount")]
    public int UserCount { get; set; }
}

public class PermissionDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("isGranted")]
    public bool IsGranted { get; set; }
}

public class CreateRoleRequest
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("permissions")]
    public List<Guid> PermissionIds { get; set; } = new();

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
}

public class UpdateRoleRequest
{
    [StringLength(50, MinimumLength = 2)]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [StringLength(200)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("permissions")]
    public List<Guid>? PermissionIds { get; set; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; }
}

public class AssignRoleToUserRequest
{
    [Required]
    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [Required]
    [JsonPropertyName("roleId")]
    public Guid RoleId { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }
}

public class RemoveRoleFromUserRequest
{
    [Required]
    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [Required]
    [JsonPropertyName("roleId")]
    public Guid RoleId { get; set; }
}

public class GetRolesRequest
{
    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 10;

    [JsonPropertyName("searchTerm")]
    public string? SearchTerm { get; set; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; }

    [JsonPropertyName("sortBy")]
    public string? SortBy { get; set; } = "name";

    [JsonPropertyName("sortDirection")]
    public string? SortDirection { get; set; } = "asc";
}

public class RoleStatsDto
{
    [JsonPropertyName("totalRoles")]
    public int TotalRoles { get; set; }

    [JsonPropertyName("activeRoles")]
    public int ActiveRoles { get; set; }

    [JsonPropertyName("systemRoles")]
    public int SystemRoles { get; set; }

    [JsonPropertyName("customRoles")]
    public int CustomRoles { get; set; }

    [JsonPropertyName("rolesByPermission")]
    public Dictionary<string, int> RolesByPermission { get; set; } = new();
}

public class UserRoleAssignmentDto
{
    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("userEmail")]
    public string UserEmail { get; set; } = string.Empty;

    [JsonPropertyName("roleId")]
    public Guid RoleId { get; set; }

    [JsonPropertyName("roleName")]
    public string RoleName { get; set; } = string.Empty;

    [JsonPropertyName("assignedAt")]
    public DateTime AssignedAt { get; set; }

    [JsonPropertyName("assignedBy")]
    public string? AssignedBy { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    [JsonPropertyName("isExpired")]
    public bool IsExpired { get; set; }
}

public class BulkAssignRolesRequest
{
    [JsonPropertyName("userIds")]
    public List<Guid> UserIds { get; set; } = new();

    [JsonPropertyName("roleIds")]
    public List<Guid> RoleIds { get; set; } = new();

    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }
}

public class BulkRemoveRolesRequest
{
    [JsonPropertyName("userIds")]
    public List<Guid> UserIds { get; set; } = new();

    [JsonPropertyName("roleIds")]
    public List<Guid> RoleIds { get; set; } = new();
}

public class CreatePermissionRequest
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [Required]
    [StringLength(50)]
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
}

public class UpdatePermissionRequest
{
    [StringLength(50, MinimumLength = 2)]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [StringLength(200)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [StringLength(50)]
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; }
}