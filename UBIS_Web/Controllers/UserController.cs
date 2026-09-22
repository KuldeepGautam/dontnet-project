namespace UBIS.Web.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UBIS.Web.Models;
using UBIS.Web.Services.Caching;
using UBIS.Web.Services.Captcha;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Configuration;
using UBIS.Web.Services.Logging;
using UBIS.Web.Services.Session;

/// <summary>
/// Single controller for Login, Logout, Dashboard, and Edit Profile, per the app's design:
/// everything the signed-in user's own identity/session touches lives here.
/// </summary>
public class UserController : Controller
{
    private static readonly TimeSpan MenuCacheTtl = TimeSpan.FromMinutes(5);

    // Negative-cache TTL for a failed MenuGenerator call (2026-07-20). AddStandardResilienceHandler's
    // circuit breaker needs at least 8 sampled requests in its 30s window before it can open (see
    // NFR_ARCHITECTURE.md §2) — a login only makes one menu-fetch call, so a handful of manual
    // retries never reach that threshold and the breaker never trips, meaning every single login
    // attempt independently pays the full 5-retry storm against a downed MenuGenerator. This short
    // negative cache remembers "just failed" for a bit so a retry fails fast instead, while still
    // healing itself (a fresh attempt goes out) once this window expires.
    private static readonly TimeSpan MenuUnavailableCacheTtl = TimeSpan.FromSeconds(20);

    private readonly IAimClient _aimClient;
    private readonly IMenuClient _menuClient;
    private readonly IWebLogClient _logClient;
    private readonly IAppCacheService _cache;
    private readonly ICaptchaService _captchaService;
    private readonly IConfiguration _configuration;
    private readonly IAntiforgery _antiforgery;
    private readonly AppSettingsCache _appSettingsCache;

    public UserController(
        IAimClient aimClient, IMenuClient menuClient, IWebLogClient logClient, IAppCacheService cache,
        ICaptchaService captchaService, IConfiguration configuration, IAntiforgery antiforgery,
        AppSettingsCache appSettingsCache)
    {
        _aimClient = aimClient;
        _menuClient = menuClient;
        _logClient = logClient;
        _cache = cache;
        _captchaService = captchaService;
        _appSettingsCache = appSettingsCache;
        _configuration = configuration;
        _antiforgery = antiforgery;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string? returnUrl = null, CancellationToken ct = default)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Dashboard));
        }

        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl,
            CaptchaImageSvg = _captchaService.GenerateChallenge(HttpContext.Session),
            ErrorMessage = TempData["LoginError"] as string,
            InfoMessage = TempData["LoginInfo"] as string
        });
    }

    /// <summary>
    /// AJAX-only captcha refresh ("New code" on the Login page) — returns just the new SVG data
    /// URI instead of the "New code" link doing a full page GET, which previously reloaded the
    /// whole Login page and discarded anything already typed into Username/Password/FinancialYear.
    /// Anonymous, same as Login itself; regenerates the session-held answer exactly like Login
    /// GET/POST already do via CaptchaService.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult RefreshCaptcha() =>
        Json(new { captchaImageSvg = _captchaService.GenerateChallenge(HttpContext.Session) });

    /// <summary>
    /// AJAX-only (2026-07-20): the Login form now submits via fetch instead of a native postback,
    /// so this always returns a JSON envelope (<c>{ success, mfaRequired?, redirectUrl?,
    /// errorMessage?, captchaImageSvg? }</c>) rather than re-rendering the view or redirecting.
    /// <c>[FromBody]</c> since the client sends JSON, not a form-urlencoded body — the antiforgery
    /// token still comes through as the <c>X-CSRF-TOKEN</c> header (see Program.cs's
    /// AddAntiforgery config), which <see cref="ValidateAntiForgeryTokenAttribute"/> checks
    /// regardless of body content-type.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return Json(new
            {
                success = false,
                errorMessage = "Please complete all required fields.",
                captchaImageSvg = _captchaService.GenerateChallenge(HttpContext.Session)
            });
        }

        // Added 2026-07 per client MOM ("Username, Password, and CAPTCHA are required"). field
        // "captcha" (bug fix 2026-08-06) tells the client to show this inline under the CAPTCHA
        // box (matching the older UI) instead of in the generic top-of-form banner.
        if (!_captchaService.Validate(HttpContext.Session, model.CaptchaAnswer))
        {
            return Json(new
            {
                success = false,
                field = "captcha",
                errorMessage = "You have entered incorrect Captcha code. Enter valid code.",
                captchaImageSvg = _captchaService.GenerateChallenge(HttpContext.Session)
            });
        }

        // Financial Year is not a user-facing choice on the login form (removed again 2026-07-30
        // per client request) - resolved silently server-side instead, from the row flagged
        // IsCurrentYear=1 in the new dbo.M_FinancialYear master (falls back to the newest row if
        // none is flagged current, so a misconfigured table doesn't hard-fail every login).
        var financialYear = await ResolveFinancialYearAsync(ct);

        var loginResult = await _aimClient.LoginAsync(new LoginRequestDto
        {
            UserName = model.UserName,
            Password = model.Password,
            FinancialYear = financialYear
        }, ct);

        if (!loginResult.IsSuccess || loginResult.Data == null)
        {
            // AIM's own 429/RATE_LIMIT_EXCEEDED branch was removed 2026-08-20 (no more temporary
            // lockout phase - see AuthenticationService.AuthenticateAsync) - the permanent-lock
            // message now flows through the default case below via loginResult.Error?.Message,
            // same as every other non-IP-binding failure.
            var errorMessage = loginResult.StatusCode switch
            {
                403 => "Access denied from an unauthorized network location.",
                _ => loginResult.Error?.Message ?? "Username or Password is wrong."
            };

            await _logClient.WarnAsync("Login failed", new { model.UserName, loginResult.StatusCode }, ct);
            return Json(new
            {
                success = false,
                errorMessage,
                captchaImageSvg = _captchaService.GenerateChallenge(HttpContext.Session)
            });
        }

        var login = loginResult.Data;

        // Login MFA (added 2026-07, default inert — see Mfa:Enabled in AIM). AIM paused before
        // issuing a token; hand off to the OTP-verification step instead of signing in directly.
        // TempData still works the same way under an AJAX response — it's Session-backed, not
        // tied to a redirect — so the client navigating itself to VerifyOtp on mfaRequired below
        // still lands on a page that can read it back.
        if (login.MfaRequired && login.MfaChallengeUserId.HasValue)
        {
            TempData["Mfa.ChallengeUserId"] = login.MfaChallengeUserId.Value.ToString();
            TempData["Mfa.FinancialYear"] = financialYear;
            TempData["Mfa.ReturnUrl"] = model.ReturnUrl;
            return Json(new { success = true, mfaRequired = true, redirectUrl = Url.Action(nameof(VerifyOtp)) });
        }

        return await CompleteSignInAsync(login, financialYear, model.ReturnUrl, ct);
    }

    /// <summary>
    /// Login MFA step (added 2026-07). Reached only when AIM's <c>Mfa:Enabled</c> is on. Still a
    /// real page navigation (Login's JSON response tells the client to browse here on
    /// mfaRequired) — only the form submissions on this page are AJAX now, not arriving here.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp(CancellationToken ct)
    {
        if (TempData.Peek("Mfa.ChallengeUserId") is not string)
        {
            return RedirectToAction(nameof(Login));
        }

        var appSettings = await _appSettingsCache.GetAsync(ct);
        return View(new VerifyOtpViewModel
        {
            InfoMessage = TempData.Peek("Mfa.Info") as string,
            PhoneOtpEnabled = appSettings.EnableSms
        });
    }

    /// <summary>AJAX-only (2026-07-20) — see the Login action's summary for the envelope shape/rationale.</summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpViewModel model, CancellationToken ct)
    {
        if (TempData.Peek("Mfa.ChallengeUserId") is not string challengeUserIdRaw
            || !int.TryParse(challengeUserIdRaw, out var challengeUserId)
            || TempData.Peek("Mfa.FinancialYear") is not string financialYear)
        {
            return Json(new
            {
                success = false,
                errorMessage = "Your sign-in session has expired. Please log in again.",
                redirectUrl = Url.Action(nameof(Login))
            });
        }

        if (!ModelState.IsValid)
        {
            return Json(new { success = false, errorMessage = "Enter the 6-digit code." });
        }

        var verifyResult = await _aimClient.VerifyLoginOtpAsync(
            new VerifyLoginOtpRequestDto { ChallengeUserId = challengeUserId, Otp = model.Otp }, ct);

        if (!verifyResult.IsSuccess || verifyResult.Data == null)
        {
            return Json(new { success = false, errorMessage = verifyResult.Error?.Message ?? "Incorrect or expired code." });
        }

        var returnUrl = TempData.Peek("Mfa.ReturnUrl") as string;
        TempData.Remove("Mfa.ChallengeUserId");
        TempData.Remove("Mfa.FinancialYear");
        TempData.Remove("Mfa.ReturnUrl");

        return await CompleteSignInAsync(verifyResult.Data, financialYear, returnUrl, ct);
    }

    /// <summary>AJAX-only (2026-07-20) — returns a small JSON status instead of redirecting back to VerifyOtp with a TempData info message.</summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendOtp(CancellationToken ct)
    {
        if (TempData.Peek("Mfa.ChallengeUserId") is not string challengeUserIdRaw
            || !int.TryParse(challengeUserIdRaw, out var challengeUserId))
        {
            return Json(new { success = false, errorMessage = "Your sign-in session has expired. Please log in again.", redirectUrl = Url.Action(nameof(Login)) });
        }

        await _aimClient.ResendLoginOtpAsync(new ResendLoginOtpRequestDto { ChallengeUserId = challengeUserId }, ct);
        return Json(new { success = true, message = "A new code has been sent." });
    }

    /// <summary>
    /// In-page "Forgot Password?" dialog on the Login screen (added 2026-07-22) — same visual
    /// pattern as the default-password-change dialog (login.js's openChangePasswordDialog), but a
    /// standalone flow: request OTP -&gt; verify OTP -&gt; set new password, all via AJAX without
    /// leaving Login. These four actions are the dialog's only server surface; the older
    /// page-based ForgotPassword/ResetPassword actions below are unrelated and still reachable
    /// directly (e.g. a real emailed reset link, once SMTP exists) but are no longer linked from
    /// the Login page itself.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPasswordAvailability(CancellationToken ct)
    {
        var result = await _aimClient.GetPasswordResetOtpAvailabilityAsync(ct);
        if (!result.IsSuccess || result.Data == null)
        {
            return Json(new { available = false, emailEnabled = false, mobileEnabled = false });
        }

        return Json(new { available = result.Data.Available, emailEnabled = result.Data.EmailEnabled, mobileEnabled = result.Data.MobileEnabled });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestPasswordResetOtp([FromBody] PasswordResetOtpRequestDto model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.UserName))
        {
            return Json(new { success = false, errorMessage = "Enter your username." });
        }

        var result = await _aimClient.RequestPasswordResetOtpAsync(model, ct);
        if (!result.IsSuccess || result.Data == null)
        {
            return Json(new { success = false, errorMessage = "Could not reach the server. Please try again." });
        }

        var data = result.Data;
        if (!data.Available)
        {
            return Json(new { success = false, available = false, errorMessage = data.Message });
        }

        await _logClient.InfoAsync("Forgot-password OTP requested", new { model.UserName }, ct);
        return Json(new { success = data.Success, available = true, message = data.Message, maskedEmail = data.MaskedEmail, maskedMobile = data.MaskedMobile });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyPasswordResetOtp([FromBody] VerifyPasswordResetOtpRequestDto model, CancellationToken ct)
    {
        var result = await _aimClient.VerifyPasswordResetOtpAsync(model, ct);
        if (!result.IsSuccess || result.Data == null || !result.Data.Success)
        {
            await _logClient.WarnAsync("Forgot-password OTP verification failed", new { model.UserName }, ct);
            return Json(new { success = false, errorMessage = result.Data?.Message ?? "Invalid or expired code." });
        }

        return Json(new { success = true, resetToken = result.Data.ResetToken });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompletePasswordReset([FromBody] ResetPasswordViewModel model, CancellationToken ct)
    {
        if (model.NewPassword != model.ConfirmNewPassword)
        {
            return Json(new { success = false, errorMessage = "Passwords do not match." });
        }

        var result = await _aimClient.ResetPasswordAsync(new ResetPasswordRequestDto
        {
            ResetToken = model.ResetToken,
            NewPassword = model.NewPassword,
            ConfirmNewPassword = model.ConfirmNewPassword
        }, ct);

        if (!result.IsSuccess || result.Data?.Success != true)
        {
            return Json(new { success = false, errorMessage = result.Error?.Message ?? "This code has expired. Please start over." });
        }

        await _logClient.InfoAsync("Password reset via forgot-password OTP dialog", new { }, ct);
        return Json(new { success = true, message = "Your password has been reset. Please sign in with your new password." });
    }

    /// <summary>
    /// "Forgot Password?" request form (added 2026-07-22, per the designer's login redesign).
    /// Wired to AIM's real <c>POST /api/authentication/forgot-password</c> — the request itself
    /// completes for real (a reset token is generated and cached server-side in AIM), but until
    /// SMTP is configured (see EmailService), that token has no way to reach the user, so a
    /// reset can't actually be completed yet. The generic notice below says so plainly rather than
    /// silently pretending the flow is fully live.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _aimClient.ForgotPasswordAsync(
            new ForgotPasswordRequestDto { Email = model.Email, UserName = model.UserName }, ct);

        // Same message whether or not the account exists (AIM's own contract — see its
        // ForgotPasswordAsync doc comment) — never let this page reveal account existence.
        model.InfoMessage = result.IsSuccess
            ? "If an account with that email exists, password reset instructions will be sent to it once available."
            : null;
        model.ErrorMessage = result.IsSuccess
            ? null
            : "Could not process your request right now. Please try again shortly.";
        model.Email = string.Empty;
        model.UserName = null;
        return View(model);
    }

    /// <summary>
    /// The page a "Forgot Password" email's reset link lands on. Added 2026-07-22. Not reachable
    /// today by a real user (no SMTP yet — see <see cref="ForgotPassword()"/>), but the flow is
    /// fully wired against AIM's real reset-password endpoint so it starts working the moment
    /// email delivery is configured, with no further code changes here.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(string? token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return View(new ResetPasswordViewModel { TokenValid = false, ErrorMessage = "This reset link is invalid or has expired." });
        }

        var validateResult = await _aimClient.ValidateResetTokenAsync(token, ct);
        var tokenValid = validateResult.IsSuccess && validateResult.Data?.Valid == true;

        return View(new ResetPasswordViewModel
        {
            ResetToken = token,
            TokenValid = tokenValid,
            ErrorMessage = tokenValid ? null : "This reset link is invalid or has expired. Please request a new one."
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken ct)
    {
        model.TokenValid = true;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.NewPassword != model.ConfirmNewPassword)
        {
            model.ErrorMessage = "Passwords do not match.";
            return View(model);
        }

        var result = await _aimClient.ResetPasswordAsync(new ResetPasswordRequestDto
        {
            ResetToken = model.ResetToken,
            NewPassword = model.NewPassword,
            ConfirmNewPassword = model.ConfirmNewPassword
        }, ct);

        if (!result.IsSuccess || result.Data?.Success != true)
        {
            model.ErrorMessage = result.Error?.Message ?? "This reset link is invalid or has expired. Please request a new one.";
            model.TokenValid = false;
            return View(model);
        }

        await _logClient.InfoAsync("Password reset via Forgot Password flow", new { }, ct);
        TempData["LoginInfo"] = "Your password has been reset. Please sign in with your new password.";
        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// Shared tail of a successful login: signs in the cookie identity and stores the minimum
    /// session needed to do that. Used by both the direct (MFA-off) path and the post-verify-otp
    /// continuation. Returns the same AJAX JSON envelope as its two callers (2026-07-20) rather
    /// than redirecting, since both call sites are now driven by fetch.
    ///
    /// Role resolution, menu-tree resolution, and the compliance snapshot (Section 2/4 —
    /// freeze/IP-binding/staleness) used to happen synchronously right here, which meant every
    /// login waited on up to three sequential AIM/MenuGenerator round trips (each up to the full
    /// 5-retry resilience window if the target was slow/down — tester-reported ~55s worst case)
    /// before the page could even redirect to Dashboard. All three are now fetched by the client
    /// in parallel right after landing on Dashboard instead — see dashboard-bootstrap.js and
    /// <see cref="ResolveMyRole"/>/<see cref="SidebarMenuPartial"/>/<see cref="RefreshComplianceStatus"/>.
    /// Trade-off (same one already accepted for the menu/compliance deferral, now extended to
    /// role): session.RoleName/RoleId start blank/0 and IsAccountFrozen/AllowedIpAddressOne/Two/
    /// IsAdmin/role-gated PreBudget checks are consequently not fully active for the brief window
    /// between sign-in and ResolveMyRole completing — every one of those already fails closed
    /// (denies rather than grants) on a blank/zero role, so the window is a temporary
    /// under-permission, never an over-permission. session.Permissions (the actual
    /// FunctionId-based authorization list) is unaffected — it comes from the login result itself,
    /// not the role call, so real feature access still works instantly. PasswordResetRequired is
    /// likewise unaffected for the same reason.
    /// </summary>
    private async Task<IActionResult> CompleteSignInAsync(
        LoginResultDto login, string financialYear, string? returnUrl, CancellationToken ct)
    {
        var sessionData = new UbisSessionData
        {
            Token = login.Token,
            RefreshToken = login.RefreshToken,
            AimSessionId = login.SessionId,
            UserName = login.UserName,
            FullName = login.FullName,
            RoleName = string.Empty,
            RoleId = 0,
            FinancialYear = financialYear,
            TokenExpiresAtUtc = login.TokenExpiresAt,
            Permissions = login.Permissions,
            PasswordResetRequired = login.PasswordResetRequired
        };
        HttpContext.Session.SetUbisSession(sessionData);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, login.UserName),
            new("FullName", login.FullName),
            new("RoleName", string.Empty)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        // SignInAsync only sets the outgoing cookie for the *next* request — it does not
        // retroactively update HttpContext.User for this one (that was already populated, as
        // anonymous, by the authentication middleware earlier in the pipeline). Antiforgery's
        // GetAndStoreTokens below embeds whatever HttpContext.User currently is into the fresh
        // token, so without this line that "fresh" token would still be anonymous-flavored and
        // fail validation on the very next (now genuinely authenticated) request.
        HttpContext.User = principal;

        await _logClient.InfoAsync("Login succeeded", new { login.UserName }, ct);

        // ComplianceInterceptorFilter (runs on every authenticated request) still redirects any
        // OTHER page to ForcePasswordReset while PasswordResetRequired is set above — this flag
        // just lets the Login page itself show the change-password dialog in place immediately,
        // instead of navigating to Dashboard only to be bounced straight back by that filter.
        // Bug fix 2026-09-16 (client report: "after login click, sometimes users are redirect to
        // .../User/Logout"): Logout is [Authorize]-gated, so a request to it while the auth cookie
        // has already expired/been invalidated (e.g. session-monitor.js's forceLogout() submitting
        // its POST /User/Logout form right as the session dies) gets challenged by the cookie
        // middleware and redirected to LoginPath with ReturnUrl=%2FUser%2FLogout attached - a
        // completely normal-looking Login page to the user, who has no way to notice the ReturnUrl
        // in the query string. Url.IsLocalUrl only checks "is this a same-site relative path," so
        // it happily accepted /User/Logout as a valid post-login destination, sending the user
        // straight back to Logout the instant they signed in. /User/Logout (and /User/Login, for
        // the same reason - a returnUrl loop back to the login page itself) are the only paths that
        // must never be honored as a post-login redirect target.
        var isLoginOrLogoutReturnUrl = !string.IsNullOrEmpty(returnUrl)
            && (returnUrl.Contains("/User/Logout", StringComparison.OrdinalIgnoreCase)
                || returnUrl.Contains("/User/Login", StringComparison.OrdinalIgnoreCase));
        var redirectUrl = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && !isLoginOrLogoutReturnUrl
            ? returnUrl
            : Url.Action(nameof(Dashboard));

        // The antiforgery token already on the page was minted while anonymous (the GET Login
        // page load, before SignInAsync above) — ASP.NET Core's default antiforgery embeds the
        // current identity into the token, so that pre-login token is deliberately rejected now
        // that the request is authenticated (correct CSRF behavior, not a bug to route around).
        // The change-password dialog needs a token minted post-authentication instead.
        var freshAntiforgeryToken = _antiforgery.GetAndStoreTokens(HttpContext).RequestToken;

        return Json(new
        {
            success = true,
            redirectUrl,
            passwordResetRequired = login.PasswordResetRequired,
            antiForgeryToken = freshAntiforgeryToken
        });
    }

    /// <summary>
    /// Deferred menu-tree fetch (2026-07-20) — called by dashboard-bootstrap.js right after
    /// Dashboard (or any authenticated page) loads, instead of Login blocking on this. Same
    /// cache/negative-cache behavior CompleteSignInAsync used to run inline. Renders the same
    /// partial SidebarMenuViewComponent already uses, so the client can just swap #sidebar's
    /// outerHTML with the response — no separate JSON menu shape to keep in sync client-side.
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> SidebarMenuPartial(CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return Unauthorized();
        }

        // Bug fix 2026-08-28 ("dsmas"/"sopanda" locally missing the whole Pre Budget Meeting app
        // from their sidebar despite every M_MapUserApp/M_MapRoleModule/M_MapRoleFunction row for
        // their roles being correct - confirmed by replaying MenuGenerator's own query directly
        // against the DB and getting the full, correct tree back): dashboard-bootstrap.js fires
        // this and ResolveMyRole in PARALLEL, neither awaiting the other, by design (so Dashboard
        // shows immediately instead of waiting on the slowest of three deferred calls) -
        // session.RoleName is only ever populated by ResolveMyRole, so if THIS request's session
        // read happened to win that race, RoleName was still blank here, and the (correct) menu got
        // cached under the shared "menu:role:" (blank) key. Any other role that also lost that same
        // race on a different request collided on that identical blank key, each serving whichever
        // role happened to populate it most recently instead of their own. Resolving the role here
        // too when it isn't populated yet closes the race instead of trusting the timing.
        if (string.IsNullOrEmpty(session.RoleName))
        {
            var roleResult = await _aimClient.GetMyRoleAsync(session.Token, session.FinancialYear, ct);
            if (roleResult.IsSuccess && roleResult.Data != null)
            {
                session.RoleName = roleResult.Data.Role.RoleName;
                session.RoleId = roleResult.Data.Role.RoleId;
                session.BaselineRoleId ??= session.RoleId;
                HttpContext.Session.SetUbisSession(session);
            }
        }

        var menuCacheKey = $"menu:role:{session.RoleName}";
        var menuUnavailableCacheKey = $"menu:unavailable:{session.RoleName}";
        var menu = await _cache.GetAsync<MenuFullResponseDto>(menuCacheKey, ct);
        var menuKnownUnavailable = await _cache.GetAsync<bool>(menuUnavailableCacheKey, ct);
        if (menu == null && !menuKnownUnavailable)
        {
            var menuResult = await _menuClient.GetFullMenuAsync(session.Token, ct);
            if (menuResult.IsSuccess && menuResult.Data != null)
            {
                menu = menuResult.Data;
                await _cache.SetAsync(menuCacheKey, menu, MenuCacheTtl, ct);
            }
            else
            {
                await _cache.SetAsync(menuUnavailableCacheKey, true, MenuUnavailableCacheTtl, ct);
            }
        }

        if (menu != null)
        {
            session.Menu = menu;
            HttpContext.Session.SetUbisSession(session);
        }

        return PartialView("~/Views/Shared/Components/SidebarMenu/Default.cshtml", new SidebarMenuViewModel
        {
            MenuLoaded = menu != null,
            Nodes = menu != null ? MenuNodeViewModel.FromMenuResponse(menu) : new List<MenuNodeViewModel>()
        });
    }

    /// <summary>
    /// Deferred role fetch (2026-08-03) — same AIM call CompleteSignInAsync used to run inline;
    /// called by dashboard-bootstrap.js right after landing on an authenticated page, in parallel
    /// with <see cref="SidebarMenuPartial"/> and <see cref="RefreshComplianceStatus"/>. Trade-off
    /// confirmed with the user: session.RoleName/RoleId (and everything gated on them - IsAdmin,
    /// PreBudgetMeetingController's role-name allow-lists) stay blank/0, which every one of those
    /// checks already treats as "no access" rather than "full access", until this completes.
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ResolveMyRole(CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return Unauthorized();
        }

        var roleResult = await _aimClient.GetMyRoleAsync(session.Token, session.FinancialYear, ct);
        if (!roleResult.IsSuccess || roleResult.Data == null)
        {
            return Json(new { success = false, roleName = (string?)null });
        }

        session.RoleName = roleResult.Data.Role.RoleName;
        session.RoleId = roleResult.Data.Role.RoleId;
        // Security-stamp baseline (2026-08-10): captured once only, first call wins - see
        // UbisSessionData's doc comment on BaselineRoleId for why this must NOT keep refreshing.
        session.BaselineRoleId ??= session.RoleId;
        HttpContext.Session.SetUbisSession(session);

        return Json(new { success = true, roleName = session.RoleName });
    }

    /// <summary>
    /// Deferred compliance-status fetch (2026-07-20) — same AIM call CompleteSignInAsync used to
    /// run inline; called by dashboard-bootstrap.js right after landing on an authenticated page.
    /// Trade-off confirmed with the user: ComplianceInterceptorFilter's IsAccountFrozen/
    /// AllowedIpAddressOne/Two enforcement isn't active until this completes and populates
    /// session, since those fields default to "not configured" until then.
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> RefreshComplianceStatus(CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return Unauthorized();
        }

        var complianceResult = await _aimClient.GetComplianceStatusAsync(session.Token, financialYear: null, ct);
        var compliance = complianceResult.Data;

        session.IsAccountFrozen = compliance?.IsAccountFrozen ?? false;
        session.AllowedIpAddressOne = compliance?.AllowedIpAddressOne;
        session.AllowedIpAddressTwo = compliance?.AllowedIpAddressTwo;
        session.RequireIpValidation = compliance?.RequireIpValidation ?? false;
        session.PasswordChangedAtUtc = compliance?.PasswordChangedAtUtc;
        session.EmailLastValidatedAtUtc = compliance?.EmailLastValidatedAtUtc;
        // Security-stamp baseline (2026-08-10): captured once only, first call wins.
        session.BaselinePasswordChangedAtUtc ??= session.PasswordChangedAtUtc;
        HttpContext.Session.SetUbisSession(session);

        var now = DateTime.UtcNow;
        var passwordIsStale = session.PasswordChangedAtUtc.HasValue && now - session.PasswordChangedAtUtc.Value > PasswordStaleAfter;
        var emailIsStale = session.EmailLastValidatedAtUtc.HasValue && now - session.EmailLastValidatedAtUtc.Value > EmailStaleAfter;

        return Json(new
        {
            loaded = complianceResult.IsSuccess,
            showComplianceWarning = (passwordIsStale || emailIsStale) && !session.ComplianceWarningAcknowledged,
            passwordIsStale,
            emailIsStale
        });
    }

    /// <summary>
    /// AJAX-only counterpart to <see cref="ForcePasswordReset(ProfileViewModel, CancellationToken)"/>,
    /// used by the dialog Login's success handler shows in place when
    /// <c>passwordResetRequired</c> comes back true — same underlying AIM call
    /// (<c>ChangePasswordAsync</c>, which uses the session's server-side JWT; it never reaches the
    /// browser) and same session bookkeeping, just returning JSON instead of a full page so the
    /// user doesn't have to navigate away from Login and back to reach the actual reset form.
    /// The page-based ForcePasswordReset flow below is untouched and still works as a fallback
    /// (e.g. if the interceptor catches a stale PasswordResetRequired session on some other page).
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteForcedPasswordChange([FromBody] ProfileViewModel model, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return Json(new { success = false, errorMessage = "Your session has expired. Please log in again.", redirectUrl = Url.Action(nameof(Login)) });
        }

        if (string.IsNullOrEmpty(model.CurrentPassword) || string.IsNullOrEmpty(model.NewPassword))
        {
            return Json(new { success = false, errorMessage = "Enter your current (default) password and a new password." });
        }

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
            return Json(new { success = false, errorMessage = firstError ?? "Please correct the highlighted fields." });
        }

        var result = await _aimClient.ChangePasswordAsync(session.Token, new ChangePasswordRequestDto
        {
            CurrentPassword = model.CurrentPassword,
            NewPassword = model.NewPassword,
            ConfirmNewPassword = model.ConfirmNewPassword ?? string.Empty
        }, ct);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, errorMessage = result.Error?.Message ?? "Password change failed." });
        }

        // Change request (approved 2026-07-23): after a first-time/default-password change, the
        // session established to reach this dialog must NOT carry through to Dashboard — the user
        // has to prove they know the *new* password via a fresh login, not ride in on the session
        // that was only ever authenticated with the (now-retired) default password. Mirrors
        // Logout's exact invalidation (AIM-side session kill + local session clear + cookie
        // sign-out) instead of just clearing the PasswordResetRequired flag and continuing on.
        await _aimClient.LogoutAsync(session.AimSessionId, ct);
        HttpContext.Session.ClearUbisSession();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        await _logClient.InfoAsync("Forced password reset completed (login dialog) — session closed, re-login required", new { session.UserName }, ct);

        TempData["LoginInfo"] = "Your password has been changed. Please sign in with your new password.";
        return Json(new { success = true, redirectUrl = Url.Action(nameof(Login)) });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session != null)
        {
            await _aimClient.LogoutAsync(session.AimSessionId, ct);
            await _logClient.InfoAsync("Logout", new { session.UserName }, ct);
        }

        HttpContext.Session.ClearUbisSession();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction(nameof(Login));
    }

    private static readonly TimeSpan PasswordStaleAfter = TimeSpan.FromDays(365);
    private static readonly TimeSpan EmailStaleAfter = TimeSpan.FromDays(180);

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        var now = DateTime.UtcNow;
        var passwordIsStale = session.PasswordChangedAtUtc.HasValue && now - session.PasswordChangedAtUtc.Value > PasswordStaleAfter;
        var emailIsStale = session.EmailLastValidatedAtUtc.HasValue && now - session.EmailLastValidatedAtUtc.Value > EmailStaleAfter;

        return View(new DashboardViewModel
        {
            UserName = session.UserName,
            FullName = session.FullName,
            RoleName = session.RoleName,
            FinancialYear = session.FinancialYear,
            PasswordIsStale = passwordIsStale,
            EmailIsStale = emailIsStale,
            ShowComplianceWarning = (passwordIsStale || emailIsStale) && !session.ComplianceWarningAcknowledged
        });
    }

    /// <summary>Dismisses the Section 4 staleness modal for the rest of this session. Added 2026-07-10.</summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public IActionResult AcknowledgeComplianceWarning()
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session != null)
        {
            session.ComplianceWarningAcknowledged = true;
            HttpContext.Session.SetUbisSession(session);
        }

        return Ok();
    }

    /// <summary>
    /// EditProfile/SubmitChangeRequest were merged into UserProfileController.Index (client
    /// requirement 2026-09-03: "merge edit profile and user profile pages... Single User Profile
    /// Page"). Kept as a redirect, not removed outright, so any old bookmark/link still lands
    /// somewhere valid instead of 404ing.
    /// </summary>
    [HttpGet]
    [Authorize]
    public IActionResult EditProfile() => RedirectToAction("Index", "UserProfile");

    /// <summary>
    /// Section 2 "Default Password Handler" destination — <see cref="Services.Session.ComplianceInterceptorFilter"/>
    /// redirects every route here while the session's PasswordResetRequired flag is set, until
    /// the user sets a new password. Added 2026-07-10.
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ForcePasswordReset(CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        if (!session.PasswordResetRequired)
        {
            return RedirectToAction(nameof(Dashboard));
        }

        return View(new ProfileViewModel { UserName = session.UserName, FullName = session.FullName, RoleName = session.RoleName });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForcePasswordReset(ProfileViewModel model, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        if (!ModelState.IsValid || string.IsNullOrEmpty(model.CurrentPassword) || string.IsNullOrEmpty(model.NewPassword))
        {
            model.StatusMessage = "Enter your current (default) password and a new password.";
            model.StatusIsError = true;
            return View(model);
        }

        var result = await _aimClient.ChangePasswordAsync(session.Token, new ChangePasswordRequestDto
        {
            CurrentPassword = model.CurrentPassword,
            NewPassword = model.NewPassword,
            ConfirmNewPassword = model.ConfirmNewPassword ?? string.Empty
        }, ct);

        if (!result.IsSuccess)
        {
            model.StatusIsError = true;
            model.StatusMessage = result.Error?.Message ?? "Password change failed.";
            return View(model);
        }

        // Match CompleteForcedPasswordChange's (the AJAX dialog version of this exact same flow)
        // already-correct behavior: force re-login rather than letting the session that only ever
        // proved the old/default password carry through to Dashboard. This page-based fallback
        // previously just cleared the flag and continued - inconsistent with its AJAX sibling.
        await _aimClient.LogoutAsync(session.AimSessionId, ct);
        HttpContext.Session.ClearUbisSession();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        await _logClient.InfoAsync("Forced password reset completed — session closed, re-login required", new { session.UserName }, ct);

        TempData["LoginInfo"] = "Your password has been changed. Please sign in with your new password.";
        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// Always-available "Change Password" page — unlike <see cref="ForcePasswordReset"/>, this is
    /// never gated on PasswordResetRequired (that flag exists only to detect a still-default
    /// password; a normal user with a normal password must still be able to voluntarily change
    /// it). Same look as ForcePasswordReset, same already-working
    /// _aimClient.ChangePasswordAsync/api/authentication/change-password EditProfile already
    /// calls — just reachable any time via its own menu link, under the normal app shell.
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ChangePassword(CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        return View(new ProfileViewModel { UserName = session.UserName, FullName = session.FullName, RoleName = session.RoleName });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ProfileViewModel model, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        model.UserName = session.UserName;
        model.FullName = session.FullName;
        model.RoleName = session.RoleName;

        if (!ModelState.IsValid || string.IsNullOrEmpty(model.CurrentPassword) || string.IsNullOrEmpty(model.NewPassword))
        {
            model.StatusMessage = "Enter your current password and a new password.";
            model.StatusIsError = true;
            return View(model);
        }

        var result = await _aimClient.ChangePasswordAsync(session.Token, new ChangePasswordRequestDto
        {
            CurrentPassword = model.CurrentPassword,
            NewPassword = model.NewPassword,
            ConfirmNewPassword = model.ConfirmNewPassword ?? string.Empty
        }, ct);

        await _logClient.InfoAsync("Password change attempt", new { session.UserName, result.IsSuccess }, ct);

        if (!result.IsSuccess)
        {
            model.StatusIsError = true;
            model.StatusMessage = result.Error?.Message ?? "Password change failed.";
            return View(model);
        }

        // Same force-logout requirement as EditProfile's password-change branch above.
        await _aimClient.LogoutAsync(session.AimSessionId, ct);
        HttpContext.Session.ClearUbisSession();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        await _logClient.InfoAsync("Password changed via Change Password page — session closed, re-login required", new { session.UserName }, ct);

        TempData["LoginInfo"] = "Your password has been changed. Please sign in with your new password.";
        return RedirectToAction(nameof(Login));
    }

    private async Task<IActionResult> ExpiredSessionRedirectAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// dbo.M_AppName.AppId for "Pre Budget Meeting" - the app whose current-year flag the login
    /// flow resolves below. Added 2026-09-08 alongside M_FinancialYear.AppId (each app now has
    /// its own current-year row/flag - see AIM's FinancialYear.cs entity for the design note).
    /// Login has always effectively resolved PreBudget's year (the dominant, and until now only
    /// real, consumer of <c>session.FinancialYear</c>) - this makes that explicit rather than
    /// implicit now that the source table can hold more than one app's answer.
    ///
    /// KNOWN GAP, deliberately not fixed in this pass: ECL's own controllers
    /// (Areas/ECL/Controllers/*) fall back to this same <c>session.FinancialYear</c> value when no
    /// explicit year is supplied - they will keep silently defaulting to PreBudget's current year,
    /// not their own, until ECL's fallback paths are migrated to resolve their own AppId (11)
    /// separately. Harmless while every app's current year still matches (seeded identically on
    /// migration day); becomes a real bug only once an app's current year is deliberately
    /// diverged via the future admin panel (see ubis2_admin_panel_planned_scope memory).
    /// </summary>
    private const int PreBudgetAppId = 7;

    /// <summary>
    /// dbo.M_FinancialYear-backed options (AIM's GET /api/authentication/financial-years, added
    /// 2026-07-30 — the central Financial Year master every module now reads from) — falls back to
    /// an empty list rather than blocking login if AIM is briefly unreachable.
    /// </summary>
    private async Task<List<FinancialYearOptionDto>> GetFinancialYearOptionsAsync(int appId, CancellationToken ct)
    {
        var result = await _aimClient.GetFinancialYearOptionsAsync(appId, ct);
        return result.IsSuccess && result.Data != null ? result.Data : new List<FinancialYearOptionDto>();
    }

    /// <summary>
    /// The Financial Year the login flow operates under, resolved silently instead of asked of the
    /// user (removed from the login form again 2026-07-30, per client request - it had briefly
    /// been a real dropdown). The row flagged <c>IsCurrentYear=1</c> in dbo.M_FinancialYear
    /// (scoped to <see cref="PreBudgetAppId"/> as of 2026-09-08 - see that field's own comment) is
    /// authoritative; falls back to the newest YearRange on file if no row is flagged current, so a
    /// misconfigured table doesn't hard-fail every login.
    /// </summary>
    private async Task<string> ResolveFinancialYearAsync(CancellationToken ct)
    {
        var options = await GetFinancialYearOptionsAsync(PreBudgetAppId, ct);
        return options.FirstOrDefault(fy => fy.IsCurrentYear)?.YearRange
            ?? options.FirstOrDefault()?.YearRange
            ?? string.Empty;
    }

    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = HttpContext.TraceIdentifier
    });
}
