// Save/Freeze/Delete actions + view-model builder for Appendix VI-A: List of User Charges levied
// by the Departments/Ministries.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    // --- Appendix VI-A ---
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAppendixVIA(int demandId, SaveAppendixUserChargesDto newRecord, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        newRecord.DemandId = demandId; newRecord.FinancialYear = session.FinancialYear;
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var result = newRecord.Id.HasValue && newRecord.Id.Value > 0
            ? await _preBudgetClient.UpdateAppendixVIAAsync(session.Token, newRecord.Id.Value, newRecord, ct)
            : await _preBudgetClient.CreateAppendixVIAAsync(session.Token, newRecord, ct);
        var model = await BuildAppendixVIAViewModelAsync(session, demandId, demandName, ct);
        // "Input grid still not blank after Submit" (client testing feedback, 2026-08-24) - asp-for
        // renders from ModelState before the model; see AppendixIV.cs's matching comment. Only on
        // success - keep the user's typed values on the page if the save itself failed.
        if (result.IsSuccess)
        {
            ModelState.Clear();
        }
        model.StatusIsError = !result.IsSuccess;
        model.StatusMessage = result.IsSuccess ? (newRecord.Id.HasValue && newRecord.Id.Value > 0 ? "Record modified successfully." : "Record saved successfully.") : (result.Error?.Message ?? "Could not save the record.");
        ViewData["Title"] = "Appendix VI-A : List of User Charges levied by the Departments/Ministries";
        return View("AppendixVIA", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixVIABulk(int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixVIAViewModelAsync(session, demandId, demandName, ct);
        foreach (var record in model.Records.Where(r => !r.IsFrozen))
        {
            await _preBudgetClient.FreezeAppendixVIAAsync(session.Token, record.Id, ct);
        }
        model = await BuildAppendixVIAViewModelAsync(session, demandId, demandName, ct);
        model.StatusMessage = "List of User Charges data frozen.";
        ViewData["Title"] = "Appendix VI-A : List of User Charges levied by the Departments/Ministries";
        return View("AppendixVIA", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppendixVIA(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.DeleteAppendixVIAAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see DeleteAppendixIA's matching comment.
        return await AppendixEntry("VI-A", demandId, ct);
    }

    /// <summary>Row-level Freeze (2026-08-21, history-grid Action-icon rebuild).</summary>
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixVIA(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.FreezeAppendixVIAAsync(session.Token, id, ct);
        return await AppendixEntry("VI-A", demandId, ct);
    }

    /// <summary>
    /// Export dropdown (2026-09-07, UBIS-Pre-budget-html/Appendix6a.html design) - PDF/Excel/CSV,
    /// same "open on click" menu now replicated app-wide from that design, same
    /// TabularReportDto/_reportingClient shape as ExportAppendixI's own PDF/Excel export, plus the
    /// new CSV format added to the Reporting microservice (Core/Reporting) alongside it. Exports
    /// the full current Demand+FinancialYear record set (matches ExportAppendixI's own scope -
    /// no client-side search/status filter passthrough), same real DB-backed data the grid itself
    /// renders from BuildAppendixVIAViewModelAsync.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportAppendixVIA(int demandId, string format, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixVIAViewModelAsync(session, demandId, demandName, ct);

        var dataRows = model.Records.Select((r, i) => new[]
        {
            (i + 1).ToString(),
            r.TitleOfCharge ?? "",
            r.Service ?? "",
            r.OrgDept ?? "",
            r.IsFrozen ? "Frozen" : "Active",
            r.TotalRevenueY1?.ToString("0.00") ?? "0.00",
            r.TotalRevenueY2?.ToString("0.00") ?? "0.00",
            r.TotalRevenueY3?.ToString("0.00") ?? "0.00",
            string.IsNullOrEmpty(r.Remarks) ? "-" : r.Remarks
        }).ToList();

        var y1 = model.YearOffset(-4);
        var y2 = model.YearOffset(-3);
        var y3 = model.YearOffset(-2);

        var report = new TabularReportDto
        {
            Title = "Appendix VI-A - List of User Charges levied by the Departments/Ministries",
            Subtitle = $"Demand: {demandName} | Financial Year: {session.FinancialYear}",
            ParaNo = model.ParaNo,
            Remarks = model.Remarks,
            Columns = new List<string>
            {
                "S.No.", "Title of the User Charge", "Services for which the User Charge is levied",
                "Organisation/Department", "Status", $"Total Revenue {y1}", $"Total Revenue {y2}", $"Total Revenue {y3}", "Remarks"
            },
            Groups = new List<ReportGroupDto> { new() { Rows = dataRows } },
            // Same "AppendixName-username-DateInMMDDYYYYhhminss" convention as ExportAppendixI.
            FileName = $"AppendixVIA-{session.UserName}-{DateTime.Now:MMddyyyyHHmmss}"
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
            return await AppendixEntry("VI-A", demandId, ct);
        }

        var (contentType, extension) = format?.ToLowerInvariant() switch
        {
            "excel" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            "csv" => ("text/csv", "csv"),
            _ => ("application/pdf", "pdf")
        };
        return File(exportResult.Data, contentType, $"{report.FileName}.{extension}");
    }

    private async Task<AppendixVIAViewModel> BuildAppendixVIAViewModelAsync(UbisSessionData session, int demandId, string demandName, CancellationToken ct)
    {
        var result = await _preBudgetClient.GetAppendixVIAAsync(session.Token, demandId, session.FinancialYear, ct);
        var model = new AppendixVIAViewModel { DemandId = demandId, DemandName = demandName, FinancialYear = session.FinancialYear, Records = result.Data ?? new() };
        await ApplyFreezeStatusAsync(model, session, demandId, "VI-A", ct);
        return model;
    }

    /// <summary>
    /// AJAX-only (2026-08-04): Appendix VI-A's TotalRevenueY1/Y2/Y3 are demand-level aggregate
    /// figures duplicated on every User Charge row for that demand+year (not per-charge), so unlike
    /// every other appendix here this is keyed by DemandId alone and fetched once per fragment load
    /// rather than on a dropdown change - see <c>applyFragment</c> in pre-budget-meeting.js.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAppendixVIAPreviousYearReference(int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();

        var priorYear = GetPrecedingFinancialYears(session.FinancialYear, 2).FirstOrDefault();
        if (string.IsNullOrEmpty(priorYear))
        {
            return Json(new { found = false });
        }

        var result = await _preBudgetClient.GetAppendixVIAAsync(session.Token, demandId, priorYear, ct);
        var record = result.Data?.FirstOrDefault();
        if (record == null)
        {
            return Json(new { found = false });
        }

        return Json(new
        {
            found = true,
            financialYear = priorYear,
            totalRevenueY1 = record.TotalRevenueY1,
            totalRevenueY2 = record.TotalRevenueY2,
            totalRevenueY3 = record.TotalRevenueY3
        });
    }
}
