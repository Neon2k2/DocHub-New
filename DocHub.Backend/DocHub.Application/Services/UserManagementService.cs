using DocHub.Core.Interfaces;
using DocHub.Core.Entities;
using DocHub.Core.Interfaces.Repositories;
using DocHub.Shared.DTOs.Common;
using DocHub.Shared.DTOs.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace DocHub.Application.Services;

public class UserManagementService : IUserManagementService
{
    private readonly IDbContext _dbContext;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordPolicyService _passwordPolicyService;
    private readonly ILogger<UserManagementService> _logger;
    private readonly ICacheService _cacheService;

    public UserManagementService(IDbContext dbContext, IUserRepository userRepository, IPasswordPolicyService passwordPolicyService, ILogger<UserManagementService> logger, ICacheService cacheService)
    {
        _dbContext = dbContext;
        _userRepository = userRepository;
        _passwordPolicyService = passwordPolicyService;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<PaginatedResponse<UserDto>> GetUsersAsync(GetUsersRequest request)
    {
        try
        {
            // Create cache key based on request parameters
            var cacheKey = $"users:{request.Page}:{request.PageSize}:{request.SearchTerm}:{request.Role}:{request.Department}:{request.IsActive}:{request.SortBy}:{request.SortDirection}";
            
            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                var query = _dbContext.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.ToLower();
                    query = query.Where(u => 
                        u.Username.ToLower().Contains(searchTerm) ||
                        u.Email.ToLower().Contains(searchTerm) ||
                        u.FirstName.ToLower().Contains(searchTerm) ||
                        u.LastName.ToLower().Contains(searchTerm) ||
                        (u.Department != null && u.Department.ToLower().Contains(searchTerm)) ||
                        (u.EmployeeId != null && u.EmployeeId.ToLower().Contains(searchTerm))
                    );
                }

                if (!string.IsNullOrEmpty(request.Role))
                {
                    query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == request.Role));
                }

                if (!string.IsNullOrEmpty(request.Department))
                {
                    query = query.Where(u => u.Department == request.Department);
                }

                if (request.IsActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == request.IsActive.Value);
                }

                // Apply sorting
                query = request.SortBy?.ToLower() switch
                {
                    "username" => request.SortDirection == "asc" ? query.OrderBy(u => u.Username) : query.OrderByDescending(u => u.Username),
                    "email" => request.SortDirection == "asc" ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email),
                    "firstname" => request.SortDirection == "asc" ? query.OrderBy(u => u.FirstName) : query.OrderByDescending(u => u.FirstName),
                    "lastname" => request.SortDirection == "asc" ? query.OrderBy(u => u.LastName) : query.OrderByDescending(u => u.LastName),
                    "department" => request.SortDirection == "asc" ? query.OrderBy(u => u.Department) : query.OrderByDescending(u => u.Department),
                    "lastlogin" => request.SortDirection == "asc" ? query.OrderBy(u => u.LastLoginAt) : query.OrderByDescending(u => u.LastLoginAt),
                    _ => request.SortDirection == "asc" ? query.OrderBy(u => u.CreatedAt) : query.OrderByDescending(u => u.CreatedAt)
                };

                var totalCount = await query.CountAsync();
                var users = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(u => MapToUserDto(u))
                    .ToListAsync();

                return new PaginatedResponse<UserDto>
                {
                    Items = users,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
                };
            }, TimeSpan.FromMinutes(5)); // Cache for 5 minutes
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            throw;
        }
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        try
        {
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user != null ? MapToUserDto(user) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, string currentUserId)
    {
        try
        {
            // Check if username already exists
            if (await _userRepository.GetByUsernameAsync(request.Username) != null)
            {
                throw new ArgumentException("Username already exists");
            }

            // Check if email already exists
            if (await _userRepository.GetByEmailAsync(request.Email) != null)
            {
                throw new ArgumentException("Email already exists");
            }

            // Validate password against policy
            var passwordValidation = _passwordPolicyService.ValidatePassword(request.Password, request.Username, request.Email);
            if (!passwordValidation.IsValid)
            {
                throw new ArgumentException($"Password does not meet requirements: {string.Join(", ", passwordValidation.Errors)}");
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                Department = request.Department ?? string.Empty,
                EmployeeId = request.EmployeeId,
                JobTitle = request.JobTitle,
                Address = request.Address,
                City = request.City,
                State = request.State,
                ZipCode = request.ZipCode,
                Country = request.Country,
                IsActive = request.IsActive,
                Notes = request.Notes,
                PasswordHash = HashPassword(request.Password),
                PasswordChangedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Assign roles
            if (request.Roles.Any())
            {
                await AssignRolesToUserAsync(user.Id, request.Roles);
            }

            // Invalidate user-related caches
            _cacheService.RemovePattern("users:*");
            _cacheService.RemovePattern("stats:*");

            // Log activity
            await LogUserActivityAsync(user.Id, "USER_CREATED", $"User created by {currentUserId}", "", "");

            return MapToUserDto(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            throw;
        }
    }

    public async Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserRequest request, string currentUserId)
    {
        try
        {
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            // Update fields if provided
            if (!string.IsNullOrEmpty(request.FirstName))
                user.FirstName = request.FirstName;

            if (!string.IsNullOrEmpty(request.LastName))
                user.LastName = request.LastName;

            if (!string.IsNullOrEmpty(request.Email))
            {
                // Check if email is already taken by another user
                var existingUser = await _userRepository.GetByEmailAsync(request.Email);
                if (existingUser != null && existingUser.Id != userId)
                {
                    throw new ArgumentException("Email already exists");
                }
                user.Email = request.Email;
            }

            if (request.PhoneNumber != null)
                user.PhoneNumber = request.PhoneNumber;

            if (request.Department != null)
                user.Department = request.Department;

            if (request.EmployeeId != null)
                user.EmployeeId = request.EmployeeId;

            if (request.JobTitle != null)
                user.JobTitle = request.JobTitle;

            if (request.Address != null)
                user.Address = request.Address;

            if (request.City != null)
                user.City = request.City;

            if (request.State != null)
                user.State = request.State;

            if (request.ZipCode != null)
                user.ZipCode = request.ZipCode;

            if (request.Country != null)
                user.Country = request.Country;

            if (request.IsActive.HasValue)
                user.IsActive = request.IsActive.Value;

            if (request.Notes != null)
                user.Notes = request.Notes;

            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            // Update roles if provided
            if (request.Roles != null)
            {
                await UpdateUserRolesAsync(userId, request.Roles);
            }

            await _dbContext.SaveChangesAsync();

            // Log activity
            await LogUserActivityAsync(userId, "USER_UPDATED", $"User updated by {currentUserId}", "", "");

            return MapToUserDto(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> DeleteUserAsync(Guid userId, string currentUserId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Soft delete - just deactivate
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _dbContext.SaveChangesAsync();

            // Log activity
            await LogUserActivityAsync(userId, "USER_DELETED", $"User deleted by {currentUserId}", "", "");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ToggleUserStatusAsync(Guid userId, bool isActive, string currentUserId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _dbContext.SaveChangesAsync();

            // Log activity
            var action = isActive ? "USER_ACTIVATED" : "USER_DEACTIVATED";
            await LogUserActivityAsync(userId, action, $"User {(isActive ? "activated" : "deactivated")} by {currentUserId}", "", "");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user status: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, string currentUserId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Verify current password
            if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Current password is incorrect");
            }

            // Validate new password
            if (request.NewPassword != request.ConfirmPassword)
            {
                throw new ArgumentException("New password and confirm password do not match");
            }

            // Validate password against policy
            var passwordValidation = _passwordPolicyService.ValidatePassword(request.NewPassword, user.Username, user.Email);
            if (!passwordValidation.IsValid)
            {
                throw new ArgumentException($"Password does not meet requirements: {string.Join(", ", passwordValidation.Errors)}");
            }

            user.PasswordHash = HashPassword(request.NewPassword);
            user.PasswordChangedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _dbContext.SaveChangesAsync();

            // Log activity
            await LogUserActivityAsync(userId, "PASSWORD_CHANGED", "Password changed", "", "");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                // Don't reveal if email exists or not
                return true;
            }

            // Generate reset token (simplified - in production, use proper token generation)
            var resetToken = GenerateSecureToken();
            
            // Store reset token in user notes temporarily (in production, use a proper reset token table)
            user.Notes = $"RESET_TOKEN:{resetToken}:{DateTime.UtcNow.AddHours(1):O}";
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _dbContext.SaveChangesAsync();

            // TODO: Send email with reset token
            _logger.LogInformation("Password reset token generated for user: {Email}", request.Email);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for email: {Email}", request.Email);
            throw;
        }
    }

    public async Task<bool> ConfirmPasswordResetAsync(ResetPasswordConfirmRequest request)
    {
        try
        {
            // Find user with reset token
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Notes != null && u.Notes.Contains($"RESET_TOKEN:{request.Token}:"));

            if (user == null)
            {
                throw new ArgumentException("Invalid or expired reset token");
            }

            // Extract token and expiry from notes
            var tokenParts = user.Notes?.Split(':');
            if (tokenParts == null || tokenParts.Length < 3 || !DateTime.TryParse(tokenParts[2], out var expiry) || DateTime.UtcNow > expiry)
            {
                throw new ArgumentException("Invalid or expired reset token");
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                throw new ArgumentException("New password and confirm password do not match");
            }

            // Validate password against policy
            var passwordValidation = _passwordPolicyService.ValidatePassword(request.NewPassword, user.Username, user.Email);
            if (!passwordValidation.IsValid)
            {
                throw new ArgumentException($"Password does not meet requirements: {string.Join(", ", passwordValidation.Errors)}");
            }

            user.PasswordHash = HashPassword(request.NewPassword);
            user.PasswordChangedAt = DateTime.UtcNow;
            user.Notes = user.Notes?.Replace($"RESET_TOKEN:{request.Token}:{tokenParts[2]}", "") ?? string.Empty;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _dbContext.SaveChangesAsync();

            // Log activity
            await LogUserActivityAsync(user.Id, "PASSWORD_RESET", "Password reset via token", "", "");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming password reset");
            throw;
        }
    }

    public async Task<bool> ForcePasswordChangeAsync(Guid userId, string newPassword, string currentUserId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.PasswordHash = HashPassword(newPassword);
            user.PasswordChangedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _dbContext.SaveChangesAsync();

            // Log activity
            await LogUserActivityAsync(userId, "PASSWORD_FORCE_CHANGED", $"Password force changed by {currentUserId}", "", "");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error force changing password for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<UserStatsDto> GetUserStatsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var last24Hours = now.AddHours(-24);

            var totalUsers = await _dbContext.Users.CountAsync();
            var activeUsers = await _dbContext.Users.CountAsync(u => u.IsActive);
            var inactiveUsers = totalUsers - activeUsers;
            var newUsersThisMonth = await _dbContext.Users.CountAsync(u => u.CreatedAt >= startOfMonth);
            var recentLogins = await _dbContext.Users.CountAsync(u => u.LastLoginAt >= last24Hours);

            var usersByRole = await _dbContext.UserRoles
                .Include(ur => ur.Role)
                .GroupBy(ur => ur.Role.Name)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            var usersByDepartment = await _dbContext.Users
                .Where(u => !string.IsNullOrEmpty(u.Department))
                .GroupBy(u => u.Department!)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            return new UserStatsDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                InactiveUsers = inactiveUsers,
                NewUsersThisMonth = newUsersThisMonth,
                UsersByRole = usersByRole,
                UsersByDepartment = usersByDepartment,
                RecentLogins = recentLogins
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user stats");
            throw;
        }
    }

    public async Task<List<UserDto>> GetRecentUsersAsync(int count = 10)
    {
        try
        {
            var users = await _dbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .OrderByDescending(u => u.CreatedAt)
                .Take(count)
                .Select(u => MapToUserDto(u))
                .ToListAsync();

            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent users");
            throw;
        }
    }

    public async Task<List<UserDto>> GetUsersByRoleAsync(string role)
    {
        try
        {
            var users = await _dbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == role))
                .Select(u => MapToUserDto(u))
                .ToListAsync();

            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users by role: {Role}", role);
            throw;
        }
    }

    public async Task<List<UserDto>> GetUsersByDepartmentAsync(string department)
    {
        try
        {
            var users = await _dbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.Department == department)
                .Select(u => MapToUserDto(u))
                .ToListAsync();

            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users by department: {Department}", department);
            throw;
        }
    }

    public async Task<List<UserDto>> SearchUsersAsync(string searchTerm, int limit = 20)
    {
        try
        {
            var searchLower = searchTerm.ToLower();
            var users = await _dbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => 
                    u.Username.ToLower().Contains(searchLower) ||
                    u.Email.ToLower().Contains(searchLower) ||
                    u.FirstName.ToLower().Contains(searchLower) ||
                    u.LastName.ToLower().Contains(searchLower) ||
                    (u.Department != null && u.Department.ToLower().Contains(searchLower)) ||
                    (u.EmployeeId != null && u.EmployeeId.ToLower().Contains(searchLower))
                )
                .Take(limit)
                .Select(u => MapToUserDto(u))
                .ToListAsync();

            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users: {SearchTerm}", searchTerm);
            throw;
        }
    }

    public async Task<List<UserDto>> GetInactiveUsersAsync(int daysSinceLastLogin = 30)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysSinceLastLogin);
            var users = await _dbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.LastLoginAt == null || u.LastLoginAt < cutoffDate)
                .Select(u => MapToUserDto(u))
                .ToListAsync();

            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inactive users");
            throw;
        }
    }

    public async Task<List<UserDto>> GetUsersCreatedInDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var users = await _dbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                .Select(u => MapToUserDto(u))
                .ToListAsync();

            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users created in date range");
            throw;
        }
    }

    public async Task<bool> LockUserAccountAsync(Guid userId, DateTime? lockUntil, string reason, string currentUserId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.AccountLockedUntil = lockUntil;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _dbContext.SaveChangesAsync();

            // Log activity
            await LogUserActivityAsync(userId, "ACCOUNT_LOCKED", $"Account locked by {currentUserId}. Reason: {reason}", "", "");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking user account: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> UnlockUserAccountAsync(Guid userId, string currentUserId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.AccountLockedUntil = null;
            user.FailedLoginAttempts = 0;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _dbContext.SaveChangesAsync();

            // Log activity
            await LogUserActivityAsync(userId, "ACCOUNT_UNLOCKED", $"Account unlocked by {currentUserId}", "", "");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking user account: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ResetFailedLoginAttemptsAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.FailedLoginAttempts = 0;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _dbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting failed login attempts for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> IsAccountLockedAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            return user.AccountLockedUntil.HasValue && user.AccountLockedUntil > DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if account is locked: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> UpdateProfileImageAsync(Guid userId, string imageUrl, string currentUserId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.ProfileImageUrl = imageUrl;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _dbContext.SaveChangesAsync();

            // Log activity
            await LogUserActivityAsync(userId, "PROFILE_IMAGE_UPDATED", "Profile image updated", "", "");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile image for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> DeleteProfileImageAsync(Guid userId, string currentUserId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.ProfileImageUrl = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _dbContext.SaveChangesAsync();

            // Log activity
            await LogUserActivityAsync(userId, "PROFILE_IMAGE_DELETED", "Profile image deleted", "", "");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile image for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> UpdateUserNotesAsync(Guid userId, string notes, string currentUserId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.Notes = notes;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _dbContext.SaveChangesAsync();

            // Log activity
            await LogUserActivityAsync(userId, "USER_NOTES_UPDATED", $"Notes updated by {currentUserId}", "", "");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user notes for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> BulkUpdateUserStatusAsync(List<Guid> userIds, bool isActive, string currentUserId)
    {
        try
        {
            var users = await _dbContext.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            foreach (var user in users)
            {
                user.IsActive = isActive;
                user.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            // Log activity
            var action = isActive ? "BULK_USER_ACTIVATED" : "BULK_USER_DEACTIVATED";
            await LogUserActivityAsync(Guid.Empty, action, $"Bulk {(isActive ? "activated" : "deactivated")} {userIds.Count} users by {currentUserId}", "", "");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk updating user status");
            throw;
        }
    }

    public async Task<bool> BulkAssignRolesAsync(List<Guid> userIds, List<string> roles, string currentUserId)
    {
        try
        {
            foreach (var userId in userIds)
            {
                await AssignRolesToUserAsync(userId, roles);
            }

            // Log activity
            await LogUserActivityAsync(Guid.Empty, "BULK_ROLES_ASSIGNED", $"Bulk assigned roles to {userIds.Count} users by {currentUserId}", "", "");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk assigning roles");
            throw;
        }
    }

    public async Task<bool> BulkDeleteUsersAsync(List<Guid> userIds, string currentUserId)
    {
        try
        {
            var users = await _dbContext.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            foreach (var user in users)
            {
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            // Log activity
            await LogUserActivityAsync(Guid.Empty, "BULK_USERS_DELETED", $"Bulk deleted {userIds.Count} users by {currentUserId}", "", "");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk deleting users");
            throw;
        }
    }

    public async Task<List<AuditLog>> GetUserActivityAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        try
        {
            var activities = await _dbContext.AuditLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return activities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user activity: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> LogUserActivityAsync(Guid userId, string action, string details, string ipAddress, string userAgent)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                Details = details,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.AuditLogs.AddAsync(auditLog);
            await _dbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging user activity: {UserId}", userId);
            return false;
        }
    }

    // Helper methods
    private async Task AssignRolesToUserAsync(Guid userId, List<string> roleNames)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return;

        // Remove existing roles
        var existingRoles = user.UserRoles.ToList();
        foreach (var role in existingRoles)
        {
            _dbContext.UserRoles.Remove(role);
        }

        // Add new roles
        foreach (var roleName in roleNames)
        {
            var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role != null)
            {
                user.UserRoles.Add(new UserRole
                {
                    UserId = userId,
                    RoleId = role.Id,
                    AssignedAt = DateTime.UtcNow
                });
            }
        }
    }

    private async Task UpdateUserRolesAsync(Guid userId, List<string> roleNames)
    {
        await AssignRolesToUserAsync(userId, roleNames);
        await _dbContext.SaveChangesAsync();
    }

    private UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Department = user.Department,
            EmployeeId = user.EmployeeId,
            JobTitle = user.JobTitle,
            Address = user.Address,
            City = user.City,
            State = user.State,
            ZipCode = user.ZipCode,
            Country = user.Country,
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            IsPhoneVerified = user.IsPhoneVerified,
            ProfileImageUrl = user.ProfileImageUrl,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt,
            PasswordChangedAt = user.PasswordChangedAt,
            EmailVerifiedAt = user.EmailVerifiedAt,
            PhoneVerifiedAt = user.PhoneVerifiedAt,
            Roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>(),
            Permissions = new UserPermissionsDto
            {
                IsAdmin = user.UserRoles?.Any(ur => ur.Role.Name == "Admin") ?? false,
                CanAccessER = user.UserRoles?.Any(ur => ur.Role.Name == "Admin" || ur.Role.Name == "ER") ?? false,
                CanAccessBilling = user.UserRoles?.Any(ur => ur.Role.Name == "Admin" || ur.Role.Name == "Billing") ?? false,
                CanManageUsers = user.UserRoles?.Any(ur => ur.Role.Name == "Admin") ?? false,
                CanManageRoles = user.UserRoles?.Any(ur => ur.Role.Name == "Admin") ?? false,
                CanViewAuditLogs = user.UserRoles?.Any(ur => ur.Role.Name == "Admin") ?? false,
                CanManageSystemSettings = user.UserRoles?.Any(ur => ur.Role.Name == "Admin") ?? false
            }
        };
    }

    private string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    private string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    // Placeholder methods for verification (to be implemented)
    public async Task<bool> VerifyEmailAsync(Guid userId, string token)
    {
        // TODO: Implement email verification
        return await Task.FromResult(true);
    }

    public async Task<bool> VerifyPhoneAsync(Guid userId, string token)
    {
        // TODO: Implement phone verification
        return await Task.FromResult(true);
    }

    public async Task<string> GenerateEmailVerificationTokenAsync(Guid userId)
    {
        // TODO: Implement email verification token generation
        return await Task.FromResult("email-verification-token");
    }

    public async Task<string> GeneratePhoneVerificationTokenAsync(Guid userId)
    {
        // TODO: Implement phone verification token generation
        return await Task.FromResult("phone-verification-token");
    }
}
