namespace UBIS.Web.Controllers;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Logging;
using UBIS.Web.Services.Session;

/// <summary>
/// Single merged "User Profile" page — client requirement 2026-09-03: "merge edit profile and
/// user profile pages... All edit profile functioning should [be] in User Profile Page". Combines
/// the old UserController.EditProfile (password change, legacy-profile display, assigned
/// statements) with this controller's existing IP change-request/Contact-update flow, plus a new
/// IP change-request history grid. UserController.EditProfile/SubmitChangeRequest were removed;
/// this is now the only profile page.
/// </summary>
[Authorize]
public class UserProfileController : Controller
{
    private readonly IUserProfileClient _userProfileClient;
    private readonly IAimClient _aimClient;
    private readonly IWebLogClient _logClient;

    public UserProfileController(IUserProfileClient userProfileClient, IAimClient aimClient, IWebLogClient logClient)
    {
        _userProfileClient = userProfileClient;
        _aimClient = aimClient;
        _logClient = logClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        return View(await BuildViewModelAsync(session, ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RaiseIpChangeRequest(UserProfileViewModel submitted, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        // Re-fetch the full profile so Email/Mobile (a separate form on this same page) don't
        // get wiped blank in the redisplay — only this form's own fields were actually posted.
        var model = await BuildViewModelAsync(session, ct);
        model.IPAddress1 = submitted.IPAddress1;
        model.IPAddress2 = submitted.IPAddress2;

        if (!ModelState.IsValid)
        {
            model.StatusIsError = true;
            model.StatusMessage = "Enter a valid IPv4 address (e.g. 192.168.1.1) for each field you want to change.";
            return View(nameof(Index), model);
        }

        var result = await _userProfileClient.RaiseIpChangeRequestAsync(session.Token, new RaiseIpChangeRequestDto
        {
            IPAddress1 = submitted.IPAddress1,
            IPAddress2 = submitted.IPAddress2,
            ExistingIp = ExtractClientIpAddress()
        }, ct);
        model.StatusIsError = !result.IsSuccess;
        model.StatusMessage = result.IsSuccess
            ? "Your IP change request has been submitted and is pending admin approval."
            : result.Error?.Message ?? "Could not submit the change request.";

        if (result.IsSuccess && result.Data != null)
        {
            model.LatestRequestStatus = result.Data.Status;
            model.LatestRequestDate = result.Data.RequestDate;
            model.LatestApproveDate = result.Data.ApproveDate;
        }

        return View(nameof(Index), model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateContact(UserProfileViewModel submitted, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        var model = await BuildViewModelAsync(session, ct);

        if (!ModelState.IsValid)
        {
            model.Email = submitted.Email;
            model.StatusIsError = true;
            model.StatusMessage = "Enter a valid email address and/or a 10-digit mobile number.";
            return View(nameof(Index), model);
        }

        var result = await _userProfileClient.UpdateContactAsync(session.Token, new UpdateContactRequestDto
        {
            Email = submitted.Email,
            Mobile = submitted.Mobile
        }, ct);

        // Re-fetch afterward (rather than echoing submitted.Mobile back into the page) so
        // MaskedMobile reflects the actual now-encrypted value AIM just stored — the plaintext
        // the user typed is never redisplayed, not even the value that was just submitted.
        if (result.IsSuccess)
        {
            model = await BuildViewModelAsync(session, ct);
        }

        model.Email = submitted.Email;
        model.StatusIsError = !result.IsSuccess;
        model.StatusMessage = result.IsSuccess
            ? "Contact details updated successfully."
            : result.Error?.Message ?? "Could not update contact details.";

        return View(nameof(Index), model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(UserProfileViewModel submitted, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        var model = await BuildViewModelAsync(session, ct);
        model.CurrentPassword = submitted.CurrentPassword;
        model.NewPassword = submitted.NewPassword;
        model.ConfirmNewPassword = submitted.ConfirmNewPassword;

        if (string.IsNullOrEmpty(submitted.CurrentPassword) || string.IsNullOrEmpty(submitted.NewPassword))
        {
            model.PasswordStatusIsError = true;
            model.PasswordStatusMessage = "Enter your current password and a new password to make a change.";
            return View(nameof(Index), model);
        }

        if (!ModelState.IsValid)
        {
            model.PasswordStatusIsError = true;
            model.PasswordStatusMessage = "Enter a valid current password and a new password of at least 12 characters that matches its confirmation.";
            return View(nameof(Index), model);
        }

        var result = await _aimClient.ChangePasswordAsync(session.Token, new ChangePasswordRequestDto
        {
            CurrentPassword = submitted.CurrentPassword,
            NewPassword = submitted.NewPassword,
            ConfirmNewPassword = submitted.ConfirmNewPassword ?? string.Empty
        }, ct);

        await _logClient.InfoAsync("Password change attempt", new { session.UserName, result.IsSuccess }, ct);

        if (!result.IsSuccess)
        {
            model.PasswordStatusIsError = true;
            model.PasswordStatusMessage = result.Error?.Message ?? "Password change failed.";
            return View(nameof(Index), model);
        }

        // Same as UserController.EditProfile's old password-change branch: a voluntary password
        // change must invalidate the current session and force re-login, since it authenticated
        // this request only against the *old* password.
        await _aimClient.LogoutAsync(session.AimSessionId, ct);
        HttpContext.Session.ClearUbisSession();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        await _logClient.InfoAsync("Password changed via profile — session closed, re-login required", new { session.UserName }, ct);

        TempData["LoginInfo"] = "Your password has been changed. Please sign in with your new password.";
        return RedirectToAction("Login", "User");
    }

    private async Task<UserProfileViewModel> BuildViewModelAsync(UbisSessionData session, CancellationToken ct)
    {
        var result = await _userProfileClient.GetMyProfileAsync(session.Token, ct);
        var legacyProfile = await _aimClient.GetLegacyProfileAsync(session.Token, ct);
        var assignedStatements = await _aimClient.GetAssignedStatementsAsync(session.Token, session.FinancialYear, ct);
        var ipHistory = await _userProfileClient.GetIpRequestHistoryAsync(session.Token, ct);

        if (!result.IsSuccess || result.Data == null)
        {
            return new UserProfileViewModel
            {
                UserName = session.UserName,
                FullName = session.FullName,
                RoleName = session.RoleName,
                DepartmentId = legacyProfile.Data?.DepartmentId,
                MaskedMobile = legacyProfile.Data?.MaskedMobile,
                UserCreationDate = legacyProfile.Data?.UserCreationDate,
                CreatedBy = legacyProfile.Data?.CreatedBy,
                HasLegacyRecord = legacyProfile.Data?.HasLegacyRecord ?? false,
                AssignedStatements = assignedStatements.Data?.Statements ?? new List<AssignedStatementDto>(),
                IpRequestHistory = ipHistory.Data ?? new List<IpRequestHistoryItemDto>(),
                StatusMessage = result.Error?.Message ?? "Could not load your profile. Please try again shortly.",
                StatusIsError = true
            };
        }

        var data = result.Data;
        return new UserProfileViewModel
        {
            UserName = data.UserName ?? session.UserName,
            FullName = session.FullName,
            RoleName = data.RoleName ?? session.RoleName,
            LastLoginDate = data.LastLoginDate,
            IPAddress1 = data.AllowedIpAddressOne,
            IPAddress2 = data.AllowedIpAddressTwo,
            Email = data.Email,
            MaskedMobile = data.MaskedMobile,
            LatestRequestStatus = data.LatestRequest?.Status,
            LatestRequestDate = data.LatestRequest?.RequestDate,
            LatestApproveDate = data.LatestRequest?.ApproveDate,
            DepartmentId = legacyProfile.Data?.DepartmentId,
            UserCreationDate = legacyProfile.Data?.UserCreationDate,
            CreatedBy = legacyProfile.Data?.CreatedBy,
            HasLegacyRecord = legacyProfile.Data?.HasLegacyRecord ?? false,
            AssignedStatements = assignedStatements.Data?.Statements ?? new List<AssignedStatementDto>(),
            IpRequestHistory = ipHistory.Data ?? new List<IpRequestHistoryItemDto>()
        };
    }

    /// <summary>Mirrors AIM's AuthenticationService.ExtractClientIpAddress — the real client IP is
    /// only knowable here, at UBIS_Web's own inbound request; UserProfile's inbound connection
    /// would just see UBIS_Web's own server IP, since UBIS_Web is its actual TCP caller.</summary>
    private string ExtractClientIpAddress()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var rawIp = forwardedFor.ToString().Split(',')[0].Trim();
            if (System.Net.IPAddress.TryParse(rawIp, out _))
            {
                return rawIp;
            }
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private async Task<IActionResult> ExpiredSessionRedirectAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "User");
    }
}
