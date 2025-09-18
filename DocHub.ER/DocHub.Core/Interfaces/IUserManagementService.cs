using DocHub.Shared.DTOs.Common;
using DocHub.Shared.DTOs.Users;
using DocHub.Core.Entities;

namespace DocHub.Core.Interfaces;

public interface IUserManagementService
{
    // User CRUD Operations
    Task<PaginatedResponse<UserDto>> GetUsersAsync(GetUsersRequest request);
    Task<UserDto?> GetUserByIdAsync(Guid userId);
    Task<UserDto> CreateUserAsync(CreateUserRequest request, string currentUserId);
    Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserRequest request, string currentUserId);
    Task<bool> DeleteUserAsync(Guid userId, string currentUserId);
    Task<bool> ToggleUserStatusAsync(Guid userId, bool isActive, string currentUserId);

    // Password Management
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, string currentUserId);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
    Task<bool> ConfirmPasswordResetAsync(ResetPasswordConfirmRequest request);
    Task<bool> ForcePasswordChangeAsync(Guid userId, string newPassword, string currentUserId);

    // User Verification
    Task<bool> VerifyEmailAsync(Guid userId, string token);
    Task<bool> VerifyPhoneAsync(Guid userId, string token);
    Task<string> GenerateEmailVerificationTokenAsync(Guid userId);
    Task<string> GeneratePhoneVerificationTokenAsync(Guid userId);

    // User Statistics
    Task<UserStatsDto> GetUserStatsAsync();
    Task<List<UserDto>> GetRecentUsersAsync(int count = 10);
    Task<List<UserDto>> GetUsersByRoleAsync(string role);
    Task<List<UserDto>> GetUsersByDepartmentAsync(string department);

    // User Search and Filtering
    Task<List<UserDto>> SearchUsersAsync(string searchTerm, int limit = 20);
    Task<List<UserDto>> GetInactiveUsersAsync(int daysSinceLastLogin = 30);
    Task<List<UserDto>> GetUsersCreatedInDateRangeAsync(DateTime startDate, DateTime endDate);

    // Account Security
    Task<bool> LockUserAccountAsync(Guid userId, DateTime? lockUntil, string reason, string currentUserId);
    Task<bool> UnlockUserAccountAsync(Guid userId, string currentUserId);
    Task<bool> ResetFailedLoginAttemptsAsync(Guid userId);
    Task<bool> IsAccountLockedAsync(Guid userId);

    // User Profile Management
    Task<bool> UpdateProfileImageAsync(Guid userId, string imageUrl, string currentUserId);
    Task<bool> DeleteProfileImageAsync(Guid userId, string currentUserId);
    Task<bool> UpdateUserNotesAsync(Guid userId, string notes, string currentUserId);

    // Bulk Operations
    Task<bool> BulkUpdateUserStatusAsync(List<Guid> userIds, bool isActive, string currentUserId);
    Task<bool> BulkAssignRolesAsync(List<Guid> userIds, List<string> roles, string currentUserId);
    Task<bool> BulkDeleteUsersAsync(List<Guid> userIds, string currentUserId);

    // User Activity
    Task<List<AuditLog>> GetUserActivityAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<bool> LogUserActivityAsync(Guid userId, string action, string details, string ipAddress, string userAgent);
}
