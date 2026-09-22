namespace UBIS.Services.Aim.Domain.ValueObjects;

/// <summary>
/// Represents an IP address as an immutable value object.
/// Validates that the address is a valid IPv4 or IPv6 address.
/// </summary>
public class IpAddress : IEquatable<IpAddress>
{
    public string Value { get; }

    private IpAddress(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates an IpAddress value object with validation.
    /// </summary>
    /// <param name="address">The IP address to validate.</param>
    /// <returns>IpAddress value object if valid.</returns>
    /// <exception cref="ArgumentException">Thrown when IP address is invalid.</exception>
    public static IpAddress Create(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("IP address cannot be empty.", nameof(address));

        var trimmedAddress = address.Trim();

        if (!System.Net.IPAddress.TryParse(trimmedAddress, out _))
            throw new ArgumentException($"'{trimmedAddress}' is not a valid IP address.", nameof(address));

        return new IpAddress(trimmedAddress);
    }

    /// <summary>
    /// Attempts to create an IpAddress value object without throwing exceptions.
    /// </summary>
    public static bool TryCreate(string? address, out IpAddress? result)
    {
        result = null;
        try
        {
            result = Create(address ?? string.Empty);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if this IP address matches another IP address (exact match).
    /// </summary>
    public bool Matches(IpAddress? other) => other != null && Value == other.Value;

    /// <summary>
    /// Checks if this IP is in a list of allowed addresses.
    /// </summary>
    public bool IsInAllowedList(params IpAddress?[] allowedAddresses)
    {
        return allowedAddresses
            .Where(ip => ip != null)
            .Any(ip => Matches(ip));
    }

    public bool Equals(IpAddress? other) => other?.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as IpAddress);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    public static bool operator ==(IpAddress? left, IpAddress? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(IpAddress? left, IpAddress? right) => !(left == right);

    public static implicit operator string(IpAddress ipAddress) => ipAddress.Value;

    public static explicit operator IpAddress(string address) => Create(address);
}
