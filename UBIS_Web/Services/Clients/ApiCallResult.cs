namespace UBIS.Web.Services.Clients;

/// <summary>Thin wrapper so controllers can branch on outcome without catching HttpRequestException.</summary>
public class ApiCallResult<T>
{
    public bool IsSuccess { get; private init; }

    public T? Data { get; private init; }

    public int StatusCode { get; private init; }

    public AimErrorDto? Error { get; private init; }

    public static ApiCallResult<T> Success(T data, int statusCode) =>
        new() { IsSuccess = true, Data = data, StatusCode = statusCode };

    public static ApiCallResult<T> Failure(int statusCode, AimErrorDto? error) =>
        new() { IsSuccess = false, StatusCode = statusCode, Error = error };
}
