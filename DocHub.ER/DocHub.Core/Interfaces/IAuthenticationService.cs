using DocHub.Shared.DTOs.Auth;
using DocHub.Shared.DTOs.Users;

namespace DocHub.Core.Interfaces;

public interface IAuthenticationService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task LogoutAsync(string userId);
    Task<bool> ValidateTokenAsync(string token);
    Task<DocHub.Shared.DTOs.Users.UserDto> GetUserAsync(string userId);
    Task<DocHub.Shared.DTOs.Users.UserDto> GetUserByEmailAsync(string email);
    Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<bool> ResetPasswordAsync(string email);
    Task<bool> ConfirmPasswordResetAsync(string email, string token, string newPassword);
}
