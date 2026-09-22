// Save/Freeze/Delete actions + view-model builder for Appendix VI-C: Details of Corpus Funds.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    // --- Appendix VI-C ---
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAppendixVIC(int demandId, SaveAppendixCorpusFundDto newRecord, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        newRecord.DemandId = demandId; newRecord.FinancialYear = session.FinancialYear;
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var result = newRecord.Id.HasValue && newRecord.Id.Value > 0
            ? await _preBudgetClient.UpdateAppendixVICAsync(session.Token, newRecord.Id.Value, newRecord, ct)
            : await _preBudgetClient.CreateAppendixVICAsync(session.Token, newRecord, ct);
        var model = await BuildAppendixVICViewModelAsync(session, demandId, demandName, ct);
        // "Input grid still not blank after Submit" (client testing feedback, 2026-08-24) - asp-for
        // renders from ModelState before the model; see AppendixIV.cs's matching comment. Only on
        // success - keep the user's typed values on the page if the save itself failed.
        if (result.IsSuccess)
        {
            ModelState.Clear();
        }
        model.StatusIsError = !result.IsSuccess;
        model.StatusMessage = result.IsSuccess ? (newRecord.Id.HasValue && newRecord.Id.Value > 0 ? "Record modified successfully." : "Record saved successfully.") : (result.Error?.Message ?? "Could not save the record.");
        ViewData["Title"] = "Appendix VI-C : Details of Corpus Funds";
        return View("AppendixVIC", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixVICBulk(int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixVICViewModelAsync(session, demandId, demandName, ct);
        foreach (var record in model.Records.Where(r => !r.IsFrozen))
        {
            await _preBudgetClient.FreezeAppendixVICAsync(session.Token, record.Id, ct);
        }
        model = await BuildAppendixVICViewModelAsync(session, demandId, demandName, ct);
        model.StatusMessage = "Details of Corpus Funds data frozen.";
        ViewData["Title"] = "Appendix VI-C : Details of Corpus Funds";
        return View("AppendixVIC", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppendixVIC(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.DeleteAppendixVICAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see DeleteAppendixIA's matching comment.
        return await AppendixEntry("VI-C", demandId, ct);
    }

    /// <summary>Row-level Freeze (2026-08-21, history-grid Action-icon rebuild).</summary>
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixVIC(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.FreezeAppendixVICAsync(session.Token, id, ct);
        return await AppendixEntry("VI-C", demandId, ct);
    }

    /// <summary>
    /// Export dropdown (2026-09-08, UBIS-Pre-budget-html/Appendix6c.html design) - PDF/Excel/CSV,
    /// same TabularReportDto/_reportingClient shape as ExportAppendixVIA/VIB. Actual Expenditure's
    /// 3 year columns use ColumnGroups for the same merged "Actual Expenditure" header the grid
    /// itself renders via a colspan.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportAppendixVIC(int demandId, string format, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixVICViewModelAsync(session, demandId, demandName, ct);

        var dataRows = model.Records.Select((r, i) => new[]
        {
            (i + 1).ToString(),
            r.AutonomousBodyName ?? "",
            r.IsPublicAccount ? "Yes" : "No",
            r.IsFrozen ? "Frozen" : "Active",
            r.AccumulatedBalancePrevYear?.ToString("0.00") ?? "0.00",
            r.AccumulatedBalance?.ToString("0.00") ?? "0.00",
            r.ActualExpenditureY1?.ToString("0.00") ?? "0.00",
            r.ActualExpenditureY2?.ToString("0.00") ?? "0.00",
            r.ActualExpenditureY3?.ToString("0.00") ?? "0.00",
            r.AllocationInBE?.ToString("0.00") ?? "0.00",
            r.ExpenditureTillSept?.ToString("0.00") ?? "0.00"
        }).ToList();

        var yMinus1 = model.PriorFinancialYear;
        var yMinus2 = model.PriorPriorFinancialYear;
        var yMinus3 = model.YearOffset(-3);
        var yMinus4 = model.YearOffset(-4);
        var balAsOnYMinus2 = yMinus2.Split('-')[0].Substring(2);
        var balAsOnYMinus1 = yMinus1.Split('-')[0].Substring(2);
        var expTillSeptYear = yMinus1.Split('-')[0].Substring(2);

        var report = new TabularReportDto
        {
            Title = "Appendix VI-C - Details of Corpus Funds",
            Subtitle = $"Demand: {demandName} | Financial Year: {session.FinancialYear}",
            ParaNo = model.ParaNo,
            Remarks = model.Remarks,
            ColumnGroups = new List<ReportColumnGroupDto>
            {
                new() { Label = null, ColumnSpan = 1 }, // S.No.
                new() { Label = null, ColumnSpan = 1 }, // Autonomous Name
                new() { Label = null, ColumnSpan = 1 }, // Public Account
                new() { Label = null, ColumnSpan = 1 }, // Status
                new() { Label = null, ColumnSpan = 1 }, // Balance prev year
                new() { Label = null, ColumnSpan = 1 }, // Balance
                new() { Label = "Actual Expenditure", ColumnSpan = 3 },
                new() { Label = null, ColumnSpan = 1 }, // Allocation in BE
                new() { Label = null, ColumnSpan = 1 }  // Expenditure till Sept
            },
            Columns = new List<string>
            {
                "S.No.", "Autonomous Name", "Public Account", "Status",
                $"Accumulated Balance 31.03.{balAsOnYMinus2}", $"Accumulated Balance 31.03.{balAsOnYMinus1}",
                yMinus4, yMinus3, yMinus2,
                $"Allocation in BE {yMinus1}", $"Expenditure till 09/{expTillSeptYear}"
            },
            Groups = new List<ReportGroupDto> { new() { Rows = dataRows } },
            FileName = $"AppendixVIC-{session.UserName}-{DateTime.Now:MMddyyyyHHmmss}"
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
            return await AppendixEntry("VI-C", demandId, ct);
        }

        var (contentType, extension) = format?.ToLowerInvariant() switch
        {
            "excel" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            "csv" => ("text/csv", "csv"),
            _ => ("application/pdf", "pdf")
        };
        return File(exportResult.Data, contentType, $"{report.FileName}.{extension}");
    }

    private async Task<AppendixVICViewModel> BuildAppendixVICViewModelAsync(UbisSessionData session, int demandId, string demandName, CancellationToken ct)
    {
        var result = await _preBudgetClient.GetAppendixVICAsync(session.Token, demandId, session.FinancialYear, ct);
        var autonomousBodies = await _preBudgetClient.GetAutonomousBodiesAsync(session.Token, demandId, session.FinancialYear, ct);
        var model = new AppendixVICViewModel
        {
            DemandId = demandId,
            DemandName = demandName,
            FinancialYear = session.FinancialYear,
            Records = result.Data ?? new(),
            AutonomousBodies = autonomousBodies.Data ?? new()
        };
        await ApplyFreezeStatusAsync(model, session, demandId, "VI-C", ct);
        return model;
    }

    /// <summary>Keyed by DemandId+AutonomousBodyId like Appendix V-A/V-C. AccumulatedBalancePrevYear/AccumulatedBalance/ActualExpenditureY1/Y2/Y3 are all historical; AllocationInBE/ExpenditureTillSept are this cycle's own figures.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAppendixVICPreviousYearReference(int demandId, int autonomousBodyId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();

        var priorYear = GetPrecedingFinancialYears(session.FinancialYear, 2).FirstOrDefault();
        if (string.IsNullOrEmpty(priorYear))
        {
            return Json(new { found = false });
        }

        var result = await _preBudgetClient.GetAppendixVICAsync(session.Token, demandId, priorYear, ct);
        var record = result.Data?.FirstOrDefault(r => r.AutonomousBodyId == autonomousBodyId);
        if (record == null)
        {
            return Json(new { found = false });
        }

        return Json(new
        {
            found = true,
            financialYear = priorYear,
            accumulatedBalancePrevYear = record.AccumulatedBalancePrevYear,
            accumulatedBalance = record.AccumulatedBalance,
            actualExpenditureY1 = record.ActualExpenditureY1,
            actualExpenditureY2 = record.ActualExpenditureY2,
            actualExpenditureY3 = record.ActualExpenditureY3
        });
    }
}
