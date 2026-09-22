namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Property-level validation for a <c>DateOnly?</c>/<c>DateTime?</c> field that must never be in
/// the future (e.g. Appendix III's "Date of Last Release" - client requirement 2026-08-27: a
/// release date is by definition something that already happened, so a future value is always
/// wrong data entry). Null is valid (field is optional) - pair with [Required] separately if a
/// given field must also be mandatory. Runs via [ApiController]'s automatic model validation, so
/// the request never reaches the controller/repository/database.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NotFutureDateAttribute : ValidationAttribute
{
    public NotFutureDateAttribute()
        : base("{0} cannot be a future date.")
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var isFuture = value switch
        {
            null => false,
            DateOnly d => d > today,
            DateTime dt => DateOnly.FromDateTime(dt) > today,
            _ => false
        };

        if (!isFuture)
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(
            FormatErrorMessage(validationContext.DisplayName),
            [validationContext.MemberName ?? string.Empty]);
    }
}
