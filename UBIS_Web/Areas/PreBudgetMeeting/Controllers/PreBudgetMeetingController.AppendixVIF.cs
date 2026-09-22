// Save/Freeze/Delete actions + view-model builder for Appendix VI-F: User Charges of Ministries/
// Departments and its various organizations Receipts collected in Consolidated Fund of India. New
// appendix, 2026-09-09 - see UBIS-Pre-budget-html/Appendix6f.html.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    // --- Appendix VI-F ---
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAppendixVIF(int demandId, SaveAppendixMinorHeadUserChargesDto newRecord, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        newRecord.DemandId = demandId; newRecord.FinancialYear = session.FinancialYear;
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var result = newRecord.Id.HasValue && newRecord.Id.Value > 0
            ? await _preBudgetClient.UpdateAppendixVIFAsync(session.Token, newRecord.Id.Value, newRecord, ct)
            : await _preBudgetClient.CreateAppendixVIFAsync(session.Token, newRecord, ct);
        var model = await BuildAppendixVIFViewModelAsync(session, demandId, demandName, ct);
        if (result.IsSuccess)
        {
            ModelState.Clear();
        }
        model.StatusIsError = !result.IsSuccess;
        model.StatusMessage = result.IsSuccess ? (newRecord.Id.HasValue && newRecord.Id.Value > 0 ? "Record modified successfully." : "Record saved successfully.") : (result.Error?.Message ?? "Could not save the record.");
        ViewData["Title"] = "Appendix VI-F : User Charges of Ministries/Departments";
        return View("AppendixVIF", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppendixVIF(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.DeleteAppendixVIFAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see DeleteAppendixIA's matching comment.
        return await AppendixEntry("VI-F", demandId, ct);
    }

    /// <summary>Row-level Freeze (Action-icon rebuild, same pattern as FreezeAppendixVIE).</summary>
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixVIF(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.FreezeAppendixVIFAsync(session.Token, id, ct);
        return await AppendixEntry("VI-F", demandId, ct);
    }

    /// <summary>
    /// Export dropdown (2026-09-09, UBIS-Pre-budget-html/Appendix6f.html design) - PDF/Excel/CSV,
    /// same TabularReportDto/_reportingClient shape as ExportAppendixVIE.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportAppendixVIF(int demandId, string format, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixVIFViewModelAsync(session, demandId, demandName, ct);

        var dataRows = model.Records.Select((r, i) => new[]
        {
            (i + 1).ToString(),
            r.MinorHeadCode,
            r.IsFrozen ? "Frozen" : "Active",
            r.BriefOnReceipts ?? "",
            r.PresentStatus ?? "",
            r.NoOfTransactions?.ToString("0.##") ?? "",
            r.RateOfService ?? "",
            r.ReceiptsCollection?.ToString("0.00") ?? "0.00",
            r.ActionTakenPlan ?? ""
        }).ToList();

        var report = new TabularReportDto
        {
            Title = "Appendix VI-F - User Charges of Ministries/Departments",
            Subtitle = $"Demand: {demandName} | Financial Year: {session.FinancialYear}",
            ParaNo = model.ParaNo,
            Remarks = model.Remarks,
            Columns = new List<string>
            {
                "S.No.", "Minor Head", "Status", "Brief on Nature of Receipts",
                "Present Status", "No of Transactions", "Rate of Service",
                $"Receipts Collection ({model.PriorFinancialYear})", "Action Taken Plan"
            },
            Groups = new List<ReportGroupDto> { new() { Rows = dataRows } },
            FileName = $"AppendixVIF-{session.UserName}-{DateTime.Now:MMddyyyyHHmmss}"
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
            return await AppendixEntry("VI-F", demandId, ct);
        }

        var (contentType, extension) = format?.ToLowerInvariant() switch
        {
            "excel" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            "csv" => ("text/csv", "csv"),
            _ => ("application/pdf", "pdf")
        };
        return File(exportResult.Data, contentType, $"{report.FileName}.{extension}");
    }

    private async Task<AppendixVIFViewModel> BuildAppendixVIFViewModelAsync(UbisSessionData session, int demandId, string demandName, CancellationToken ct)
    {
        var result = await _preBudgetClient.GetAppendixVIFAsync(session.Token, demandId, session.FinancialYear, ct);
        var model = new AppendixVIFViewModel
        {
            DemandId = demandId,
            DemandName = demandName,
            FinancialYear = session.FinancialYear,
            Records = result.Data ?? new()
        };
        await ApplyFreezeStatusAsync(model, session, demandId, "VI-F", ct);
        return model;
    }

    /// <summary>
    /// AJAX-only: Minor Head autocomplete (client requirement 2026-09-09 - see
    /// AppendixVIFController.SearchMinorHeads's own comment for the reference SQL this proxies).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SearchAppendixVIFMinorHeads(int demandId, string query, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();
        var result = await _preBudgetClient.SearchMinorHeadsAsync(session.Token, demandId, session.FinancialYear, query, ct);
        return Json(result.Data ?? new());
    }

    /// <summary>AJAX-only: resolves a Minor Head code's name once picked/typed.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAppendixVIFMinorHeadName(string minorHeadCode, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();
        var result = await _preBudgetClient.GetMinorHeadNameAsync(session.Token, minorHeadCode, session.FinancialYear, ct);
        return Json(result.Data ?? new MinorHeadNameDto { Found = false });
    }
}
