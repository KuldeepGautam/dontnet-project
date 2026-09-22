namespace UBIS.Services.Aim.Infrastructure.Otp;

/// <summary>
/// Channel gate for the OTP-based "Forgot Password" flow on the Login page (added 2026-07-22) —
/// deliberately separate from <see cref="MfaOptions"/> (login MFA is a different feature; sharing
/// one on/off switch between the two would couple unrelated concerns). Email defaults to disabled:
/// no SMTP relay is configured yet (<c>Core/Email/WebApi/appsettings.json</c>'s <c>Smtp:Host</c> is
/// blank) - so with it off, the UI correctly shows "contact your administrator" instead of a
/// request that silently never delivers. Flip to true once SMTP is operational.
/// The Mobile/SMS side of this gate moved to dbo.AppSettings.EnableSms (2026-08-07, client
/// requirement to centralize these flags in the DB instead of per-service config) - see
/// AuthenticationService.GetPasswordResetOtpAvailabilityAsync and OtpService.DispatchOtpAsync,
/// which now read AppSettingsReader directly instead of a MobileEnabled property here.
/// </summary>
public class PasswordResetOtpOptions
{
    public bool EmailEnabled { get; set; } = false;
}
