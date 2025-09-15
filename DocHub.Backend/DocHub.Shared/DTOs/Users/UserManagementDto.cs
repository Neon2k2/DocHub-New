using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DocHub.Shared.DTOs.Users;

// User Management DTOs
public class UserDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("fullName")]
    public string FullName => $"{FirstName} {LastName}".Trim();

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("department")]
    public string? Department { get; set; }

    [JsonPropertyName("employeeId")]
    public string? EmployeeId { get; set; }

    [JsonPropertyName("jobTitle")]
    public string? JobTitle { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("zipCode")]
    public string? ZipCode { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("isEmailVerified")]
    public bool IsEmailVerified { get; set; }

    [JsonPropertyName("isPhoneVerified")]
    public bool IsPhoneVerified { get; set; }

    [JsonPropertyName("profileImageUrl")]
    public string? ProfileImageUrl { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }

    [JsonPropertyName("passwordChangedAt")]
    public DateTime? PasswordChangedAt { get; set; }

    [JsonPropertyName("emailVerifiedAt")]
    public DateTime? EmailVerifiedAt { get; set; }

    [JsonPropertyName("phoneVerifiedAt")]
    public DateTime? PhoneVerifiedAt { get; set; }

    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = new();

    [JsonPropertyName("permissions")]
    public UserPermissionsDto Permissions { get; set; } = new();
}

public class UserPermissionsDto
{
    [JsonPropertyName("isAdmin")]
    public bool IsAdmin { get; set; }

    [JsonPropertyName("canAccessER")]
    public bool CanAccessER { get; set; }

    [JsonPropertyName("canAccessBilling")]
    public bool CanAccessBilling { get; set; }

    [JsonPropertyName("canManageUsers")]
    public bool CanManageUsers { get; set; }

    [JsonPropertyName("canManageRoles")]
    public bool CanManageRoles { get; set; }

    [JsonPropertyName("canViewAuditLogs")]
    public bool CanViewAuditLogs { get; set; }

    [JsonPropertyName("canManageSystemSettings")]
    public bool CanManageSystemSettings { get; set; }
}

public class CreateUserRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [Phone]
    [StringLength(20)]
    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [StringLength(100)]
    [JsonPropertyName("department")]
    public string? Department { get; set; }

    [StringLength(50)]
    [JsonPropertyName("employeeId")]
    public string? EmployeeId { get; set; }

    [StringLength(100)]
    [JsonPropertyName("jobTitle")]
    public string? JobTitle { get; set; }

    [StringLength(200)]
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [StringLength(100)]
    [JsonPropertyName("city")]
    public string? City { get; set; }

    [StringLength(50)]
    [JsonPropertyName("state")]
    public string? State { get; set; }

    [StringLength(20)]
    [JsonPropertyName("zipCode")]
    public string? ZipCode { get; set; }

    [StringLength(100)]
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = new();

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [StringLength(1000)]
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

public class UpdateUserRequest
{
    [StringLength(50, MinimumLength = 2)]
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [StringLength(50, MinimumLength = 2)]
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [EmailAddress]
    [StringLength(100)]
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [Phone]
    [StringLength(20)]
    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [StringLength(100)]
    [JsonPropertyName("department")]
    public string? Department { get; set; }

    [StringLength(50)]
    [JsonPropertyName("employeeId")]
    public string? EmployeeId { get; set; }

    [StringLength(100)]
    [JsonPropertyName("jobTitle")]
    public string? JobTitle { get; set; }

    [StringLength(200)]
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [StringLength(100)]
    [JsonPropertyName("city")]
    public string? City { get; set; }

    [StringLength(50)]
    [JsonPropertyName("state")]
    public string? State { get; set; }

    [StringLength(20)]
    [JsonPropertyName("zipCode")]
    public string? ZipCode { get; set; }

    [StringLength(100)]
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("roles")]
    public List<string>? Roles { get; set; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; }

    [StringLength(1000)]
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    [StringLength(100, MinimumLength = 8)]
    [JsonPropertyName("currentPassword")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [JsonPropertyName("confirmPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required]
    [EmailAddress]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordConfirmRequest
{
    [Required]
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [JsonPropertyName("confirmPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class GetUsersRequest
{
    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 10;

    [JsonPropertyName("searchTerm")]
    public string? SearchTerm { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("department")]
    public string? Department { get; set; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; }

    [JsonPropertyName("sortBy")]
    public string? SortBy { get; set; } = "createdAt";

    [JsonPropertyName("sortDirection")]
    public string? SortDirection { get; set; } = "desc";
}

public class UserStatsDto
{
    [JsonPropertyName("totalUsers")]
    public int TotalUsers { get; set; }

    [JsonPropertyName("activeUsers")]
    public int ActiveUsers { get; set; }

    [JsonPropertyName("inactiveUsers")]
    public int InactiveUsers { get; set; }

    [JsonPropertyName("newUsersThisMonth")]
    public int NewUsersThisMonth { get; set; }

    [JsonPropertyName("usersByRole")]
    public Dictionary<string, int> UsersByRole { get; set; } = new();

    [JsonPropertyName("usersByDepartment")]
    public Dictionary<string, int> UsersByDepartment { get; set; } = new();

    [JsonPropertyName("recentLogins")]
    public int RecentLogins { get; set; }
}
