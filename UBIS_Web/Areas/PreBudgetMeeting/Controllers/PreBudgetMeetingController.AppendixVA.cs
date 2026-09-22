// Save/Freeze/Delete actions + view-model builder for Appendix V-A: Grant in Aid to Autonomous and
// other Bodies. Split 2026-09-16 out of the former shared PreBudgetMeetingController.AppendixVGroup.cs
// (which built a combined AppendixVGroupViewModel by fetching V-A+V-B+V-C's data on every request,
// regardless of which one was actually being viewed/saved) - V-A/V-B/V-C were already split into 3
// standalone pages on 2026-09-14, but the controller/view-model layer stayed shared; this file
// finishes that split, matching every other independent appendix's own dedicated controller file.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    // --- Appendix V-A ---
    private async Task<AppendixVAViewModel> BuildAppendixVAViewModelAsync(UbisSessionData session, int demandId, string demandName, CancellationToken ct)
    {
        var vaResult = await _preBudgetClient.GetAppendixVAAsync(session.Token, demandId, session.FinancialYear, ct);
        var autonomousBodies = await _preBudgetClient.GetAutonomousBodiesForAppendixVAAsync(session.Token, demandId, session.FinancialYear, ct);

        var model = new AppendixVAViewModel
        {
            DemandId = demandId,
            DemandName = demandName,
            FinancialYear = session.FinancialYear,
            AppendixCode = "V-A",
            VARecords = vaResult.Data ?? new(),
            AutonomousBodies = autonomousBodies.Data ?? new()
        };

        var statusResult = await _preBudgetClient.GetAppendicesWithStatusAsync(session.Token, demandId, session.FinancialYear, ct);
        var statusByCode = statusResult.Data?.ToDictionary(a => a.Code, StringComparer.OrdinalIgnoreCase) ?? new();
        if (statusByCode.TryGetValue("V-A", out var vaStatus))
        {
            model.IsVAFrozen = vaStatus.IsFrozen;
            model.IsVALocked = vaStatus.IsFrozen || vaStatus.IsTargetDateExpired;
        }
        model.IsAppendixFrozen = model.IsVAFrozen;
        model.IsTargetDateExpired = model.IsVALocked && !model.IsVAFrozen;

        return model;
    }

    [HttpPost] [ValidateAntiForgeryToken]
    // Root cause of "Modify not working, it is adding blank fields" (client testing feedback,
    // 2026-08-25): every other appendix's Save action parameter is named `newRecord` to match its
    // view's `Model.NewRecord` property, so `asp-for="NewRecord.X"` posts field names ASP.NET
    // Core's default model binding already expects. This page posts `VANewRecord.X` instead (kept
    // from the former tabbed page's one-view-model/3-named-sub-objects shape) - without an explicit
    // prefix, none of those posted values (Id included) ever actually bound to this `newRecord`
    // parameter, so `newRecord.Id` was always null/0 regardless of which record was being edited,
    // silently taking the Create path every time instead of Update.
    public async Task<IActionResult> SaveAppendixVA(int demandId, [Bind(Prefix = "VANewRecord")] SaveAppendixGrantInAidDto newRecord, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        newRecord.DemandId = demandId; newRecord.FinancialYear = session.FinancialYear;
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var result = newRecord.Id.HasValue && newRecord.Id.Value > 0
            ? await _preBudgetClient.UpdateAppendixVAAsync(session.Token, newRecord.Id.Value, newRecord, ct)
            : await _preBudgetClient.CreateAppendixVAAsync(session.Token, newRecord, ct);
        var model = await BuildAppendixVAViewModelAsync(session, demandId, demandName, ct);
        // "Input grid still not blank after Submit" (client testing feedback, 2026-08-24) - asp-for
        // renders from ModelState before the model; see AppendixIV.cs's matching comment. Only on
        // success - keep the user's typed values on the page if the save itself failed.
        if (result.IsSuccess)
        {
            ModelState.Clear();
        }
        model.StatusIsError = !result.IsSuccess;
        model.StatusMessage = result.IsSuccess ? (newRecord.Id.HasValue && newRecord.Id.Value > 0 ? "Record modified successfully." : "Record saved successfully.") : (result.Error?.Message ?? "Could not save the record.");
        ViewData["Title"] = "Appendix V-A : Grant in Aid to Autonomous and other Bodies";
        return View("AppendixVA", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixVABulk(int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixVAViewModelAsync(session, demandId, demandName, ct);
        foreach (var record in model.VARecords.Where(r => !r.IsFrozen))
        {
            await _preBudgetClient.FreezeAppendixVAAsync(session.Token, record.Id, ct);
        }
        model = await BuildAppendixVAViewModelAsync(session, demandId, demandName, ct);
        model.StatusMessage = "Grant in Aid to Autonomous and other Bodies data frozen.";
        ViewData["Title"] = "Appendix V-A : Grant in Aid to Autonomous and other Bodies";
        return View("AppendixVA", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppendixVA(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.DeleteAppendixVAAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see DeleteAppendixIA's matching comment.
        return await AppendixEntry("V-A", demandId, ct);
    }

    /// <summary>Row-level Freeze (2026-08-21, history-grid Action-icon rebuild) - distinct from the
    /// existing FreezeAppendixVABulk; the client method already existed, only this per-row controller
    /// action was missing.</summary>
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixVA(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.FreezeAppendixVAAsync(session.Token, id, ct);
        return await AppendixEntry("V-A", demandId, ct);
    }

    /// <summary>
    /// AJAX-only (2026-08-04): same self-referential prior-year pre-fill pattern as Appendix I/II/
    /// IV - unlike IV's Actuals-upto-Sept-only scope, here ALL 4 historical columns per GiA category
    /// (Actuals, ActualsUptoSeptPrevYear, BE, ActualsUptoSept) are pre-filled and locked, since
    /// RE/NBE are this year's live proposals but the other 4 are already-known historical fact.
    /// Keyed by DemandId+AutonomousBodyId (one row per Autonomous Body, not per-demand like I/II).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAppendixVAPreviousYearReference(int demandId, int autonomousBodyId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();

        var priorYear = GetPrecedingFinancialYears(session.FinancialYear, 2).FirstOrDefault();
        if (string.IsNullOrEmpty(priorYear))
        {
            return Json(new { found = false });
        }

        var result = await _preBudgetClient.GetAppendixVAAsync(session.Token, demandId, priorYear, ct);
        var record = result.Data?.FirstOrDefault(r => r.AutonomousBodyId == autonomousBodyId);
        if (record == null)
        {
            return Json(new { found = false });
        }

        return Json(new
        {
            found = true,
            financialYear = priorYear,
            giaGeneralActuals = record.GiaGeneralActuals,
            giaGeneralActualsUptoSeptPrevYear = record.GiaGeneralActualsUptoSeptPrevYear,
            giaGeneralBE = record.GiaGeneralBE,
            giaGeneralActualsUptoSept = record.GiaGeneralActualsUptoSept,
            giaCcaActuals = record.GiaCcaActuals,
            giaCcaActualsUptoSeptPrevYear = record.GiaCcaActualsUptoSeptPrevYear,
            giaCcaBE = record.GiaCcaBE,
            giaCcaActualsUptoSept = record.GiaCcaActualsUptoSept,
            giaSalaryActuals = record.GiaSalaryActuals,
            giaSalaryActualsUptoSeptPrevYear = record.GiaSalaryActualsUptoSeptPrevYear,
            giaSalaryBE = record.GiaSalaryBE,
            giaSalaryActualsUptoSept = record.GiaSalaryActualsUptoSept
        });
    }
}
