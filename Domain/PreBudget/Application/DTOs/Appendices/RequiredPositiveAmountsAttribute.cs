namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

using System.ComponentModel.DataAnnotations;
using System.Reflection;

/// <summary>
/// Class-level validation: every named <c>decimal?</c> property on the annotated DTO must be a
/// real, non-zero value - null (empty) or exactly 0 both fail. Unlike
/// <see cref="NonNegativeAmountsAttribute"/> (which scans every decimal property generically),
/// this targets an explicit whitelist of property names, since "must be filled in with something
/// real" is a per-field business rule, not something safe to assume for every amount field on
/// every DTO (e.g. Appendix II's own MoF Approval Details/Remarks amounts, where applicable,
/// legitimately can be blank).
///
/// Added for Appendix II's Q1/Q2 Actuals and "As per Approved QEP" fields (client requirement
/// 2026-08-27): "Cannot Save or Modify with 0 or empty values for Actuals (current or previous
/// years), As per Approved QEP" - both fields were already HTML-`required` (blocks empty) but
/// `min="0"` still let a literal 0 through, which the client's own reference data treats as
/// "not actually filled in" for these fields specifically.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RequiredPositiveAmountsAttribute : ValidationAttribute
{
    private readonly string[] _propertyNames;

    public RequiredPositiveAmountsAttribute(params string[] propertyNames)
    {
        _propertyNames = propertyNames;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        var type = value.GetType();
        var hasZeroOrEmpty = _propertyNames
            .Select(name => type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance))
            .Where(p => p is not null)
            .Select(p => p!.GetValue(value) as decimal?)
            .Any(v => v is null or 0m);

        return hasZeroOrEmpty
            ? new ValidationResult(ErrorMessage ?? "One or more required amount fields cannot be blank or zero.")
            : ValidationResult.Success;
    }
}
