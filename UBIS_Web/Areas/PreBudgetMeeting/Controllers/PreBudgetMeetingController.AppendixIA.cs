// Save/Freeze/Delete actions + view-model builder for Appendix I-A: Projected Demand by Ministry.
// Also carries the AJAX previous-year-reference endpoints for Appendix I-A and Appendix II, which
// were grouped alongside Appendix I-A in the pre-split junk-drawer partial-class file.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    // --- Appendix I-A ---
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAppendixIA(int demandId, SaveAppendixProjectedDemandDto newRecord, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        newRecord.DemandId = demandId; newRecord.FinancialYear = session.FinancialYear;
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var result = newRecord.Id.HasValue && newRecord.Id.Value > 0
            ? await _preBudgetClient.UpdateAppendixIAAsync(session.Token, newRecord.Id.Value, newRecord, ct)
            : await _preBudgetClient.CreateAppendixIAAsync(session.Token, newRecord, ct);
        var model = await BuildAppendixIAViewModelAsync(session, demandId, demandName, ct);
        // "Input grid still not blank after Submit" (client testing feedback, 2026-08-24) - asp-for
        // renders from ModelState before the model; see AppendixIV.cs's matching comment. Only on
        // success - keep the user's typed values on the page if the save itself failed.
        if (result.IsSuccess)
        {
            ModelState.Clear();
        }
        model.StatusIsError = !result.IsSuccess;
        model.StatusMessage = result.IsSuccess
            ? (newRecord.Id.HasValue && newRecord.Id.Value > 0 ? "Record modified successfully." : "Record saved successfully.")
            // The WebApi's DUPLICATE_ENTRY message is deliberately generic - it has no Demand
            // display name locally (its own M_Demand mapping is DemandId/DemandNo/FinancialYear
            // only). UBIS_Web already resolved demandName above for the page header, so it enriches
            // the message here into the client's exact wording (2026-08-27): "Data for Demand
            // {Name} already exists."
            : (result.Error?.Code == "DUPLICATE_ENTRY" ? $"Data for Demand {demandName} already exists." : result.Error?.Message ?? "Could not save the record.");
        ViewData["Title"] = "Appendix I-A - Projected Demand by Ministry";
        return View("AppendixIA", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixIA(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.FreezeAppendixIAAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see FreezeAppendixTemplate's matching comment
        // (PreBudgetMeetingController.cs).
        return await AppendixEntry("I-A", demandId, ct);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppendixIA(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.DeleteAppendixIAAsync(session.Token, id, ct);
        // Returns the fragment directly rather than RedirectToAction - same "dropdown erased"
        // bug class fixed for FreezeAppendixTemplate 2026-08-06 (RedirectToActionResult isn't
        // recognized by OnActionExecuted's fragment-detection, so a redirect round trip dumps a
        // full page into the AJAX container). This action only just became reachable from the
        // fragment-loaded UI (the Delete button was added 2026-08-07), so apply the same fix now
        // rather than let a newly-added button trigger a bug that was already found and fixed once.
        return await AppendixEntry("I-A", demandId, ct);
    }

    private async Task<AppendixIAViewModel> BuildAppendixIAViewModelAsync(UbisSessionData session, int demandId, string demandName, CancellationToken ct)
    {
        var result = await _preBudgetClient.GetAppendixIAAsync(session.Token, demandId, session.FinancialYear, ct);
        var model = new AppendixIAViewModel { DemandId = demandId, DemandName = demandName, FinancialYear = session.FinancialYear, Records = result.Data ?? new() };
        await ApplyFreezeStatusAsync(model, session, demandId, "I-A", ct);
        return model;
    }

    /// <summary>
    /// Export dropdown (2026-09-08, extended to Appendix I-A from the VI-A..E design) - PDF/Excel/
    /// CSV, same TabularReportDto/_reportingClient shape as ExportAppendixVIA.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportAppendixIA(int demandId, string format, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixIAViewModelAsync(session, demandId, demandName, ct);

        // Client requirement 2026-09-17: the export must match the government circular's own
        // Appendix-I-A table exactly (screenshot supplied) - same Year/Revenue/Capital/Total
        // shape and header format as Appendix I's own circular export, but Appendix I-A only ever
        // has 2 year-rows (prior year actual, current year proposed-by-Ministry) instead of a
        // rolling 5-year history, and the proposed year has no RE figures at all since Revenue/
        // Capital RE only exist for the settled prior year.
        // Client requirement 2026-09-18: a cell with no underlying data (as opposed to a genuine
        // computed zero) is now passed as null rather than "" or a literal "--" - the shared report
        // generators render a null cell as "0.00" in Excel/CSV and "..." in PDF (see ReportGroup.Rows'
        // own doc comment), so this no longer needs its own placeholder text.
        var r = model.Records.FirstOrDefault();
        var dataRows = new List<string[]>
        {
            new[]
            {
                FormatFinancialYearShort(model.PriorFinancialYear),
                r?.PrevYrMinRevenueBE?.ToString("F2")!,
                r?.PrevYrMinRevenueRE?.ToString("F2")!,
                r?.PrevYrMinCapitalBE?.ToString("F2")!,
                r?.PrevYrMinCapitalRE?.ToString("F2")!,
                r?.PrevYrTotalBE.ToString("F2")!,
                r?.PrevYrTotalRE.ToString("F2")!
            },
            new[]
            {
                $"{FormatFinancialYearShort(model.FinancialYear)} (proposed by Ministry)",
                r?.CurrYrMinRevenueBE?.ToString("F2")!,
                null!,
                r?.CurrYrMinCapitalBE?.ToString("F2")!,
                null!,
                (r != null ? r.TotalBE.ToString("F2") : null)!,
                null!
            }
        };

        var report = new TabularReportDto
        {
            Title = "Appendix-I-A: Projected Demand by Ministry/Department (on net basis)",
            // Client requirement 2026-09-18: the header's Financial Year is the prior/settled year
            // (FY-1) that the first data row's figures actually belong to, not the current session's
            // entry-year - was showing 2026-2027, should show 2025-2026.
            Subtitle = $"Demand: {demandName} | Financial Year: {model.PriorFinancialYear}",
            UnitNote = "(₹ in crore)",
            ParaNo = model.ParaNo,
            Remarks = model.Remarks,
            ColumnGroups = new List<ReportColumnGroupDto>
            {
                new() { Label = null, ColumnSpan = 1 }, // Year
                new() { Label = "Revenue", ColumnSpan = 2 },
                new() { Label = "Capital", ColumnSpan = 2 },
                new() { Label = "Total", ColumnSpan = 2 }
            },
            Columns = new List<string> { "Year", "BE", "RE", "BE", "RE", "BE", "RE" },
            // Client requirement 2026-09-17: every column header centered - but every numeric
            // figure (every column except Year) stays right-aligned regardless (RightAlignedColumns
            // wins over CenterAlignedColumns for DATA cells - see TabularReport's own doc comment).
            CenterAlignedColumns = new List<int> { 0, 1, 2, 3, 4, 5, 6 },
            RightAlignedColumns = new List<int> { 1, 2, 3, 4, 5, 6 },
            Groups = new List<ReportGroupDto> { new() { Rows = dataRows } },
            FileName = $"AppendixIA-{session.UserName}-{DateTime.Now:MMddyyyyHHmmss}"
        };

        var exportResult = format?.ToLowerInvariant() switch
        {
            "excel" => await _reportingClient.ExportExcelAsync(session.Token, report, ct),
            "csv" => await _reportingClient.ExportCsvAsync(session.Token, report, ct),
            _ => await _reportingClient.ExportPdfAsync(session.Token, report, ct)
        };

        if (!exportResult.IsSuccess || exportResult.Data == null)
        {
            TempData["ErrorMessage"] = exportResult.Error?.Message ?? "Could not generate the export. Please try again.";
            return await AppendixEntry("I-A", demandId, ct);
        }

        var (contentType, extension) = format?.ToLowerInvariant() switch
        {
            "excel" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            "csv" => ("text/csv", "csv"),
            _ => ("application/pdf", "pdf")
        };
        return File(exportResult.Data, contentType, $"{report.FileName}.{extension}");
    }

    /// <summary>
    /// AJAX-only. Appendix I-A's "Previous Year" Revenue/Capital BE columns, per the client's
    /// reference SQL (2026-08-07): resolved via M_Demand.PrevDemandId + dbo.SBEData (a new DemandId
    /// is minted every FY for the same logical Demand, so "last year's" data must be looked up
    /// through that PrevDemandId chain, not the current DemandId). Fixed 2026-08-07 - previously
    /// read Appendix I's own saved row for the same DemandId+prior FY string, which is wrong per
    /// this reference SQL (Appendix I's row isn't the source of truth SBEData is, and using the
    /// current DemandId against a prior year silently returns nothing once the Demand's yearly
    /// DemandId has rolled over). Revenue/Capital RE are NOT pre-filled - the client's own note says
    /// "RE to be filled by user, BE data is auto-filled" - so those boxes stay editable, unlike the
    /// old version which (incorrectly) locked them too.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAppendixIAPreviousYearReference(int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();

        var priorYear = GetPrecedingFinancialYears(session.FinancialYear, 2).FirstOrDefault();
        if (string.IsNullOrEmpty(priorYear))
        {
            return Json(new { found = false });
        }

        var result = await _preBudgetClient.GetAppendixIAPreviousYearBeAsync(session.Token, demandId, priorYear, ct);
        if (!result.IsSuccess || result.Data == null || !result.Data.Found)
        {
            return Json(new { found = false });
        }

        return Json(new
        {
            found = true,
            financialYear = priorYear,
            revenueBE = result.Data.RevenueBE,
            capitalBE = result.Data.CapitalBE
        });
    }

    /// <summary>
    /// AJAX-only. Appendix II's "Previous FY" Q1/Q2 "As per approved QEP" columns, per the client's
    /// reference SQL (2026-08-07): resolved via M_Demand.PrevDemandId + dbo.T_QEPData, NOT this same
    /// demand's own Appendix II row for the prior year (fixed 2026-08-07 - the old self-referential
    /// version was wrong per this reference SQL, which re-derives from the QEP-specific legacy
    /// source table). Q1/Q2 Actuals have no client-provided source and stay self-referential
    /// (Appendix II's own prior-year row), unchanged from before.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAppendixIIPreviousYearReference(int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();

        var priorYear = GetPrecedingFinancialYears(session.FinancialYear, 2).FirstOrDefault();
        if (string.IsNullOrEmpty(priorYear))
        {
            return Json(new { found = false });
        }

        var approvedQepTask = _preBudgetClient.GetAppendixIIPreviousYearApprovedQepAsync(session.Token, demandId, ct);
        var ownPriorYearTask = _preBudgetClient.GetAppendixIIAsync(session.Token, demandId, priorYear, ct);
        await Task.WhenAll(approvedQepTask, ownPriorYearTask);

        var approvedQepResult = approvedQepTask.Result;
        var record = ownPriorYearTask.Result.Data?.FirstOrDefault();

        var hasApprovedQep = approvedQepResult.IsSuccess && approvedQepResult.Data is { Found: true };
        if (!hasApprovedQep && record == null)
        {
            return Json(new { found = false });
        }

        return Json(new
        {
            found = true,
            financialYear = priorYear,
            q1ApprovedQep = hasApprovedQep ? approvedQepResult.Data!.Q1ApprovedQep : record?.Q1ApprovedQep,
            q1Actuals = record?.Q1Actuals,
            q2ApprovedQep = hasApprovedQep ? approvedQepResult.Data!.Q2ApprovedQep : record?.Q2ApprovedQep,
            q2Actuals = record?.Q2Actuals
        });
    }
}
