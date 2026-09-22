namespace UBIS.Services.Reporting.Application.DTOs;

/// <summary>
/// Generic result wrapper for API responses. Reporting's own independent copy of this shape — this
/// solution's established convention (confirmed via PreBudget/AIM/ECL) is each microservice keeping a
/// small copy of cross-cutting types like this rather than a shared library.
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; private set; }

    public T? Data { get; private set; }

    public Error? Error { get; private set; }

    private Result() { }

    public static Result<T> Success(T data) => new() { IsSuccess = true, Data = data, Error = null };

    public static Result<T> Failure(Error error) => new() { IsSuccess = false, Data = default, Error = error };

    public static Result<T> Failure(string code, string message) => Failure(new Error(code, message));
}

/// <summary>Error information wrapper for API responses.</summary>
public class Error
{
    public string Code { get; set; }

    public string Message { get; set; }

    public string? Details { get; set; }

    public Error(string code, string message, string? details = null)
    {
        Code = code;
        Message = message;
        Details = details;
    }

    public static Error ValidationError(string fieldName, string reason) =>
        new("VALIDATION_ERROR", $"{fieldName}: {reason}");

    public static Error InternalError(string? message = null) =>
        new("INTERNAL_ERROR", message ?? "An internal error occurred. Please try again later.");
}
