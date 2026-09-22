namespace UBIS.Services.PreBudget.Application.DTOs.Appendices;

using System.ComponentModel.DataAnnotations;
using System.Reflection;

/// <summary>
/// Class-level validation: every public <c>decimal</c>/<c>decimal?</c> property on the annotated
/// DTO must be null or &gt;= 0. Every one of the 22 appendix Save*Dto request types is FRS
/// monetary-amount fields (BE/RE/Actuals/Balances/Releases/etc.) - none are legitimately negative,
/// so this is applied once per DTO class instead of hand-annotating every individual property
/// (and automatically covers any amount field added to a DTO later). Runs via [ApiController]'s
/// automatic model validation, so a negative amount gets a clean 400 before it ever reaches a
/// controller/repository/database.
///
/// <paramref name="excludedPropertyNames"/> (added 2026-08-27 for Appendix IV-A: "Actuals upto
/// 9/2025 can be negative") opts specific property names on the annotated DTO out of this check,
/// for the rare field the client confirms is genuinely allowed to be negative - every other DTO's
/// existing [NonNegativeAmounts] (no arguments) is unaffected.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class NonNegativeAmountsAttribute : ValidationAttribute
{
    private readonly HashSet<string> _excludedPropertyNames;

    public NonNegativeAmountsAttribute(params string[] excludedPropertyNames)
    {
        _excludedPropertyNames = new HashSet<string>(excludedPropertyNames, StringComparer.Ordinal);
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        var negativeFields = value.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(decimal) || p.PropertyType == typeof(decimal?))
            .Where(p => !_excludedPropertyNames.Contains(p.Name))
            .Select(p => new { p.Name, Value = p.GetValue(value) as decimal? })
            .Where(x => x.Value is < 0)
            .Select(x => x.Name)
            .ToList();

        if (negativeFields.Count == 0)
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(
            $"The following amount field(s) cannot be negative: {string.Join(", ", negativeFields)}.");
    }
}
