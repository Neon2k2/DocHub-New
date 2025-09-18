using DocHub.Core.Entities;
using DocHub.Core.Interfaces;
using DocHub.Core.Interfaces.Repositories;
using DocHub.Shared.DTOs.Auth;
using DocHub.Shared.DTOs.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace DocHub.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IDbContext _dbContext;
    private readonly ISessionManagementService _sessionManagementService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthenticationService(
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<AuthenticationService> logger,
        IDbContext dbContext,
        ISessionManagementService sessionManagementService,
        IHttpContextAccessor httpContextAccessor)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
        _dbContext = dbContext;
        _sessionManagementService = sessionManagementService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Login attempt for: '{EmailOrUsername}'", request.EmailOrUsername);
            
            // Try to find user by email first, then by username
            _logger.LogInformation("🔍 [AUTH-LOGIN] Searching for user by email: '{EmailOrUsername}'", request.EmailOrUsername);
            var user = await _userRepository.GetByEmailAsync(request.EmailOrUsername);
            _logger.LogInformation("📧 [AUTH-LOGIN] User found by email: {Found}", user != null);
            if (user != null)
            {
                _logger.LogInformation("👤 [AUTH-LOGIN] Found user: Email='{Email}', Username='{Username}', IsActive={IsActive}", 
                    user.Email, user.Username, user.IsActive);
            }
            
            if (user == null)
            {
                _logger.LogInformation("🔍 [AUTH-LOGIN] Searching for user by username: '{EmailOrUsername}'", request.EmailOrUsername);
                user = await _userRepository.GetByUsernameAsync(request.EmailOrUsername);
                _logger.LogInformation("👤 [AUTH-LOGIN] User found by username: {Found}", user != null);
                if (user != null)
                {
                    _logger.LogInformation("👤 [AUTH-LOGIN] Found user: Email='{Email}', Username='{Username}', IsActive={IsActive}", 
                        user.Email, user.Username, user.IsActive);
                }
            }
            
            if (user == null)
            {
                _logger.LogWarning("❌ [AUTH-LOGIN] User not found for: {EmailOrUsername}", request.EmailOrUsername);
                throw new UnauthorizedAccessException("Invalid email or username, or password");
            }
            
            if (!user.IsActive)
            {
                _logger.LogWarning("❌ [AUTH-LOGIN] User found but inactive for: {EmailOrUsername}, IsActive: {IsActive}", request.EmailOrUsername, user.IsActive);
                throw new UnauthorizedAccessException("Invalid email or username, or password");
            }

            _logger.LogInformation("🔐 [AUTH-LOGIN] Verifying password for user: {Username}", user.Username);
            var passwordValid = VerifyPassword(request.Password, user.PasswordHash);
            _logger.LogInformation("🔐 [AUTH-LOGIN] Password verification result: {Valid}", passwordValid);
            
            if (!passwordValid)
            {
                _logger.LogWarning("❌ [AUTH-LOGIN] Password verification failed for user: {Username}", user.Username);
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            await _dbContext.SaveChangesAsync();

            // Prepare contextual info
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? string.Empty;
            var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString() ?? string.Empty;

            // Create persistent refresh token (DB)
            var refreshToken = GenerateRefreshToken(user.Id);
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _sessionManagementService.CreateRefreshTokenAsync(user.Id, refreshToken, refreshTokenExpiry);

            // Create session record and include SessionId in JWT
            var sessionToken = Guid.NewGuid().ToString("N");
            var session = await _sessionManagementService.CreateSessionAsync(
                user.Id,
                sessionToken,
                refreshToken,
                ipAddress,
                userAgent);

            var token = GenerateJwtToken(user, session.Id);

            return new LoginResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
                User = MapToUserDto(user),
                ModuleAccess = GetUserModuleAccess(user),
                Roles = GetUserRoles(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for: {EmailOrUsername}", request.EmailOrUsername);
            throw;
        }
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            // Validate refresh token against DB store
            var stored = await _sessionManagementService.GetRefreshTokenAsync(request.RefreshToken);
            if (stored == null || !stored.IsActive)
            {
                throw new UnauthorizedAccessException("Invalid or expired refresh token");
            }

            var user = await _userRepository.GetByIdAsync(stored.UserId);
            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedAccessException("User not found or inactive");
            }

            // Revoke old token and issue a new pair
            await _sessionManagementService.RevokeRefreshTokenAsync(stored.Id, "Rotated on refresh", stored.UserId.ToString());
            var newRefreshToken = GenerateRefreshToken(user.Id);
            var newRefreshExpiry = DateTime.UtcNow.AddDays(7);
            await _sessionManagementService.CreateRefreshTokenAsync(user.Id, newRefreshToken, newRefreshExpiry);

            var newJwt = GenerateJwtToken(user, null);

            _logger.LogInformation("Token refreshed successfully for user: {UserId}", user.Id);

            return new LoginResponse
            {
                Token = newJwt,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
                User = MapToUserDto(user),
                ModuleAccess = GetUserModuleAccess(user),
                Roles = GetUserRoles(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            throw;
        }
    }

    public async Task LogoutAsync(string userId)
    {
        try
        {
            _logger.LogInformation("User {UserId} logged out", userId);
            // Revoke all active refresh tokens and terminate other sessions
            await _sessionManagementService.RevokeAllUserRefreshTokensAsync(Guid.Parse(userId), "User logout", userId);
            await _sessionManagementService.TerminateAllUserSessionsAsync(Guid.Parse(userId), "User logout", userId, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user: {UserId}", userId);
            throw;
        }
        
        return;
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(GetJwtSecret());

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = GetJwtIssuer(),
                ValidateAudience = true,
                ValidAudience = GetJwtAudience(),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public async Task<DocHub.Shared.DTOs.Users.UserDto> GetUserAsync(string userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            return MapToUserDto(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user: {UserId}", userId);
            throw;
        }
    }

    public async Task<DocHub.Shared.DTOs.Users.UserDto> GetUserByEmailAsync(string email)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            return MapToUserDto(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email: {Email}", email);
            throw;
        }
    }

    public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            if (!VerifyPassword(currentPassword, user.PasswordHash))
            {
                return false;
            }

            user.PasswordHash = HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            await _dbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ResetPasswordAsync(string email)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal if user exists or not
                return true;
            }

            // In a real implementation, you would generate a reset token
            // and send it via email
            _logger.LogInformation("Password reset requested for email: {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for email: {Email}", email);
            throw;
        }
    }

    public Task<bool> ConfirmPasswordResetAsync(string email, string token, string newPassword)
    {
        try
        {
            // In a real implementation, you would validate the reset token
            // and update the password
            // For now, we'll return false indicating password reset is not implemented
            _logger.LogInformation("Password reset confirmation requested for email: {Email}", email);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming password reset for email: {Email}", email);
            throw;
        }
    }

    private string GenerateJwtToken(User user, Guid? sessionId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(GetJwtSecret());
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName)
        };

        // Add session id claim if available
        if (sessionId.HasValue)
        {
            claims.Add(new Claim("SessionId", sessionId.Value.ToString()));
        }

        // Add role claims for [Authorize(Roles=...)] support
        foreach (var role in GetUserRoles(user))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
            Issuer = GetJwtIssuer(),
            Audience = GetJwtAudience(),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateRefreshToken(Guid userId)
    {
        // Simple implementation: embed user ID and timestamp in the token
        // In production, you'd store refresh tokens in a database with proper security
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var tokenData = $"{userId}|{timestamp}";
        var tokenBytes = System.Text.Encoding.UTF8.GetBytes(tokenData);
        return Convert.ToBase64String(tokenBytes);
    }

    private string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    private DocHub.Shared.DTOs.Users.UserDto MapToUserDto(User user)
    {
        var roles = GetUserRoles(user);
        var isAdmin = roles.Contains("Admin");
        
        return new DocHub.Shared.DTOs.Users.UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Department = user.Department,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = roles,
            Permissions = new UserPermissionsDto
            {
                IsAdmin = isAdmin,
                CanAccessER = isAdmin || user.Department?.ToLower() == "er",
                CanAccessBilling = isAdmin || user.Department?.ToLower() == "billing",
                CanManageUsers = isAdmin,
                CanManageRoles = isAdmin,
                CanViewAuditLogs = isAdmin,
                CanManageSystemSettings = isAdmin
            }
        };
    }

    private List<string> GetUserModuleAccess(User user)
    {
        // For now, return all modules - in a real implementation, this would be based on user permissions
        return new List<string> { "ER", "Admin", "Billing" };
    }

    private List<string> GetUserRoles(User user)
    {
        return user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>();
    }

    private string GetJwtSecret()
    {
        return _configuration["Jwt:Secret"]
            ?? _configuration["Jwt:SecretKey"]
            ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
    }

    private string GetJwtIssuer()
    {
        return _configuration["Jwt:Issuer"] ?? "DocHub.API";
    }

    private string GetJwtAudience()
    {
        return _configuration["Jwt:Audience"] ?? "DocHub.Client";
    }

    private int GetTokenExpirationMinutes()
    {
        return int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");
    }
}
