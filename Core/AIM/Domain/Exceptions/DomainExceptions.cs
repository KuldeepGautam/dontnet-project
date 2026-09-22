namespace UBIS.Services.Aim.Domain.Exceptions;

/// <summary>
/// Base exception for domain-specific errors in the AIM microservice.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Thrown when authentication fails (invalid credentials).
/// </summary>
public class AuthenticationFailedException : DomainException
{
    public AuthenticationFailedException(string message = "Authentication failed. Invalid credentials.")
        : base(message) { }
}

/// <summary>
/// Thrown when user account is inactive.
/// </summary>
public class InactiveUserException : DomainException
{
    public InactiveUserException(string message = "User account is inactive.")
        : base(message) { }
}

/// <summary>
/// Thrown when user IP address does not match allowed IP addresses.
/// </summary>
public class IpBindingException : DomainException
{
    public IpBindingException(string message = "Access denied from unauthorized network location.")
        : base(message) { }
}

/// <summary>
/// Thrown when rate limiting is triggered (too many failed login attempts).
/// </summary>
public class RateLimitExceededException : DomainException
{
    public RateLimitExceededException(string message = "Account temporarily locked due to excessive failed attempts.")
        : base(message) { }
}

/// <summary>
/// Thrown when user is not found.
/// </summary>
public class UserNotFoundException : DomainException
{
    public UserNotFoundException(string identifier)
        : base($"User '{identifier}' not found.") { }
}

/// <summary>
/// Thrown when a required resource is not found.
/// </summary>
public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, object identifier)
        : base($"{entityName} with identifier '{identifier}' not found.") { }
}

/// <summary>
/// Thrown when user credentials are invalid.
/// </summary>
public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException(string message = "Invalid username or password.")
        : base(message) { }
}

/// <summary>
/// Thrown when a security event audit violation occurs.
/// </summary>
public class SecurityAuditException : DomainException
{
    public SecurityAuditException(string message = "Security audit violation detected.")
        : base(message) { }
}

/// <summary>
/// Thrown when password reset is required.
/// </summary>
public class PasswordResetRequiredException : DomainException
{
    public PasswordResetRequiredException(string message = "Password reset is required.")
        : base(message) { }
}
