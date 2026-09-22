namespace UBIS.Web.Services.Captcha;

using Microsoft.AspNetCore.Http;

/// <summary>Server-rendered login CAPTCHA (added 2026-07 per client MOM). See <see cref="CaptchaService"/>.</summary>
public interface ICaptchaService
{
    /// <summary>Generates a fresh challenge, stores the answer in <paramref name="session"/>, and
    /// returns a ready-to-embed <c>data:image/svg+xml;base64,...</c> URI.</summary>
    string GenerateChallenge(ISession session);

    /// <summary>Validates the submitted answer against the one stored in <paramref name="session"/>
    /// and always clears it afterwards — pass or fail, a CAPTCHA is never reusable.</summary>
    bool Validate(ISession session, string? submittedAnswer);
}
