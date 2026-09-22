// Save/Freeze/Delete actions + view-model builder for Appendix VI-D: Available Internal Resources
// with Grantee Bodies/Autonomous Institutions.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    // --- Appendix VI-D ---
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAppendixVID(int demandId, SaveAppendixInternalResourcesDto newRecord, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        newRecord.DemandId = demandId; newRecord.FinancialYear = session.FinancialYear;
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var result = newRecord.Id.HasValue && newRecord.Id.Value > 0
            ? await _preBudgetClient.UpdateAppendixVIDAsync(session.Token, newRecord.Id.Value, newRecord, ct)
            : await _preBudgetClient.CreateAppendixVIDAsync(session.Token, newRecord, ct);
        var model = await BuildAppendixVIDViewModelAsync(session, demandId, demandName, ct);
        // "Input grid still not blank after Submit" (client testing feedback, 2026-08-24) - asp-for
        // renders from ModelState before the model; see AppendixIV.cs's matching comment. Only on
        // success - keep the user's typed values on the page if the save itself failed.
        if (result.IsSuccess)
        {
            ModelState.Clear();
        }
        model.StatusIsError = !result.IsSuccess;
        model.StatusMessage = result.IsSuccess ? (newRecord.Id.HasValue && newRecord.Id.Value > 0 ? "Record modified successfully." : "Record saved successfully.") : (result.Error?.Message ?? "Could not save the record.");
        ViewData["Title"] = "Appendix VI-D : Available Internal Resources with Grantee Bodies/Autonomous Institutions";
        return View("AppendixVID", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixVIDBulk(int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixVIDViewModelAsync(session, demandId, demandName, ct);
        foreach (var record in model.Records.Where(r => !r.IsFrozen))
        {
            await _preBudgetClient.FreezeAppendixVIDAsync(session.Token, record.Id, ct);
        }
        model = await BuildAppendixVIDViewModelAsync(session, demandId, demandName, ct);
        model.StatusMessage = "Available Internal Resources data frozen.";
        ViewData["Title"] = "Appendix VI-D : Available Internal Resources with Grantee Bodies/Autonomous Institutions";
        return View("AppendixVID", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppendixVID(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.DeleteAppendixVIDAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see DeleteAppendixIA's matching comment.
        return await AppendixEntry("VI-D", demandId, ct);
    }

    /// <summary>Row-level Freeze (2026-08-21, history-grid Action-icon rebuild).</summary>
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixVID(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.FreezeAppendixVIDAsync(session.Token, id, ct);
        return await AppendixEntry("VI-D", demandId, ct);
    }

    /// <summary>
    /// Export dropdown (2026-09-08, UBIS-Pre-budget-html/Appendix6d.html design) - PDF/Excel/CSV,
    /// same TabularReportDto/_reportingClient shape as ExportAppendixVIA/VIB/VIC.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportAppendixVID(int demandId, string format, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixVIDViewModelAsync(session, demandId, demandName, ct);

        var dataRows = model.Records.Select((r, i) => new[]
        {
            (i + 1).ToString(),
            r.AutonomousBodyName ?? "",
            r.IsFrozen ? "Frozen" : "Active",
            r.AsOnMarch31?.ToString("0.00") ?? "0.00",
            r.AsOnJune30?.ToString("0.00") ?? "0.00",
            r.ExpectedNextMarch31?.ToString("0.00") ?? "0.00",
            r.ExpectedNextFY?.ToString("0.00") ?? "0.00",
            r.Remarks ?? ""
        }).ToList();

        var march31Year = model.YearOffset(-2).Split('-')[1];
        var sept30Year = model.PriorFinancialYear.Split('-')[0];
        var expectedMarch31Year = model.FinancialYear.Split('-')[0];

        var report = new TabularReportDto
        {
            Title = "Appendix VI-D - Available Internal Resources with Grantee Bodies/Autonomous Institutions",
            Subtitle = $"Demand: {demandName} | Financial Year: {session.FinancialYear}",
            ParaNo = model.ParaNo,
            Remarks = model.Remarks,
            Columns = new List<string>
            {
                "S.No.", "Name Of Institution", "Status",
                $"As on 31 Mar {march31Year}", $"As on 30 Sep {sept30Year}",
                $"Expected upto 31 Mar {expectedMarch31Year}", $"Expected {model.FinancialYear}",
                "Remarks"
            },
            Groups = new List<ReportGroupDto> { new() { Rows = dataRows } },
            FileName = $"AppendixVID-{session.UserName}-{DateTime.Now:MMddyyyyHHmmss}"
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
            return await AppendixEntry("VI-D", demandId, ct);
        }

        var (contentType, extension) = format?.ToLowerInvariant() switch
        {
            "excel" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            "csv" => ("text/csv", "csv"),
            _ => ("application/pdf", "pdf")
        };
        return File(exportResult.Data, contentType, $"{report.FileName}.{extension}");
    }

    private async Task<AppendixVIDViewModel> BuildAppendixVIDViewModelAsync(UbisSessionData session, int demandId, string demandName, CancellationToken ct)
    {
        var result = await _preBudgetClient.GetAppendixVIDAsync(session.Token, demandId, session.FinancialYear, ct);
        var autonomousBodies = await _preBudgetClient.GetAutonomousBodiesAsync(session.Token, demandId, session.FinancialYear, ct);
        var model = new AppendixVIDViewModel
        {
            DemandId = demandId,
            DemandName = demandName,
            FinancialYear = session.FinancialYear,
            Records = result.Data ?? new(),
            AutonomousBodies = autonomousBodies.Data ?? new()
        };
        await ApplyFreezeStatusAsync(model, session, demandId, "VI-D", ct);
        return model;
    }

    /// <summary>
    /// Appendix VI-D's Name of Institute switched from free text back to an AutonomousBodyId FK
    /// (client testing feedback, 2026-08-24 - "should be Autonomous dropdown as in Appendix VI-C/
    /// Appendix VI-E"), so the prior-year match here is by DemandId+AutonomousBodyId, same as VI-C.
    /// AsOnMarch31/AsOnJune30 are historical fact; ExpectedNextMarch31/ExpectedNextFY are
    /// forward-looking projections for the CURRENT cycle and stay manual.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAppendixVIDPreviousYearReference(int demandId, int autonomousBodyId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();

        var priorYear = GetPrecedingFinancialYears(session.FinancialYear, 2).FirstOrDefault();
        if (string.IsNullOrEmpty(priorYear))
        {
            return Json(new { found = false });
        }

        var result = await _preBudgetClient.GetAppendixVIDAsync(session.Token, demandId, priorYear, ct);
        var record = result.Data?.FirstOrDefault(r => r.AutonomousBodyId == autonomousBodyId);
        if (record == null)
        {
            return Json(new { found = false });
        }

        return Json(new
        {
            found = true,
            financialYear = priorYear,
            asOnMarch31 = record.AsOnMarch31,
            asOnJune30 = record.AsOnJune30
        });
    }
}
