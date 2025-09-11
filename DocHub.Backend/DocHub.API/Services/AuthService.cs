using DocHub.API.Data;
using DocHub.API.DTOs;
using DocHub.API.Models;
using DocHub.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DocHub.API.Services;

public class AuthService : IAuthService
{
    private readonly DocHubDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(DocHubDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        try
        {
            // Try to find user by email or username
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => (u.Email == request.EmailOrUsername || u.Username == request.EmailOrUsername) && u.Status == "Active");

            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "Invalid email/username or password"
                };
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var moduleAccess = user.UserRoles.Select(ur => ur.ModuleAccess).Where(ma => !string.IsNullOrEmpty(ma)).Cast<string>().ToList();

            return new AuthResult
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                User = new UserSummary
                {
                    Id = user.Id.ToString(),
                    Username = user.Username,
                    Name = $"{user.FirstName} {user.LastName}".Trim(),
                    Email = user.Email,
                    Role = roles.FirstOrDefault() ?? "User",
                    Phone = user.Phone,
                    Status = user.Status,
                    IsEmailVerified = user.IsEmailVerified,
                    LastLogin = user.LastLoginAt,
                    CreatedAt = user.CreatedAt,
                    Roles = roles,
                    ModuleAccess = moduleAccess,
                    IsActive = user.Status == "Active"
                },
                Roles = roles,
                ModuleAccess = moduleAccess,
                ExpiresAt = DateTime.UtcNow.AddHours(8)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email/username: {EmailOrUsername}", request.EmailOrUsername);
            return new AuthResult
            {
                Success = false,
                Message = "An error occurred during login"
            };
        }
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email || u.Username == request.Username))
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "User with this email or username already exists"
                };
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Phone = request.Phone,
                Status = "Active",
                IsEmailVerified = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Assign default role (User)
            var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            if (userRole != null)
            {
                var userRoleAssignment = new UserRole
                {
                    UserId = user.Id,
                    RoleId = userRole.Id,
                    ModuleAccess = "ER" // Default access to ER module
                };
                _context.UserRoles.Add(userRoleAssignment);
                await _context.SaveChangesAsync();
            }

            var token = GenerateJwtToken(user);
            var roles = new List<string> { "User" };
            var moduleAccess = new List<string> { "ER" };

            return new AuthResult
            {
                Success = true,
                Token = token,
                User = new UserSummary
                {
                    Id = user.Id.ToString(),
                    Username = user.Username,
                    Name = $"{user.FirstName} {user.LastName}".Trim(),
                    Email = user.Email,
                    Role = roles.FirstOrDefault() ?? "User",
                    Phone = user.Phone,
                    Status = user.Status,
                    IsEmailVerified = user.IsEmailVerified,
                    CreatedAt = user.CreatedAt,
                    Roles = roles,
                    ModuleAccess = moduleAccess,
                    IsActive = user.Status == "Active"
                },
                Roles = roles,
                ModuleAccess = moduleAccess,
                ExpiresAt = DateTime.UtcNow.AddHours(8)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email: {Email}", request.Email);
            return new AuthResult
            {
                Success = false,
                Message = "An error occurred during registration"
            };
        }
    }

    public bool ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"] ?? "YourSecretKeyHere");
            
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<UserSummary?> GetUserFromTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "userid");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return null;

            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId && u.Status == "Active");

            if (user == null) return null;

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var moduleAccess = user.UserRoles.Select(ur => ur.ModuleAccess).Where(ma => !string.IsNullOrEmpty(ma)).Cast<string>().ToList();

            return new UserSummary
            {
                Id = user.Id.ToString(),
                Username = user.Username,
                Name = $"{user.FirstName} {user.LastName}".Trim(),
                Email = user.Email,
                Role = roles.FirstOrDefault() ?? "User",
                Phone = user.Phone,
                Status = user.Status,
                IsEmailVerified = user.IsEmailVerified,
                LastLogin = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
                Roles = roles,
                ModuleAccess = moduleAccess,
                IsActive = user.Status == "Active"
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<string>> GetUserRolesAsync(Guid userId)
    {
        return await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId && ur.Status == "Active")
            .Select(ur => ur.Role.Name)
            .ToListAsync();
    }

    public async Task<List<string>> GetUserModuleAccessAsync(Guid userId)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId && ur.Status == "Active" && !string.IsNullOrEmpty(ur.ModuleAccess))
            .Select(ur => ur.ModuleAccess!)
            .ToListAsync();
    }

    public async Task<bool> HasModuleAccessAsync(Guid userId, string module)
    {
        return await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && 
                           ur.Status == "Active" && 
                           (ur.ModuleAccess == module || ur.ModuleAccess == "Admin"));
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !VerifyPassword(request.CurrentPassword, user.PasswordHash))
                return false;

            user.PasswordHash = HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(string email)
    {
        // TODO: Implement password reset logic
        return await Task.FromResult(false);
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        // TODO: Implement email verification logic
        return await Task.FromResult(false);
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"] ?? "YourSecretKeyHere");
        
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var moduleAccess = user.UserRoles.Select(ur => ur.ModuleAccess).Where(ma => !string.IsNullOrEmpty(ma)).ToList();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("userid", user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName)
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add module access claims
        foreach (var module in moduleAccess)
        {
            claims.Add(new Claim("module", module ?? string.Empty));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(8),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
