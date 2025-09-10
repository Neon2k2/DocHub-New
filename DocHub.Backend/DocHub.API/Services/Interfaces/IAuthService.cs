using DocHub.API.DTOs;

namespace DocHub.API.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    bool ValidateToken(string token);
    Task<UserSummary?> GetUserFromTokenAsync(string token);
    Task<List<string>> GetUserRolesAsync(Guid userId);
    Task<List<string>> GetUserModuleAccessAsync(Guid userId);
    Task<bool> HasModuleAccessAsync(Guid userId, string module);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<bool> ResetPasswordAsync(string email);
    Task<bool> VerifyEmailAsync(string token);
}
