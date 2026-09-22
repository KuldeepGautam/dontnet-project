// Save/Freeze/Delete actions + view-model builder for Appendix III: CNA/SNA Balances of Schemes.
// Also carries the Scheme/Sub-Scheme/BE AJAX helpers for Appendix III, and the Appendix III-A
// BE-previous-year AJAX endpoint, which were grouped alongside Appendix III in the pre-split
// junk-drawer partial-class file.
namespace UBIS.Web.Areas.PreBudgetMeeting.Controllers;

using Microsoft.AspNetCore.Mvc;
using UBIS.Web.Areas.PreBudgetMeeting.Models;
using UBIS.Web.Services.Clients;
using UBIS.Web.Services.Session;

public partial class PreBudgetMeetingController
{
    // --- Appendix III ---
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAppendixIII(int demandId, SaveAppendixCnaSnaBalanceDto newRecord, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        newRecord.DemandId = demandId; newRecord.FinancialYear = session.FinancialYear;
        var demandName = await ResolveDemandNameAsync(session, demandId, ct);

        // Bug report 2026-08-27: "Adding 2nd time, updating record, it should not update." The
        // Edit button (re-added 2026-08-25, see populateAppendixIIIEntryForm) is now the real way
        // to update an existing row and correctly sets NewRecord.Id itself - the auto-lookup-and-
        // reassign fallback this replaced (2026-08-06, from when there was no Edit button at all)
        // was silently converting a genuine "Add a new entry" attempt into an Update the instant its
        // Balance Type/Scheme/SubScheme happened to match an existing row, overwriting that row's
        // other fields with no warning. A second Add for the same combination should be rejected,
        // not merged - same duplicate-block pattern used elsewhere in this module.
        if (!newRecord.Id.HasValue || newRecord.Id.Value <= 0)
        {
            var existingResult = await _preBudgetClient.GetAppendixIIIAsync(session.Token, demandId, session.FinancialYear, ct);
            var duplicate = existingResult.Data?.Any(r =>
                string.Equals(r.CategoryType, newRecord.CategoryType, StringComparison.OrdinalIgnoreCase) &&
                r.SchemeId == newRecord.SchemeId &&
                r.SubSchemeId == newRecord.SubSchemeId) ?? false;
            if (duplicate)
            {
                var blockedModel = await BuildAppendixIIIViewModelAsync(session, demandId, demandName, ct);
                blockedModel.NewRecord = newRecord;
                blockedModel.StatusIsError = true;
                blockedModel.StatusMessage = "A record for this Balance Type/Scheme/Sub-Scheme already exists. Use Edit on the existing row to modify it.";
                ViewData["Title"] = "Appendix III : CNA/SNA Balances of Schemes";
                return View("AppendixIII", blockedModel);
            }
        }

        var result = newRecord.Id.HasValue && newRecord.Id.Value > 0
            ? await _preBudgetClient.UpdateAppendixIIIAsync(session.Token, newRecord.Id.Value, newRecord, ct)
            : await _preBudgetClient.CreateAppendixIIIAsync(session.Token, newRecord, ct);
        var model = await BuildAppendixIIIViewModelAsync(session, demandId, demandName, ct);
        // "Input grid still not blank after Submit" (client testing feedback, 2026-08-24) - asp-for
        // renders from ModelState before the model; see AppendixIV.cs's matching comment. Only on
        // success - keep the user's typed values on the page if the save itself failed.
        if (result.IsSuccess)
        {
            ModelState.Clear();
        }

        // Bug fix 2026-08-06 (tester report): after Save, the Balance Type/Scheme/Sub-Scheme
        // dropdowns were reverting to "Select" and the grid below started showing every row again
        // instead of staying filtered to the just-saved Scheme. Root cause: this rebuild only ever
        // returned a blank NewRecord, so the form re-rendered with nothing selected. Carrying the
        // just-submitted selection forward here (paired with the pre-budget-meeting.js applyFragment
        // change that re-runs the Scheme/Sub-Scheme cascade and grid filter for this form) keeps the
        // user's filter in place across the AJAX round trip instead of resetting it.
        model.NewRecord.CategoryType = newRecord.CategoryType;
        model.NewRecord.SchemeId = newRecord.SchemeId;
        model.NewRecord.SubSchemeId = newRecord.SubSchemeId;

        model.StatusIsError = !result.IsSuccess;
        model.StatusMessage = result.IsSuccess ? (newRecord.Id.HasValue && newRecord.Id.Value > 0 ? "Record modified successfully." : "Record saved successfully.") : (result.Error?.Message ?? "Could not save the record.");
        ViewData["Title"] = "Appendix III : CNA/SNA Balances of Schemes";
        return View("AppendixIII", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixIII(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.FreezeAppendixIIIAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see FreezeAppendixTemplate's matching comment
        // (PreBudgetMeetingController.cs).
        return await AppendixEntry("III", demandId, ct);
    }

    // "Freeze Data" button on the Appendix III entry form isn't scoped to one row (there's no
    // per-row id to bind there, just the shared entry form) - it freezes every not-yet-frozen
    // record for this demand/year in one action, same bulk pattern as FreezeAppendixIBulk.
    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> FreezeAppendixIIIBulk(int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();

        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixIIIViewModelAsync(session, demandId, demandName, ct);

        foreach (var record in model.Records.Where(r => !r.IsFrozen))
        {
            await _preBudgetClient.FreezeAppendixIIIAsync(session.Token, record.Id, ct);
        }

        model = await BuildAppendixIIIViewModelAsync(session, demandId, demandName, ct);
        model.StatusMessage = "CNA/SNA Balances of Schemes data frozen.";

        ViewData["Title"] = "Appendix III : CNA/SNA Balances of Schemes";
        return View("AppendixIII", model);
    }

    [HttpPost] [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppendixIII(int id, int demandId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return await ExpiredSessionRedirectAsync();
        await _preBudgetClient.DeleteAppendixIIIAsync(session.Token, id, ct);
        // Fragment-return, not RedirectToAction - see DeleteAppendixIA's matching comment.
        return await AppendixEntry("III", demandId, ct);
    }

    private async Task<AppendixIIIViewModel> BuildAppendixIIIViewModelAsync(UbisSessionData session, int demandId, string demandName, CancellationToken ct)
    {
        var result = await _preBudgetClient.GetAppendixIIIAsync(session.Token, demandId, session.FinancialYear, ct);
        var categories = await _preBudgetClient.GetAppendixIIICategoriesAsync(session.Token, ct);
        var model = new AppendixIIIViewModel
        {
            DemandId = demandId,
            DemandName = demandName,
            FinancialYear = session.FinancialYear,
            Records = result.Data ?? new(),
            Categories = categories.Data ?? new()
        };
        await ApplyFreezeStatusAsync(model, session, demandId, "III", ct);
        return model;
    }

    /// <summary>
    /// Export dropdown - PDF/Excel/CSV, same TabularReportDto/_reportingClient shape as
    /// ExportAppendixVIA/ExportAppendixIA. Rebuilt 2026-09-21 against a client-supplied reference
    /// file: each of the three sections is grouped by Scheme (ordered by SchemeSrNo) with a
    /// Sub-Scheme breakdown when the Scheme has one - same Scheme/Sub-Scheme structure as Appendix
    /// IV's BuildStructuredAppendixIVReport, but simpler: "S.No." is a genuinely separate column
    /// here (so a Scheme heading/direct row's Name stays plain, unprefixed - only a Sub-Scheme row
    /// embeds "{SchemeSrNo}.{SubSchemeSrNo:00}- {Name}" inline, with no leading indent, since its
    /// own S.No. cell is blank), and there is exactly ONE "Total" row per section (summing every
    /// underlying record in that section), not a separate subtotal per Scheme.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportAppendixIII(int demandId, string format, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null)
        {
            return await ExpiredSessionRedirectAsync();
        }

        var demandName = await ResolveDemandNameAsync(session, demandId, ct);
        var model = await BuildAppendixIIIViewModelAsync(session, demandId, demandName, ct);
        var priorYearStart = model.PriorFinancialYear.Split('-')[0];

        static string SchemeLabel(AppendixCnaSnaBalanceDto r) => r.SchemeNameRaw ?? "-";
        static string SubSchemeLabel(AppendixCnaSnaBalanceDto r) => $"{r.SchemeSrNo ?? 0}.{(r.SubSchemeSrNo ?? 0):00}- {r.SubSchemeNameRaw ?? "-"}";
        static string RowName(AppendixCnaSnaBalanceDto r) => r.SubSchemeId.HasValue ? SubSchemeLabel(r) : SchemeLabel(r);

        // Builds one section's Groups: a scheme-numbered heading row (S.No. + Name only, every
        // other cell blank - not a full-width merged label, matching the reference file exactly)
        // plus its Sub-Scheme rows when the Scheme has a breakdown, or a single directly-numbered
        // row when it doesn't - then one "Total " row summing every record actually in this section
        // (not per-Scheme).
        List<ReportGroupDto> BuildSchemeGroups(List<AppendixCnaSnaBalanceDto> sectionRecords, int columnCount, Func<AppendixCnaSnaBalanceDto, string[]> rowCells, Func<List<AppendixCnaSnaBalanceDto>, string[]> totalCells)
        {
            var groups = new List<ReportGroupDto>();
            var schemeGroups = sectionRecords
                .GroupBy(r => r.SchemeId)
                .OrderBy(g => g.First().SchemeSrNo ?? 0)
                .ToList();

            var schemeIndex = 0;
            foreach (var schemeGroup in schemeGroups)
            {
                schemeIndex++;
                var schemeRows = schemeGroup.ToList();
                var hasSubSchemeBreakdown = schemeRows.Any(r => r.SubSchemeId.HasValue);
                if (hasSubSchemeBreakdown)
                {
                    var headingRow = Enumerable.Repeat("", columnCount).ToArray();
                    headingRow[0] = schemeIndex.ToString();
                    headingRow[1] = SchemeLabel(schemeRows[0]);
                    var rows = new List<string[]> { headingRow };
                    rows.AddRange(schemeRows.Select(rowCells));
                    groups.Add(new ReportGroupDto { Rows = rows });
                }
                else
                {
                    var cells = rowCells(schemeRows[0]);
                    cells[0] = schemeIndex.ToString();
                    groups.Add(new ReportGroupDto { Rows = new List<string[]> { cells } });
                }
            }

            if (sectionRecords.Count > 0)
            {
                groups.Add(new ReportGroupDto { Totals = new List<ReportTotalRowDto> { new() { Cells = totalCells(sectionRecords) } } });
            }

            return groups;
        }

        var centralSectorRecords = model.Records.Where(r => r.CategoryType == "CentralSectorScheme").ToList();
        var centrallySponsoredRecords = model.Records.Where(r => r.CategoryType == "CentrallySponsoredScheme").ToList();
        var exemptedRecords = model.Records.Where(r => r.CategoryType is "ExemptedFromCna" or "ExemptedFromSna").ToList();

        string[] CnaRowCells(AppendixCnaSnaBalanceDto r) => new[]
        {
            "", RowName(r),
            r.BE?.ToString("0.00")!, r.BalanceAsOnAprilOpening?.ToString("0.00")!,
            r.ReleasesDuringFY?.ToString("0.00")!, r.BalanceAsOnSeptClosing?.ToString("0.00")!,
            r.DateOfLastRelease?.ToString("dd/MM/yyyy") ?? "-", r.AmountOfLastRelease?.ToString("0.00")!
        };
        string[] CnaTotalCells(List<AppendixCnaSnaBalanceDto> rows) => new[]
        {
            "", "Total ",
            rows.Sum(r => r.BE ?? 0m).ToString("0.00"), rows.Sum(r => r.BalanceAsOnAprilOpening ?? 0m).ToString("0.00"),
            rows.Sum(r => r.ReleasesDuringFY ?? 0m).ToString("0.00"), rows.Sum(r => r.BalanceAsOnSeptClosing ?? 0m).ToString("0.00"),
            "", rows.Sum(r => r.AmountOfLastRelease ?? 0m).ToString("0.00")
        };

        string[] SnaRowCells(AppendixCnaSnaBalanceDto r) => new[]
        {
            "", RowName(r),
            r.BE?.ToString("0.00")!, r.BalanceAsOnAprilOpening?.ToString("0.00")!,
            r.ReleasesDuringFY?.ToString("0.00")!, r.BalanceAsOnSeptClosing?.ToString("0.00")!,
            r.NotTransferredToSnaAsOnSept?.ToString("0.00")!,
            r.DateOfLastRelease?.ToString("dd/MM/yyyy") ?? "-", r.AmountOfLastRelease?.ToString("0.00")!
        };
        string[] SnaTotalCells(List<AppendixCnaSnaBalanceDto> rows) => new[]
        {
            "", "Total ",
            rows.Sum(r => r.BE ?? 0m).ToString("0.00"), rows.Sum(r => r.BalanceAsOnAprilOpening ?? 0m).ToString("0.00"),
            rows.Sum(r => r.ReleasesDuringFY ?? 0m).ToString("0.00"), rows.Sum(r => r.BalanceAsOnSeptClosing ?? 0m).ToString("0.00"),
            rows.Sum(r => r.NotTransferredToSnaAsOnSept ?? 0m).ToString("0.00"),
            "", rows.Sum(r => r.AmountOfLastRelease ?? 0m).ToString("0.00")
        };

        // Exempted list groups the same way (Scheme heading + Sub-Scheme rows carrying the actual
        // Category/Reason data), but has no numeric Total row of its own.
        var exemptedGroups = new List<ReportGroupDto>();
        var exemptedSchemeGroups = exemptedRecords.GroupBy(r => r.SchemeId).OrderBy(g => g.First().SchemeSrNo ?? 0).ToList();
        var exemptedIndex = 0;
        foreach (var schemeGroup in exemptedSchemeGroups)
        {
            exemptedIndex++;
            var schemeRows = schemeGroup.ToList();
            var hasSubSchemeBreakdown = schemeRows.Any(r => r.SubSchemeId.HasValue);
            string CategoryLabel(AppendixCnaSnaBalanceDto r) => r.CategoryType == "ExemptedFromCna" ? "CNA" : "SNA-SPARSH";
            if (hasSubSchemeBreakdown)
            {
                var rows = new List<string[]> { new[] { exemptedIndex.ToString(), SchemeLabel(schemeRows[0]), "", "" } };
                rows.AddRange(schemeRows.Select(r => new[] { "", RowName(r), CategoryLabel(r), string.IsNullOrEmpty(r.ReasonForExemption) ? "-" : r.ReasonForExemption }));
                exemptedGroups.Add(new ReportGroupDto { Rows = rows });
            }
            else
            {
                var r = schemeRows[0];
                exemptedGroups.Add(new ReportGroupDto { Rows = new List<string[]> { new[] { exemptedIndex.ToString(), RowName(r), CategoryLabel(r), string.IsNullOrEmpty(r.ReasonForExemption) ? "-" : r.ReasonForExemption } } });
            }
        }

        var report = new TabularReportDto
        {
            Title = demandName,
            TitleBoxLines = new List<string> { "Appendix III \nCNA/SNA Balances of Schemes \n" },
            ParaNo = model.ParaNo,
            Remarks = model.Remarks,
            Sections = new List<ReportSectionDto>
            {
                new()
                {
                    Title = "CNA Balances of the Central Sector Schemes",
                    UnitNote = "(In crore of Rupees)",
                    Columns = new List<string>
                    {
                        "S.No.", "Name of the Scheme", $"BE {model.PriorFinancialYear}",
                        $"CNA Balance as on 01.04.{priorYearStart}", $"Releases During the Current FY till 30.09.{priorYearStart}",
                        $"CNA Balance as On 30.09.{priorYearStart}", "Date Of Last Release", "Amount Of Last Release"
                    },
                    CenterAlignedColumns = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 },
                    RightAlignedColumns = new List<int> { 2, 3, 4, 5, 7 },
                    Groups = BuildSchemeGroups(centralSectorRecords, 8, CnaRowCells, CnaTotalCells)
                },
                new()
                {
                    Title = "SNA Balances of the Centrally Sponsored Sector Schemes",
                    UnitNote = "(In crore of Rupees)",
                    Columns = new List<string>
                    {
                        "S.No.", "Name of the Scheme", $"BE {model.PriorFinancialYear}",
                        $"SNA Balance as on 01.04.{priorYearStart}", $"Releases During the Current FY till 30.09.{priorYearStart}",
                        $"SNA Balance as On 30.09.{priorYearStart}",
                        $"Central releases lying in State Treasury but not transferred to SNA Account as on 30.09.{priorYearStart}",
                        "Date Of Last Release", "Amount Of Last Release"
                    },
                    CenterAlignedColumns = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8 },
                    RightAlignedColumns = new List<int> { 2, 3, 4, 5, 6, 8 },
                    Groups = BuildSchemeGroups(centrallySponsoredRecords, 9, SnaRowCells, SnaTotalCells)
                },
                new()
                {
                    Title = "List of Schemes exempted from CNA & SNA",
                    Columns = new List<string> { "S.No.", "Name of the Scheme", "Category Name", "Reason for exemption" },
                    CenterAlignedColumns = new List<int> { 0, 2 },
                    Groups = exemptedGroups
                }
            },
            FileName = $"AppendixIII-{session.UserName}-{DateTime.Now:MMddyyyyHHmmss}"
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
            return await AppendixEntry("III", demandId, ct);
        }

        var (contentType, extension) = format?.ToLowerInvariant() switch
        {
            "excel" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            "csv" => ("text/csv", "csv"),
            _ => ("application/pdf", "pdf")
        };
        return File(exportResult.Data, contentType, $"{report.FileName}.{extension}");
    }

    // AJAX-only: populates the Scheme dropdown once a Balance Type is chosen on Appendix III.
    [HttpGet]
    public async Task<IActionResult> GetAppendixIIISchemes(int demandId, string categoryType, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();
        var result = await _preBudgetClient.GetAppendixIIISchemesAsync(session.Token, demandId, categoryType, session.FinancialYear, ct);
        return Json(result.Data ?? new());
    }

    /// <summary>
    /// Appendix III-A's BE auto-load (corrected 2026-08-24 per legacy screenshot - supersedes the
    /// 2026-08-06 note): BE is for Current FY-1, not the current filing year, and is fetched from
    /// III-A's own history rather than typed in - keyed by DemandId+exact-text-EntityName (free
    /// text, not an FK - same convention as VI-D/VII-A).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAppendixIIIABePreviousYear(int demandId, string entityName, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();

        var yMinus1 = GetPrecedingFinancialYears(session.FinancialYear, 2).FirstOrDefault();
        if (string.IsNullOrEmpty(yMinus1))
        {
            return Json(new { found = false });
        }

        var result = await _preBudgetClient.GetAppendixIIIAAsync(session.Token, demandId, yMinus1, ct);
        var record = result.Data?.FirstOrDefault(r => string.Equals(r.EntityName, entityName, StringComparison.OrdinalIgnoreCase));
        if (record == null)
        {
            return Json(new { found = false });
        }

        return Json(new { found = true, financialYear = yMinus1, be = record.BE });
    }

    // AJAX-only: populates the SubScheme dropdown once a Scheme is chosen on Appendix III.
    [HttpGet]
    public async Task<IActionResult> GetAppendixIIISubSchemes(int schemeId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();
        var result = await _preBudgetClient.GetAppendixIIISubSchemesAsync(session.Token, schemeId, ct);
        return Json(result.Data ?? new());
    }

    // AJAX-only: BE auto-load once a Scheme is chosen on Appendix III (client review 2026-08-05).
    [HttpGet]
    public async Task<IActionResult> GetAppendixIIIBe(int demandId, int schemeId, CancellationToken ct)
    {
        var session = HttpContext.Session.GetUbisSession();
        if (session == null) return Unauthorized();
        // Bug fix 2026-09-15 (client report + direct DB verification: "BE 2025-2026" showed 0 for
        // Scheme "Cooperative Training" under Demand 16, and "pick BETotal for current year not
        // nbetotal"): this used to sum NbeTotal from SbeNbeSummaryByYear for PriorFinancialYear
        // (per the earlier 2026-08-31 fix). Now sums BE_Plan straight from raw SBEData instead
        // (AppendixIIIController.GetBeByScheme, same source/pattern as Appendix VI-B's own BE
        // auto-load), keyed by demandId - already year-scoped per dbo.vw_Demand - so no separate
        // FinancialYear needs passing here.
        var result = await _preBudgetClient.GetAppendixIIIBeByStructureAsync(session.Token, demandId, schemeId, ct);
        return Json(new { be = result.Data?.Be ?? 0m });
    }
}
