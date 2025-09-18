using DocHub.Shared.DTOs.Users;

namespace DocHub.Core.Interfaces;

public interface IPasswordPolicyService
{
    PasswordValidationResult ValidatePassword(string password, string? username = null, string? email = null);
    PasswordStrength GetPasswordStrength(string password);
    List<string> GetPasswordRequirements();
    bool IsPasswordExpired(DateTime? passwordChangedAt, int maxAgeDays = 90);
    int GetDaysUntilPasswordExpiry(DateTime? passwordChangedAt, int maxAgeDays = 90);
}

public class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public PasswordStrength Strength { get; set; }
    public int Score { get; set; }
}

public enum PasswordStrength
{
    VeryWeak = 0,
    Weak = 1,
    Fair = 2,
    Good = 3,
    Strong = 4,
    VeryStrong = 5
}
