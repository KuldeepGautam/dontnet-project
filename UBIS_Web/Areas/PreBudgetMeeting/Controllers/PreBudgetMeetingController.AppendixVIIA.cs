// Save/Freeze/Delete actions + view-model builder for Appendix VII-A: Recoveries.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    // --- Appendix VII-A ---
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAppendixVIIA(int demandId, SaveAppendixRecoveriesDto newRecord, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        newRecord.DemandId = demandId; newRecord.FinancialYear = session.FinancialYear;
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var result = newRecord.Id.HasValue && newRecord.Id.Value > 0
            ? await _preBudgetClient.UpdateAppendixVIIAAsync(session.Token, newRecord.Id.Value, newRecord, ct)
            : await _preBudgetClient.CreateAppendixVIIAAsync(session.Token, newRecord, ct);
        var model = await BuildAppendixVIIAViewModelAsync(session, demandId, demandName, ct);
        // "Input grid still not blank after Submit" (client testing feedback, 2026-08-24) - asp-for
        // renders from ModelState before the model; see AppendixIV.cs's matching comment. Only on
        // success - keep the user's typed values on the page if the save itself failed.
        if (result.IsSuccess)
        {
            ModelState.Clear();
        }
        model.StatusIsError = !result.IsSuccess;
        model.StatusMessage = result.IsSuccess ? (newRecord.Id.HasValue && newRecord.Id.Value > 0 ? "Record modified successfully." : "Record saved successfully.") : (result.Error?.Message ?? "Could not save the record.");
        ViewData["Title"] = "Appendix VII-A - Recoveries";
        return View("AppendixVIIA", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixVIIA(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.FreezeAppendixVIIAAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see FreezeAppendixTemplate's matching comment
        // (PreBudgetMeetingController.cs).
        return await AppendixEntry("VII-A", demandId, ct);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppendixVIIA(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.DeleteAppendixVIIAAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see DeleteAppendixIA's matching comment.
        return await AppendixEntry("VII-A", demandId, ct);
    }

    /// <summary>
    /// Export dropdown (2026-09-10, drawer/grid redesign) - PDF/Excel/CSV, same TabularReportDto/
    /// _reportingClient shape as ExportAppendixVI.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportAppendixVIIA(int demandId, string format, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixVIIAViewModelAsync(session, demandId, demandName, ct);

        var dataRows = model.Records.Select((r, i) => new[]
        {
            (i + 1).ToString(),
            r.IsFrozen ? "Frozen" : "Active",
            r.MajorHeadName ?? "",
            r.SchemeName ?? "",
            r.Actuals?.ToString("0.00") ?? "0.00",
            r.BE?.ToString("0.00") ?? "0.00",
            r.RE?.ToString("0.00") ?? "0.00",
            r.NBE?.ToString("0.00") ?? "0.00"
        }).ToList();

        var report = new TabularReportDto
        {
            Title = "Appendix VII-A - Recoveries in Reduction of Expenditure",
            Subtitle = $"Demand: {demandName} | Financial Year: {session.FinancialYear}",
            ParaNo = model.ParaNo,
            Remarks = model.Remarks,
            Columns = new List<string>
            {
                "S.No.", "Status", "Major Head", "Scheme Name",
                $"Actual {model.PriorPriorFinancialYear}", $"B.E. {model.PriorFinancialYear}",
                $"R.E. {model.PriorFinancialYear}", $"B.E. {model.FinancialYear}"
            },
            Groups = new List<ReportGroupDto> { new() { Rows = dataRows } },
            FileName = $"AppendixVIIA-{session.UserName}-{DateTime.Now:MMddyyyyHHmmss}"
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
            return await AppendixEntry("VII-A", demandId, ct);
        }

        var (contentType, extension) = format?.ToLowerInvariant() switch
        {
            "excel" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            "csv" => ("text/csv", "csv"),
            _ => ("application/pdf", "pdf")
        };
        return File(exportResult.Data, contentType, $"{report.FileName}.{extension}");
    }

    private async Task<AppendixVIIAViewModel> BuildAppendixVIIAViewModelAsync(UbisSessionData session, int demandId, string demandName, CancellationToken ct)
    {
        var result = await _preBudgetClient.GetAppendixVIIAAsync(session.Token, demandId, session.FinancialYear, ct);
        var majorHeads = await _preBudgetClient.GetAppendixVIIAMajorHeadsAsync(session.Token, demandId, ct);
        var model = new AppendixVIIAViewModel
        {
            DemandId = demandId,
            DemandName = demandName,
            FinancialYear = session.FinancialYear,
            Records = result.Data ?? new(),
            MajorHeads = majorHeads.Data ?? new()
        };
        await ApplyAppendixPermissionsAsync(model, session, "VII-A", ct);
        await ApplyFreezeStatusAsync(model, session, demandId, "VII-A", ct);
        return model;
    }

    /// <summary>
    /// AJAX-only (2026-08-04): Appendix VII-A has no year-relative column headers at all ("Actuals",
    /// "BE", "RE", "NBE" - unlike VII-B/XI's explicit Y-1/Y-2/Y-3 labels), so only Actuals is treated
    /// as historical fact per this session's established convention (Actuals = already-known/settled,
    /// BE/RE/NBE = this cycle's live estimates). SchemeName is free text, not an FK - keyed by
    /// DemandId+exact-text-SchemeName, same as VI-D's NameOfInstitute.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAppendixVIIAPreviousYearReference(int demandId, string schemeName, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();

        var priorYear = GetPrecedingFinancialYears(session.FinancialYear, 2).FirstOrDefault();
        if (string.IsNullOrEmpty(priorYear))
        {
            return Json(new { found = false });
        }

        var result = await _preBudgetClient.GetAppendixVIIAAsync(session.Token, demandId, priorYear, ct);
        var record = result.Data?.FirstOrDefault(r => string.Equals(r.SchemeName, schemeName, StringComparison.OrdinalIgnoreCase));
        if (record == null)
        {
            return Json(new { found = false });
        }

        return Json(new
        {
            found = true,
            financialYear = priorYear,
            actuals = record.Actuals
        });
    }
}
