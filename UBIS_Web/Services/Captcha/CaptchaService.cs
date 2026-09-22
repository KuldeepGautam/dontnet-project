namespace UBIS.Web.Services.Captcha;

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Server-rendered inline-SVG distorted-text CAPTCHA (added 2026-07, closing a client MOM gap).
/// No image-library dependency: <c>System.Drawing.Common</c> is Windows-only-supported since
/// .NET 6, and every service in this repo targets plain cross-platform <c>net10.0</c>, so the
/// "image" is built as an SVG string instead. The answer never reaches the browser — it lives in
/// the existing Redis-backed ASP.NET Core Session, which is already active on anonymous requests
/// (<c>UseSession()</c> runs before <c>UseAuthentication()</c> in Program.cs).
/// </summary>
public class CaptchaService : ICaptchaService
{
    private const string SessionKey = "captcha:answer";

    // Excludes visually ambiguous characters (0/O, 1/I/l).
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public string GenerateChallenge(ISession session)
    {
        var answer = GenerateRandomText(5);
        session.SetString(SessionKey, answer);
        return BuildDataUri(RenderSvg(answer));
    }

    public bool Validate(ISession session, string? submittedAnswer)
    {
        var expected = session.GetString(SessionKey);
        session.Remove(SessionKey); // one-shot — never reusable, win or lose

        // Client requirement 2026-09-01: CAPTCHA must be case-sensitive - the rendered answer is
        // always uppercase (see Alphabet above), so a lowercase submission must now fail instead
        // of matching case-insensitively.
        return !string.IsNullOrEmpty(expected)
            && !string.IsNullOrEmpty(submittedAnswer)
            && string.Equals(expected, submittedAnswer.Trim(), StringComparison.Ordinal);
    }

    private static string GenerateRandomText(int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(chars);
    }

    private static string RenderSvg(string text)
    {
        var sb = new StringBuilder();
        sb.Append("<svg xmlns='http://www.w3.org/2000/svg' width='160' height='60' viewBox='0 0 160 60'>");
        sb.Append("<rect width='160' height='60' fill='#f1f5f9'/>");

        // A handful of noise lines so the answer isn't just plain flat text.
        for (var i = 0; i < 4; i++)
        {
            var x1 = RandomNumberGenerator.GetInt32(0, 160);
            var y1 = RandomNumberGenerator.GetInt32(0, 60);
            var x2 = RandomNumberGenerator.GetInt32(0, 160);
            var y2 = RandomNumberGenerator.GetInt32(0, 60);
            sb.Append($"<line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='#cbd5e1' stroke-width='1'/>");
        }

        var spacing = 160 / (text.Length + 1);
        for (var i = 0; i < text.Length; i++)
        {
            var x = spacing * (i + 1);
            var y = 38 + RandomNumberGenerator.GetInt32(-6, 7);
            var rotate = RandomNumberGenerator.GetInt32(-25, 26);
            var hue = RandomNumberGenerator.GetInt32(190, 260);
            sb.Append(
                $"<text x='{x}' y='{y}' font-size='28' font-family='monospace' font-weight='bold' " +
                $"fill='hsl({hue},55%,35%)' text-anchor='middle' transform='rotate({rotate} {x} {y})'>{text[i]}</text>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string BuildDataUri(string svgMarkup) =>
        "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svgMarkup));
}
