// Save/Freeze/Delete actions + view-model builder for Appendix IV-A: SCSP Expenditure.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    // --- Appendix IV-A ---
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAppendixIVA(int demandId, SaveAppendixScspExpenditureDto newRecord, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        newRecord.DemandId = demandId; newRecord.FinancialYear = session.FinancialYear;
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var result = newRecord.Id.HasValue && newRecord.Id.Value > 0
            ? await _preBudgetClient.UpdateAppendixIVAAsync(session.Token, newRecord.Id.Value, newRecord, ct)
            : await _preBudgetClient.CreateAppendixIVAAsync(session.Token, newRecord, ct);
        var model = await BuildAppendixIVAViewModelAsync(session, demandId, demandName, ct);
        // "Input grid still not blank after Submit" (client testing feedback, 2026-08-24) - asp-for
        // renders from ModelState before the model, so the just-POSTed values keep showing until
        // ModelState is cleared. Only on success - see PreBudgetMeetingController.AppendixIV.cs.
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
        ViewData["Title"] = "Appendix IV-A - SCSP Expenditure";
        return View("AppendixIVA", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixIVA(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.FreezeAppendixIVAAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see FreezeAppendixTemplate's matching comment
        // (PreBudgetMeetingController.cs).
        return await AppendixEntry("IV-A", demandId, ct);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppendixIVA(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.DeleteAppendixIVAAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see DeleteAppendixIA's matching comment.
        return await AppendixEntry("IV-A", demandId, ct);
    }

    /// <summary>
    /// Export dropdown (client-supplied reference files, 2026-09-21): Category -> Scheme ->
    /// Sub-Scheme structured report, same shape/labeling convention as Appendix IV's own
    /// BuildStructuredAppendixIVReport but with IV-A's own, narrower column set - no
    /// %w.r.t. B.E. column, a single "Saving/Excess in R.E. over B.E." column (ProposedRE - BE,
    /// already computed server-side as AddlReSought) instead of a separate Additional-RE-sought
    /// column, no Additional-BE-sought column at all, and - per explicit client instruction
    /// 2026-09-21 ("use this column only the format provided for Single Demand User" for the
    /// Budget-section report too) - no "recommended by Budget" columns for either role. Both
    /// "Single Demand Users" and "Budget Division" get the exact same export here; only the
    /// on-screen grid's Remarks column differs by role (see AppendixIVA.cshtml).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportAppendixIVA(int demandId, string format, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixIVAViewModelAsync(session, demandId, demandName, ct);
        var priorYearStart = model.PriorFinancialYear.Split('-')[0];
        var currentYearStart = model.FinancialYear.Split('-')[0];

        // Client instruction 2026-09-21: Budget Division/ABO-DS-Director/Section User's export
        // additionally shows the RE/NBE "recommended by Budget" columns and only Remarks (Budget
        // Div) (never Remarks (Ministry) too) - same BudgetSideRemarksRoleNames set and mutual-
        // exclusivity convention as Appendix IV's BuildStructuredAppendixIVReport.
        var includeBudgetColumns = BudgetSideRemarksRoleNames.Contains(session.RoleName, StringComparer.OrdinalIgnoreCase);

        static string SchemeLabel(AppendixScspExpenditureDto r) => $"{r.SchemeSrNo ?? 0}. {r.SchemeNameRaw ?? "-"}";
        static string SubSchemeLabel(AppendixScspExpenditureDto r) => $"    {r.SchemeSrNo ?? 0}.{(r.SubSchemeSrNo ?? 0):00}- {r.SubSchemeNameRaw ?? "-"}";
        static string RowName(AppendixScspExpenditureDto r) => r.SubSchemeId.HasValue ? SubSchemeLabel(r) : SchemeLabel(r);

        string[] RowCells(AppendixScspExpenditureDto r)
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

        string[] TotalCells(string label, List<AppendixScspExpenditureDto> rows)
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

        var categoryGroups = model.Records
            .GroupBy(r => (r.CategoryType, r.CategorySerialNo))
            .OrderBy(g => g.Key.CategorySerialNo ?? "~")
            .ToList();

        var groups = new List<ReportGroupDto>();
        foreach (var categoryGroup in categoryGroups)
        {
            groups.Add(new ReportGroupDto { Label = categoryGroup.Key.CategoryType ?? "Uncategorized" });

            var schemeGroups = categoryGroup
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

            groups.Add(new ReportGroupDto { Totals = new List<ReportTotalRowDto> { new() { Cells = TotalCells("Total", categoryGroup.ToList()) } } });
        }

        if (categoryGroups.Count > 1)
        {
            groups.Add(new ReportGroupDto { Totals = new List<ReportTotalRowDto> { new() { Cells = TotalCells("Grand Total", model.Records) } } });
        }

        var ivaColumns = new List<string>
        {
            "Name of Scheme/Item",
            $"Actual\n{model.PriorFinancialYear}", $"Actual upto\n9/{priorYearStart}",
            $"B.E.\n{model.FinancialYear}", $"Actual upto\n9/{currentYearStart}",
            $"R.E.{model.FinancialYear}\n proposed by Ministry",
            $"Saving/Excess \nin R.E.{model.FinancialYear}\nover \nB.E.{model.FinancialYear}"
        };
        if (includeBudgetColumns) ivaColumns.Add($"R.E.{model.FinancialYear} recommended by Budget");
        ivaColumns.Add($"B.E.{model.YearOffset(1)} proposed by Min/Dep");
        if (includeBudgetColumns) ivaColumns.Add($"B.E.{model.YearOffset(1)} recommended by Budget");
        ivaColumns.Add(includeBudgetColumns ? "Remarks\nby Budget Division" : "Remarks\nby Ministry");

        var report = new TabularReportDto
        {
            Title = demandName,
            TitleBoxLines = new List<string> { $"Appendix IV A \nEstimates of Expenditure Under Special Component Plan for Scheduled Castes (Minor Head 789)" },
            ParaNo = model.ParaNo,
            UnitNote = "(In crore of Rupees)",
            Remarks = model.Remarks,
            Columns = ivaColumns,
            RightAlignedColumns = Enumerable.Range(1, ivaColumns.Count - 2).ToList(),
            Groups = groups,
            FileName = $"AppendixIVA-{(includeBudgetColumns ? "BudgetDivision" : "SingleDemandUser")}-{session.UserName}-{DateTime.Now:MMddyyyyHHmmss}"
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
            return await AppendixEntry("IV-A", demandId, ct);
        }

        var (contentType, extension) = format?.ToLowerInvariant() switch
        {
            "excel" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            "csv" => ("text/csv", "csv"),
            _ => ("application/pdf", "pdf")
        };
        return File(exportResult.Data, contentType, $"{report.FileName}.{extension}");
    }

    private async Task<AppendixIVAViewModel> BuildAppendixIVAViewModelAsync(UbisSessionData session, int demandId, string demandName, CancellationToken ct)
    {
        var result = await _preBudgetClient.GetAppendixIVAAsync(session.Token, demandId, session.FinancialYear, ct);
        var schemes = await _preBudgetClient.GetAppendixIVSchemesAsync(session.Token, demandId, ct);
        var model = new AppendixIVAViewModel
        {
            DemandId = demandId,
            DemandName = demandName,
            FinancialYear = session.FinancialYear,
            Records = result.Data ?? new(),
            Schemes = schemes.Data ?? new()
        };
        ApplyRemarksColumnVisibility(model, session.RoleName);
        await ApplyFreezeStatusAsync(model, session, demandId, "IV-A", ct);
        return model;
    }

    /// <summary>
    /// AJAX-only (2026-08-04): same self-referential "Actuals upto Sept" pre-fill as Appendix IV's
    /// <see cref="GetAppendixIVPreviousYearReference"/> - Scheme/SubScheme are shared reference data
    /// (dbo.M_Scheme/M_SubScheme are not appendix-specific), so this reuses the exact same Scheme/
    /// SubScheme dropdowns and cascade endpoint as Appendix IV, just looks up Appendix IV-A's own
    /// prior-year row instead.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAppendixIVAPreviousYearReference(int demandId, int schemeId, int? subSchemeId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();

        var priorYear = GetPrecedingFinancialYears(session.FinancialYear, 2).FirstOrDefault();
        if (string.IsNullOrEmpty(priorYear))
        {
            return Json(new { found = false });
        }

        var result = await _preBudgetClient.GetAppendixIVAAsync(session.Token, demandId, priorYear, ct);
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
