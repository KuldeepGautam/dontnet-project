// Save/Freeze/Delete actions + view-model builder for Appendix X: Loan to Government Servants.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    // --- Appendix X ---
    // Fixed 3-row screen (House Building Advances / Motor Cars / Computers - the FRS's actual 3
    // major heads, see AppendixXViewModel.FixedSubHeadNames) - one Submit saves every row that
    // has actual field values, looping Create/Update per row against the same per-record endpoints
    // the old free-text CRUD form used. row.Id is normally unset and every Submit attempts a
    // Create; row.Id is only populated when the user clicked Edit on an existing grid row first
    // (pre-budget-meeting.js's populateAppendixXEntryForm sets the hidden Id field), in which case
    // that one row updates in place instead. Client requirement 2026-08-31 ("if data is available
    // in grid then no entry should be made, dialogbox validation must appear, edit mode can edit
    // values") reversed the earlier 2026-08-27 "duplicate additions allowed" decision - now that
    // there are only 3 fixed, meaningful sub-heads (not an open-ended free-text list), a second
    // Create for a sub-head that already has a saved row for this Demand+FinancialYear is rejected
    // server-side (AppendixXController.CreateRecord, authoritative - SubHeadName is free text with
    // no unique DB constraint of its own) and surfaces here as a normal per-row error, same as any
    // other save failure - the existing showStatusMessageAsDialog() conversion (pre-budget-
    // meeting.js's applyFragment, already wired for every appendix on this page) turns it into the
    // required dialog with no extra plumbing needed.
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAppendixX(int demandId, List<SaveAppendixLoansToGovtServantsDto> rows, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);

        var anyError = false;
        var anyCreated = false;
        var anyModified = false;
        string? errorMessage = null;
        foreach (var row in rows ?? new())
        {
            if (row.ActualsY1 is null && row.ActualsY2 is null && row.ActualsY3 is null &&
                row.ActualsUptoSept is null && row.BE is null && row.RE is null && row.NBE is null)
            {
                continue;
            }

            row.DemandId = demandId;
            row.FinancialYear = session.FinancialYear;
            var isUpdate = row.Id.HasValue && row.Id.Value > 0;
            var result = isUpdate
                ? await _preBudgetClient.UpdateAppendixXAsync(session.Token, row.Id!.Value, row, ct)
                : await _preBudgetClient.CreateAppendixXAsync(session.Token, row, ct);
            if (!result.IsSuccess)
            {
                anyError = true;
                errorMessage = result.Error?.Message ?? $"Could not save '{row.SubHeadName}'.";
            }
            else if (isUpdate)
            {
                anyModified = true;
            }
            else
            {
                anyCreated = true;
            }
        }

        var model = await BuildAppendixXViewModelAsync(session, demandId, demandName, ct);
        // "Input grid still not blank after Submit" (client testing feedback, 2026-08-24) - asp-for
        // renders from ModelState before the model; see AppendixIV.cs's matching comment. Only when
        // every row saved cleanly - keep the user's typed values on the page on partial failure.
        if (!anyError)
        {
            ModelState.Clear();
        }
        model.StatusIsError = anyError;
        model.StatusMessage = anyError
            ? errorMessage
            : (anyModified && anyCreated ? "Records saved and modified successfully." : anyModified ? "Record modified successfully." : "Record saved successfully.");
        ViewData["Title"] = "Appendix X - Loan to Government Servants";
        return View("AppendixX", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixX(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.FreezeAppendixXAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see FreezeAppendixTemplate's matching comment
        // (PreBudgetMeetingController.cs).
        return await AppendixEntry("X", demandId, ct);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppendixX(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.DeleteAppendixXAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see DeleteAppendixIA's matching comment.
        return await AppendixEntry("X", demandId, ct);
    }

    private async Task<AppendixXViewModel> BuildAppendixXViewModelAsync(UbisSessionData session, int demandId, string demandName, CancellationToken ct)
    {
        var result = await _preBudgetClient.GetAppendixXAsync(session.Token, demandId, session.FinancialYear, ct);
        var model = new AppendixXViewModel { DemandId = demandId, DemandName = demandName, FinancialYear = session.FinancialYear, Records = result.Data ?? new() };
        await ApplyAppendixPermissionsAsync(model, session, "X", ct);
        await ApplyFreezeStatusAsync(model, session, demandId, "X", ct);
        return model;
    }

    /// <summary>Keyed by DemandId+exact-text-SubHeadName (free text, not an FK - same as VI-D/VII-A). ActualsY1/Y2/Y3 are explicitly historical per the view's "(Y-1)"/"(Y-2)"/"(Y-3)" labels; ActualsUptoSept has no such label so - unlike VII-B - is treated as this cycle's own figure, same as Appendix I's own ActualsUptoSept convention, and stays manual alongside BE/RE/NBE.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAppendixXPreviousYearReference(int demandId, string subHeadName, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();

        var priorYear = GetPrecedingFinancialYears(session.FinancialYear, 2).FirstOrDefault();
        if (string.IsNullOrEmpty(priorYear))
        {
            return Json(new { found = false });
        }

        var result = await _preBudgetClient.GetAppendixXAsync(session.Token, demandId, priorYear, ct);
        var record = result.Data?.FirstOrDefault(r => string.Equals(r.SubHeadName, subHeadName, StringComparison.OrdinalIgnoreCase));
        if (record == null)
        {
            return Json(new { found = false });
        }

        return Json(new
        {
            found = true,
            financialYear = priorYear,
            actualsY1 = record.ActualsY1,
            actualsY2 = record.ActualsY2,
            actualsY3 = record.ActualsY3
        });
    }
}
