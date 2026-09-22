// Save/Freeze/Delete actions + view-model builder for Appendix VI: Non Tax Revenue.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    // --- Appendix VI ---
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAppendixVI(int demandId, SaveAppendixNonTaxRevenueDto newRecord, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        newRecord.DemandId = demandId; newRecord.FinancialYear = session.FinancialYear;
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var result = newRecord.Id.HasValue && newRecord.Id.Value > 0
            ? await _preBudgetClient.UpdateAppendixVIAsync(session.Token, newRecord.Id.Value, newRecord, ct)
            : await _preBudgetClient.CreateAppendixVIAsync(session.Token, newRecord, ct);
        var model = await BuildAppendixVIViewModelAsync(session, demandId, demandName, ct);
        // "Input grid still not blank after Submit" (client testing feedback, 2026-08-24) - asp-for
        // renders from ModelState before the model; see AppendixIV.cs's matching comment. Only on
        // success - keep the user's typed values on the page if the save itself failed.
        if (result.IsSuccess)
        {
            ModelState.Clear();
        }
        model.StatusIsError = !result.IsSuccess;
        model.StatusMessage = result.IsSuccess ? (newRecord.Id.HasValue && newRecord.Id.Value > 0 ? "Record modified successfully." : "Record saved successfully.") : (result.Error?.Message ?? "Could not save the record.");
        ViewData["Title"] = "Appendix VI : Non Tax Revenue";
        return View("AppendixVI", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixVIBulk(int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixVIViewModelAsync(session, demandId, demandName, ct);
        foreach (var record in model.Records.Where(r => !r.IsFrozen))
        {
            await _preBudgetClient.FreezeAppendixVIAsync(session.Token, record.Id, ct);
        }
        model = await BuildAppendixVIViewModelAsync(session, demandId, demandName, ct);
        model.StatusMessage = "Non Tax Revenue data frozen.";
        ViewData["Title"] = "Appendix VI : Non Tax Revenue";
        return View("AppendixVI", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppendixVI(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.DeleteAppendixVIAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see DeleteAppendixIA's matching comment.
        return await AppendixEntry("VI", demandId, ct);
    }

    /// <summary>Row-level Freeze (2026-08-21, history-grid Action-icon rebuild) - distinct from the
    /// existing FreezeAppendixVIBulk above; the client method (_preBudgetClient.FreezeAppendixVIAsync)
    /// already existed, only this per-row controller action was missing.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixVI(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.FreezeAppendixVIAsync(session.Token, id, ct);
        return await AppendixEntry("VI", demandId, ct);
    }

    /// <summary>
    /// Export dropdown (2026-09-10, drawer/grid redesign) - PDF/Excel/CSV, same TabularReportDto/
    /// _reportingClient shape as ExportAppendixIII.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportAppendixVI(int demandId, string format, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixVIViewModelAsync(session, demandId, demandName, ct);

        var dataRows = model.Records.Select((r, i) => new[]
        {
            (i + 1).ToString(),
            r.IsFrozen ? "Frozen" : "Active",
            r.ReceiptType ?? "",
            r.PsuReceiptName ?? "",
            r.Actuals?.ToString("0.00") ?? "0.00",
            r.BE?.ToString("0.00") ?? "0.00",
            r.ActualsUptoSept?.ToString("0.00") ?? "0.00",
            r.ProposedBE?.ToString("0.00") ?? "0.00",
            r.ProposedCollectionQ3?.ToString("0.00") ?? "0.00",
            r.ProposedCollectionQ4?.ToString("0.00") ?? "0.00",
            r.Remarks ?? ""
        }).ToList();

        var report = new TabularReportDto
        {
            Title = "Appendix VI - Non Tax Revenue",
            Subtitle = $"Demand: {demandName} | Financial Year: {session.FinancialYear}",
            ParaNo = model.ParaNo,
            Remarks = model.Remarks,
            Columns = new List<string>
            {
                "S.No.", "Status", "Receipt Type", "PSU/Receipt Name", $"Actual {model.YearOffset(-2)}",
                $"B.E. {model.PriorFinancialYear}", "Actuals Upto Sept", $"Proposed B.E. {model.FinancialYear}",
                "Proposed Collection Q-3", "Proposed Collection Q-4", "Remarks"
            },
            Groups = new List<ReportGroupDto> { new() { Rows = dataRows } },
            FileName = $"AppendixVI-{session.UserName}-{DateTime.Now:MMddyyyyHHmmss}"
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
            return await AppendixEntry("VI", demandId, ct);
        }

        var (contentType, extension) = format?.ToLowerInvariant() switch
        {
            "excel" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            "csv" => ("text/csv", "csv"),
            _ => ("application/pdf", "pdf")
        };
        return File(exportResult.Data, contentType, $"{report.FileName}.{extension}");
    }

    private async Task<AppendixVIViewModel> BuildAppendixVIViewModelAsync(UbisSessionData session, int demandId, string demandName, CancellationToken ct)
    {
        var result = await _preBudgetClient.GetAppendixVIAsync(session.Token, demandId, session.FinancialYear, ct);
        var receiptTypes = await _preBudgetClient.GetAppendixVIReceiptTypesAsync(session.Token, ct);
        var model = new AppendixVIViewModel
        {
            DemandId = demandId,
            DemandName = demandName,
            FinancialYear = session.FinancialYear,
            Records = result.Data ?? new(),
            ReceiptTypes = receiptTypes.Data ?? new()
        };
        await ApplyFreezeStatusAsync(model, session, demandId, "VI", ct);
        return model;
    }

    /// <summary>
    /// AJAX-only (2026-08-04, re-keyed to ReceiptTypeId 2026-09-10): Appendix VI is multiple rows per
    /// demand+year (one per Receipt Type), so the prior-year lookup is keyed by DemandId+ReceiptTypeId.
    /// Actuals/BE/ActualsUptoSept are historical fact; the Proposed-group fields are this cycle's live
    /// proposal and stay manual.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAppendixVIPreviousYearReference(int demandId, int receiptTypeId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();

        var priorYear = GetPrecedingFinancialYears(session.FinancialYear, 2).FirstOrDefault();
        if (string.IsNullOrEmpty(priorYear))
        {
            return Json(new { found = false });
        }

        var result = await _preBudgetClient.GetAppendixVIAsync(session.Token, demandId, priorYear, ct);
        var record = result.Data?.FirstOrDefault(r => r.ReceiptTypeId == receiptTypeId);
        if (record == null)
        {
            return Json(new { found = false });
        }

        return Json(new
        {
            found = true,
            financialYear = priorYear,
            actuals = record.Actuals,
            be = record.BE,
            actualsUptoSept = record.ActualsUptoSept
        });
    }
}
