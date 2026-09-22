namespace UBIS.Services.Email.Application.DTOs;

public class Result
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? Error { get; init; }

    public static Result Ok(string? message = null) => new()
    {
        Success = true,
        Message = message ?? "Operation completed successfully."
    };

    public static Result Fail(string error, string? message = null) => new()
    {
        Success = false,
        Error = error,
        Message = message ?? "Operation failed."
    };
}
