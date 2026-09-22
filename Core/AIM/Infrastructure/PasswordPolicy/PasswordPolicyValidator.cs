namespace UBIS.Services.Aim.Infrastructure.PasswordPolicy;

using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using UBIS.Services.Aim.Application.Interfaces;
using UBIS.Services.Aim.Infrastructure.Persistence;

public class PasswordPolicyOptions
{
    /// <summary>Client decision 2026-08-21: 8 characters minimum, combined with mandatory
    /// upper/lower/special-character complexity below — a QA tester found an all-numeric
    /// password was being accepted, which this raises alongside the new complexity checks.</summary>
    public int MinLength { get; set; } = 8;
    public int RecommendedLength { get; set; } = 15;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireSpecialChar { get; set; } = true;
    public int BlockSequentialDigitsMinRun { get; set; } = 4;
    public List<string> BlockedTerms { get; set; } = new List<string> { "password", "admin123", "welcome1", "qwerty", "changeme" };

    /// <summary>
    /// Plaintext default password assigned to migrated/newly-provisioned accounts. Matches the
    /// real value found in all 2,468 rows of the DBA's <c>M_Users</c> export exactly (case-
    /// sensitive compare) — see <c>db-scripts/M_Users_HashDefaultPassword.sql</c>.
    /// A login using this exact password forces <see cref="Domain.Entities.User.PasswordResetRequired"/>
    /// to true (added 2026-07, replaces the earlier OTP-based first-time-setup design).
    /// </summary>
    public string DefaultPassword { get; set; } = "welcome@123";

    /// <summary>
    /// Maximum age of a password before login forces a reset. Added 2026-07 per client MOM
    /// ("users must change their password once every year").
    /// </summary>
    public int MaxPasswordAgeDays { get; set; } = 365;
}

public class PasswordPolicyValidator : IPasswordPolicyValidator
{
    private readonly AimDbContext _db;
    private readonly PasswordPolicyOptions _options;

    public PasswordPolicyValidator(AimDbContext db, IOptions<PasswordPolicyOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public Task<(bool IsValid, PasswordValidationError? Error)> ValidateAsync(int userId, string password, CancellationToken ct = default)
    {
        if (password.Length < _options.MinLength)
            return Task.FromResult((false, (PasswordValidationError?)PasswordValidationError.TooShort));

        if (_options.RequireUppercase && !password.Any(char.IsUpper))
            return Task.FromResult((false, (PasswordValidationError?)PasswordValidationError.MissingUppercase));

        if (_options.RequireLowercase && !password.Any(char.IsLower))
            return Task.FromResult((false, (PasswordValidationError?)PasswordValidationError.MissingLowercase));

        if (_options.RequireSpecialChar && password.All(char.IsLetterOrDigit))
            return Task.FromResult((false, (PasswordValidationError?)PasswordValidationError.MissingSpecialChar));

        // Sequential digits
        if (HasSequentialDigits(password, _options.BlockSequentialDigitsMinRun))
            return Task.FromResult((false, (PasswordValidationError?)PasswordValidationError.SequentialDigits));

        // Personal info checks
        var user = _db.Users.Find(userId);
        if (user != null)
        {
            var emailLocal = user.Email?.Split('@')[0] ?? string.Empty;
            if (!string.IsNullOrEmpty(user.Username) && password.Contains(user.Username, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult((false, (PasswordValidationError?)PasswordValidationError.ContainsPersonalInfo));
            if (!string.IsNullOrEmpty(user.FullName))
            {
                var parts = user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                    if (!string.IsNullOrEmpty(p) && password.Contains(p, StringComparison.OrdinalIgnoreCase))
                        return Task.FromResult((false, (PasswordValidationError?)PasswordValidationError.ContainsPersonalInfo));
            }
            if (!string.IsNullOrEmpty(emailLocal) && password.Contains(emailLocal, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult((false, (PasswordValidationError?)PasswordValidationError.ContainsPersonalInfo));
        }

        // Blocked terms
        foreach (var term in _options.BlockedTerms)
        {
            if (password.Contains(term, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult((false, (PasswordValidationError?)PasswordValidationError.CommonPassword));
        }

        return Task.FromResult((true, (PasswordValidationError?)null));
    }

    private static bool HasSequentialDigits(string s, int runLength)
    {
        var digits = Regex.Matches(s, "\\d").Select(m => m.Value[0]).ToArray();
        if (digits.Length < runLength) return false;

        for (int i = 0; i <= digits.Length - runLength; i++)
        {
            bool asc = true, desc = true;
            for (int j = 1; j < runLength; j++)
            {
                if (digits[i + j] != digits[i + j - 1] + 1) asc = false;
                if (digits[i + j] != digits[i + j - 1] - 1) desc = false;
            }
            if (asc || desc) return true;
        }
        return false;
    }
}
