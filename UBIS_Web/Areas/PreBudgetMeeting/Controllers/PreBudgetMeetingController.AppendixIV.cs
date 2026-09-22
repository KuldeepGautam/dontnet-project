// Save/Freeze/Delete actions + view-model builder for Appendix IV: Estimates of Schemes.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    // --- Appendix IV ---
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAppendixIV(int demandId, SaveAppendixEstimatesOfSchemesDto newRecord, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        newRecord.DemandId = demandId; newRecord.FinancialYear = session.FinancialYear;
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var result = newRecord.Id.HasValue && newRecord.Id.Value > 0
            ? await _preBudgetClient.UpdateAppendixIVAsync(session.Token, newRecord.Id.Value, newRecord, ct)
            : await _preBudgetClient.CreateAppendixIVAsync(session.Token, newRecord, ct);
        var model = await BuildAppendixIVViewModelAsync(session, demandId, demandName, ct);
        // Root cause of "input grid still not blank after Submit" (client testing feedback,
        // 2026-08-24): asp-for tag helpers render from ModelState first when a matching entry
        // exists (ASP.NET Core's standard "redisplay the posted value on validation failure"
        // behavior), which silently overrides whatever the freshly-built model above says - so
        // leaving NewRecord's fields blank on the model had no visible effect until ModelState
        // (still holding this POST's raw submitted values) is cleared too. Only on success - if
        // the save itself failed, keep the user's typed values so they don't lose their work.
        // Applies to every appendix Save action, not just this one.
        if (result.IsSuccess)
        {
            ModelState.Clear();
        }
        // Client testing feedback (2026-08-24): "Sub-Scheme should not clear/lock after Submit" -
        // the rebuilt model's NewRecord defaults to blank, which is right for the manually-typed
        // fields (Actuals/ProposedRE/ProposedNBE/Remarks - "if field data is not coming from db, it
        // should be blank"), but wiping SchemeId/SubSchemeId too meant every field including the
        // cascade itself reset, forcing the user to re-pick Scheme+SubScheme for every single row.
        // Carry the just-submitted selection forward; AppendixIV.cshtml's fragment-load JS replays
        // the SubScheme cascade + BE auto-load + grid filter against it, same pattern as Appendix III.
        model.NewRecord.SchemeId = newRecord.SchemeId;
        model.NewRecord.SubSchemeId = newRecord.SubSchemeId;
        model.StatusIsError = !result.IsSuccess;
        model.StatusMessage = result.IsSuccess ? (newRecord.Id.HasValue && newRecord.Id.Value > 0 ? "Record modified successfully." : "Record saved successfully.") : (result.Error?.Message ?? "Could not save the record.");
        ViewData["Title"] = "Appendix IV : Estimates of Schemes";
        return View("AppendixIV", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixIV(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.FreezeAppendixIVAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see FreezeAppendixTemplate's matching comment
        // (PreBudgetMeetingController.cs).
        return await AppendixEntry("IV", demandId, ct);
    }

    // "Freeze Data" on the entry form isn't scoped to one row - it freezes every not-yet-frozen
    // record for this demand/year, same bulk pattern as Appendix I/III/III-A.
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixIVBulk(int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();

        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixIVViewModelAsync(session, demandId, demandName, ct);

        foreach (var record in model.Records.Where(r => !r.IsFrozen))
        {
            await _preBudgetClient.FreezeAppendixIVAsync(session.Token, record.Id, ct);
        }

        model = await BuildAppendixIVViewModelAsync(session, demandId, demandName, ct);
        model.StatusMessage = "Estimates of Schemes data frozen.";

        ViewData["Title"] = "Appendix IV : Estimates of Schemes";
        return View("AppendixIV", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppendixIV(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.DeleteAppendixIVAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see DeleteAppendixIA's matching comment.
        return await AppendixEntry("IV", demandId, ct);
    }

    /// <summary>
    /// Export dropdown (2026-09-18) - rebuilt to match the government circular's own Appendix-IV
    /// "Estimates of Schemes" table exactly (screenshot supplied): rows grouped under a full-width
    /// Category heading (same Centrally Sponsored/Central Sector split as Appendix III/III-B,
    /// resolved server-side via the Scheme's own CategoryId even though Appendix IV itself has no
    /// Category selection at data-entry time), a per-group autogenerated Total row, and a final
    /// grand Total (CSS+CS) row - none of which the on-screen grid computes today, so every total
    /// here is computed fresh from the group's own rows rather than trusted from any stored field.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportAppendixIV(int demandId, string format, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixIVViewModelAsync(session, demandId, demandName, ct);
        var priorYearStart = model.PriorFinancialYear.Split('-')[0];
        var currentYearStart = model.FinancialYear.Split('-')[0];
        var nextFinancialYear = model.YearOffset(1);
        var currentShort = FormatFinancialYearShort(model.FinancialYear)[2..];
        var nextShort = FormatFinancialYearShort(nextFinancialYear)[2..];

        // Client requirement 2026-09-18: a numeric cell with no underlying data (as opposed to a
        // genuine computed zero/percentage) is passed as null rather than "0.00"/"" - the shared
        // report generators render a null cell as "0.00" in Excel/CSV and "..." in PDF (see
        // ReportGroup.Rows' own doc comment). Scheme name/Remarks text columns keep their own "-"/""
        // placeholder, unrelated to this numeric convention.
        static string? FormatPercent(decimal? value) => value.HasValue ? $"{value.Value:F2}%" : null;

        string[] RowCells(int sl, AppendixEstimatesOfSchemesDto r) => new[]
        {
            sl.ToString(),
            r.SchemeName ?? "-",
            r.Actuals?.ToString("0.00")!,
            r.ActualsUptoSeptPrevYear?.ToString("0.00")!,
            r.BE?.ToString("0.00")!,
            r.ActualsUptoSept?.ToString("0.00")!,
            FormatPercent(r.PercentWrtBE)!,
            r.ProposedRE?.ToString("0.00")!,
            r.AddlReSought?.ToString("0.00")!,
            r.ProposedNBE?.ToString("0.00")!,
            r.AddlNbeSought?.ToString("0.00")!,
            r.RemarksMinistry ?? ""
        };

        string[] TotalCells(string label, List<AppendixEstimatesOfSchemesDto> rows)
        {
            var totalActuals = rows.Sum(r => r.Actuals ?? 0m);
            var totalActualsUptoSeptPrevYear = rows.Sum(r => r.ActualsUptoSeptPrevYear ?? 0m);
            var totalBE = rows.Sum(r => r.BE ?? 0m);
            var totalActualsUptoSept = rows.Sum(r => r.ActualsUptoSept ?? 0m);
            var totalProposedRE = rows.Sum(r => r.ProposedRE ?? 0m);
            var totalAddlReSought = rows.Sum(r => r.AddlReSought ?? 0m);
            var totalProposedNBE = rows.Sum(r => r.ProposedNBE ?? 0m);
            var totalAddlNbeSought = rows.Sum(r => r.AddlNbeSought ?? 0m);
            var totalPercent = totalBE > 0 ? (decimal?)Math.Round(totalActualsUptoSept / totalBE * 100, 2) : null;
            return new[]
            {
                label, "",
                totalActuals.ToString("0.00"), totalActualsUptoSeptPrevYear.ToString("0.00"),
                totalBE.ToString("0.00"), totalActualsUptoSept.ToString("0.00"),
                FormatPercent(totalPercent)!,
                totalProposedRE.ToString("0.00"), totalAddlReSought.ToString("0.00"),
                totalProposedNBE.ToString("0.00"), totalAddlNbeSought.ToString("0.00"),
                ""
            };
        }

        var categoryGroups = model.Records.GroupBy(r => r.CategoryType).ToList();
        var groups = categoryGroups.Select(g =>
        {
            var rows = g.ToList();
            return new ReportGroupDto
            {
                Label = CategoryHeading(g.Key),
                Rows = rows.Select((r, i) => RowCells(i + 1, r)).ToList(),
                Totals = new List<ReportTotalRowDto> { new() { Cells = TotalCells($"Total ({CategoryAbbreviation(g.Key)})", rows) } }
            };
        }).ToList();

        // Grand total across every category, only meaningful (and only rendered) when there's more
        // than one group - a single-category demand has nothing to sum "CSS+CS" over.
        if (categoryGroups.Count > 1)
        {
            var grandLabel = "Total (" + string.Join("+", categoryGroups.Select(g => CategoryAbbreviation(g.Key))) + ")";
            groups.Add(new ReportGroupDto { Rows = new List<string[]>(), Totals = new List<ReportTotalRowDto> { new() { Cells = TotalCells(grandLabel, model.Records) } } });
        }

        // Client instruction 2026-09-21: Budget Division's own copy of this export additionally
        // shows the RE/NBE "recommended by Budget" columns (BudgetRecommendedRE/NBE - already on the
        // entity/DTO, entered elsewhere, never shown on any export before now) and is grouped one
        // level deeper than the Ministry-facing report above: Category -> Scheme -> Sub-Scheme, with
        // a bold Scheme "Total" row whenever a Scheme's data is broken down by Sub-Scheme (matching
        // the supplied circular reference file's "1. Secretariat" / "1.01- Secretariat" / "Total"
        // shape), or the Scheme's own single row shown directly when it has no Sub-Scheme breakdown.
        // "Single Demand Users" (this system's Ministry-User-equivalent role - see
        // AutonomousMasterPageRoleNames' own comment) get the same Category->Scheme->Sub-Scheme
        // structure and PDF format (client instruction 2026-09-21: "columns may be slightly
        // different that Budget User, rest all same as Budget User"), but without the three
        // Budget-only columns (RE/NBE recommended-by-Budget, Remarks by Budget Division) - a
        // Ministry-side user has no business seeing Budget Division's internal recommendations.
        // Everyone else keeps the original flat Sl-No./Remarks report unchanged. Client instruction
        // 2026-09-21: ABO-DS-Director/Section User get the exact same "Budget side" export as
        // Budget Division (BudgetSideRemarksRoleNames - same set the grid's remarks-column gating
        // already uses), not just Budget Division alone.
        var isBudgetSide = BudgetSideRemarksRoleNames.Contains(session.RoleName, StringComparer.OrdinalIgnoreCase);
        var isSingleDemandUser = string.Equals(session.RoleName, "Single Demand Users", StringComparison.OrdinalIgnoreCase);
        var report = isBudgetSide || isSingleDemandUser
            ? BuildStructuredAppendixIVReport(session, demandName, model, priorYearStart, currentYearStart, includeBudgetColumns: isBudgetSide)
            : new TabularReportDto
            {
                Title = "Appendix-IV: Estimates of Schemes",
                Subtitle = $"Demand: {demandName} | Financial Year: {session.FinancialYear}",
                ParaNo = model.ParaNo,
                UnitNote = "(₹ in crore)",
                Remarks = model.Remarks,
                Columns = new List<string>
                {
                    "Sl No.", "Name of Scheme",
                    $"Actuals {FormatFinancialYearShort(model.PriorFinancialYear)}", $"Actuals upto 9/{priorYearStart}",
                    $"BE {FormatFinancialYearShort(model.FinancialYear)}", $"Actuals upto 9/{currentYearStart}",
                    $"% w.r.t. BE {currentShort}",
                    $"RE {currentShort} prop. By Min/Deptt", $"Addl. RE {currentShort} sought over BE {currentShort}",
                    $"BE {nextShort} prop. By Min/Dep", $"Addl. BE {nextShort} sought over BE {currentShort}",
                    "Remarks"
                },
                CenterAlignedColumns = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 },
                RightAlignedColumns = new List<int> { 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                Groups = groups,
                FileName = $"AppendixIV-{session.UserName}-{DateTime.Now:MMddyyyyHHmmss}"
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
            return await AppendixEntry("IV", demandId, ct);
        }

        var (contentType, extension) = format?.ToLowerInvariant() switch
        {
            "excel" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            "csv" => ("text/csv", "csv"),
            _ => ("application/pdf", "pdf")
        };
        return File(exportResult.Data, contentType, $"{report.FileName}.{extension}");
    }

    /// <summary>
    /// Category -> Scheme -> Sub-Scheme structured Appendix IV export (client-supplied reference
    /// files, 2026-09-21), shared by both Budget Division and "Single Demand Users" - same grouping
    /// (Category, then per-Scheme rows with a bold "Total" row whenever a Scheme's data is broken
    /// down by Sub-Scheme, or its own single row shown directly when it isn't), same row-label
    /// punctuation ("1. Secretariat" for a Scheme heading/direct row, "    1.01- Secretariat" -
    /// 4-space indent, no space before the dash - for a Sub-Scheme row, built from the raw Scheme/
    /// SubScheme names rather than SchemeDisplayFormatter/SubSchemeDisplayFormatter's shared
    /// "N - Name" grid/dropdown format), and the same plain "Total"/"Grand Total" labels with no
    /// "(CS)"/"(CSS)" abbreviation suffix. <paramref name="includeBudgetColumns"/> controls the only
    /// actual difference between the two reference files: Budget Division sees the two "recommended
    /// by Budget" RE/NBE columns and the Remarks-by-Budget-Division column; Single Demand Users (this
    /// system's Ministry-User-equivalent role) do not - a Ministry-side user has no business seeing
    /// Budget Division's internal recommendations.
    /// </summary>
    private static TabularReportDto BuildStructuredAppendixIVReport(
        UbisSessionData session, string demandName, AppendixIVViewModel model,
        string priorYearStart, string currentYearStart, bool includeBudgetColumns)
    {
        // Reference files store this as a plain number (57.87), not "57.87%" text.
        static string? FormatPercent(decimal? value) => value.HasValue ? value.Value.ToString("0.00") : null;

        // "1. Secretariat" for a Scheme heading/direct row, "    1.01- Secretariat" (4-space indent,
        // no space before the dash) for a Sub-Scheme row - exact punctuation from the reference
        // files, not SchemeDisplayFormatter/SubSchemeDisplayFormatter's "N - Name"/"N.NN - Name".
        static string SchemeLabel(AppendixEstimatesOfSchemesDto r) => $"{r.SchemeSrNo ?? 0}. {r.SchemeNameRaw ?? "-"}";
        static string SubSchemeLabel(AppendixEstimatesOfSchemesDto r) => $"    {r.SchemeSrNo ?? 0}.{(r.SubSchemeSrNo ?? 0):00}- {r.SubSchemeNameRaw ?? "-"}";
        static string RowName(AppendixEstimatesOfSchemesDto r) => r.SubSchemeId.HasValue ? SubSchemeLabel(r) : SchemeLabel(r);

        string[] RowCells(AppendixEstimatesOfSchemesDto r)
        {
            var cells = new List<string>
            {
                RowName(r),
                r.Actuals?.ToString("0.00")!,
                r.ActualsUptoSeptPrevYear?.ToString("0.00")!,
                r.BE?.ToString("0.00")!,
                r.ActualsUptoSept?.ToString("0.00")!,
                FormatPercent(r.PercentWrtBE)!,
                r.ProposedRE?.ToString("0.00")!,
                r.AddlReSought?.ToString("0.00")!
            };
            if (includeBudgetColumns) cells.Add(r.BudgetRecommendedRE?.ToString("0.00")!);
            cells.Add(r.ProposedNBE?.ToString("0.00")!);
            cells.Add(r.AddlNbeSought?.ToString("0.00")!);
            if (includeBudgetColumns) cells.Add(r.BudgetRecommendedNBE?.ToString("0.00")!);
            // Client instruction 2026-09-21: "similar excel columns" to the grid - Budget-side roles
            // see only Remarks (Budget Div), Single Demand Users see only Remarks (Ministry), never
            // both in the same export.
            cells.Add(includeBudgetColumns ? (r.RemarksBudget ?? "") : (r.RemarksMinistry ?? ""));
            return cells.ToArray();
        }

        string[] TotalCells(string label, List<AppendixEstimatesOfSchemesDto> rows)
        {
            var totalActuals = rows.Sum(r => r.Actuals ?? 0m);
            var totalActualsUptoSeptPrevYear = rows.Sum(r => r.ActualsUptoSeptPrevYear ?? 0m);
            var totalBE = rows.Sum(r => r.BE ?? 0m);
            var totalActualsUptoSept = rows.Sum(r => r.ActualsUptoSept ?? 0m);
            var totalProposedRE = rows.Sum(r => r.ProposedRE ?? 0m);
            var totalAddlReSought = rows.Sum(r => r.AddlReSought ?? 0m);
            var totalBudgetRecommendedRE = rows.Sum(r => r.BudgetRecommendedRE ?? 0m);
            var totalProposedNBE = rows.Sum(r => r.ProposedNBE ?? 0m);
            var totalAddlNbeSought = rows.Sum(r => r.AddlNbeSought ?? 0m);
            var totalBudgetRecommendedNBE = rows.Sum(r => r.BudgetRecommendedNBE ?? 0m);
            var totalPercent = totalBE > 0 ? (decimal?)Math.Round(totalActualsUptoSept / totalBE * 100, 2) : null;
            var cells = new List<string>
            {
                label,
                totalActuals.ToString("0.00"), totalActualsUptoSeptPrevYear.ToString("0.00"),
                totalBE.ToString("0.00"), totalActualsUptoSept.ToString("0.00"),
                FormatPercent(totalPercent)!,
                totalProposedRE.ToString("0.00"), totalAddlReSought.ToString("0.00")
            };
            if (includeBudgetColumns) cells.Add(totalBudgetRecommendedRE.ToString("0.00"));
            cells.Add(totalProposedNBE.ToString("0.00"));
            cells.Add(totalAddlNbeSought.ToString("0.00"));
            if (includeBudgetColumns) cells.Add(totalBudgetRecommendedNBE.ToString("0.00"));
            cells.Add("");
            return cells.ToArray();
        }

        // Category order: M_Category.SerialNo ("I".."VI") - same string-sort convention already
        // used by AppendixIIIBController.GetCategories' `.OrderBy(c => c.SerialNo)`. A Scheme with no
        // CategoryId (CategorySerialNo null) sorts last rather than first/throwing.
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

        // Full "YYYY-YYYY" years here, not FormatFinancialYearShort's "YY" form the Ministry report
        // above uses - matching both reference files' own exact header text and line-break points
        // ("Actual\n2024-2025", "Additional \nR.E.2025-2026\n sought over \nB.E.2025-2026", etc.),
        // confirmed identical across both the Budget Division and Single Demand User reference
        // files for every column they share.
        var columns = new List<string>
        {
            "Name of Scheme/Item",
            $"Actual\n{model.PriorFinancialYear}", $"Actual upto\n9/{priorYearStart}",
            $"B.E.\n{model.FinancialYear}", $"Actual upto\n9/{currentYearStart}",
            $"%w.r.t. B.E.\n{model.FinancialYear}",
            $"R.E.{model.FinancialYear}\n proposed by Ministry",
            $"Additional \nR.E.{model.FinancialYear}\n sought over \nB.E.{model.FinancialYear}"
        };
        if (includeBudgetColumns) columns.Add($"R.E.{model.FinancialYear} recommended by Budget");
        columns.Add($"B.E.{model.YearOffset(1)} proposed by Ministry");
        columns.Add($"Additional \nB.E.{model.YearOffset(1)}\n sought over \nB.E.{model.FinancialYear}");
        if (includeBudgetColumns) columns.Add($"B.E.{model.YearOffset(1)} recommended by Budget");
        columns.Add(includeBudgetColumns ? "Remarks\nby Budget Division" : "Remarks\nby Ministry");

        return new TabularReportDto
        {
            Title = demandName,
            TitleBoxLines = new List<string> { "Appendix IV \nEstimates of Schemes" },
            ParaNo = model.ParaNo,
            UnitNote = "(In crore of Rupees)",
            Remarks = model.Remarks,
            Columns = columns,
            // Text columns are now always exactly Name (index 0) + the single Remarks column at the
            // end (Ministry-side or Budget-side, never both) - see RowCells' own mutual-exclusivity
            // comment above.
            RightAlignedColumns = Enumerable.Range(1, columns.Count - 2).ToList(),
            Groups = groups,
            FileName = $"AppendixIV-{(includeBudgetColumns ? "BudgetDivision" : "SingleDemandUser")}-{session.UserName}-{DateTime.Now:MMddyyyyHHmmss}"
        };
    }

    private async Task<AppendixIVViewModel> BuildAppendixIVViewModelAsync(UbisSessionData session, int demandId, string demandName, CancellationToken ct)
    {
        var result = await _preBudgetClient.GetAppendixIVAsync(session.Token, demandId, session.FinancialYear, ct);
        var schemes = await _preBudgetClient.GetAppendixIVSchemesAsync(session.Token, demandId, ct);
        var model = new AppendixIVViewModel
        {
            DemandId = demandId,
            DemandName = demandName,
            FinancialYear = session.FinancialYear,
            Records = result.Data ?? new(),
            Schemes = schemes.Data ?? new()
        };
        ApplyRemarksColumnVisibility(model, session.RoleName);
        await ApplyFreezeStatusAsync(model, session, demandId, "IV", ct);
        return model;
    }

    // AJAX-only: BE auto-load once a Scheme (and optionally SubScheme) is chosen on Appendix IV (client review 2026-08-05).
    [HttpGet]
    public async Task<IActionResult> GetAppendixIVBe(int demandId, int schemeId, int? subSchemeId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();
        var result = await _preBudgetClient.GetAppendixIVBeByStructureAsync(session.Token, demandId, schemeId, subSchemeId, session.FinancialYear, ct);
        return Json(new { be = result.Data?.Be ?? 0m });
    }

    // AJAX-only: populates the Sub-Scheme dropdown once a Scheme is chosen on Appendix IV.
    [HttpGet]
    public async Task<IActionResult> GetAppendixIVSubSchemes(int schemeId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();
        var result = await _preBudgetClient.GetAppendixIVSubSchemesAsync(session.Token, schemeId, ct);
        return Json(result.Data ?? new());
    }

    /// <summary>
    /// AJAX-only (2026-08-04): Appendix IV's "Actuals upto Sept" reference columns used to be
    /// hand-typed for every new Scheme/SubScheme entry even when that same Scheme/SubScheme already
    /// has an Appendix IV row on file for the prior year - pulls
    /// <see cref="AppendixEstimatesOfSchemes.ActualsUptoSept"/> and
    /// <see cref="AppendixEstimatesOfSchemes.ActualsUptoSeptPrevYear"/> straight from that prior-year
    /// row (self-referential, keyed by Scheme+SubScheme rather than just DemandId since Appendix IV
    /// is one row per scheme, not one row per demand). Returns found=false when this Scheme/
    /// SubScheme has no Appendix IV row for the prior year yet, so the caller falls back to leaving
    /// the boxes editable.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAppendixIVPreviousYearReference(int demandId, int schemeId, int? subSchemeId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();

        var priorYear = GetPrecedingFinancialYears(session.FinancialYear, 2).FirstOrDefault();
        if (string.IsNullOrEmpty(priorYear))
        {
            return Json(new { found = false });
        }

        var result = await _preBudgetClient.GetAppendixIVAsync(session.Token, demandId, priorYear, ct);
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
