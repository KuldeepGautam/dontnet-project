// Save/Freeze/Delete actions + view-model builder for Appendix V-B: Details of Establishment
// Expenditure - Object Head wise. Split 2026-09-16 out of the former shared
// PreBudgetMeetingController.AppendixVGroup.cs - see PreBudgetMeetingController.AppendixVA.cs's
// header comment for the full history.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    // --- Appendix V-B ---
    private async Task<AppendixVBViewModel> BuildAppendixVBViewModelAsync(UbisSessionData session, int demandId, string demandName, CancellationToken ct)
    {
        var vbResult = await _preBudgetClient.GetAppendixVBAsync(session.Token, demandId, session.FinancialYear, ct);
        var objectHeads = await _preBudgetClient.GetAppendixVBObjectHeadsAsync(session.Token, demandId, session.FinancialYear, ct);

        var model = new AppendixVBViewModel
        {
            DemandId = demandId,
            DemandName = demandName,
            FinancialYear = session.FinancialYear,
            AppendixCode = "V-B",
            VBRecords = vbResult.Data ?? new(),
            ObjectHeads = objectHeads.Data ?? new()
        };

        var statusResult = await _preBudgetClient.GetAppendicesWithStatusAsync(session.Token, demandId, session.FinancialYear, ct);
        var statusByCode = statusResult.Data?.ToDictionary(a => a.Code, StringComparer.OrdinalIgnoreCase) ?? new();
        if (statusByCode.TryGetValue("V-B", out var vbStatus))
        {
            model.IsVBFrozen = vbStatus.IsFrozen;
            model.IsVBLocked = vbStatus.IsFrozen || vbStatus.IsTargetDateExpired;
        }
        model.IsAppendixFrozen = model.IsVBFrozen;
        model.IsTargetDateExpired = model.IsVBLocked && !model.IsVBFrozen;

        return model;
    }

    [HttpPost] [ValidateAntiForgeryToken]
    // See PreBudgetMeetingController.AppendixVA.cs's SaveAppendixVA comment on the
    // [Bind(Prefix=...)] fix - same root cause, same fix, applied to VBNewRecord instead.
    public async Task<IActionResult> SaveAppendixVB(int demandId, [Bind(Prefix = "VBNewRecord")] SaveAppendixEstablishmentByObjectHeadDto newRecord, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        newRecord.DemandId = demandId; newRecord.FinancialYear = session.FinancialYear;
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var result = newRecord.Id.HasValue && newRecord.Id.Value > 0
            ? await _preBudgetClient.UpdateAppendixVBAsync(session.Token, newRecord.Id.Value, newRecord, ct)
            : await _preBudgetClient.CreateAppendixVBAsync(session.Token, newRecord, ct);
        var model = await BuildAppendixVBViewModelAsync(session, demandId, demandName, ct);
        // "Input grid still not blank after Submit" (client testing feedback, 2026-08-24) - asp-for
        // renders from ModelState before the model; see AppendixIV.cs's matching comment. Only on
        // success - keep the user's typed values on the page if the save itself failed.
        if (result.IsSuccess)
        {
            ModelState.Clear();
        }
        model.StatusIsError = !result.IsSuccess;
        model.StatusMessage = result.IsSuccess ? (newRecord.Id.HasValue && newRecord.Id.Value > 0 ? "Record modified successfully." : "Record saved successfully.") : (result.Error?.Message ?? "Could not save the record.");
        ViewData["Title"] = "Appendix V-B : Details of Establishment Expenditure - Object Head wise";
        return View("AppendixVB", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixVBBulk(int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixVBViewModelAsync(session, demandId, demandName, ct);
        foreach (var record in model.VBRecords.Where(r => !r.IsFrozen))
        {
            await _preBudgetClient.FreezeAppendixVBAsync(session.Token, record.Id, ct);
        }
        model = await BuildAppendixVBViewModelAsync(session, demandId, demandName, ct);
        model.StatusMessage = "Establishment Expenditure - Object Head wise data frozen.";
        ViewData["Title"] = "Appendix V-B : Details of Establishment Expenditure - Object Head wise";
        return View("AppendixVB", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppendixVB(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.DeleteAppendixVBAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see DeleteAppendixIA's matching comment
        // (same "dropdown erased" bug class, newly reachable now that a Delete button exists).
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixVBViewModelAsync(session, demandId, demandName, ct);
        ViewData["Title"] = "Appendix V-B : Details of Establishment Expenditure - Object Head wise";
        return View("AppendixVB", model);
    }

    /// <summary>Row-level Freeze (2026-08-21, history-grid Action-icon rebuild).</summary>
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixVB(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.FreezeAppendixVBAsync(session.Token, id, ct);
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixVBViewModelAsync(session, demandId, demandName, ct);
        ViewData["Title"] = "Appendix V-B : Details of Establishment Expenditure - Object Head wise";
        return View("AppendixVB", model);
    }

    /// <summary>
    /// Keyed by DemandId+ObjectHeadId, like <see cref="PreBudgetMeetingController.GetAppendixVAPreviousYearReference"/>.
    /// BE and Actuals are both resolved via M_Demand.PrevDemandId + dbo.DDG, matched by Object Head
    /// code, per the client's reference SQL (2026-08-07 for BE, 2026-08-10 for Actuals - the client
    /// explicitly confirmed Actuals must also auto-load, not be manually typed). ActualsUptoSeptPrevYear/
    /// ActualsUptoSept have no client-provided source and stay self-referential (Appendix V-B's own
    /// prior-year row).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAppendixVBPreviousYearReference(int demandId, int objectHeadId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();

        var priorYear = GetPrecedingFinancialYears(session.FinancialYear, 2).FirstOrDefault();
        if (string.IsNullOrEmpty(priorYear))
        {
            return Json(new { found = false });
        }

        var beTask = _preBudgetClient.GetAppendixVBPreviousYearBeAsync(session.Token, demandId, objectHeadId, ct);
        var ownPriorYearTask = _preBudgetClient.GetAppendixVBAsync(session.Token, demandId, priorYear, ct);
        await Task.WhenAll(beTask, ownPriorYearTask);

        var beResult = beTask.Result;
        var record = ownPriorYearTask.Result.Data?.FirstOrDefault(r => r.ObjectHeadId == objectHeadId);

        var hasBe = beResult.IsSuccess && beResult.Data != null;
        if (!hasBe && record == null)
        {
            return Json(new { found = false });
        }

        return Json(new
        {
            found = true,
            financialYear = priorYear,
            actuals = hasBe ? beResult.Data!.Actuals : record?.Actuals,
            actualsUptoSeptPrevYear = record?.ActualsUptoSeptPrevYear,
            be = hasBe ? beResult.Data!.Be : record?.BE,
            actualsUptoSept = record?.ActualsUptoSept
        });
    }
}
