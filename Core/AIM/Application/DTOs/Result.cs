namespace UBIS.Services.Aim.Application.DTOs;

/// <summary>
/// Generic result wrapper for API responses.
/// Supports both success and failure scenarios with typed data.
/// </summary>
/// <typeparam name="T">Type of data in successful response.</typeparam>
public class Result<T>
{
    /// <summary>
    /// Indicates if operation was successful.
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Data payload (null if operation failed).
    /// </summary>
    public T? Data { get; private set; }

    /// <summary>
    /// Error information (null if operation succeeded).
    /// </summary>
    public Error? Error { get; private set; }

    private Result() { }

    /// <summary>
    /// Creates a successful result with data.
    /// </summary>
    public static Result<T> Success(T data)
    {
        return new Result<T>
        {
            IsSuccess = true,
            Data = data,
            Error = null
        };
    }

    /// <summary>
    /// Creates a failure result with error information.
    /// </summary>
    public static Result<T> Failure(Error error)
    {
        return new Result<T>
        {
            IsSuccess = false,
            Data = default,
            Error = error
        };
    }

    /// <summary>
    /// Creates a failure result with simple error message.
    /// </summary>
    public static Result<T> Failure(string code, string message)
    {
        return Failure(new Error(code, message));
    }
}

/// <summary>
/// Error information wrapper for API responses.
/// </summary>
public class Error
{
    /// <summary>
    /// Error code (e.g., 'INVALID_CREDENTIALS', 'NOT_FOUND').
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Optional additional details.
    /// </summary>
    public string? Details { get; set; }

    public Error(string code, string message, string? details = null)
    {
        Code = code;
        Message = message;
        Details = details;
    }

    // Predefined error factories
    public static Error InvalidCredentials(string? message = null) =>
        new("INVALID_CREDENTIALS", message ?? "Invalid username or password.");

    public static Error IpBindingFailure(string? message = null) =>
        new("IP_BINDING_FAILURE", message ?? "Access denied from unauthorized network location.");

    public static Error RateLimitExceeded(string? message = null) =>
        new("RATE_LIMIT_EXCEEDED", message ?? "Account temporarily locked due to excessive failed attempts.");

    public static Error UserNotFound(string identifier) =>
        new("USER_NOT_FOUND", $"User '{identifier}' not found.");

    public static Error InvalidPassword(string? message = null) =>
        new("INVALID_PASSWORD", message ?? "Password does not meet policy requirements.");

    public static Error PasswordMismatch(string? message = null) =>
        new("PASSWORD_MISMATCH", message ?? "Password confirmation does not match.");

    public static Error InactiveUser(string? message = null) =>
        new("INACTIVE_USER", message ?? "User account is inactive.");

    public static Error NotFound(string entityName, object identifier) =>
        new("NOT_FOUND", $"{entityName} with identifier '{identifier}' not found.");

    public static Error InternalError(string? message = null) =>
        new("INTERNAL_ERROR", message ?? "An internal error occurred. Please try again later.");

    public static Error ValidationError(string fieldName, string reason) =>
        new("VALIDATION_ERROR", $"{fieldName}: {reason}");

    public static Error EmailNotConfigured(string? message = null) =>
        new("EMAIL_NOT_CONFIGURED", message ?? "Your account has no email address on file. Please contact your administrator to add one before you can sign in.");

    public static Error FinancialYearNotPermitted(string? message = null) =>
        new("FINANCIAL_YEAR_NOT_PERMITTED", message ?? "You are not permitted to access the system for the current financial year.");

    public static Error PasswordReused(string? message = null) =>
        new("PASSWORD_REUSED", message ?? "You cannot reuse your current password or any of your last 3 passwords.");

    public static Error PasswordPolicyViolation(string message) =>
        new("PASSWORD_POLICY_VIOLATION", message);
}
