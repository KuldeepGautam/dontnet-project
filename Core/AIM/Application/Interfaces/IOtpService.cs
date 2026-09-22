namespace UBIS.Services.Aim.Application.Interfaces;

using UBIS.Services.Aim.Application.DTOs;

public enum OtpPurpose { LoginMfa, PasswordSetup, PasswordReset }

public interface IOtpService
{
    Task<string> GenerateAndStoreOtpAsync(int userId, OtpPurpose purpose, CancellationToken ct = default);

    Task<Result<bool>> VerifyOtpAsync(int userId, OtpPurpose purpose, string suppliedOtp, CancellationToken ct = default);
}
