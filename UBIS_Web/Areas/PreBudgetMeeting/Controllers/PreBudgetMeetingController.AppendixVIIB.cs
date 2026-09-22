// Save/Freeze/Delete actions + view-model builder for Appendix VII-B: Commercial Undertaking
// Receipts.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    // --- Appendix VII-B ---
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAppendixVIIB(int demandId, SaveAppendixCommercialUndertakingReceiptsDto newRecord, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        newRecord.DemandId = demandId; newRecord.FinancialYear = session.FinancialYear;
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var result = newRecord.Id.HasValue && newRecord.Id.Value > 0
            ? await _preBudgetClient.UpdateAppendixVIIBAsync(session.Token, newRecord.Id.Value, newRecord, ct)
            : await _preBudgetClient.CreateAppendixVIIBAsync(session.Token, newRecord, ct);
        var model = await BuildAppendixVIIBViewModelAsync(session, demandId, demandName, ct);
        // "Input grid still not blank after Submit" (client testing feedback, 2026-08-24) - asp-for
        // renders from ModelState before the model; see AppendixIV.cs's matching comment. Only on
        // success - keep the user's typed values on the page if the save itself failed.
        if (result.IsSuccess)
        {
            ModelState.Clear();
        }
        // Client report 2026-08-31: "if correctly show dialog box but if freeze major head
        // dropdown. If user want to select other Major Head to do data entry, he couldn't" - a
        // duplicate-entry rejection re-renders this fragment, and the Major Head <select> has no
        // @foreach in its markup (it's AJAX-populated, cascading from Scheme - see
        // GetAppendixVIIBMajorHeads), so it always comes back to its static disabled/blank state
        // with nothing to re-trigger the cascade. Carry the just-submitted Scheme/Major Head/
        // Transaction Type forward the same way AppendixIV.cs carries Scheme/SubScheme forward;
        // AppendixVIIB.cshtml's fragment-load JS replays the Major Head cascade against it (and
        // leaves the dropdown enabled either way, so the user can pick a different Major Head).
        model.NewRecord.SchemeId = newRecord.SchemeId;
        model.NewRecord.MajorHeadId = newRecord.MajorHeadId;
        model.NewRecord.TransactionType = newRecord.TransactionType;
        model.StatusIsError = !result.IsSuccess;
        model.StatusMessage = result.IsSuccess ? (newRecord.Id.HasValue && newRecord.Id.Value > 0 ? "Record modified successfully." : "Record saved successfully.") : (result.Error?.Message ?? "Could not save the record.");
        ViewData["Title"] = "Appendix VII-B - Commercial Undertaking Receipts";
        return View("AppendixVIIB", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixVIIB(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.FreezeAppendixVIIBAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see FreezeAppendixTemplate's matching comment
        // (PreBudgetMeetingController.cs): OnActionExecuted only fragment-izes a ViewResult, so a
        // RedirectResult here gets silently followed by fetch() all the way to the full page,
        // which then looks like the Demand/Appendix dropdowns got wiped.
        return await AppendixEntry("VII-B", demandId, ct);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppendixVIIB(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.DeleteAppendixVIIBAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see DeleteAppendixIA's matching comment.
        return await AppendixEntry("VII-B", demandId, ct);
    }

    /// <summary>
    /// Export dropdown (2026-09-10, drawer/grid redesign) - PDF/Excel/CSV, same TabularReportDto/
    /// _reportingClient shape as ExportAppendixVI.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportAppendixVIIB(int demandId, string format, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixVIIBViewModelAsync(session, demandId, demandName, ct);
        var yMinus3 = model.YearOffset(-3);

        var dataRows = model.Records.Select((r, i) => new[]
        {
            (i + 1).ToString(),
            r.IsFrozen ? "Frozen" : "Active",
            r.SchemeName ?? "",
            r.MajorHeadCode ?? "",
            r.TransactionType ?? "",
            r.ActualsY2?.ToString("0.00") ?? "0.00",
            r.ActualsY1?.ToString("0.00") ?? "0.00",
            r.BE?.ToString("0.00") ?? "0.00",
            r.ActualsUptoSeptPrevYear?.ToString("0.00") ?? "0.00",
            r.ActualsUptoSept?.ToString("0.00") ?? "0.00",
            r.RE?.ToString("0.00") ?? "0.00",
            r.IncreasedBE?.ToString("0.00") ?? "0.00",
            r.NBE?.ToString("0.00") ?? "0.00"
        }).ToList();

        var report = new TabularReportDto
        {
            Title = "Appendix VII-B - Commercial Undertaking Receipts",
            Subtitle = $"Demand: {demandName} | Financial Year: {session.FinancialYear}",
            ParaNo = model.ParaNo,
            Remarks = model.Remarks,
            Columns = new List<string>
            {
                "S.No.", "Status", "Departmental Commercial Undertaking", "Major Head", "Type of Transaction",
                $"Actuals {yMinus3}", $"Actuals {model.PriorPriorFinancialYear}", $"BE {model.PriorFinancialYear}",
                "Actuals Upto Sept (Prev Yr)", "Actuals Upto Sept", $"RE {model.PriorFinancialYear}",
                "Increase(+)/Decrease(-) over BE", $"BE {model.FinancialYear}"
            },
            Groups = new List<ReportGroupDto> { new() { Rows = dataRows } },
            FileName = $"AppendixVIIB-{session.UserName}-{DateTime.Now:MMddyyyyHHmmss}"
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
            return await AppendixEntry("VII-B", demandId, ct);
        }

        var (contentType, extension) = format?.ToLowerInvariant() switch
        {
            "excel" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            "csv" => ("text/csv", "csv"),
            _ => ("application/pdf", "pdf")
        };
        return File(exportResult.Data, contentType, $"{report.FileName}.{extension}");
    }

    private async Task<AppendixVIIBViewModel> BuildAppendixVIIBViewModelAsync(UbisSessionData session, int demandId, string demandName, CancellationToken ct)
    {
        var result = await _preBudgetClient.GetAppendixVIIBAsync(session.Token, demandId, session.FinancialYear, ct);
        var schemes = await _preBudgetClient.GetAppendixVIIBSchemesAsync(session.Token, ct);
        var transactionTypes = await _preBudgetClient.GetAppendixVIIBTransactionTypesAsync(session.Token, ct);
        var model = new AppendixVIIBViewModel
        {
            DemandId = demandId,
            DemandName = demandName,
            FinancialYear = session.FinancialYear,
            Records = result.Data ?? new(),
            Schemes = schemes.Data ?? new(),
            TransactionTypes = transactionTypes.Data ?? new()
        };
        await ApplyAppendixPermissionsAsync(model, session, "VII-B", ct);
        await ApplyFreezeStatusAsync(model, session, demandId, "VII-B", ct);
        return model;
    }

    // AJAX-only: Major Head dropdown, cascading from the selected Scheme (client reference SQL, 2026-08-07).
    [HttpGet]
    public async Task<IActionResult> GetAppendixVIIBMajorHeads(int demandId, int schemeId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();
        var result = await _preBudgetClient.GetAppendixVIIBMajorHeadsAsync(session.Token, demandId, schemeId, ct);
        return Json(result.Data ?? new());
    }

    /// <summary>
    /// AJAX-only: BE/Actuals auto-load once Scheme + Major Head + Transaction Type are all chosen,
    /// per the client's reference SQL (2026-08-07). Confirmed against a live screenshot of this
    /// exact page (2026-08-07): BE populates NewRecord.BE ("BE" column, current year); Actuals
    /// populates NewRecord.ActualsY2 (the OLDEST/leftmost Actuals column, e.g. "Actuals 2023-2024"
    /// when NBE targets 2026-2027) - not ActualsY1 as first implemented, corrected after matching
    /// the screenshot's own numbers against this endpoint's query results.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAppendixVIIBBeActuals(int demandId, int schemeId, int majorHeadId, string transactionType, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();
        var result = await _preBudgetClient.GetAppendixVIIBBeActualsAsync(session.Token, demandId, schemeId, majorHeadId, transactionType, ct);
        if (!result.IsSuccess || result.Data == null)
        {
            return Json(new { be = 0m, actualsY2 = 0m });
        }
        return Json(new { be = result.Data.Be, actualsY2 = result.Data.Actuals });
    }

    /// <summary>Keyed by DemandId+SchemeId. ActualsY2/ActualsY1/ActualsUptoSeptPrevYear/ActualsUptoSept are all explicitly historical per the view's own "(Y-2)"/"(Y-1)"/"(Prev Yr)" labels; BE/RE/IncreasedBE/NBE are this cycle's live proposals.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAppendixVIIBPreviousYearReference(int demandId, int schemeId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();

        var priorYear = GetPrecedingFinancialYears(session.FinancialYear, 2).FirstOrDefault();
        if (string.IsNullOrEmpty(priorYear))
        {
            return Json(new { found = false });
        }

        var result = await _preBudgetClient.GetAppendixVIIBAsync(session.Token, demandId, priorYear, ct);
        var record = result.Data?.FirstOrDefault(r => r.SchemeId == schemeId);
        if (record == null)
        {
            return Json(new { found = false });
        }

        return Json(new
        {
            found = true,
            financialYear = priorYear,
            actualsY2 = record.ActualsY2,
            actualsY1 = record.ActualsY1,
            actualsUptoSeptPrevYear = record.ActualsUptoSeptPrevYear,
            actualsUptoSept = record.ActualsUptoSept
        });
    }
}
