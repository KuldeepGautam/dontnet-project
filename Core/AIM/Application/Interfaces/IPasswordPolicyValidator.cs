namespace UBIS.Services.Aim.Application.Interfaces;

public enum PasswordValidationError
{
    None,
    TooShort,
    SequentialDigits,
    ContainsPersonalInfo,
    CommonPassword,
    MissingUppercase,
    MissingLowercase,
    MissingSpecialChar
}

public interface IPasswordPolicyValidator
{
    Task<(bool IsValid, PasswordValidationError? Error)> ValidateAsync(int userId, string password, CancellationToken ct = default);
}
