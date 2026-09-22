// Save/Freeze/Delete actions + view-model builder for Appendix III-B: Status of Scheme Appraisal/
// Approval during the XVI Finance Commission Cycle. New appendix, 2026-09-09 - see
// Appendix_Papes_I_to IIIB/Appendix-III-B.html.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    // --- Appendix III-B ---
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAppendixIIIB(int demandId, SaveAppendixSchemeAppraisalStatusDto newRecord, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        newRecord.DemandId = demandId; newRecord.FinancialYear = session.FinancialYear;
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var result = newRecord.Id.HasValue && newRecord.Id.Value > 0
            ? await _preBudgetClient.UpdateAppendixIIIBAsync(session.Token, newRecord.Id.Value, newRecord, ct)
            : await _preBudgetClient.CreateAppendixIIIBAsync(session.Token, newRecord, ct);
        var model = await BuildAppendixIIIBViewModelAsync(session, demandId, demandName, ct);
        if (result.IsSuccess)
        {
            ModelState.Clear();
        }
        model.StatusIsError = !result.IsSuccess;
        model.StatusMessage = result.IsSuccess ? (newRecord.Id.HasValue && newRecord.Id.Value > 0 ? "Record modified successfully." : "Record saved successfully.") : (result.Error?.Message ?? "Could not save the record.");
        ViewData["Title"] = "Appendix III-B : Status of Scheme Appraisal/Approval";
        return View("AppendixIIIB", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppendixIIIB(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.DeleteAppendixIIIBAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see DeleteAppendixIA's matching comment.
        return await AppendixEntry("III-B", demandId, ct);
    }

    /// <summary>Row-level Freeze (Action-icon rebuild, same pattern as FreezeAppendixVIF).</summary>
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixIIIB(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.FreezeAppendixIIIBAsync(session.Token, id, ct);
        return await AppendixEntry("III-B", demandId, ct);
    }

    /// <summary>
    /// Export dropdown (2026-09-09, Appendix_Papes_I_to IIIB/Appendix-III-B.html design) - PDF/
    /// Excel/CSV, same TabularReportDto/_reportingClient shape as ExportAppendixVIF.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportAppendixIIIB(int demandId, string format, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixIIIBViewModelAsync(session, demandId, demandName, ct);

        // Client requirement 2026-09-18: export must match the government circular's own
        // Appendix-III-B table exactly (screenshot supplied) - one shared column set (Sl No / Name
        // of Scheme / Status of fresh appraisal/approval / Scheme Approval valid upto / Remarks),
        // rows grouped under a full-width Category heading with Sl No restarting at 1 per group -
        // not the app's own flat grid with Status/Category-as-a-column (this app's own UI
        // convenience, not part of the approved format). Category grouping is data-driven (M_Category
        // is a live SerialNo II/IV lookup, not a fixed enum like Appendix III's Balance Type), so
        // groups are built from whatever distinct CategoryType values actually exist for this
        // Demand/year, in the order they first appear, rather than a hardcoded CSS/CS pair.
        var groups = model.Records
            .GroupBy(r => r.CategoryType)
            .Select(g => new ReportGroupDto
            {
                Label = CategoryHeading(g.Key),
                Rows = g.Select((r, i) => new[]
                {
                    (i + 1).ToString(),
                    r.SchemeName,
                    r.StatusOfFreshAppraisalApproval,
                    r.SchemeApprovalValidUpto?.ToString("dd-MMM-yyyy") ?? "-",
                    r.Remarks ?? ""
                }).ToList()
            }).ToList();

        var report = new TabularReportDto
        {
            Title = "Appendix-III-B: Status of Scheme Appraisal/ Approval during the XVI Finance Commission Cycle",
            Subtitle = $"Demand: {demandName} | Financial Year: {session.FinancialYear}",
            ParaNo = model.ParaNo,
            Remarks = model.Remarks,
            Columns = new List<string>
            {
                "Sl No.", "Name of Scheme", "Status of fresh appraisal / approval", "Scheme Approval valid upto", "Remarks"
            },
            CenterAlignedColumns = new List<int> { 0 },
            Groups = groups,
            FileName = $"AppendixIIIB-{session.UserName}-{DateTime.Now:MMddyyyyHHmmss}"
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
            return await AppendixEntry("III-B", demandId, ct);
        }

        var (contentType, extension) = format?.ToLowerInvariant() switch
        {
            "excel" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            "csv" => ("text/csv", "csv"),
            _ => ("application/pdf", "pdf")
        };
        return File(exportResult.Data, contentType, $"{report.FileName}.{extension}");
    }

    // AJAX-only: Scheme dropdown, cascading from the Category chosen on Appendix III-B (client
    // instruction 2026-09-10). Same shape as GetAppendixIIISchemes / GetAppendixVIBSchemes.
    [HttpGet]
    public async Task<IActionResult> GetAppendixIIIBSchemes(int demandId, int categoryId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();
        var result = await _preBudgetClient.GetAppendixIIIBSchemesAsync(session.Token, demandId, categoryId, ct);
        return Json(result.Data ?? new());
    }

    private async Task<AppendixIIIBViewModel> BuildAppendixIIIBViewModelAsync(UbisSessionData session, int demandId, string demandName, CancellationToken ct)
    {
        var result = await _preBudgetClient.GetAppendixIIIBAsync(session.Token, demandId, session.FinancialYear, ct);
        var categories = await _preBudgetClient.GetAppendixIIIBCategoriesAsync(session.Token, session.FinancialYear, ct);
        var model = new AppendixIIIBViewModel
        {
            DemandId = demandId,
            DemandName = demandName,
            FinancialYear = session.FinancialYear,
            Records = result.Data ?? new(),
            Categories = categories.Data ?? new()
        };
        await ApplyFreezeStatusAsync(model, session, demandId, "III-B", ct);
        return model;
    }
}
