// Save/Freeze/Delete actions + view-model builder for Appendix VI-B: Pending Liabilities of
// Ministries. Also carries the Scheme/Sub-Scheme AJAX dropdown helpers for Appendix VI-B.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    // --- Appendix VI-B ---
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAppendixVIB(int demandId, SaveAppendixPendingLiabilitiesDto newRecord, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        newRecord.DemandId = demandId; newRecord.FinancialYear = session.FinancialYear;
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var result = newRecord.Id.HasValue && newRecord.Id.Value > 0
            ? await _preBudgetClient.UpdateAppendixVIBAsync(session.Token, newRecord.Id.Value, newRecord, ct)
            : await _preBudgetClient.CreateAppendixVIBAsync(session.Token, newRecord, ct);
        var model = await BuildAppendixVIBViewModelAsync(session, demandId, demandName, ct);
        // "Input grid still not blank after Submit" (client testing feedback, 2026-08-24) - asp-for
        // renders from ModelState before the model; see AppendixIV.cs's matching comment. Only on
        // success - keep the user's typed values on the page if the save itself failed.
        if (result.IsSuccess)
        {
            ModelState.Clear();
        }
        model.StatusIsError = !result.IsSuccess;
        model.StatusMessage = result.IsSuccess ? (newRecord.Id.HasValue && newRecord.Id.Value > 0 ? "Record modified successfully." : "Record saved successfully.") : (result.Error?.Message ?? "Could not save the record.");
        ViewData["Title"] = "Appendix VI-B : Pending Liabilities of Ministries";
        return View("AppendixVIB", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixVIBBulk(int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixVIBViewModelAsync(session, demandId, demandName, ct);
        foreach (var record in model.Records.Where(r => !r.IsFrozen))
        {
            await _preBudgetClient.FreezeAppendixVIBAsync(session.Token, record.Id, ct);
        }
        model = await BuildAppendixVIBViewModelAsync(session, demandId, demandName, ct);
        model.StatusMessage = "Pending Liabilities data frozen.";
        ViewData["Title"] = "Appendix VI-B : Pending Liabilities of Ministries";
        return View("AppendixVIB", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppendixVIB(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.DeleteAppendixVIBAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see DeleteAppendixIA's matching comment.
        return await AppendixEntry("VI-B", demandId, ct);
    }

    /// <summary>Row-level Freeze (2026-08-21, history-grid Action-icon rebuild).</summary>
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixVIB(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.FreezeAppendixVIBAsync(session.Token, id, ct);
        return await AppendixEntry("VI-B", demandId, ct);
    }

    /// <summary>
    /// Export dropdown (2026-09-08, UBIS-Pre-budget-html/Appendix6b.html design) - PDF/Excel/CSV,
    /// same TabularReportDto/_reportingClient shape as ExportAppendixVIA. Exports the full
    /// current Demand+FinancialYear record set (same scope as every other appendix's export).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportAppendixVIB(int demandId, string format, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixVIBViewModelAsync(session, demandId, demandName, ct);

        var dataRows = model.Records.Select((r, i) => new[]
        {
            (i + 1).ToString(),
            r.CategoryName ?? "",
            r.SchemeName ?? "",
            r.SubSchemeName ?? "",
            r.IsFrozen ? "Frozen" : "Active",
            r.PendingLiabilityAsOnMarch31?.ToString("0.00") ?? "0.00",
            r.BE?.ToString("0.00") ?? "0.00",
            r.EstimatedExpenditure?.ToString("0.00") ?? "0.00",
            string.IsNullOrEmpty(r.Remarks) ? "-" : r.Remarks
        }).ToList();

        var yMinus1 = model.PriorFinancialYear;
        var pendingLiabilityAsOnYear = yMinus1.Split('-')[0];

        var report = new TabularReportDto
        {
            Title = "Appendix VI-B - Pending Liabilities of Ministries",
            Subtitle = $"Demand: {demandName} | Financial Year: {session.FinancialYear}",
            ParaNo = model.ParaNo,
            Remarks = model.Remarks,
            Columns = new List<string>
            {
                "S.No.", "Category", "Scheme", "Sub-Scheme", "Status",
                $"Pending Liability upto 31 March {pendingLiabilityAsOnYear}", $"B.E. {yMinus1}", $"Estimated Expenditure in {yMinus1}", "Remarks"
            },
            Groups = new List<ReportGroupDto> { new() { Rows = dataRows } },
            FileName = $"AppendixVIB-{session.UserName}-{DateTime.Now:MMddyyyyHHmmss}"
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
            return await AppendixEntry("VI-B", demandId, ct);
        }

        var (contentType, extension) = format?.ToLowerInvariant() switch
        {
            "excel" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            "csv" => ("text/csv", "csv"),
            _ => ("application/pdf", "pdf")
        };
        return File(exportResult.Data, contentType, $"{report.FileName}.{extension}");
    }

    private async Task<AppendixVIBViewModel> BuildAppendixVIBViewModelAsync(UbisSessionData session, int demandId, string demandName, CancellationToken ct)
    {
        var result = await _preBudgetClient.GetAppendixVIBAsync(session.Token, demandId, session.FinancialYear, ct);
        var categories = await _preBudgetClient.GetAppendixVIBCategoriesAsync(session.Token, session.FinancialYear, ct);
        var model = new AppendixVIBViewModel
        {
            DemandId = demandId,
            DemandName = demandName,
            FinancialYear = session.FinancialYear,
            Records = result.Data ?? new(),
            Categories = categories.Data ?? new()
        };
        await ApplyFreezeStatusAsync(model, session, demandId, "VI-B", ct);
        return model;
    }

    // AJAX-only: populates the Scheme dropdown once a Category is chosen on Appendix VI-B (added 2026-07-30).
    [HttpGet]
    public async Task<IActionResult> GetAppendixVIBSchemes(int demandId, int categoryId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();
        var result = await _preBudgetClient.GetAppendixVIBSchemesAsync(session.Token, demandId, categoryId, ct);
        return Json(result.Data ?? new());
    }

    // AJAX-only: populates the Sub-Scheme dropdown once a Scheme is chosen on Appendix VI-B.
    [HttpGet]
    public async Task<IActionResult> GetAppendixVIBSubSchemes(int schemeId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();
        var result = await _preBudgetClient.GetAppendixVIBSubSchemesAsync(session.Token, schemeId, ct);
        return Json(result.Data ?? new());
    }

    // AJAX-only: BE auto-load once Category+Scheme are chosen (client reference SQL, 2026-08-25):
    // "select sum(BE_Plan) from SBEData where DemandId=... and CategoryId=... and SchemeID=...".
    [HttpGet]
    public async Task<IActionResult> GetAppendixVIBBe(int demandId, int categoryId, int schemeId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();
        var result = await _preBudgetClient.GetAppendixVIBBeAsync(session.Token, demandId, categoryId, schemeId, ct);
        return Json(new { be = result.Data?.Be ?? 0m });
    }

    /// <summary>Same self-referential prior-year pre-fill pattern, keyed by DemandId+SchemeId+SubSchemeId like Appendix IV. Only PendingLiabilityAsOnMarch31 is historical - BE/EstimatedExpenditure are this cycle's own figures.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAppendixVIBPreviousYearReference(int demandId, int schemeId, int? subSchemeId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();

        var priorYear = GetPrecedingFinancialYears(session.FinancialYear, 2).FirstOrDefault();
        if (string.IsNullOrEmpty(priorYear))
        {
            return Json(new { found = false });
        }

        var result = await _preBudgetClient.GetAppendixVIBAsync(session.Token, demandId, priorYear, ct);
        var record = result.Data?.FirstOrDefault(r => r.SchemeId == schemeId && r.SubSchemeId == subSchemeId);
        if (record == null)
        {
            return Json(new { found = false });
        }

        return Json(new
        {
            found = true,
            financialYear = priorYear,
            pendingLiabilityAsOnMarch31 = record.PendingLiabilityAsOnMarch31
        });
    }
}
