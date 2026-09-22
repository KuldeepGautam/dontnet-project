// Save/Freeze/Delete actions + view-model builder for Appendix V-C: Details of Establishment
// Expenditure - Other than AB. Split 2026-09-16 out of the former shared
// PreBudgetMeetingController.AppendixVGroup.cs - see PreBudgetMeetingController.AppendixVA.cs's
// header comment for the full history.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    // --- Appendix V-C ---
    private async Task<AppendixVCViewModel> BuildAppendixVCViewModelAsync(UbisSessionData session, int demandId, string demandName, CancellationToken ct)
    {
        var vcResult = await _preBudgetClient.GetAppendixVCAsync(session.Token, demandId, session.FinancialYear, ct);

        var model = new AppendixVCViewModel
        {
            DemandId = demandId,
            DemandName = demandName,
            FinancialYear = session.FinancialYear,
            AppendixCode = "V-C",
            VCRecords = vcResult.Data ?? new()
        };

        var statusResult = await _preBudgetClient.GetAppendicesWithStatusAsync(session.Token, demandId, session.FinancialYear, ct);
        var statusByCode = statusResult.Data?.ToDictionary(a => a.Code, StringComparer.OrdinalIgnoreCase) ?? new();
        if (statusByCode.TryGetValue("V-C", out var vcStatus))
        {
            model.IsVCFrozen = vcStatus.IsFrozen;
            model.IsVCLocked = vcStatus.IsFrozen || vcStatus.IsTargetDateExpired;
        }
        model.IsAppendixFrozen = model.IsVCFrozen;
        model.IsTargetDateExpired = model.IsVCLocked && !model.IsVCFrozen;

        return model;
    }

    [HttpPost] [ValidateAntiForgeryToken]
    // See PreBudgetMeetingController.AppendixVA.cs's SaveAppendixVA comment on the
    // [Bind(Prefix=...)] fix - same root cause, same fix, applied to VCNewRecord instead.
    public async Task<IActionResult> SaveAppendixVC(int demandId, [Bind(Prefix = "VCNewRecord")] SaveAppendixEstablishmentOtherThanABDto newRecord, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        newRecord.DemandId = demandId; newRecord.FinancialYear = session.FinancialYear;
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var result = newRecord.Id.HasValue && newRecord.Id.Value > 0
            ? await _preBudgetClient.UpdateAppendixVCAsync(session.Token, newRecord.Id.Value, newRecord, ct)
            : await _preBudgetClient.CreateAppendixVCAsync(session.Token, newRecord, ct);
        var model = await BuildAppendixVCViewModelAsync(session, demandId, demandName, ct);
        // "Input grid still not blank after Submit" (client testing feedback, 2026-08-24) - asp-for
        // renders from ModelState before the model; see AppendixIV.cs's matching comment. Only on
        // success - keep the user's typed values on the page if the save itself failed.
        if (result.IsSuccess)
        {
            ModelState.Clear();
        }
        model.StatusIsError = !result.IsSuccess;
        model.StatusMessage = result.IsSuccess ? (newRecord.Id.HasValue && newRecord.Id.Value > 0 ? "Record modified successfully." : "Record saved successfully.") : (result.Error?.Message ?? "Could not save the record.");
        ViewData["Title"] = "Appendix V-C : Details of Establishment Expenditure - Other than AB";
        return View("AppendixVC", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixVCBulk(int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixVCViewModelAsync(session, demandId, demandName, ct);
        foreach (var record in model.VCRecords.Where(r => !r.IsFrozen))
        {
            await _preBudgetClient.FreezeAppendixVCAsync(session.Token, record.Id, ct);
        }
        model = await BuildAppendixVCViewModelAsync(session, demandId, demandName, ct);
        model.StatusMessage = "Establishment Expenditure - Other than AB data frozen.";
        ViewData["Title"] = "Appendix V-C : Details of Establishment Expenditure - Other than AB";
        return View("AppendixVC", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppendixVC(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.DeleteAppendixVCAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see DeleteAppendixIA's matching comment.
        return await AppendixEntry("V-C", demandId, ct);
    }

    /// <summary>Row-level Freeze (2026-08-21, history-grid Action-icon rebuild).</summary>
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixVC(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.FreezeAppendixVCAsync(session.Token, id, ct);
        return await AppendixEntry("V-C", demandId, ct);
    }
}
