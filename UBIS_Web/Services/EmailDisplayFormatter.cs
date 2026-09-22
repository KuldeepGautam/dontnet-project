namespace UBIS.Web.Services;

/// <summary>Single point of reference for how an email address is shown as read-only page text
/// (grids, profile summaries, etc.) - client requirement 2026-08-27: obfuscate "@" and "." so an
/// email displayed on a page doesn't read as a plain, scrapeable address, e.g.
/// "abc@test.com" -> "abc[At]test[dot]com". Never apply this to an editable input's bound value
/// (e.g. the Email textbox on UserProfile/Index.cshtml) - only to plain display text, otherwise the
/// obfuscated string itself would get submitted back as the "real" email.</summary>
public static class EmailDisplayFormatter
{
    public static string Obfuscate(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return string.Empty;
        }

        return email.Replace("@", "[At]").Replace(".", "[dot]");
    }
}
