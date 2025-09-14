using DocHub.Shared.DTOs.Auth;

namespace DocHub.Core.Interfaces;

public interface IAuthenticationService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task LogoutAsync(string userId);
    Task<bool> ValidateTokenAsync(string token);
    Task<UserDto> GetUserAsync(string userId);
    Task<UserDto> GetUserByEmailAsync(string email);
    Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<bool> ResetPasswordAsync(string email);
    Task<bool> ConfirmPasswordResetAsync(string email, string token, string newPassword);
}
