// Save/Freeze/Delete actions + view-model builder for Appendix VI-G: User Charges of Autonomous
// Bodies / various organizations. New appendix, 2026-09-09 - see
// UBIS-Pre-budget-html/Appendix6g.html.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    // --- Appendix VI-G ---
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAppendixVIG(int demandId, SaveAppendixUserChargesAutonomousBodyDto newRecord, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        newRecord.DemandId = demandId; newRecord.FinancialYear = session.FinancialYear;
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var result = newRecord.Id.HasValue && newRecord.Id.Value > 0
            ? await _preBudgetClient.UpdateAppendixVIGAsync(session.Token, newRecord.Id.Value, newRecord, ct)
            : await _preBudgetClient.CreateAppendixVIGAsync(session.Token, newRecord, ct);
        var model = await BuildAppendixVIGViewModelAsync(session, demandId, demandName, ct);
        if (result.IsSuccess)
        {
            ModelState.Clear();
        }
        model.StatusIsError = !result.IsSuccess;
        model.StatusMessage = result.IsSuccess ? (newRecord.Id.HasValue && newRecord.Id.Value > 0 ? "Record modified successfully." : "Record saved successfully.") : (result.Error?.Message ?? "Could not save the record.");
        ViewData["Title"] = "Appendix VI-G : User Charges of Autonomous Bodies";
        return View("AppendixVIG", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppendixVIG(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.DeleteAppendixVIGAsync(session.Token, id, ct);
        return await AppendixEntry("VI-G", demandId, ct);
    }

    /// <summary>Row-level Freeze (Action-icon rebuild, same pattern as FreezeAppendixVIE).</summary>
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixVIG(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.FreezeAppendixVIGAsync(session.Token, id, ct);
        return await AppendixEntry("VI-G", demandId, ct);
    }

    /// <summary>
    /// Export dropdown (2026-09-09, UBIS-Pre-budget-html/Appendix6g.html design) - PDF/Excel/CSV,
    /// same TabularReportDto/_reportingClient shape as ExportAppendixVIE.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportAppendixVIG(int demandId, string format, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixVIGViewModelAsync(session, demandId, demandName, ct);

        var dataRows = model.Records.Select((r, i) => new[]
        {
            (i + 1).ToString(),
            r.AutonomousBodyName ?? "",
            r.IsFrozen ? "Frozen" : "Active",
            r.BriefOnRevenueSources ?? "",
            r.PresentStatus ?? "",
            r.ReceiptsCollected?.ToString("0.00") ?? "0.00",
            r.TotalRevenueExpenditure?.ToString("0.00") ?? "0.00",
            r.TotalCapitalExpenditure?.ToString("0.00") ?? "0.00"
        }).ToList();

        var report = new TabularReportDto
        {
            Title = "Appendix VI-G - User Charges of Autonomous Bodies",
            Subtitle = $"Demand: {demandName} | Financial Year: {session.FinancialYear}",
            ParaNo = model.ParaNo,
            Remarks = model.Remarks,
            Columns = new List<string>
            {
                "S.No.", "Autonomous Body", "Status", "Brief on Revenue Sources",
                "Present Status", $"Receipts Collected ({model.PriorFinancialYear})",
                $"Total Revenue Expenditure ({model.PriorFinancialYear})",
                $"Total Capital Expenditure ({model.PriorFinancialYear})"
            },
            Groups = new List<ReportGroupDto> { new() { Rows = dataRows } },
            FileName = $"AppendixVIG-{session.UserName}-{DateTime.Now:MMddyyyyHHmmss}"
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
            return await AppendixEntry("VI-G", demandId, ct);
        }

        var (contentType, extension) = format?.ToLowerInvariant() switch
        {
            "excel" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            "csv" => ("text/csv", "csv"),
            _ => ("application/pdf", "pdf")
        };
        return File(exportResult.Data, contentType, $"{report.FileName}.{extension}");
    }

    private async Task<AppendixVIGViewModel> BuildAppendixVIGViewModelAsync(UbisSessionData session, int demandId, string demandName, CancellationToken ct)
    {
        var result = await _preBudgetClient.GetAppendixVIGAsync(session.Token, demandId, session.FinancialYear, ct);
        var autonomousBodies = await _preBudgetClient.GetAutonomousBodiesAsync(session.Token, demandId, session.FinancialYear, ct);
        var model = new AppendixVIGViewModel
        {
            DemandId = demandId,
            DemandName = demandName,
            FinancialYear = session.FinancialYear,
            Records = result.Data ?? new(),
            // Same numeric-Code ordering fix already applied to VI-E's own dropdown (2026-09-09) -
            // scoped here too rather than touching the shared GetAutonomousBodiesAsync ordering
            // (still Name-ordered for V-A/VI-C/VI-D, which weren't asked to change).
            AutonomousBodies = (autonomousBodies.Data ?? new())
                .OrderBy(a => int.TryParse(a.Code, out var codeNumber) ? codeNumber : int.MaxValue)
                .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
        await ApplyFreezeStatusAsync(model, session, demandId, "VI-G", ct);
        return model;
    }
}
