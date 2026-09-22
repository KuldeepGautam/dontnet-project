namespace UBIS.Services.Aim.Application.Validation;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Rejects any email address that doesn't end with an approved government domain
/// (case-insensitive). Air-gapped intranet compliance rule: no personal/commercial email
/// domains are accepted anywhere the system asks for an email address.
/// </summary>
public sealed class GovernmentEmailAttribute : ValidationAttribute
{
    private static readonly string[] AllowedDomains = { "@nic.in", "@gov.in" };

    public GovernmentEmailAttribute()
        : base("Email address must end with @nic.in or @gov.in.")
    {
    }

    public override bool IsValid(object? value)
    {
        // Absence is a [Required]/DTO-default concern, not this attribute's — only validate format when present.
        if (value is not string email || string.IsNullOrWhiteSpace(email))
        {
            return true;
        }

        foreach (var domain in AllowedDomains)
        {
            if (email.EndsWith(domain, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
