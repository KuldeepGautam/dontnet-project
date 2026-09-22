namespace UBIS.Services.Aim.Infrastructure.Services;

using System.Security.Claims;
using BCrypt.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using UBIS.Services.Aim.Application.DTOs;
using UBIS.Services.Aim.Application.DTOs.Auth;
using UBIS.Services.Aim.Application.Interfaces;
using UBIS.Services.Aim.Domain.Entities;
using UBIS.Services.Aim.Infrastructure.Configuration;
using UBIS.Services.Aim.Infrastructure.Otp;
using UBIS.Services.Aim.Infrastructure.PasswordPolicy;
using UBIS.Services.Aim.Infrastructure.Persistence;
using UBIS.Services.Aim.Infrastructure.Security;

/// <summary>
/// Implements authentication against the real, DBA-owned <c>dbo.M_Users</c> table (int identity,
/// one row per person — see <see cref="User"/>). Rewritten 2026-07-13 to drop the earlier invented
/// Guid identity model and CRUD permission matrix; see COMPLIANCE_NOTES.md for the full history.
/// Updated 2026-08-03: M_User was consolidated from one row per person per financial year down to
/// one row per person, with the year association moved to <c>dbo.M_MapUserFY</c>
/// (<see cref="UserFinancialYear"/>) — every LoginId+FinancialYear lookup below now resolves the
/// person by LoginId/UserId alone, then separately checks M_MapUserFY for year eligibility.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    // Client report 2026-09-01 ("Roles/Demands stop loading" after page changes, seen by
    // multiple testers): TokenRefreshFilter (UBIS_Web) runs on every action, so a single page
    // load's several near-simultaneous requests (a data widget's AJAX call, the heartbeat poll,
    // security-stamp-status, etc.) can all detect the access token is close to expiring and race
    // to refresh with the SAME refresh token. Refresh tokens are single-use (rotated immediately
    // below), so only the first arrival used to succeed - every other concurrent caller got
    // rejected outright with "invalid or expired", even though the session was perfectly healthy
    // a moment earlier. Keeping the just-rotated result available for a short replay window lets
    // every racer succeed with the SAME new token pair instead of only the winner.
    private static readonly TimeSpan RefreshReplayGrace = TimeSpan.FromSeconds(20);

    private readonly AimDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogWriterClient _logWriter;
    private readonly IOtpService _otpService;
    private readonly JwtOptions _jwtOptions;
    private readonly PasswordPolicyOptions _passwordPolicyOptions;
    private readonly MfaOptions _mfaOptions;
    private readonly PasswordResetOtpOptions _passwordResetOtpOptions;
    private readonly IConfiguration _configuration;
    private readonly AppSettingsReader _appSettingsReader;
    private readonly IMobileProtectionService _mobileProtectionService;
    private readonly IPasswordPolicyValidator _passwordPolicyValidator;

    public AuthenticationService(
        AimDbContext dbContext,
        ICacheService cacheService,
        IHttpContextAccessor httpContextAccessor,
        ILogWriterClient logWriter,
        IOtpService otpService,
        IOptions<JwtOptions> jwtOptions,
        IOptions<PasswordPolicyOptions> passwordPolicyOptions,
        IOptions<MfaOptions> mfaOptions,
        IOptions<PasswordResetOtpOptions> passwordResetOtpOptions,
        IConfiguration configuration,
        AppSettingsReader appSettingsReader,
        IMobileProtectionService mobileProtectionService,
        IPasswordPolicyValidator passwordPolicyValidator)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _httpContextAccessor = httpContextAccessor;
        _logWriter = logWriter;
        _otpService = otpService;
        _jwtOptions = jwtOptions.Value;
        _passwordPolicyOptions = passwordPolicyOptions.Value;
        _mfaOptions = mfaOptions.Value;
        _passwordResetOtpOptions = passwordResetOtpOptions.Value;
        _configuration = configuration;
        _appSettingsReader = appSettingsReader;
        _mobileProtectionService = mobileProtectionService;
        _passwordPolicyValidator = passwordPolicyValidator;
    }

    /// <summary>
    /// Dedicated switch for the login-time IP-binding check (dbo.AppSettings.EnableIPLogging) —
    /// added 2026-07-24 as SecurityConfiguration:EnableUserIPSettings, split out from the
    /// internal-caller header check (dbo.AppSettings.DeveloperEnv) after an incident where that
    /// flag doubling for both this check AND that one meant flipping one silently changed the
    /// other; moved to the DB-backed AppSettings table 2026-08-07 (client requirement to centralize
    /// these flags instead of per-service config files). Mirrors UBIS_Web's own EnableIPLogging read
    /// (ComplianceInterceptorFilter's per-request re-check, via AIM's api/app-settings endpoint since
    /// UBIS_Web has no direct DB access) — both read the same DB row now, so they can no longer
    /// drift out of sync the way the old hand-maintained pair of config values could.
    /// </summary>
    private Task<bool> IsIpBindingEnabledAsync() => _appSettingsReader.GetBoolAsync("EnableIPLogging");

    /// <summary>
    /// Rows from dbo.M_FinancialYear for one app, newest first — the Login page's dropdown source
    /// of truth (2026-07-30), replacing the old distinct-M_Users.FinancialYear fallback.
    ///
    /// <paramref name="appId"/> added 2026-09-08 (client instruction: each app - PreBudget, ECL,
    /// SBE, etc. - now has its own row set and its own <c>IsCurrentYear</c> flag; see
    /// dbo.M_AppName for the AppId space). Cached per AppId (short TTL, same 5-minute precedent
    /// already used for the menu tree) since this is read on every login and rarely changes.
    /// </summary>
    public async Task<List<FinancialYearOptionDto>> GetFinancialYearOptionsAsync(int appId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"ubis:financial-years:app:{appId}";
        var cached = await _cacheService.GetAsync<List<FinancialYearOptionDto>>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        var options = await _dbContext.FinancialYears
            .Where(fy => fy.AppId == appId)
            .OrderByDescending(fy => fy.YearRange)
            .Select(fy => new FinancialYearOptionDto
            {
                Id = fy.Id,
                YearRange = fy.YearRange,
                BudgetType = fy.BudgetType,
                IsCurrentYear = fy.IsCurrentYear,
                BudgetCycle = fy.BudgetCycle,
                AppId = fy.AppId
            })
            .ToListAsync(cancellationToken);

        await _cacheService.SetAsync(cacheKey, options, TimeSpan.FromMinutes(5), cancellationToken: cancellationToken);
        return options;
    }

    /// <summary>
    /// Authenticates a user for a specific financial year — <c>M_Users</c> has one row per person
    /// (as of the 2026-08-03 consolidation); LoginId alone identifies the row, and eligibility for
    /// <paramref name="request"/>'s FinancialYear is checked separately against <c>dbo.M_MapUserFY</c>.
    /// </summary>
    public async Task<Result<LoginResultDto>> AuthenticateAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clientIp = ExtractClientIpAddress();

            // Step 2: Credential Evaluation — M_User is one row per person (as of the 2026-08-03
            // consolidation), so LoginId alone identifies the row.
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == request.UserName, cancellationToken);

            // Step 2.1: Financial-year eligibility gate (added 2026-07-22; split 2026-08-03 per
            // client requirement into two distinct messages instead of one shared one — LoginId
            // values in this system are known department/office slugs (e.g. "commerce",
            // "agricoop"), not secret identifiers, so distinguishing "unknown username" from
            // "known username, wrong year" isn't a meaningful enumeration risk here; the client
            // explicitly prioritized clearer tester/user feedback over that theoretical protection.
            // Re-implemented 2026-08-03 against dbo.M_MapUserFY (see UserFinancialYear) instead of
            // M_User.FinancialYear directly — same two-branch behavior/wording as before.
            if (user == null)
            {
                await LogSecurityEventAsync(null, clientIp, "LoginFailure",
                    $"Unknown user: {request.UserName}", cancellationToken);

                return Result<LoginResultDto>.Failure(Error.InvalidCredentials("User not found."));
            }

            var eligibleForYear = await _dbContext.UserFinancialYears
                .AnyAsync(fy => fy.UserId == user.UserId && fy.FinancialYear == request.FinancialYear, cancellationToken);

            if (!eligibleForYear)
            {
                await LogSecurityEventAsync(null, clientIp, "LoginFailure",
                    $"User not permitted in financial year {request.FinancialYear}: {request.UserName}", cancellationToken);

                return Result<LoginResultDto>.Failure(
                    Error.FinancialYearNotPermitted(
                        $"User '{request.UserName}' does not exist for financial year {request.FinancialYear}. Contact your administrator if you believe this is incorrect."));
            }

            // Step 2.1a: Persistent account lockout (added 2026-08-17, simplified 2026-08-20 per
            // client direction: no temporary/self-expiring phase — 5 failed attempts locks the
            // account outright, permanent/admin-visible until explicitly cleared, see
            // ComplianceController.UnlockUser. A prior version also had a separate 15-minute
            // self-expiring Redis rate-limit that tripped at the same 5th attempt; removed as
            // redundant once permanent lock already blocks every attempt from #6 onward.
            if (user.IsLocked)
            {
                await LogSecurityEventAsync(user.UserId, clientIp, "LoginFailure",
                    $"Locked user attempted login: {request.UserName}", cancellationToken);

                return Result<LoginResultDto>.Failure(
                    Error.InactiveUser("Account is locked due to repeated failed login attempts. Contact your administrator."));
            }

            if (user.PasswordHash == null || !TryVerifyPassword(request.Password, user.PasswordHash))
            {
                user.FailedLoginAttempts = (user.FailedLoginAttempts ?? 0) + 1;

                // Stamp forensic first/second failure timestamps, then lock persistently at the
                // 5th consecutive failure (added 2026-08-17; distinct from the self-expiring
                // 15-minute Redis rate-limit at Step 1 above).
                if (user.FailedLoginAttempts == 1)
                {
                    user.FirstFailLoginAttemptDate = DateTime.UtcNow;
                }
                else if (user.FailedLoginAttempts >= 5)
                {
                    user.IsLocked = true;
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                await LogSecurityEventAsync(user.UserId, clientIp, "LoginFailure",
                    $"Failed login attempt for user: {request.UserName}", cancellationToken);

                return Result<LoginResultDto>.Failure(
                    Error.InvalidCredentials("Username or Password is wrong."));
            }

            // Step 2.5: Default-password / annual password-age enforcement (added 2026-07).
            // Migrated/newly-provisioned users are given a known default password out of band;
            // logging in with it — or with any password older than MaxPasswordAgeDays — forces a
            // change via the existing PasswordResetRequired flag (ComplianceInterceptorFilter in
            // UBIS_Web already redirects on this flag, so no downstream change is needed there).
            if (!user.PasswordResetRequired)
            {
                var isDefaultPassword = string.Equals(request.Password, _passwordPolicyOptions.DefaultPassword, StringComparison.Ordinal);
                if (isDefaultPassword)
                {
                    user.PasswordResetRequired = true;
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                else if (user.LastPasswordChangeDate is null)
                {
                    // Unknown history — baseline to now rather than retroactively forcing every
                    // existing user to reset immediately.
                    user.LastPasswordChangeDate = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                else if (user.LastPasswordChangeDate.Value.AddDays(_passwordPolicyOptions.MaxPasswordAgeDays) < DateTime.UtcNow)
                {
                    user.PasswordResetRequired = true;
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }

            // Step 3: Account State Evaluation. IsActive is the single source of truth as of
            // 2026-08-17 (replaces the legacy Active/UserFreez/UserMFFreez three-flag model).
            if (!user.IsActive)
            {
                await LogSecurityEventAsync(user.UserId, clientIp, "LoginFailure",
                    $"Inactive/frozen user attempted login: {request.UserName}", cancellationToken);

                return Result<LoginResultDto>.Failure(
                    Error.InactiveUser("Access denied. Your profile is inactive. Contact administrator."));
            }

            // Step 4: Network IP Binding Enforcement (added 2026-07-23: per-user admin override via
            // IPAuthFlag — "Y" means this user's IP must be validated, anything else means
            // an admin has granted this user a bypass). Gated by the dedicated
            // dbo.AppSettings.EnableIPLogging switch (see IsIpBindingEnabledAsync), not
            // IsDevelopmentTime — flip that on once hosted on the real server so this starts
            // enforcing for IPAuthFlag="Y" users against their real allowed IPs. Source moved
            // 2026-08-17 from M_User.IPadres1/2/IPAuthFlag to dbo.M_MapUserIPAddress.
            var ipMapping = await _dbContext.MapUserIPAddresses
                .FirstOrDefaultAsync(m => m.UserId == user.UserId, cancellationToken);
            if (await IsIpBindingEnabledAsync()
                && ipMapping != null
                && string.Equals(ipMapping.IPAuthFlag, "Y", StringComparison.OrdinalIgnoreCase)
                && !IsIpAllowed(clientIp, ipMapping.IPAddress1, ipMapping.IPAddress2))
            {
                _dbContext.SecurityEvents.Add(new SecurityEvent
                {
                    EventType = "IpBindingFailure",
                    UserId = user.UserId,
                    IpAddress = clientIp,
                    Timestamp = DateTime.UtcNow,
                    Detail = $"Unauthorized network access attempt from IP: {clientIp}"
                });
                await _dbContext.SaveChangesAsync(cancellationToken);

                return Result<LoginResultDto>.Failure(
                    Error.IpBindingFailure($"Access denied from unauthorized network location. Your IP ({clientIp}) is not allowed."));
            }

            // Step 5: Resolve Role — via M_MapUserRole (authoritative as of 2026-07-13), not the
            // legacy User.Role column. One row per user (consolidated 2026-08-04, was one row per
            // (UserId, FinancialYear) - role never actually varied by year), so UserId alone pins
            // the row.
            var userCharge = await _dbContext.MapUserRoles
                .FirstOrDefaultAsync(uc => uc.UserId == user.UserId && uc.IsActive, cancellationToken);

            if (userCharge == null)
            {
                return Result<LoginResultDto>.Failure(
                    Error.NotFound("MapUserRole", user.UserId.ToString()));
            }

            var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleId == userCharge.RoleId, cancellationToken);

            if (role == null)
            {
                return Result<LoginResultDto>.Failure(
                    Error.NotFound("Role", $"{user.UserId}:{userCharge.RoleId}"));
            }

            // Step 5.5: Login MFA (added 2026-07, default off — see MfaOptions). When enabled,
            // pause before issuing any token/session: stash just enough state to resume after the
            // OTP is verified, and hand control back to the caller with MfaRequired = true.
            if (_mfaOptions.Enabled)
            {
                // "EnableEmail" gate (client-requested, 2026-07): Mfa:Enabled + Mfa:Channel=Email
                // is the same on/off switch — reused rather than adding a second, overlapping
                // flag. Without this check, a user with no email on file would silently never
                // receive an OTP and sit stuck on the verify-code screen with no explanation.
                if (string.Equals(_mfaOptions.Channel, "Email", StringComparison.OrdinalIgnoreCase)
                    && string.IsNullOrWhiteSpace(user.Email))
                {
                    return Result<LoginResultDto>.Failure(Error.EmailNotConfigured());
                }

                await _otpService.GenerateAndStoreOtpAsync(user.UserId, OtpPurpose.LoginMfa, cancellationToken);
                await _cacheService.SetAsync(
                    $"ubis:mfa:pending:{user.UserId}",
                    new MfaPendingLogin(user.UserId, request.FinancialYear),
                    TimeSpan.FromMinutes(10),
                    cancellationToken: cancellationToken);

                await LogSecurityEventAsync(user.UserId, clientIp, "OtpRequested",
                    $"Login MFA challenge issued for user: {user.Username}", cancellationToken);

                return Result<LoginResultDto>.Success(new LoginResultDto
                {
                    MfaRequired = true,
                    MfaChallengeUserId = user.UserId
                });
            }

            return Result<LoginResultDto>.Success(
                await IssueLoginResultAsync(user, role, userCharge, request.FinancialYear, clientIp, cancellationToken));
        }
        catch (Exception ex)
        {
            await _logWriter.ErrorAsync("Unhandled error during authentication.", ex, new { request.UserName }, cancellationToken);
            return Result<LoginResultDto>.Failure(
                Error.InternalError($"An error occurred during authentication: {ex.Message}"));
        }
    }

    public async Task<Result<LoginResultDto>> VerifyLoginOtpAsync(
        VerifyLoginOtpRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pendingKey = $"ubis:mfa:pending:{request.ChallengeUserId}";
            var pending = await _cacheService.GetAsync<MfaPendingLogin>(pendingKey, cancellationToken);
            if (pending == null)
            {
                return Result<LoginResultDto>.Failure(
                    Error.InvalidCredentials("Login challenge has expired. Please log in again."));
            }

            var otpResult = await _otpService.VerifyOtpAsync(
                request.ChallengeUserId, OtpPurpose.LoginMfa, request.Otp, cancellationToken);
            if (!otpResult.IsSuccess)
            {
                return Result<LoginResultDto>.Failure(otpResult.Error!);
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(
                u => u.UserId == request.ChallengeUserId, cancellationToken);
            if (user == null || !user.IsActive)
            {
                return Result<LoginResultDto>.Failure(Error.InactiveUser("Account is no longer active."));
            }

            var userCharge = await _dbContext.MapUserRoles
                .FirstOrDefaultAsync(uc => uc.UserId == user.UserId && uc.IsActive, cancellationToken);
            if (userCharge == null)
            {
                return Result<LoginResultDto>.Failure(Error.NotFound("MapUserRole", user.UserId.ToString()));
            }

            var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleId == userCharge.RoleId, cancellationToken);
            if (role == null)
            {
                return Result<LoginResultDto>.Failure(Error.NotFound("Role", $"{user.UserId}:{userCharge.RoleId}"));
            }

            await _cacheService.RemoveAsync(pendingKey, cancellationToken);
            var clientIp = ExtractClientIpAddress();
            return Result<LoginResultDto>.Success(
                await IssueLoginResultAsync(user, role, userCharge, pending.FinancialYear, clientIp, cancellationToken));
        }
        catch (Exception ex)
        {
            await _logWriter.ErrorAsync("Unhandled error during login OTP verification.", ex, new { request.ChallengeUserId }, cancellationToken);
            return Result<LoginResultDto>.Failure(
                Error.InternalError($"An error occurred: {ex.Message}"));
        }
    }

    public async Task<Result<bool>> ResendLoginOtpAsync(
        ResendLoginOtpRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pendingKey = $"ubis:mfa:pending:{request.ChallengeUserId}";
            var pending = await _cacheService.GetAsync<MfaPendingLogin>(pendingKey, cancellationToken);
            if (pending == null)
            {
                return Result<bool>.Failure(
                    Error.InvalidCredentials("Login challenge has expired. Please log in again."));
            }

            await _otpService.GenerateAndStoreOtpAsync(request.ChallengeUserId, OtpPurpose.LoginMfa, cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await _logWriter.ErrorAsync("Unhandled error during OTP resend.", ex, new { request.ChallengeUserId }, cancellationToken);
            return Result<bool>.Failure(
                Error.InternalError($"An error occurred: {ex.Message}"));
        }
    }

    /// <summary>
    /// Shared tail of a successful authentication: compiles claims, resets rate limits, opens the
    /// Redis session, issues the JWT + refresh token. Used by both the normal (MFA-off) path and
    /// the post-verify-otp continuation, so the two never drift apart.
    /// </summary>
    private async Task<LoginResultDto> IssueLoginResultAsync(
        User user, Role role, MapUserRole userCharge, string financialYear, string clientIp, CancellationToken cancellationToken)
    {
        // Existence-based Function access (added 2026-07-13): a Role->Function mapping row
        // (not frozen) grants full access — the real dbo.M_MapRoleFunction table has no
        // CRUD columns, so there's no finer distinction to make.
        var accessibleFunctionIds = await _dbContext.RoleFunctionMappings
            .Where(rf => rf.RoleId == role.RoleId && rf.Active != "N" && rf.RFFreez != "Y")
            .Select(rf => rf.FunctionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Existence-based Demand access (added 2026-07-13, user-confirmed): dbo.M_MapUserDemand
        // holds a comma-separated DemandId list per row — flatten every active row for this user
        // into a distinct int list, same claim shape as Function access.
        var demandIdCsvValues = await _dbContext.UserDemandMappings
            .Where(m => m.UserId == user.UserId && m.IsActive != false)
            .Select(m => m.DemandIds)
            .ToListAsync(cancellationToken);
        var accessibleDemandIds = await ParseDemandIdsAsync(demandIdCsvValues, financialYear, cancellationToken);

        user.FailedLoginAttempts = 0;
        user.LastLoginDate = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Bug fix 2026-09-15 (client report: "many users facing logout issue just after logging
        // in"): this used to reuse an existing ubis:session:{sid} key for the user (client
        // requirement 2026-08-27, "do not generate new token on same machine if user already
        // login") instead of always minting a new one. That reuse meant a fresh login shared its
        // Redis key with any OTHER still-open tab/device session for the same user - and
        // session-monitor.js's idle-timeout logout-all (which revokes every session for the user,
        // by design, on a 15-minute idle countdown that keeps running even in a backgrounded tab
        // nobody is looking at) could delete that shared key moments after the fresh login wrote
        // it. The new login's own 5-second heartbeat then saw the key gone, got a 401, and force-
        // logged-out - looking exactly like "logged out right after logging in." Always minting a
        // fresh sid means a stale tab's logout-all can no longer reach forward and kill a session
        // it never knew existed. Trades back some of the original 2026-08-27 "don't pile up
        // sessions" benefit (a double-submitted login form, or a stale tab re-hitting /User/Login,
        // now does create a second Redis record) for closing this correctness bug - RevokeAllSessionsForUserAsync
        // on a genuine security event (password change, IP-approval kill switch) still finds and
        // revokes every one of them regardless of how many exist, since it scans by UserId, not sid.
        var sessionId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_jwtOptions.ExpirationMinutes);

        var sessionData = new UBIS.Services.Aim.Application.DTOs.User.SessionRecordDto
        {
            UserId = user.UserId,
            UserName = user.Username,
            RoleId = role.RoleId,
            RoleName = role.RoleName,
            Permissions = accessibleFunctionIds,
            LoginTimeUtc = now,
            IPAddress = clientIp,
            Status = "Active",
            LastActivityUtc = now
        };
        await _cacheService.SetAsync(
            $"ubis:session:{sessionId}",
            sessionData,
            TimeSpan.FromMinutes(_jwtOptions.ExpirationMinutes),
            sliding: true,
            cancellationToken: cancellationToken);

        var refreshToken = await IssueRefreshTokenAsync(
            user.UserId, role.RoleId, financialYear, sessionId, cancellationToken);

        await LogSecurityEventAsync(user.UserId, clientIp, "LoginSuccess",
            $"Successful login for user: {user.Username} with session: {sessionId}", cancellationToken);

        return new LoginResultDto
        {
            Token = GenerateJwtToken(user, role, accessibleDemandIds, financialYear, sessionId, accessibleFunctionIds, expiresAt),
            RefreshToken = refreshToken,
            SessionId = sessionId,
            UserName = user.Username,
            FullName = user.FullName,
            PasswordResetRequired = user.PasswordResetRequired,
            TokenExpiresAt = expiresAt,
            Permissions = accessibleFunctionIds
        };
    }

    private sealed record MfaPendingLogin(int UserId, string FinancialYear);

    public async Task<Result<LoginResultDto>> RefreshTokenAsync(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var refreshKey = $"ubis:refresh:{request.RefreshToken}";
            var replayKey = $"ubis:refresh:replay:{request.RefreshToken}";
            var stored = await _cacheService.GetAsync<RefreshTokenData>(refreshKey, cancellationToken);
            if (stored == null)
            {
                // See RefreshReplayGrace's doc comment - a concurrent racer for the same token
                // that lost the rotation below may still be within the short replay window.
                var replay = await _cacheService.GetAsync<LoginResultDto>(replayKey, cancellationToken);
                if (replay != null)
                {
                    return Result<LoginResultDto>.Success(replay);
                }

                return Result<LoginResultDto>.Failure(
                    Error.InvalidCredentials("Refresh token is invalid or has expired. Please log in again."));
            }

            // Rotate: the old refresh token can only ever be used once.
            await _cacheService.RemoveAsync(refreshKey, cancellationToken);

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == stored.UserId, cancellationToken);
            if (user == null || !user.IsActive)
            {
                return Result<LoginResultDto>.Failure(Error.InactiveUser("Account is no longer active."));
            }

            var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleId == stored.RoleId, cancellationToken);
            if (role == null)
            {
                return Result<LoginResultDto>.Failure(Error.NotFound("Role", stored.RoleId));
            }

            var userCharge = await _dbContext.MapUserRoles
                .FirstOrDefaultAsync(uc => uc.UserId == user.UserId && uc.IsActive, cancellationToken);
            if (userCharge == null)
            {
                return Result<LoginResultDto>.Failure(Error.NotFound("MapUserRole", user.UserId.ToString()));
            }

            var accessibleFunctionIds = await _dbContext.RoleFunctionMappings
                .Where(rf => rf.RoleId == role.RoleId && rf.Active != "N" && rf.RFFreez != "Y")
                .Select(rf => rf.FunctionId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var demandIdCsvValues = await _dbContext.UserDemandMappings
                .Where(m => m.UserId == user.UserId && m.IsActive != false)
                .Select(m => m.DemandIds)
                .ToListAsync(cancellationToken);
            var accessibleDemandIds = await ParseDemandIdsAsync(demandIdCsvValues, stored.FinancialYear, cancellationToken);

            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);
            var newRefreshToken = await IssueRefreshTokenAsync(
                user.UserId, role.RoleId, stored.FinancialYear, stored.SessionId, cancellationToken);

            var refreshedResult = new LoginResultDto
            {
                Token = GenerateJwtToken(user, role, accessibleDemandIds, stored.FinancialYear, stored.SessionId, accessibleFunctionIds, expiresAt),
                RefreshToken = newRefreshToken,
                SessionId = stored.SessionId,
                UserName = user.Username,
                FullName = user.FullName,
                PasswordResetRequired = user.PasswordResetRequired,
                TokenExpiresAt = expiresAt,
                Permissions = accessibleFunctionIds
            };

            // See RefreshReplayGrace's doc comment - lets a same-token racer that arrives just
            // after this rotation reuse this exact result instead of failing.
            await _cacheService.SetAsync(replayKey, refreshedResult, RefreshReplayGrace, cancellationToken: cancellationToken);

            return Result<LoginResultDto>.Success(refreshedResult);
        }
        catch (Exception ex)
        {
            await _logWriter.ErrorAsync("Unhandled error during token refresh.", ex, ct: cancellationToken);
            return Result<LoginResultDto>.Failure(
                Error.InternalError($"An error occurred while refreshing the session: {ex.Message}"));
        }
    }

    private async Task<string> IssueRefreshTokenAsync(
        int userId, int roleId, string financialYear, string sessionId, CancellationToken ct)
    {
        var refreshToken = Guid.NewGuid().ToString("N");
        await _cacheService.SetAsync(
            $"ubis:refresh:{refreshToken}",
            new RefreshTokenData(userId, roleId, financialYear, sessionId),
            TimeSpan.FromDays(_jwtOptions.RefreshTokenExpirationDays),
            cancellationToken: ct);
        return refreshToken;
    }

    private sealed record RefreshTokenData(int UserId, int RoleId, string FinancialYear, string SessionId);

    public async Task<Result<ChangePasswordResultDto>> ChangePasswordAsync(
        int userId,
        ChangePasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.NewPassword != request.ConfirmNewPassword)
            {
                return Result<ChangePasswordResultDto>.Failure(
                    Error.PasswordMismatch("Password confirmation does not match new password."));
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
            if (user == null)
            {
                return Result<ChangePasswordResultDto>.Failure(Error.UserNotFound(userId.ToString()));
            }

            if (user.PasswordHash == null || !TryVerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                return Result<ChangePasswordResultDto>.Failure(
                    Error.InvalidCredentials("Current password is incorrect."));
            }

            var applyResult = await ApplyNewPasswordAsync(user, request.NewPassword, cancellationToken);
            if (!applyResult.IsSuccess)
            {
                return Result<ChangePasswordResultDto>.Failure(applyResult.Error!);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            var clientIp = ExtractClientIpAddress();
            var revokedCount = await RevokeAllSessionsForUserAsync(user.UserId, cancellationToken);
            await LogSecurityEventAsync(user.UserId, clientIp, "PasswordChanged",
                $"User password was successfully changed; {revokedCount} outstanding session(s) revoked", cancellationToken);

            return Result<ChangePasswordResultDto>.Success(new ChangePasswordResultDto
            {
                Success = true,
                Message = "Password changed successfully.",
                ChangedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            await _logWriter.ErrorAsync("Unhandled error during password change.", ex, new { UserId = userId }, cancellationToken);
            return Result<ChangePasswordResultDto>.Failure(
                Error.InternalError($"An error occurred: {ex.Message}"));
        }
    }

    /// <summary>
    /// No-session counterpart to <see cref="AuthenticateAsync"/> for the one case a user genuinely
    /// cannot authenticate their way around: a first-time login blocked by
    /// User.PasswordResetRequired, before any JWT/Redis session was ever issued. Re-verifies the
    /// current (default) password directly instead of trusting a bearer token, only allows the
    /// change while PasswordResetRequired is actually set (not a general no-auth password-change
    /// backdoor), then signs the user straight in on success — same rate-limiting/IP-binding/role
    /// resolution as a normal login, so the frontend doesn't need a second round trip through
    /// /login with the new password.
    /// </summary>
    public async Task<Result<LoginResultDto>> ChangeDefaultPasswordAsync(
        ChangeDefaultPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.NewPassword != request.ConfirmNewPassword)
            {
                return Result<LoginResultDto>.Failure(
                    Error.PasswordMismatch("Password confirmation does not match new password."));
            }

            var clientIp = ExtractClientIpAddress();

            var user = await _dbContext.Users.FirstOrDefaultAsync(
                u => u.Username == request.UserName, cancellationToken);

            // Same permanent-lockout gate as AuthenticateAsync's Step 2.1a — this is a fully
            // anonymous endpoint (no session/JWT exists yet for a first-time-login user), so
            // without this check it would be an unthrottled brute-force vector once the old
            // 15-minute Redis rate-limit was removed (2026-08-20, same client direction as
            // AuthenticateAsync: no temporary phase, just a 5-attempt permanent lock).
            if (user != null && user.IsLocked)
            {
                await LogSecurityEventAsync(user.UserId, clientIp, "LoginFailure",
                    $"Locked user attempted forced password change: {request.UserName}", cancellationToken);

                return Result<LoginResultDto>.Failure(
                    Error.InactiveUser("Account is locked due to repeated failed login attempts. Contact your administrator."));
            }

            var eligibleForYear = user != null && await _dbContext.UserFinancialYears
                .AnyAsync(fy => fy.UserId == user.UserId && fy.FinancialYear == request.FinancialYear, cancellationToken);

            if (!eligibleForYear || user?.PasswordHash == null || !TryVerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                if (user != null)
                {
                    user.FailedLoginAttempts = (user.FailedLoginAttempts ?? 0) + 1;
                    if (user.FailedLoginAttempts == 1)
                    {
                        user.FirstFailLoginAttemptDate = DateTime.UtcNow;
                    }
                    else if (user.FailedLoginAttempts >= 5)
                    {
                        user.IsLocked = true;
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                await LogSecurityEventAsync(user?.UserId, clientIp, "LoginFailure",
                    $"Failed forced-password-change attempt for user: {request.UserName}", cancellationToken);

                return Result<LoginResultDto>.Failure(
                    Error.InvalidCredentials("Current password is incorrect."));
            }

            if (!user.PasswordResetRequired)
            {
                return Result<LoginResultDto>.Failure(
                    Error.InvalidCredentials(
                        "Password reset is not required for this account. Log in and use the authenticated change-password endpoint instead."));
            }

            if (!user.IsActive)
            {
                await LogSecurityEventAsync(user.UserId, clientIp, "LoginFailure",
                    $"Inactive/frozen user attempted forced password change: {request.UserName}", cancellationToken);

                return Result<LoginResultDto>.Failure(
                    Error.InactiveUser("Access denied. Your profile is inactive. Contact administrator."));
            }

            var defaultChangeIpMapping = await _dbContext.MapUserIPAddresses
                .FirstOrDefaultAsync(m => m.UserId == user.UserId, cancellationToken);
            if (await IsIpBindingEnabledAsync()
                && !IsIpAllowed(clientIp, defaultChangeIpMapping?.IPAddress1, defaultChangeIpMapping?.IPAddress2))
            {
                _dbContext.SecurityEvents.Add(new SecurityEvent
                {
                    EventType = "IpBindingFailure",
                    UserId = user.UserId,
                    IpAddress = clientIp,
                    Timestamp = DateTime.UtcNow,
                    Detail = $"Unauthorized network access attempt from IP: {clientIp}"
                });
                await _dbContext.SaveChangesAsync(cancellationToken);

                return Result<LoginResultDto>.Failure(
                    Error.IpBindingFailure($"Access denied from unauthorized network location. Your IP ({clientIp}) is not allowed."));
            }

            var userCharge = await _dbContext.MapUserRoles
                .FirstOrDefaultAsync(uc => uc.UserId == user.UserId && uc.IsActive, cancellationToken);
            if (userCharge == null)
            {
                return Result<LoginResultDto>.Failure(
                    Error.NotFound("MapUserRole", user.UserId.ToString()));
            }

            var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleId == userCharge.RoleId, cancellationToken);
            if (role == null)
            {
                return Result<LoginResultDto>.Failure(Error.NotFound("Role", $"{user.UserId}:{userCharge.RoleId}"));
            }

            var applyResult = await ApplyNewPasswordAsync(user, request.NewPassword, cancellationToken);
            if (!applyResult.IsSuccess)
            {
                return Result<LoginResultDto>.Failure(applyResult.Error!);
            }
            await _dbContext.SaveChangesAsync(cancellationToken);

            await LogSecurityEventAsync(user.UserId, clientIp, "PasswordChanged",
                "User completed forced password change on first login", cancellationToken);

            return Result<LoginResultDto>.Success(
                await IssueLoginResultAsync(user, role, userCharge, request.FinancialYear, clientIp, cancellationToken));
        }
        catch (Exception ex)
        {
            await _logWriter.ErrorAsync("Unhandled error during forced password change.", ex, new { request.UserName }, cancellationToken);
            return Result<LoginResultDto>.Failure(
                Error.InternalError($"An error occurred: {ex.Message}"));
        }
    }

    public async Task<Result<ForgotPasswordResultDto>> ForgotPasswordAsync(
        ForgotPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if (user == null)
            {
                return Result<ForgotPasswordResultDto>.Success(new ForgotPasswordResultDto
                {
                    Success = true,
                    Message = "If an account with that email exists, a password reset link will be sent."
                });
            }

            var resetToken = await GenerateAndCacheResetTokenAsync(user, cancellationToken);

            var clientIp = ExtractClientIpAddress();
            await LogSecurityEventAsync(user.UserId, clientIp, "PasswordResetRequested",
                "User requested password reset", cancellationToken);

            return Result<ForgotPasswordResultDto>.Success(new ForgotPasswordResultDto
            {
                Success = true,
                Message = "If an account with that email exists, a password reset link will be sent.",
                ResetToken = resetToken // For development only; remove in production
            });
        }
        catch (Exception ex)
        {
            await _logWriter.ErrorAsync("Unhandled error during forgot-password request.", ex, ct: cancellationToken);
            return Result<ForgotPasswordResultDto>.Failure(
                Error.InternalError($"An error occurred: {ex.Message}"));
        }
    }

    public async Task<Result<ResetPasswordResultDto>> ResetPasswordAsync(
        ResetPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.NewPassword != request.ConfirmNewPassword)
            {
                return Result<ResetPasswordResultDto>.Failure(
                    Error.PasswordMismatch("Password confirmation does not match new password."));
            }

            var userIdResult = await ValidateResetTokenAsync(request.ResetToken, cancellationToken);
            if (!userIdResult.IsSuccess)
            {
                return Result<ResetPasswordResultDto>.Failure(userIdResult.Error!);
            }

            var userId = userIdResult.Data;
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

            if (user == null)
            {
                return Result<ResetPasswordResultDto>.Failure(Error.UserNotFound(userId.ToString()));
            }

            var applyResult = await ApplyNewPasswordAsync(user, request.NewPassword, cancellationToken);
            if (!applyResult.IsSuccess)
            {
                return Result<ResetPasswordResultDto>.Failure(applyResult.Error!);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await _cacheService.RemoveAsync($"ubis:reset:{request.ResetToken}", cancellationToken);

            var revokedCount = await RevokeAllSessionsForUserAsync(user.UserId, cancellationToken);
            var clientIp = ExtractClientIpAddress();
            await LogSecurityEventAsync(user.UserId, clientIp, "PasswordReset",
                $"User password was reset using reset token; {revokedCount} outstanding session(s) revoked", cancellationToken);

            return Result<ResetPasswordResultDto>.Success(new ResetPasswordResultDto
            {
                Success = true,
                Message = "Password has been reset successfully.",
                ResetAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            await _logWriter.ErrorAsync("Unhandled error during password reset.", ex, ct: cancellationToken);
            return Result<ResetPasswordResultDto>.Failure(
                Error.InternalError($"An error occurred: {ex.Message}"));
        }
    }

    /// <summary>See IAuthenticationService's doc comment.</summary>
    public async Task<PasswordResetAvailabilityDto> GetPasswordResetOtpAvailabilityAsync(CancellationToken cancellationToken = default) => new()
    {
        EmailEnabled = _passwordResetOtpOptions.EmailEnabled,
        MobileEnabled = await _appSettingsReader.GetBoolAsync("EnableSms", ct: cancellationToken)
    };

    public async Task<PasswordResetOtpRequestResultDto> RequestPasswordResetOtpAsync(
        PasswordResetOtpRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var availability = await GetPasswordResetOtpAvailabilityAsync(cancellationToken);
        if (!availability.Available)
        {
            return new PasswordResetOtpRequestResultDto
            {
                Available = false,
                Success = false,
                Message = "Password reset by OTP is not available right now. Contact your administrator to reset your password."
            };
        }

        try
        {
            // M_User is one row per person (as of the 2026-08-03 consolidation) — LoginId alone
            // identifies the row; Email/Mobile are person-level fields regardless.
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == request.UserName, cancellationToken);

            const string genericMessage = "If an account with that username exists, a verification code has been sent.";

            if (user == null)
            {
                // Anti-enumeration: identical shape whether or not the username exists.
                return new PasswordResetOtpRequestResultDto { Available = true, Success = true, Message = genericMessage };
            }

            await _otpService.GenerateAndStoreOtpAsync(user.UserId, OtpPurpose.PasswordReset, cancellationToken);

            var clientIp = ExtractClientIpAddress();
            await LogSecurityEventAsync(user.UserId, clientIp, "PasswordResetOtpRequested",
                $"Forgot-password OTP requested for user: {user.Username}", cancellationToken);

            var plainMobile = _mobileProtectionService.Unprotect(user.EncryptedMobile);

            return new PasswordResetOtpRequestResultDto
            {
                Available = true,
                Success = true,
                Message = genericMessage,
                MaskedEmail = availability.EmailEnabled && !string.IsNullOrWhiteSpace(user.Email) ? MaskEmail(user.Email) : null,
                MaskedMobile = availability.MobileEnabled && !string.IsNullOrWhiteSpace(plainMobile) ? _mobileProtectionService.Mask(plainMobile) : null
            };
        }
        catch (Exception ex)
        {
            await _logWriter.ErrorAsync("Unhandled error during password-reset OTP request.", ex, ct: cancellationToken);
            return new PasswordResetOtpRequestResultDto
            {
                Available = true,
                Success = false,
                Message = "An error occurred. Please try again."
            };
        }
    }

    public async Task<VerifyPasswordResetOtpResultDto> VerifyPasswordResetOtpAsync(
        VerifyPasswordResetOtpRequestDto request,
        CancellationToken cancellationToken = default)
    {
        const string genericFailure = "Invalid or expired code.";

        try
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == request.UserName, cancellationToken);

            if (user == null)
            {
                // Same message as a genuinely wrong/expired code — no existence leak.
                return new VerifyPasswordResetOtpResultDto { Success = false, Message = genericFailure };
            }

            var verifyResult = await _otpService.VerifyOtpAsync(user.UserId, OtpPurpose.PasswordReset, request.Otp, cancellationToken);
            if (!verifyResult.IsSuccess)
            {
                return new VerifyPasswordResetOtpResultDto { Success = false, Message = verifyResult.Error?.Message ?? genericFailure };
            }

            var resetToken = await GenerateAndCacheResetTokenAsync(user, cancellationToken);

            var clientIp = ExtractClientIpAddress();
            await LogSecurityEventAsync(user.UserId, clientIp, "PasswordResetOtpVerified",
                $"Forgot-password OTP verified for user: {user.Username}", cancellationToken);

            return new VerifyPasswordResetOtpResultDto
            {
                Success = true,
                Message = "Code verified. Set a new password to continue.",
                ResetToken = resetToken
            };
        }
        catch (Exception ex)
        {
            await _logWriter.ErrorAsync("Unhandled error during password-reset OTP verification.", ex, ct: cancellationToken);
            return new VerifyPasswordResetOtpResultDto { Success = false, Message = "An error occurred. Please try again." };
        }
    }

    /// <summary>Shared by <see cref="ForgotPasswordAsync"/> (email-link flow) and
    /// <see cref="VerifyPasswordResetOtpAsync"/> (OTP flow) — both hand the caller a token that
    /// <see cref="ResetPasswordAsync"/> later consumes.</summary>
    private async Task<string> GenerateAndCacheResetTokenAsync(User user, CancellationToken cancellationToken)
    {
        var resetToken = Guid.NewGuid().ToString();

        await _cacheService.SetAsync(
            $"ubis:reset:{resetToken}",
            new { UserId = user.UserId, Email = user.Email, CreatedAt = DateTime.UtcNow },
            TimeSpan.FromHours(1),
            cancellationToken: cancellationToken);

        return resetToken;
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return email;
        var local = parts[0];
        var domain = parts[1];
        return local.Length <= 2 ? "**@" + domain : local[..2] + "***@" + domain;
    }

    public async Task<Result<int>> ValidateResetTokenAsync(
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenData = await _cacheService.GetAsync<dynamic>(
                $"ubis:reset:{resetToken}", cancellationToken);

            if (tokenData == null)
            {
                return Result<int>.Failure(
                    Error.ValidationError("ResetToken", "Token is invalid or has expired."));
            }

            return Result<int>.Success((int)tokenData.UserId);
        }
        catch (Exception ex)
        {
            await _logWriter.ErrorAsync("Unhandled error during reset-token validation.", ex, ct: cancellationToken);
            return Result<int>.Failure(
                Error.InternalError($"An error occurred: {ex.Message}"));
        }
    }

    public async Task<Result<bool>> LogoutAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _cacheService.RemoveAsync($"ubis:session:{sessionId}", cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await _logWriter.ErrorAsync("Unhandled error during logout.", ex, new { SessionIdSuffix = sessionId.Length > 4 ? sessionId[^4..] : sessionId }, cancellationToken);
            return Result<bool>.Failure(
                Error.InternalError($"An error occurred during logout: {ex.Message}"));
        }
    }

    public async Task<bool> IsSessionValidAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        return await _cacheService.ExistsAsync($"ubis:session:{sessionId}", cancellationToken);
    }

    // Helper Methods
    private string ExtractClientIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return "Unknown";

        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var rawIp = forwardedFor.ToString().Split(',')[0].Trim();
            if (System.Net.IPAddress.TryParse(rawIp, out _))
            {
                return rawIp;
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private bool IsIpAllowed(string clientIp, string? allowedOne, string? allowedTwo)
    {
        if (string.IsNullOrEmpty(clientIp) || clientIp == "Unknown")
            return false;

        return (clientIp == allowedOne && !string.IsNullOrEmpty(allowedOne)) ||
               (clientIp == allowedTwo && !string.IsNullOrEmpty(allowedTwo));
    }

    private string GenerateJwtToken(
        User user,
        Role role,
        List<int> accessibleDemandIds,
        string financialYear,
        string sessionId,
        List<int> accessibleFunctionIds,
        DateTime expiresAtUtc)
    {
        var claims = new List<Claim>
        {
            new("sub", user.UserId.ToString()),
            new("UserName", user.Username),
            new("FullName", user.FullName),
            new("RoleId", role.RoleId.ToString()),
            new("RoleName", role.RoleName),
            new("userRole", role.RoleName),
            new("FinancialYear", financialYear),
            new("sid", sessionId)
        };

        // Existence-based Function access claims (added 2026-07-13, replaces the earlier
        // invented "AIM:{FunctionCode}:{C|R|U|D}" CRUD-matrix shape — the real
        // dbo.M_MapRoleFunction table has no CRUD columns and dbo.M_Function has no
        // FunctionCode column, so Functions are identified by FunctionId directly).
        foreach (var functionId in accessibleFunctionIds)
        {
            claims.Add(new Claim($"AIM:Function:{functionId}", "true"));
        }

        // Demand access claims (added 2026-07-13, user-confirmed): AIM:Demand:{id} claims are the
        // multi-Demand access list from dbo.M_MapUserDemand, same existence-based pattern as
        // Function claims. (The single-default-Demand "DemandCode" claim was removed 2026-08-04
        // along with M_MapUserRole.DemandCode - it was unused downstream and 99.9% NULL in
        // practice; M_MapUserDemand remains the real source of Demand access.)
        foreach (var demandId in accessibleDemandIds)
        {
            claims.Add(new Claim($"AIM:Demand:{demandId}", "true"));
        }

        return JwtTokenFactory.CreateToken(_jwtOptions, claims, expiresAtUtc);
    }

    /// <summary>
    /// Flattens the legacy comma-separated DemandId lists from multiple
    /// <c>dbo.M_MapUserDemand</c> rows into one distinct int list, skipping blank/non-numeric
    /// entries rather than throwing (legacy data quality is not guaranteed). The literal value
    /// "ALL" (same broad-access convention as the old app's <c>UserDetails.DemandId</c> — confirmed
    /// 2,739 of ~6,942 legacy rows used it) is expanded to every active <c>dbo.M_Demand.DemandId</c>
    /// for <paramref name="financialYear"/>, rather than being silently dropped as an unparseable
    /// int — fixed 2026-07-23 after this dropped every "ALL"-access user's Demand claims to zero,
    /// confirmed live against a real ABO/DS/Director test account whose GetMyDemands result was
    /// unexpectedly empty despite M_MapUserDemand.DemandId = 'ALL'.
    /// </summary>
    private async Task<List<int>> ParseDemandIdsAsync(IEnumerable<string?> csvValues, string financialYear, CancellationToken cancellationToken)
    {
        var result = new HashSet<int>();
        var hasAll = false;

        foreach (var csv in csvValues)
        {
            if (string.IsNullOrWhiteSpace(csv)) continue;

            foreach (var part in csv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (string.Equals(part, "ALL", StringComparison.OrdinalIgnoreCase))
                {
                    hasAll = true;
                }
                else if (int.TryParse(part, out var demandId))
                {
                    result.Add(demandId);
                }
            }
        }

        if (hasAll)
        {
            // No IsActive filter: confirmed live that dbo.M_Demand.IsActive is NULL for every one
            // of the 105 real 2026-2027 rows (never populated in this dataset) — filtering on it
            // silently excluded every demand. Every row for the year is treated as active.
            // DemandType == "E" filter added 2026-08-28 (client requirement) so an "ALL"-access
            // user's claims stay consistent with what GetMyDemands actually lists — an explicit
            // per-DemandId M_MapUserDemand assignment (the non-ALL branch above) is untouched by
            // this filter, only the broad "every Demand" wildcard expansion is scoped to type E.
            var allDemandIds = await _dbContext.Demands
                .Where(d => d.FinancialYear == financialYear && d.DemandType == "E")
                .Select(d => d.DemandId)
                .ToListAsync(cancellationToken);
            foreach (var id in allDemandIds)
            {
                result.Add(id);
            }
        }

        return result.ToList();
    }

    /// <summary>
    /// Safely verifies a password against a stored hash. <c>BCrypt.Verify</c> throws
    /// <c>SaltParseException</c> (message includes "invalid salt") when the stored value isn't a
    /// well-formed BCrypt hash. Treat any such malformed-hash case as a normal verification
    /// failure (wrong credentials), not an unhandled exception that would 500 the login request.
    /// </summary>
    private static bool TryVerifyPassword(string suppliedPassword, string storedHash)
    {
        try
        {
            return BCrypt.Verify(suppliedPassword, storedHash);
        }
        catch (Exception ex) when (ex is SaltParseException or FormatException or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Shared password-set logic for <see cref="ChangePasswordAsync"/>,
    /// <see cref="ChangeDefaultPasswordAsync"/>, and <see cref="ResetPasswordAsync"/> (added
    /// 2026-08-17). Rejects the new password if it BCrypt-verifies against the user's current hash
    /// or any of <c>OldPassword1/2/3</c>; on success rotates the 3-deep history before overwriting
    /// the current hash. Does not call <see cref="AimDbContext.SaveChangesAsync"/> — callers save
    /// alongside their own other changes in the same request.
    /// </summary>
    /// <summary>
    /// FR-006 (Token Refresh &amp; Revocation), added 2026-08-21: deletes every
    /// <c>ubis:session:{sid}</c> Redis key belonging to this user, including the session the caller
    /// is currently authenticated with — a password change/reset revokes everything, forcing a full
    /// re-login everywhere, matching the FRS's literal "all previously issued tokens are revoked".
    /// Same scan+filter approach SessionMonitorController's Active Session Monitor already uses to
    /// list sessions (no separate per-user index needed at this scale); duplicated rather than
    /// shared with ComplianceController.ForceLogoutUser's identical logic, consistent with this
    /// solution's per-layer duplication convention elsewhere.
    /// </summary>
    private async Task<int> RevokeAllSessionsForUserAsync(int userId, CancellationToken ct)
    {
        var keys = await _cacheService.GetKeysByPatternAsync("ubis:session:*", ct);
        var revoked = 0;
        foreach (var key in keys)
        {
            var record = await _cacheService.GetAsync<UBIS.Services.Aim.Application.DTOs.User.SessionRecordDto>(key, ct);
            if (record?.UserId == userId)
            {
                await _cacheService.RemoveAsync(key, ct);
                revoked++;
            }
        }
        return revoked;
    }

    private async Task<Result<bool>> ApplyNewPasswordAsync(User user, string newPlainPassword, CancellationToken ct)
    {
        var (isValid, policyError) = await _passwordPolicyValidator.ValidateAsync(user.UserId, newPlainPassword, ct);
        if (!isValid)
        {
            var message = policyError switch
            {
                PasswordValidationError.TooShort =>
                    $"Password must be at least {_passwordPolicyOptions.MinLength} characters long.",
                PasswordValidationError.MissingUppercase =>
                    "Password must contain at least one uppercase letter.",
                PasswordValidationError.MissingLowercase =>
                    "Password must contain at least one lowercase letter.",
                PasswordValidationError.MissingSpecialChar =>
                    "Password must contain at least one special character.",
                PasswordValidationError.SequentialDigits =>
                    "Password cannot contain a sequential run of digits (e.g. 1234 or 4321).",
                PasswordValidationError.ContainsPersonalInfo =>
                    "Password cannot contain your username, name, or email.",
                PasswordValidationError.CommonPassword =>
                    "Password is too common or predictable. Choose a stronger password.",
                _ => "Password does not meet policy requirements."
            };
            return Result<bool>.Failure(Error.PasswordPolicyViolation(message));
        }

        if (user.PasswordHash != null && TryVerifyPassword(newPlainPassword, user.PasswordHash))
        {
            return Result<bool>.Failure(Error.PasswordReused());
        }

        foreach (var oldHash in new[] { user.OldPassword1, user.OldPassword2, user.OldPassword3 })
        {
            if (oldHash != null && TryVerifyPassword(newPlainPassword, oldHash))
            {
                return Result<bool>.Failure(Error.PasswordReused());
            }
        }

        user.OldPassword3 = user.OldPassword2;
        user.OldPassword2 = user.OldPassword1;
        user.OldPassword1 = user.PasswordHash;

        user.PasswordHash = BCrypt.HashPassword(newPlainPassword, 12);
        user.PasswordResetRequired = false;
        user.LastPasswordChangeDate = DateTime.UtcNow;

        return Result<bool>.Success(true);
    }

    private async Task LogSecurityEventAsync(
        int? userId,
        string ipAddress,
        string eventType,
        string detail,
        CancellationToken cancellationToken)
    {
        try
        {
            _dbContext.SecurityEvents.Add(new SecurityEvent
            {
                EventType = eventType,
                UserId = userId,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow,
                Detail = detail
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _logWriter.ErrorAsync("Failed to persist SecurityEvent.", ex, new { EventType = eventType, UserId = userId }, cancellationToken);
        }

        var properties = new { EventType = eventType, UserId = userId, IpAddress = ipAddress };
        if (eventType.Contains("Failure", StringComparison.OrdinalIgnoreCase))
        {
            await _logWriter.WarnAsync(detail, properties, cancellationToken);
        }
        else
        {
            await _logWriter.InfoAsync(detail, properties, cancellationToken);
        }
    }
}
