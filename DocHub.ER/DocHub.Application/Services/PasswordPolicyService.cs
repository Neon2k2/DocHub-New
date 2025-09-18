using DocHub.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DocHub.Application.Services;

public class PasswordPolicyService : IPasswordPolicyService
{
    private readonly ILogger<PasswordPolicyService> _logger;

    // Password policy configuration - Relaxed for better user experience
    private const int MinLength = 6;  // Reduced from 8
    private const int MaxLength = 128;
    private const bool RequireUppercase = false;  // Made optional
    private const bool RequireLowercase = true;   // Keep this
    private const bool RequireDigit = true;       // Keep this
    private const bool RequireSpecialChar = false; // Made optional
    private const int MinUniqueChars = 3;         // Reduced from 4
    private const bool PreventCommonPasswords = false; // Disabled
    private const bool PreventUserInfo = false;   // Disabled

    // Common weak passwords
    private static readonly string[] CommonPasswords = {
        "password", "123456", "123456789", "qwerty", "abc123", "password123",
        "admin", "letmein", "welcome", "monkey", "1234567890", "dragon",
        "master", "hello", "freedom", "whatever", "qazwsx", "trustno1"
    };

    public PasswordPolicyService(ILogger<PasswordPolicyService> logger)
    {
        _logger = logger;
    }

    public PasswordValidationResult ValidatePassword(string password, string? username = null, string? email = null)
    {
        var result = new PasswordValidationResult();
        var errors = new List<string>();

        // Basic length validation
        if (string.IsNullOrEmpty(password))
        {
            errors.Add("Password is required");
            result.IsValid = false;
            result.Strength = PasswordStrength.VeryWeak;
            result.Errors = errors;
            return result;
        }

        if (password.Length < MinLength)
        {
            errors.Add($"Password must be at least {MinLength} characters long");
        }

        if (password.Length > MaxLength)
        {
            errors.Add($"Password must be no more than {MaxLength} characters long");
        }

        // Character type validation
        if (RequireUppercase && !password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase letter");
        }

        if (RequireLowercase && !password.Any(char.IsLower))
        {
            errors.Add("Password must contain at least one lowercase letter");
        }

        if (RequireDigit && !password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one digit");
        }

        if (RequireSpecialChar && !HasSpecialCharacter(password))
        {
            errors.Add("Password must contain at least one special character (!@#$%^&*)");
        }

        // Unique character validation
        var uniqueChars = password.Distinct().Count();
        if (uniqueChars < MinUniqueChars)
        {
            errors.Add($"Password must contain at least {MinUniqueChars} unique characters");
        }

        // Common password validation
        if (PreventCommonPasswords && IsCommonPassword(password))
        {
            errors.Add("Password is too common and easily guessable");
        }

        // User information validation
        if (PreventUserInfo && !string.IsNullOrEmpty(username) && password.ToLower().Contains(username.ToLower()))
        {
            errors.Add("Password cannot contain your username");
        }

        if (PreventUserInfo && !string.IsNullOrEmpty(email))
        {
            var emailUsername = email.Split('@')[0];
            if (password.ToLower().Contains(emailUsername.ToLower()))
            {
                errors.Add("Password cannot contain your email username");
            }
        }

        // Consecutive character validation - Disabled for better UX
        // if (HasConsecutiveCharacters(password))
        // {
        //     errors.Add("Password cannot contain more than 2 consecutive identical characters");
        // }

        // Sequential character validation - Disabled for better UX
        // if (HasSequentialCharacters(password))
        // {
        //     errors.Add("Password cannot contain sequential characters (abc, 123, etc.)");
        // }

        // Calculate password strength
        result.Strength = GetPasswordStrength(password);
        result.Score = CalculatePasswordScore(password);

        // Set validation result
        result.IsValid = errors.Count == 0;
        result.Errors = errors;

        return result;
    }

    public PasswordStrength GetPasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
            return PasswordStrength.VeryWeak;

        int score = CalculatePasswordScore(password);

        return score switch
        {
            < 20 => PasswordStrength.VeryWeak,
            < 40 => PasswordStrength.Weak,
            < 60 => PasswordStrength.Fair,
            < 80 => PasswordStrength.Good,
            < 90 => PasswordStrength.Strong,
            _ => PasswordStrength.VeryStrong
        };
    }

    public List<string> GetPasswordRequirements()
    {
        var requirements = new List<string>
        {
            $"At least {MinLength} characters long",
            $"No more than {MaxLength} characters long",
            $"At least {MinUniqueChars} unique characters"
        };

        // Only add requirements that are actually enabled
        // if (RequireUppercase) // Disabled for better UX
        //     requirements.Add("At least one uppercase letter (A-Z)");

        if (RequireLowercase)
            requirements.Add("At least one lowercase letter (a-z)");

        if (RequireDigit)
            requirements.Add("At least one digit (0-9)");

        // if (RequireSpecialChar) // Disabled for better UX
        //     requirements.Add("At least one special character (!@#$%^&*)");

        // requirements.Add("Cannot contain your username or email"); // Disabled for better UX
        // requirements.Add("Cannot contain common passwords"); // Disabled for better UX
        // requirements.Add("Cannot contain more than 2 consecutive identical characters"); // Disabled for better UX
        // requirements.Add("Cannot contain sequential characters (abc, 123, etc.)"); // Disabled for better UX

        return requirements;
    }

    public bool IsPasswordExpired(DateTime? passwordChangedAt, int maxAgeDays = 90)
    {
        if (!passwordChangedAt.HasValue)
            return true; // Never changed, consider expired

        return DateTime.UtcNow > passwordChangedAt.Value.AddDays(maxAgeDays);
    }

    public int GetDaysUntilPasswordExpiry(DateTime? passwordChangedAt, int maxAgeDays = 90)
    {
        if (!passwordChangedAt.HasValue)
            return 0; // Already expired

        var expiryDate = passwordChangedAt.Value.AddDays(maxAgeDays);
        var daysUntilExpiry = (expiryDate - DateTime.UtcNow).Days;

        return Math.Max(0, daysUntilExpiry);
    }

    private bool HasSpecialCharacter(string password)
    {
        return password.Any(c => !char.IsLetterOrDigit(c));
    }

    private bool IsCommonPassword(string password)
    {
        var lowerPassword = password.ToLower();
        return CommonPasswords.Any(common => lowerPassword.Contains(common));
    }

    private bool HasConsecutiveCharacters(string password)
    {
        for (int i = 0; i < password.Length - 2; i++)
        {
            if (password[i] == password[i + 1] && password[i + 1] == password[i + 2])
            {
                return true;
            }
        }
        return false;
    }

    private bool HasSequentialCharacters(string password)
    {
        for (int i = 0; i < password.Length - 2; i++)
        {
            // Check for ascending sequences (abc, 123, etc.)
            if (IsSequential(password[i], password[i + 1], password[i + 2], 1))
            {
                return true;
            }
            // Check for descending sequences (cba, 321, etc.)
            if (IsSequential(password[i], password[i + 1], password[i + 2], -1))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsSequential(char c1, char c2, char c3, int direction)
    {
        return (c2 == c1 + direction) && (c3 == c2 + direction);
    }

    private int CalculatePasswordScore(string password)
    {
        int score = 0;

        // Length score
        score += Math.Min(password.Length * 2, 25);

        // Character type diversity
        if (password.Any(char.IsLower)) score += 5;
        if (password.Any(char.IsUpper)) score += 5;
        if (password.Any(char.IsDigit)) score += 5;
        if (HasSpecialCharacter(password)) score += 10;

        // Unique character bonus
        var uniqueChars = password.Distinct().Count();
        score += Math.Min(uniqueChars * 2, 20);

        // Complexity bonus
        if (password.Length >= 12) score += 10;
        if (password.Length >= 16) score += 10;

        // Pattern penalties
        if (HasConsecutiveCharacters(password)) score -= 10;
        if (HasSequentialCharacters(password)) score -= 10;
        if (IsCommonPassword(password)) score -= 20;

        // User info penalty
        // Note: This would need username/email context to implement properly

        return Math.Max(0, Math.Min(100, score));
    }
}
