// Save/Freeze/Delete actions + view-model builder for Appendix VI-E: Details of Autonomous Bodies
// for which Corpus Fund has been created out of Grants-in-aid (GiA) support.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    // --- Appendix VI-E ---
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAppendixVIE(int demandId, SaveAppendixCorpusFundAbGiaDto newRecord, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        newRecord.DemandId = demandId; newRecord.FinancialYear = session.FinancialYear;
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var result = newRecord.Id.HasValue && newRecord.Id.Value > 0
            ? await _preBudgetClient.UpdateAppendixVIEAsync(session.Token, newRecord.Id.Value, newRecord, ct)
            : await _preBudgetClient.CreateAppendixVIEAsync(session.Token, newRecord, ct);
        var model = await BuildAppendixVIEViewModelAsync(session, demandId, demandName, ct);
        // "Input grid still not blank after Submit" (client testing feedback, 2026-08-24) - asp-for
        // renders from ModelState before the model; see AppendixIV.cs's matching comment. Only on
        // success - keep the user's typed values on the page if the save itself failed.
        if (result.IsSuccess)
        {
            ModelState.Clear();
        }
        model.StatusIsError = !result.IsSuccess;
        model.StatusMessage = result.IsSuccess ? (newRecord.Id.HasValue && newRecord.Id.Value > 0 ? "Record modified successfully." : "Record saved successfully.") : (result.Error?.Message ?? "Could not save the record.");
        ViewData["Title"] = "Appendix VI-E : Details of Autonomous Bodies for which Corpus Fund has been created out of Grants-in-aid (GiA) support";
        return View("AppendixVIE", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixVIEBulk(int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixVIEViewModelAsync(session, demandId, demandName, ct);
        foreach (var record in model.Records.Where(r => !r.IsFrozen))
        {
            await _preBudgetClient.FreezeAppendixVIEAsync(session.Token, record.Id, ct);
        }
        model = await BuildAppendixVIEViewModelAsync(session, demandId, demandName, ct);
        model.StatusMessage = "Corpus Fund from GiA Support data frozen.";
        ViewData["Title"] = "Appendix VI-E : Details of Autonomous Bodies for which Corpus Fund has been created out of Grants-in-aid (GiA) support";
        return View("AppendixVIE", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppendixVIE(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.DeleteAppendixVIEAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see DeleteAppendixIA's matching comment.
        return await AppendixEntry("VI-E", demandId, ct);
    }

    /// <summary>Row-level Freeze (2026-08-21, history-grid Action-icon rebuild).</summary>
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixVIE(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.FreezeAppendixVIEAsync(session.Token, id, ct);
        return await AppendixEntry("VI-E", demandId, ct);
    }

    /// <summary>
    /// Export dropdown (2026-09-08, UBIS-Pre-budget-html/Appendix6e.html design) - PDF/Excel/CSV,
    /// same TabularReportDto/_reportingClient shape as ExportAppendixVIA/VIB/VIC/VID.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportAppendixVIE(int demandId, string format, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixVIEViewModelAsync(session, demandId, demandName, ct);

        var dataRows = model.Records.Select((r, i) => new[]
        {
            (i + 1).ToString(),
            r.AutonomousBodyName ?? "",
            r.IsFrozen ? "Frozen" : "Active",
            r.CorpusFundBalance1?.ToString("0.00") ?? "0.00",
            r.CorpusFundBalance2?.ToString("0.00") ?? "0.00",
            r.CorpusFundBankName ?? "",
            r.CorpusFundReason ?? "",
            r.GiaRE?.ToString("0.00") ?? "0.00",
            r.GiaBE?.ToString("0.00") ?? "0.00"
        }).ToList();

        var yMinus1 = model.PriorFinancialYear;
        var asOnMarchYear = yMinus1.Split('-')[0];
        var asOnSeptYear = yMinus1.Split('-')[0];
        var yShort = yMinus1.Split('-')[0] + "-" + yMinus1.Split('-')[1].Substring(2);

        var report = new TabularReportDto
        {
            Title = "Appendix VI-E - Corpus Fund from Grants-in-aid (GiA) Support",
            Subtitle = $"Demand: {demandName} | Financial Year: {session.FinancialYear}",
            ParaNo = model.ParaNo,
            Remarks = model.Remarks,
            Columns = new List<string>
            {
                "S.No.", "Autonomous Body", "Status",
                $"Balance 31.03.{asOnMarchYear}", $"Balance 30.09.{asOnSeptYear}",
                "Bank Name", "Reason for Continuance", $"RE {yShort}", $"BE {yShort}"
            },
            Groups = new List<ReportGroupDto> { new() { Rows = dataRows } },
            FileName = $"AppendixVIE-{session.UserName}-{DateTime.Now:MMddyyyyHHmmss}"
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
            return await AppendixEntry("VI-E", demandId, ct);
        }

        var (contentType, extension) = format?.ToLowerInvariant() switch
        {
            "excel" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            "csv" => ("text/csv", "csv"),
            _ => ("application/pdf", "pdf")
        };
        return File(exportResult.Data, contentType, $"{report.FileName}.{extension}");
    }

    private async Task<AppendixVIEViewModel> BuildAppendixVIEViewModelAsync(UbisSessionData session, int demandId, string demandName, CancellationToken ct)
    {
        var result = await _preBudgetClient.GetAppendixVIEAsync(session.Token, demandId, session.FinancialYear, ct);
        var autonomousBodies = await _preBudgetClient.GetAutonomousBodiesAsync(session.Token, demandId, session.FinancialYear, ct);
        var model = new AppendixVIEViewModel
        {
            DemandId = demandId,
            DemandName = demandName,
            FinancialYear = session.FinancialYear,
            Records = result.Data ?? new(),
            // Client requirement 2026-09-09: "Autonomous Body must be loaded in dropdown ... order
            // by autonomous number as integer." GetAutonomousBodiesAsync itself still orders by
            // Name (unchanged - it's shared with V-A/VI-C/VI-D's own dropdowns, which weren't asked
            // to change) - re-sorted here, scoped to VI-E only, same numeric-not-string-sort
            // reasoning as MajorHeadDisplayFormatter.NumericSortKey (Code is a varchar column, so a
            // plain string sort would put "10" before "2").
            AutonomousBodies = (autonomousBodies.Data ?? new())
                .OrderBy(a => int.TryParse(a.Code, out var codeNumber) ? codeNumber : int.MaxValue)
                .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
        await ApplyFreezeStatusAsync(model, session, demandId, "VI-E", ct);
        return model;
    }

    /// <summary>Keyed by DemandId+AutonomousBodyId like V-A/V-C/VI-C. Only CorpusFundBalance1 (as on 31 March, prior year) is historical - CorpusFundBalance2 is this cycle's own actual-to-date snapshot, GiaRE/GiaBE are this cycle's live proposals.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAppendixVIEPreviousYearReference(int demandId, int autonomousBodyId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();

        var priorYear = GetPrecedingFinancialYears(session.FinancialYear, 2).FirstOrDefault();
        if (string.IsNullOrEmpty(priorYear))
        {
            return Json(new { found = false });
        }

        var result = await _preBudgetClient.GetAppendixVIEAsync(session.Token, demandId, priorYear, ct);
        var record = result.Data?.FirstOrDefault(r => r.AutonomousBodyId == autonomousBodyId);
        if (record == null)
        {
            return Json(new { found = false });
        }

        return Json(new
        {
            found = true,
            financialYear = priorYear,
            corpusFundBalance1 = record.CorpusFundBalance1
        });
    }
}
