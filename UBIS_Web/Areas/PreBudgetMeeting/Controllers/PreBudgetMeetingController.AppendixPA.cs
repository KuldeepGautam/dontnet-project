// Save/Freeze/Delete actions + view-model builder for the Public Account (PA-ReceiptPayment)
// template.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    // --- Public Account Template ---
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAppendixPA(int demandId, SavePublicAccountReceiptPaymentDto newRecord, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        newRecord.DemandId = demandId; newRecord.FinancialYear = session.FinancialYear;
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var result = newRecord.Id.HasValue && newRecord.Id.Value > 0
            ? await _preBudgetClient.UpdateAppendixPAAsync(session.Token, newRecord.Id.Value, newRecord, ct)
            : await _preBudgetClient.CreateAppendixPAAsync(session.Token, newRecord, ct);
        var model = await BuildAppendixPAViewModelAsync(session, demandId, demandName, ct);
        // "Input grid still not blank after Submit" (client testing feedback, 2026-08-24) - asp-for
        // renders from ModelState before the model; see AppendixIV.cs's matching comment. Only on
        // success - keep the user's typed values on the page if the save itself failed.
        if (result.IsSuccess)
        {
            ModelState.Clear();
        }
        model.StatusIsError = !result.IsSuccess;
        model.StatusMessage = result.IsSuccess ? (newRecord.Id.HasValue && newRecord.Id.Value > 0 ? "Record modified successfully." : "Record saved successfully.") : (result.Error?.Message ?? "Could not save the record.");
        ViewData["Title"] = "Public Account Template";
        return View("AppendixPA", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixPA(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.FreezeAppendixPAAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see FreezeAppendixTemplate's matching comment
        // (PreBudgetMeetingController.cs).
        return await AppendixEntry("PA-ReceiptPayment", demandId, ct);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppendixPA(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.DeleteAppendixPAAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see DeleteAppendixIA's matching comment.
        return await AppendixEntry("PA-ReceiptPayment", demandId, ct);
    }

    private async Task<AppendixPAViewModel> BuildAppendixPAViewModelAsync(UbisSessionData session, int demandId, string demandName, CancellationToken ct)
    {
        var result = await _preBudgetClient.GetAppendixPAAsync(session.Token, demandId, session.FinancialYear, ct);
        var majorHeads = await _preBudgetClient.GetAppendixVIIAMajorHeadsAsync(session.Token, demandId, ct);
        var model = new AppendixPAViewModel
        {
            DemandId = demandId,
            DemandName = demandName,
            FinancialYear = session.FinancialYear,
            Records = result.Data ?? new(),
            MajorHeads = majorHeads.Data ?? new()
        };
        await ApplyAppendixPermissionsAsync(model, session, "PA-ReceiptPayment", ct);
        await ApplyFreezeStatusAsync(model, session, demandId, "PA-ReceiptPayment", ct);
        return model;
    }

    /// <summary>
    /// Keyed by DemandId+MajorHeadId. PA-ReceiptPayment's field list is explicitly provisional per
    /// the FRS itself (Open Item OI-05) with no year-relative labels anywhere in the view - treated
    /// Actual*/BalanceAtEnd* as historical (Actuals = already-settled fact per this session's
    /// standing convention; BalanceAtEnd* = a point-in-time snapshot, same treatment as V-C/VI-C's
    /// "Accumulated Balance as on ..." fields) and BE*/Adjustment*/RE*/NBE*/Remarks* as this cycle's
    /// own live figures. Revisit if/when OI-05 is resolved and the real field semantics are settled.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAppendixPAPreviousYearReference(int demandId, int majorHeadId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();

        var priorYear = GetPrecedingFinancialYears(session.FinancialYear, 2).FirstOrDefault();
        if (string.IsNullOrEmpty(priorYear))
        {
            return Json(new { found = false });
        }

        var result = await _preBudgetClient.GetAppendixPAAsync(session.Token, demandId, priorYear, ct);
        var record = result.Data?.FirstOrDefault(r => r.MajorHeadId == majorHeadId);
        if (record == null)
        {
            return Json(new { found = false });
        }

        return Json(new
        {
            found = true,
            financialYear = priorYear,
            actualReceipt = record.ActualReceipt,
            actualPayment = record.ActualPayment,
            balanceAtEndReceipt = record.BalanceAtEndReceipt,
            balanceAtEndPayment = record.BalanceAtEndPayment
        });
    }
}
