namespace UBIS.Services.Ecl.Application.DTOs;

/// <summary>
/// Generic result wrapper for API responses. ECL's own independent copy of this shape — this
/// solution's established convention (confirmed via PreBudget/AIM) is each microservice keeping a
/// small copy of cross-cutting types like this rather than a shared library.
/// </summary>
/// <typeparam name="T">Type of data in successful response.</typeparam>
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

    public static Error NotFound(string entityName, object identifier) =>
        new("NOT_FOUND", $"{entityName} with identifier '{identifier}' not found.");

    public static Error InternalError(string? message = null) =>
        new("INTERNAL_ERROR", message ?? "An internal error occurred. Please try again later.");

    public static Error ValidationError(string fieldName, string reason) =>
        new("VALIDATION_ERROR", $"{fieldName}: {reason}");

    public static Error Forbidden(string? message = null) =>
        new("FORBIDDEN", message ?? "You are not permitted to perform this action.");

    // ---- ECL workflow-specific errors ----

    public static Error AlreadyPendingDoeApproval(string? message = null) =>
        new("ALREADY_PENDING_DOE_APPROVAL", message ?? "This outlay is already pending DOE approval.");

    public static Error AlreadyApprovedByDoe(string? message = null) =>
        new("ALREADY_APPROVED_BY_DOE", message ?? "This outlay is already approved by DOE.");

    public static Error NotPendingDoeApproval(string? message = null) =>
        new("NOT_PENDING_DOE_APPROVAL", message ?? "This outlay is not currently pending DOE approval.");

    public static Error RejectionRequiresRemarks(string? message = null) =>
        new("REJECTION_REQUIRES_REMARKS", message ?? "DOE remarks are required when rejecting an outlay.");

    public static Error ReapprovalNotAllowed(string? message = null) =>
        new("REAPPROVAL_NOT_ALLOWED", message ?? "Reapproval can only be requested for an outlay that is currently approved.");

    public static Error PdfTooLarge(string? message = null) =>
        new("PDF_TOO_LARGE", message ?? "The uploaded file exceeds the 5 MB size limit.");

    public static Error NotAValidPdf(string? message = null) =>
        new("NOT_A_VALID_PDF", message ?? "The uploaded file is not a valid PDF document.");

    public static Error AlreadySentForDoeApproval(string? message = null) =>
        new("ALREADY_SENT_FOR_DOE_APPROVAL", message ?? "This outlay has already been sent for DOE approval.");
}
