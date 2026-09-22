namespace UBIS.Services.Aim.Application.Interfaces;

using UBIS.Services.Aim.Application.DTOs;

/// <summary>
/// Dispatches OTP codes over SMS. Added 2026-07 alongside the login MFA toggle — the client is in
/// touch with NIC HQ for intranet SMTP/SMS gateway access, so this exists as a ready abstraction;
/// the only implementation registered today (<see cref="Infrastructure.Sms.NotConfiguredSmsServiceClient"/>)
/// is a stub that fails gracefully until a real gateway is wired up.
/// </summary>
public interface ISmsServiceClient
{
    Task<Result<bool>> SendOtpSmsAsync(string toMobile, string otp, CancellationToken ct = default);
}
