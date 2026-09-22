// Save/Freeze/Delete actions + view-model builder for Appendix IV-B: TASP Expenditure.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    // --- Appendix IV-B ---
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAppendixIVB(int demandId, SaveAppendixTaspExpenditureDto newRecord, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        newRecord.DemandId = demandId; newRecord.FinancialYear = session.FinancialYear;
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var result = newRecord.Id.HasValue && newRecord.Id.Value > 0
            ? await _preBudgetClient.UpdateAppendixIVBAsync(session.Token, newRecord.Id.Value, newRecord, ct)
            : await _preBudgetClient.CreateAppendixIVBAsync(session.Token, newRecord, ct);
        var model = await BuildAppendixIVBViewModelAsync(session, demandId, demandName, ct);
        // "Input grid still not blank after Submit" (client testing feedback, 2026-08-24) - see
        // PreBudgetMeetingController.AppendixIV.cs's matching comment. Only on success.
        if (result.IsSuccess)
        {
            ModelState.Clear();
        }
        // Same carry-forward fix as Appendix IV (client testing feedback, 2026-08-24) - keep
        // Scheme/SubScheme selected across Submit instead of resetting the whole cascade.
        model.NewRecord.SchemeId = newRecord.SchemeId;
        model.NewRecord.SubSchemeId = newRecord.SubSchemeId;
        model.StatusIsError = !result.IsSuccess;
        model.StatusMessage = result.IsSuccess ? (newRecord.Id.HasValue && newRecord.Id.Value > 0 ? "Record modified successfully." : "Record saved successfully.") : (result.Error?.Message ?? "Could not save the record.");
        ViewData["Title"] = "Appendix IV-B - TASP Expenditure";
        return View("AppendixIVB", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixIVB(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.FreezeAppendixIVBAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see FreezeAppendixTemplate's matching comment
        // (PreBudgetMeetingController.cs).
        return await AppendixEntry("IV-B", demandId, ct);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppendixIVB(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.DeleteAppendixIVBAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see DeleteAppendixIA's matching comment (same
        // "dropdown erased" bug class: a redirect isn't recognized by OnActionExecuted's
        // fragment-detection).
        return await AppendixEntry("IV-B", demandId, ct);
    }

    /// <summary>
    /// Export dropdown - Category -> Scheme -> Sub-Scheme structured report, same shape/columns as
    /// Appendix IV-A's ExportAppendixIVA (identical entity/DTO field set) - added 2026-09-21
    /// alongside IV-A's own export (this appendix previously had none, relying on the generic
    /// scrape-the-visible-grid exporter, which can't produce this grouping). Budget Division/
    /// ABO-DS-Director/Section User additionally see the RE/NBE "recommended by Budget" columns and
    /// only Remarks (Budget Div); Single Demand Users see neither, only Remarks (Ministry).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportAppendixIVB(int demandId, string format, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixIVBViewModelAsync(session, demandId, demandName, ct);
        var priorYearStart = model.PriorFinancialYear.Split('-')[0];
        var currentYearStart = model.FinancialYear.Split('-')[0];

        var includeBudgetColumns = BudgetSideRemarksRoleNames.Contains(session.RoleName, StringComparer.OrdinalIgnoreCase);

        static string SchemeLabel(AppendixTaspExpenditureDto r) => $"{r.SchemeSrNo ?? 0}. {r.SchemeNameRaw ?? "-"}";
        static string SubSchemeLabel(AppendixTaspExpenditureDto r) => $"    {r.SchemeSrNo ?? 0}.{(r.SubSchemeSrNo ?? 0):00}- {r.SubSchemeNameRaw ?? "-"}";
        static string RowName(AppendixTaspExpenditureDto r) => r.SubSchemeId.HasValue ? SubSchemeLabel(r) : SchemeLabel(r);

        string[] RowCells(AppendixTaspExpenditureDto r)
        {
            var cells = new List<string>
            {
                RowName(r),
                r.Actuals?.ToString("0.00")!,
                r.ActualsUptoSeptPrevYear?.ToString("0.00")!,
                r.BE?.ToString("0.00")!,
                r.ActualsUptoSept?.ToString("0.00")!,
                r.ProposedRE?.ToString("0.00")!,
                r.AddlReSought?.ToString("0.00")!
            };
            if (includeBudgetColumns) cells.Add(r.BudgetRecommendedRE?.ToString("0.00")!);
            cells.Add(r.ProposedNBE?.ToString("0.00")!);
            if (includeBudgetColumns) cells.Add(r.BudgetRecommendedNBE?.ToString("0.00")!);
            cells.Add(includeBudgetColumns ? (r.RemarksBudget ?? "") : (r.RemarksMinistry ?? ""));
            return cells.ToArray();
        }

        string[] TotalCells(string label, List<AppendixTaspExpenditureDto> rows)
        {
            var totalActuals = rows.Sum(r => r.Actuals ?? 0m);
            var totalActualsUptoSeptPrevYear = rows.Sum(r => r.ActualsUptoSeptPrevYear ?? 0m);
            var totalBE = rows.Sum(r => r.BE ?? 0m);
            var totalActualsUptoSept = rows.Sum(r => r.ActualsUptoSept ?? 0m);
            var totalProposedRE = rows.Sum(r => r.ProposedRE ?? 0m);
            var totalAddlReSought = rows.Sum(r => r.AddlReSought ?? 0m);
            var totalBudgetRecommendedRE = rows.Sum(r => r.BudgetRecommendedRE ?? 0m);
            var totalProposedNBE = rows.Sum(r => r.ProposedNBE ?? 0m);
            var totalBudgetRecommendedNBE = rows.Sum(r => r.BudgetRecommendedNBE ?? 0m);
            var cells = new List<string>
            {
                label,
                totalActuals.ToString("0.00"), totalActualsUptoSeptPrevYear.ToString("0.00"),
                totalBE.ToString("0.00"), totalActualsUptoSept.ToString("0.00"),
                totalProposedRE.ToString("0.00"), totalAddlReSought.ToString("0.00")
            };
            if (includeBudgetColumns) cells.Add(totalBudgetRecommendedRE.ToString("0.00"));
            cells.Add(totalProposedNBE.ToString("0.00"));
            if (includeBudgetColumns) cells.Add(totalBudgetRecommendedNBE.ToString("0.00"));
            cells.Add("");
            return cells.ToArray();
        }

        // Appendix IV-B has no Category concept at all (unlike IV/IV-A, no CategoryType/
        // CategorySerialNo field on this DTO) - single flat Scheme->Sub-Scheme grouping, no Category
        // heading row.
        var groups = new List<ReportGroupDto>();
        var schemeGroups = model.Records
            .GroupBy(r => r.SchemeId)
            .OrderBy(g => g.First().SchemeSrNo ?? 0)
            .ToList();

        foreach (var schemeGroup in schemeGroups)
        {
            var schemeRows = schemeGroup.ToList();
            var hasSubSchemeBreakdown = schemeRows.Any(r => r.SubSchemeId.HasValue);
            if (hasSubSchemeBreakdown)
            {
                groups.Add(new ReportGroupDto
                {
                    Label = SchemeLabel(schemeRows[0]),
                    Rows = schemeRows.Select(RowCells).ToList(),
                    Totals = new List<ReportTotalRowDto> { new() { Cells = TotalCells("Total", schemeRows) } }
                });
            }
            else
            {
                groups.Add(new ReportGroupDto { Rows = new List<string[]> { RowCells(schemeRows[0]) } });
            }
        }

        if (model.Records.Count > 0)
        {
            groups.Add(new ReportGroupDto { Totals = new List<ReportTotalRowDto> { new() { Cells = TotalCells("Grand Total", model.Records) } } });
        }

        var ivbColumns = new List<string>
        {
            "Name of Scheme/Item",
            $"Actual\n{model.PriorFinancialYear}", $"Actual upto\n9/{priorYearStart}",
            $"B.E.\n{model.FinancialYear}", $"Actual upto\n9/{currentYearStart}",
            $"R.E.{model.FinancialYear}\n proposed by Ministry",
            $"Saving/Excess \nin R.E.{model.FinancialYear}\nover \nB.E.{model.FinancialYear}"
        };
        if (includeBudgetColumns) ivbColumns.Add($"R.E.{model.FinancialYear} recommended by Budget");
        ivbColumns.Add($"B.E.{model.YearOffset(1)} proposed by Min/Dep");
        if (includeBudgetColumns) ivbColumns.Add($"B.E.{model.YearOffset(1)} recommended by Budget");
        ivbColumns.Add(includeBudgetColumns ? "Remarks\nby Budget Division" : "Remarks\nby Ministry");

        var report = new TabularReportDto
        {
            Title = demandName,
            TitleBoxLines = new List<string> { "Appendix IV B \nEstimates of Expenditure Under Tribal Area Sub Plan (Minor Head 796)" },
            ParaNo = model.ParaNo,
            UnitNote = "(In crore of Rupees)",
            Remarks = model.Remarks,
            Columns = ivbColumns,
            RightAlignedColumns = Enumerable.Range(1, ivbColumns.Count - 2).ToList(),
            Groups = groups,
            FileName = $"AppendixIVB-{(includeBudgetColumns ? "BudgetDivision" : "SingleDemandUser")}-{session.UserName}-{DateTime.Now:MMddyyyyHHmmss}"
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
            return await AppendixEntry("IV-B", demandId, ct);
        }

        var (contentType, extension) = format?.ToLowerInvariant() switch
        {
            "excel" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            "csv" => ("text/csv", "csv"),
            _ => ("application/pdf", "pdf")
        };
        return File(exportResult.Data, contentType, $"{report.FileName}.{extension}");
    }

    private async Task<AppendixIVBViewModel> BuildAppendixIVBViewModelAsync(UbisSessionData session, int demandId, string demandName, CancellationToken ct)
    {
        var result = await _preBudgetClient.GetAppendixIVBAsync(session.Token, demandId, session.FinancialYear, ct);
        var schemes = await _preBudgetClient.GetAppendixIVSchemesAsync(session.Token, demandId, ct);
        var model = new AppendixIVBViewModel
        {
            DemandId = demandId,
            DemandName = demandName,
            FinancialYear = session.FinancialYear,
            Records = result.Data ?? new(),
            Schemes = schemes.Data ?? new()
        };
        ApplyRemarksColumnVisibility(model, session.RoleName);
        await ApplyFreezeStatusAsync(model, session, demandId, "IV-B", ct);
        return model;
    }

    /// <summary>Same as <see cref="GetAppendixIVAPreviousYearReference"/>, against Appendix IV-B's own prior-year row.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAppendixIVBPreviousYearReference(int demandId, int schemeId, int? subSchemeId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();

        var priorYear = GetPrecedingFinancialYears(session.FinancialYear, 2).FirstOrDefault();
        if (string.IsNullOrEmpty(priorYear))
        {
            return Json(new { found = false });
        }

        var result = await _preBudgetClient.GetAppendixIVBAsync(session.Token, demandId, priorYear, ct);
        var record = result.Data?.FirstOrDefault(r => r.SchemeId == schemeId && r.SubSchemeId == subSchemeId);
        if (record == null)
        {
            return Json(new { found = false });
        }

        return Json(new
        {
            found = true,
            financialYear = priorYear,
            actuals = record.Actuals,
            actualsUptoSeptPrevYear = record.ActualsUptoSeptPrevYear,
            actualsUptoSept = record.ActualsUptoSept
        });
    }
}
