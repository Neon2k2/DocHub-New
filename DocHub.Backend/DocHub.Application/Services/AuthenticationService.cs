using DocHub.Core.Entities;
using DocHub.Core.Interfaces;
using DocHub.Core.Interfaces.Repositories;
using DocHub.Shared.DTOs.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DocHub.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IDbContext _dbContext;

    public AuthenticationService(
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<AuthenticationService> logger,
        IDbContext dbContext)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
        _dbContext = dbContext;
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

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken(user.Id);

            // Store refresh token (you might want to store this in a separate table)
            // For now, we'll include it in the response

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

            // Simple implementation: decode the refresh token to extract user ID
            // In production, you'd store refresh tokens in a database with expiration dates
            
            try
            {
                // Decode the base64 refresh token
                var tokenBytes = Convert.FromBase64String(request.RefreshToken);
                var tokenData = System.Text.Encoding.UTF8.GetString(tokenBytes);
                
                // For this implementation, we'll embed the user ID in the refresh token
                // Format: "userId|timestamp" - this is NOT secure for production!
                var parts = tokenData.Split('|');
                if (parts.Length != 2 || !Guid.TryParse(parts[0], out var userId))
                {
                    throw new UnauthorizedAccessException("Invalid refresh token format");
                }
                
                // Check if the refresh token is not too old (7 days max)
                if (long.TryParse(parts[1], out var timestamp))
                {
                    var tokenDate = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                    if (DateTime.UtcNow.Subtract(tokenDate).TotalDays > 7)
                    {
                        throw new UnauthorizedAccessException("Refresh token expired");
                    }
                }
                else
                {
                    throw new UnauthorizedAccessException("Invalid refresh token timestamp");
                }
                
                // Get the user from database
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null || !user.IsActive)
                {
                    throw new UnauthorizedAccessException("User not found or inactive");
                }
                
                // Generate new tokens
                var newToken = GenerateJwtToken(user);
                var newRefreshToken = GenerateRefreshToken(user.Id);
                
                _logger.LogInformation("Token refreshed successfully for user: {UserId}", userId);
                
                return new LoginResponse
                {
                    Token = newToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
                    User = MapToUserDto(user),
                    ModuleAccess = GetUserModuleAccess(user),
                    Roles = GetUserRoles(user)
                };
            }
            catch (FormatException)
            {
                throw new UnauthorizedAccessException("Invalid refresh token format");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            throw;
        }
    }

    public Task LogoutAsync(string userId)
    {
        try
        {
            // In a real implementation, you would invalidate the refresh token
            // and potentially add the access token to a blacklist
            _logger.LogInformation("User {UserId} logged out", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user: {UserId}", userId);
            throw;
        }
        
        return Task.CompletedTask;
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

    public async Task<UserDto> GetUserAsync(string userId)
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

    public async Task<UserDto> GetUserByEmailAsync(string email)
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

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(GetJwtSecret());
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("firstName", user.FirstName),
                new Claim("lastName", user.LastName)
            }),
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

    private UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = GetUserRoles(user)
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
        return _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
    }

    private string GetJwtIssuer()
    {
        return _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
    }

    private string GetJwtAudience()
    {
        return _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");
    }

    private int GetTokenExpirationMinutes()
    {
        return int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");
    }
}
