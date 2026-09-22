namespace UBIS.Services.Aim.Infrastructure.Otp;

/// <summary>
/// Login MFA toggle (added 2026-07). Client's MOM states email/SMS MFA is not feasible yet on the
/// intranet, but must be "designed in configurable mode" for when SMTP/SMS gateway access is
/// granted by NIC HQ. Defaults to disabled so login behavior is unchanged until flipped on.
/// </summary>
public class MfaOptions
{
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// "Email" (fully functional, dispatches via <see cref="Application.Interfaces.IEmailServiceClient"/>)
    /// or "Sms" (dispatches via <see cref="Application.Interfaces.ISmsServiceClient"/>, currently a
    /// stub — no real SMS gateway exists yet).
    /// </summary>
    public string Channel { get; set; } = "Email";
}
