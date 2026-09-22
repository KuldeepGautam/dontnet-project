namespace UBIS.Web.Services.Clients;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using UBIS.Web.Services.Logging;

/// <summary>Typed HttpClient for the AIM (auth/identity) microservice.</summary>
public class AimClient : IAimClient
{
    private readonly HttpClient _http;
    private readonly IWebLogClient _logClient;

    public AimClient(HttpClient http, IWebLogClient logClient)
    {
        _http = http;
        _logClient = logClient;
    }

    public Task<ApiCallResult<List<FinancialYearOptionDto>>> GetFinancialYearOptionsAsync(int appId, CancellationToken ct = default) =>
        SendAsync<List<FinancialYearOptionDto>>(HttpMethod.Get, $"api/authentication/financial-years?appId={appId}", body: null, bearerToken: null, ct);

    public Task<ApiCallResult<LoginResultDto>> LoginAsync(LoginRequestDto request, CancellationToken ct = default) =>
        SendAsync<LoginResultDto>(HttpMethod.Post, "api/authentication/login", request, bearerToken: null, ct);

    public Task<ApiCallResult<LoginResultDto>> RefreshAsync(string refreshToken, CancellationToken ct = default) =>
        SendAsync<LoginResultDto>(
            HttpMethod.Post, "api/authentication/refresh", new RefreshTokenRequestDto { RefreshToken = refreshToken }, bearerToken: null, ct);

    public Task<ApiCallResult<object?>> LogoutAsync(string sessionId, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Post, $"api/authentication/logout?sessionId={Uri.EscapeDataString(sessionId)}", body: null, bearerToken: null, ct);

    // Idle-timeout revamp (2026-09-03): self-service "logout everywhere" - revokes every active
    // session for the caller (identified from their own JWT, not a parameter), not just this one.
    public Task<ApiCallResult<object?>> LogoutAllSessionsAsync(string bearerToken, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Post, "api/users/logout-all-sessions", body: null, bearerToken, ct);

    public Task<ApiCallResult<UserRoleDto>> GetMyRoleAsync(string bearerToken, string financialYear, CancellationToken ct = default) =>
        SendAsync<UserRoleDto>(
            HttpMethod.Get, $"api/userrole/my-role?financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);

    public Task<ApiCallResult<UserEmailLookupDto>> GetEmailAsync(string bearerToken, CancellationToken ct = default) =>
        SendAsync<UserEmailLookupDto>(HttpMethod.Get, "api/users/email", body: null, bearerToken, ct);

    public Task<ApiCallResult<object?>> ChangePasswordAsync(string bearerToken, ChangePasswordRequestDto request, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Post, "api/authentication/change-password", request, bearerToken, ct);

    public Task<ApiCallResult<ComplianceStatusDto>> GetComplianceStatusAsync(string bearerToken, string? financialYear = null, CancellationToken ct = default) =>
        SendAsync<ComplianceStatusDto>(HttpMethod.Get, "api/users/compliance-status" + (financialYear != null ? $"?financialYear={Uri.EscapeDataString(financialYear)}" : ""), body: null, bearerToken, ct);

    public Task<ApiCallResult<LegacyProfileDto>> GetLegacyProfileAsync(string bearerToken, CancellationToken ct = default) =>
        SendAsync<LegacyProfileDto>(HttpMethod.Get, "api/users/legacy-profile", body: null, bearerToken, ct);

    public Task<ApiCallResult<object?>> SubmitChangeRequestAsync(string bearerToken, SubmitChangeRequestDto request, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Post, "api/users/change-request", request, bearerToken, ct);

    public Task<ApiCallResult<ChangeRequestStatusDto>> GetChangeRequestStatusAsync(string bearerToken, CancellationToken ct = default) =>
        SendAsync<ChangeRequestStatusDto>(HttpMethod.Get, "api/users/change-request/status", body: null, bearerToken, ct);

    public Task<ApiCallResult<object?>> SendSessionHeartbeatAsync(string bearerToken, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Post, "api/authentication/session-heartbeat", body: null, bearerToken, ct);

    public Task<ApiCallResult<List<ActiveSessionDto>>> GetActiveSessionsAsync(string bearerToken, CancellationToken ct = default) =>
        SendAsync<List<ActiveSessionDto>>(HttpMethod.Get, "api/users/admin/active-sessions", body: null, bearerToken, ct);

    public Task<ApiCallResult<object?>> ForceLogoutUserAsync(string bearerToken, int userId, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Post, $"api/users/admin/{userId}/force-logout", body: null, bearerToken, ct);

    public Task<ApiCallResult<LoginResultDto>> VerifyLoginOtpAsync(VerifyLoginOtpRequestDto request, CancellationToken ct = default) =>
        SendAsync<LoginResultDto>(HttpMethod.Post, "api/authentication/login/verify-otp", request, bearerToken: null, ct);

    public Task<ApiCallResult<object?>> ResendLoginOtpAsync(ResendLoginOtpRequestDto request, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Post, "api/authentication/login/resend-otp", request, bearerToken: null, ct);

    public Task<ApiCallResult<AdminChangeRequestListDto>> GetPendingChangeRequestsAsync(string bearerToken, CancellationToken ct = default) =>
        SendAsync<AdminChangeRequestListDto>(HttpMethod.Get, "api/users/admin/change-requests/pending", body: null, bearerToken, ct);

    public Task<ApiCallResult<object?>> ApproveChangeRequestAsync(string bearerToken, int rowId, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Post, $"api/users/admin/change-requests/{rowId}/approve", body: null, bearerToken, ct);

    public Task<ApiCallResult<object?>> RejectChangeRequestAsync(string bearerToken, int rowId, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Post, $"api/users/admin/change-requests/{rowId}/reject", body: null, bearerToken, ct);

    public Task<ApiCallResult<AssignedStatementsDto>> GetAssignedStatementsAsync(string bearerToken, string financialYear, CancellationToken ct = default) =>
        SendAsync<AssignedStatementsDto>(HttpMethod.Get, $"api/users/assigned-statements?financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);

    public Task<ApiCallResult<ForgotPasswordResultDto>> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken ct = default) =>
        SendAsync<ForgotPasswordResultDto>(HttpMethod.Post, "api/authentication/forgot-password", request, bearerToken: null, ct);

    public Task<ApiCallResult<ValidateResetTokenResultDto>> ValidateResetTokenAsync(string resetToken, CancellationToken ct = default) =>
        SendAsync<ValidateResetTokenResultDto>(HttpMethod.Get, $"api/authentication/validate-reset-token/{Uri.EscapeDataString(resetToken)}", body: null, bearerToken: null, ct);

    public Task<ApiCallResult<ResetPasswordResultDto>> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken ct = default) =>
        SendAsync<ResetPasswordResultDto>(HttpMethod.Post, "api/authentication/reset-password", request, bearerToken: null, ct);

    public Task<ApiCallResult<PasswordResetAvailabilityDto>> GetPasswordResetOtpAvailabilityAsync(CancellationToken ct = default) =>
        SendAsync<PasswordResetAvailabilityDto>(HttpMethod.Get, "api/authentication/password-reset/availability", body: null, bearerToken: null, ct);

    public Task<ApiCallResult<PasswordResetOtpRequestResultDto>> RequestPasswordResetOtpAsync(PasswordResetOtpRequestDto request, CancellationToken ct = default) =>
        SendAsync<PasswordResetOtpRequestResultDto>(HttpMethod.Post, "api/authentication/password-reset/request-otp", request, bearerToken: null, ct);

    public Task<ApiCallResult<VerifyPasswordResetOtpResultDto>> VerifyPasswordResetOtpAsync(VerifyPasswordResetOtpRequestDto request, CancellationToken ct = default) =>
        SendAsync<VerifyPasswordResetOtpResultDto>(HttpMethod.Post, "api/authentication/password-reset/verify-otp", request, bearerToken: null, ct);

    public Task<ApiCallResult<MyDemandsDto>> GetMyDemandsAsync(string bearerToken, string financialYear, CancellationToken ct = default) =>
        SendAsync<MyDemandsDto>(HttpMethod.Get, $"api/users/demands?financialYear={Uri.EscapeDataString(financialYear)}", body: null, bearerToken, ct);

    public Task<ApiCallResult<AppSettingsSnapshotDto>> GetAppSettingsAsync(CancellationToken ct = default) =>
        SendAsync<AppSettingsSnapshotDto>(HttpMethod.Get, "api/app-settings", body: null, bearerToken: null, ct);

    public Task<ApiCallResult<object?>> UpdateIdleTimerMinutesAsync(string bearerToken, int minutes, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Put, "api/app-settings/idle-timer-minutes", new { minutes }, bearerToken, ct);

    private async Task<ApiCallResult<TResponse>> SendAsync<TResponse>(
        HttpMethod method, string relativeUrl, object? body, string? bearerToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, relativeUrl);
        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (!string.IsNullOrEmpty(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Catches both (a) retries/circuit-breaker (Program.cs's AddStandardResilienceHandler
            // config) exhausted after genuinely giving up on AIM, and (b) the caller's own request
            // being cancelled (e.g. the browser navigated away/refreshed mid-request) - these used
            // to be deliberately distinguished (via ct.IsCancellationRequested) so only case (a)
            // was caught, letting case (b) propagate unhandled. In practice that meant every normal
            // navigate-away-while-loading surfaced as a raw unhandled TaskCanceledException (a
            // scary Development-mode stack trace / debugger break, tester-reported 2026-08-03) for
            // no functional benefit — the response was going nowhere useful either way. Both cases
            // now fail the same graceful way; the caller (e.g. PreBudgetDataandReport) already has
            // a StatusMessage fallback for a failed ApiCallResult, so this degrades cleanly.
            await _logClient.ErrorAsync(
                "AIM call failed after retries/circuit-breaker exhausted, or the request was cancelled.", ex, new { relativeUrl }, ct).ConfigureAwait(false);
            return ApiCallResult<TResponse>.Failure(
                StatusCodes.Status503ServiceUnavailable,
                new AimErrorDto { Code = "SERVICE_UNAVAILABLE", Message = "AIM is temporarily unavailable. Please try again shortly." });
        }

        using (response)
        {
            var statusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                if (typeof(TResponse) == typeof(object))
                {
                    return ApiCallResult<TResponse>.Success(default!, statusCode);
                }

                var data = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct).ConfigureAwait(false);
                return ApiCallResult<TResponse>.Success(data!, statusCode);
            }

            AimErrorDto? error = null;
            try
            {
                error = await response.Content.ReadFromJsonAsync<AimErrorDto>(cancellationToken: ct).ConfigureAwait(false);
            }
            catch
            {
                // Some failure responses (e.g. Forbid()) carry no body — statusCode alone drives the message.
            }

            return ApiCallResult<TResponse>.Failure(statusCode, error);
        }
    }
}
