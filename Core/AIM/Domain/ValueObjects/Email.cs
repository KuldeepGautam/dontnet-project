namespace UBIS.Services.Aim.Domain.ValueObjects;

using System.Text.RegularExpressions;

/// <summary>
/// Represents an email address as an immutable value object.
/// Validates email format to ensure domain integrity.
/// </summary>
public class Email : IEquatable<Email>
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates an Email value object with validation.
    /// </summary>
    /// <param name="email">The email address to validate.</param>
    /// <returns>Email value object if valid.</returns>
    /// <exception cref="ArgumentException">Thrown when email is invalid.</exception>
    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        if (email.Length > 320)
            throw new ArgumentException("Email cannot exceed 320 characters.", nameof(email));

        if (!EmailRegex.IsMatch(email))
            throw new ArgumentException("Email format is invalid.", nameof(email));

        return new Email(email);
    }

    /// <summary>
    /// Attempts to create an Email value object without throwing exceptions.
    /// </summary>
    public static bool TryCreate(string? email, out Email? result)
    {
        result = null;
        try
        {
            result = Create(email ?? string.Empty);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool Equals(Email? other) => other?.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as Email);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    public static bool operator ==(Email? left, Email? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Email? left, Email? right) => !(left == right);

    public static implicit operator string(Email email) => email.Value;

    public static explicit operator Email(string email) => Create(email);
}
