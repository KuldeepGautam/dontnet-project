namespace UBIS.Services.Reporting.Infrastructure.Services;

using ClosedXML.Excel;
using UBIS.Services.Reporting.Application.Interfaces;
using UBIS.Services.Reporting.Domain.Entities;

/// <summary>
/// Renders a TabularReport to .xlsx via ClosedXML — MIT-licensed, chosen over EPPlus (Polyform
/// Noncommercial since v5) for the same reason as the PDF generator: no revenue-based restriction
/// that could become a compliance problem once this is reused across 50+ pages.
/// </summary>
public class ExcelReportGenerator : IReportGenerator
{
    public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public string FileExtension => "xlsx";

    public byte[] Generate(TabularReport report)
    {
        report.EnsureValid();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SanitizeSheetName(report.Title));

        var currentRow = 1;

        // When Sections is used, the top-level Columns list is empty (the report has no single
        // table of its own) - widen the shared Title/Subtitle/ParaNo/UnitNote/Remarks merge to the
        // widest section's column count instead, so those single-cell-merge calls below never get
        // an invalid (zero-width) range.
        var reportWidth = report.Sections is { Count: > 0 }
            ? report.Sections.Max(s => s.Columns.Count)
            : report.Columns.Count;

        if (report.TitleBoxLines is { Count: > 0 })
        {
            // Client requirement 2026-09-18 (Appendix II circular format): Title + TitleBoxLines
            // render as a bordered, bold-centered box ("{DemandNo}. {DemandName}" / "Appendix II" /
            // "{Appendix full name}", each its own bordered row) instead of the plain unbordered
            // Title every other caller gets.
            var boxStartRow = currentRow;
            foreach (var line in new[] { report.Title }.Concat(report.TitleBoxLines))
            {
                sheet.Cell(currentRow, 1).Value = line;
                sheet.Cell(currentRow, 1).Style.Font.Bold = true;
                sheet.Cell(currentRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                // Added 2026-09-21 (Budget Division's Appendix IV export, client-supplied reference
                // file): a TitleBoxLines entry can itself carry embedded "\n" line breaks (e.g.
                // "Appendix IV \nEstimates of Schemes\n") - WrapText is what actually renders those
                // as separate visual lines in Excel rather than one run-on line.
                sheet.Cell(currentRow, 1).Style.Alignment.WrapText = true;
                sheet.Range(currentRow, 1, currentRow, reportWidth).Merge();
                currentRow++;
            }
            var boxRange = sheet.Range(boxStartRow, 1, currentRow - 1, reportWidth);
            boxRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            boxRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }
        else
        {
            sheet.Cell(currentRow, 1).Value = report.Title;
            sheet.Cell(currentRow, 1).Style.Font.Bold = true;
            sheet.Cell(currentRow, 1).Style.Font.FontSize = 14;
            sheet.Cell(currentRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            sheet.Range(currentRow, 1, currentRow, reportWidth).Merge();
            currentRow++;
        }

        if (!string.IsNullOrEmpty(report.Subtitle))
        {
            sheet.Cell(currentRow, 1).Value = report.Subtitle;
            sheet.Cell(currentRow, 1).Style.Font.Italic = true;
            sheet.Cell(currentRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            sheet.Range(currentRow, 1, currentRow, reportWidth).Merge();
            currentRow++;
        }

        if (!string.IsNullOrEmpty(report.ParaNo))
        {
            sheet.Cell(currentRow, 1).Value = $"(See Para {report.ParaNo})";
            sheet.Cell(currentRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            sheet.Range(currentRow, 1, currentRow, reportWidth).Merge();
            currentRow++;
        }

        if (!string.IsNullOrEmpty(report.UnitNote))
        {
            sheet.Cell(currentRow, 1).Value = report.UnitNote;
            sheet.Cell(currentRow, 1).Style.Font.Bold = true;
            sheet.Cell(currentRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            sheet.Range(currentRow, 1, currentRow, reportWidth).Merge();
            currentRow++;
        }

        currentRow++;

        if (report.Sections is { Count: > 0 })
        {
            foreach (var reportSection in report.Sections)
            {
                RenderSectionHeading(sheet, ref currentRow, reportWidth, reportSection.Title, reportSection.ParaNo, reportSection.UnitNote);
                RenderTable(sheet, ref currentRow, reportSection.Columns, reportSection.ColumnGroups, reportSection.Groups,
                    reportSection.RightAlignedColumns, reportSection.CenterAlignedColumns);
                currentRow++;
            }
        }
        else
        {
            RenderTable(sheet, ref currentRow, report.Columns, report.ColumnGroups, report.Groups,
                report.RightAlignedColumns, report.CenterAlignedColumns);
        }

        if (!string.IsNullOrEmpty(report.Remarks))
        {
            currentRow++;
            sheet.Cell(currentRow, 1).Value = report.Remarks;
            sheet.Cell(currentRow, 1).Style.Font.Italic = true;
            sheet.Range(currentRow, 1, currentRow, reportWidth).Merge();
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>Renders one Sections[] entry's own bold/underlined heading + optional
    /// "(See Para X)"/unit-note lines directly above its table - added 2026-09-18 for Appendix
    /// III's three-table circular export (CNA balances / SNA balances / exempted-schemes list
    /// under one shared Title).</summary>
    private static void RenderSectionHeading(IXLWorksheet sheet, ref int currentRow, int reportWidth, string title, string? paraNo, string? unitNote)
    {
        var titleCell = sheet.Cell(currentRow, 1);
        titleCell.Value = title;
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.Underline = XLFontUnderlineValues.Single;
        titleCell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        sheet.Range(currentRow, 1, currentRow, reportWidth).Merge();
        currentRow++;

        if (!string.IsNullOrEmpty(paraNo))
        {
            var cell = sheet.Cell(currentRow, 1);
            cell.Value = $"(See Para {paraNo})";
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            sheet.Range(currentRow, 1, currentRow, reportWidth).Merge();
            currentRow++;
        }

        if (!string.IsNullOrEmpty(unitNote))
        {
            var cell = sheet.Cell(currentRow, 1);
            cell.Value = unitNote;
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            sheet.Range(currentRow, 1, currentRow, reportWidth).Merge();
            currentRow++;
        }
    }

    /// <summary>Builds and populates one table (either the report's single top-level table, or one
    /// Sections[] entry's own table) - factored out of Generate() 2026-09-18 so both call sites
    /// share identical header-group/alignment/total-row/border logic.</summary>
    private static void RenderTable(IXLWorksheet sheet, ref int currentRow, List<string> columns,
        List<ReportColumnGroup>? columnGroups, List<ReportGroup> groups,
        List<int>? rightAlignedColumns, List<int>? centerAlignedColumns)
    {
        // Optional grouped/merged header row (e.g. "Revenue" spanning BE/RE/Actuals/Actuals-Upto-
        // Sept) rendered above the plain Columns header row below it. A column not covered by any
        // group (ColumnGroups entry with an empty Label) instead gets ITS OWN cell vertically
        // merged across both header rows, showing its Columns[] text directly - same shape as a
        // native HTML <th rowspan="2"> next to a <th colspan="4">.
        var headerRow = currentRow;
        if (columnGroups is { Count: > 0 })
        {
            var col = 1;
            foreach (var group in columnGroups)
            {
                if (string.IsNullOrEmpty(group.Label))
                {
                    var cell = sheet.Cell(headerRow, col);
                    cell.Value = columns[col - 1];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                    cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    cell.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                    sheet.Range(headerRow, col, headerRow + 1, col).Merge();
                }
                else
                {
                    var cell = sheet.Cell(headerRow, col);
                    cell.Value = group.Label;
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                    cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    sheet.Range(headerRow, col, headerRow, col + group.ColumnSpan - 1).Merge();
                }
                col += group.ColumnSpan;
            }
            currentRow++;
        }

        var columnsHeaderRow = currentRow;
        var skipVerticallyMerged = columnGroups is { Count: > 0 }
            ? BuildVerticalMergeSet(columnGroups)
            : new HashSet<int>();
        for (var i = 0; i < columns.Count; i++)
        {
            if (skipVerticallyMerged.Contains(i)) continue;

            var cell = sheet.Cell(columnsHeaderRow, i + 1);
            cell.Value = columns[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            // Added 2026-09-21: long column headers otherwise overflow into the next cell instead
            // of wrapping onto multiple lines within their own (often narrow) column width.
            cell.Style.Alignment.WrapText = true;
            if (centerAlignedColumns?.Contains(i) == true)
            {
                cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }
        }
        currentRow++;

        foreach (var group in groups)
        {
            if (!string.IsNullOrEmpty(group.Label))
            {
                var cell = sheet.Cell(currentRow, 1);
                cell.Value = group.Label;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");
                sheet.Range(currentRow, 1, currentRow, columns.Count).Merge();
                currentRow++;
            }

            foreach (var dataRow in group.Rows)
            {
                for (var i = 0; i < dataRow.Length; i++)
                {
                    var cell = sheet.Cell(currentRow, i + 1);
                    // Client requirement 2026-09-18: a null cell means "no data for this
                    // combination" (e.g. Appendix I-A's proposed-year row has no RE figures at
                    // all) rather than a genuine computed zero - Excel renders it as "0.00" (still
                    // a usable number for anyone recalculating in the sheet), while PdfReportGenerator
                    // renders the same null as "..." instead. A caller that wants a literal blank
                    // cell must still pass "" explicitly, not null.
                    cell.Value = dataRow[i] ?? "0.00";
                    // Client requirement 2026-09-17: numeric figures must be right-aligned even on
                    // a column whose header is centered - RightAlignedColumns wins over
                    // CenterAlignedColumns for DATA cells only; the header row above still centers
                    // per CenterAlignedColumns, unaffected by this.
                    if (rightAlignedColumns?.Contains(i) == true)
                    {
                        cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                    }
                    else if (centerAlignedColumns?.Contains(i) == true)
                    {
                        cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }
                }
                currentRow++;
            }

            foreach (var total in group.Totals)
            {
                for (var i = 0; i < total.Cells.Length; i++)
                {
                    var cell = sheet.Cell(currentRow, i + 1);
                    cell.Value = total.Cells[i] ?? "0.00";
                    cell.Style.Font.Bold = true;
                    cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    if (rightAlignedColumns?.Contains(i) == true)
                    {
                        cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                    }
                    else if (centerAlignedColumns?.Contains(i) == true)
                    {
                        cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }
                }
                currentRow++;
            }
        }

        var usedRange = sheet.Range(headerRow, 1, currentRow - 1, columns.Count);
        usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
    }

    /// <summary>0-based Columns[] indexes that are covered by a labeled (real) column group -
    /// the complement of these are the indexes already fully rendered by the vertical-merge branch
    /// above and must NOT get a second, duplicate cell written in the plain Columns header row.</summary>
    private static HashSet<int> BuildVerticalMergeSet(List<ReportColumnGroup> columnGroups)
    {
        var skip = new HashSet<int>();
        var col = 0;
        foreach (var group in columnGroups)
        {
            if (string.IsNullOrEmpty(group.Label))
            {
                skip.Add(col);
            }
            col += group.ColumnSpan;
        }
        return skip;
    }

    /// <summary>Excel sheet names can't exceed 31 chars or contain \ / ? * [ ] :.</summary>
    private static string SanitizeSheetName(string title)
    {
        var invalidChars = new[] { '\\', '/', '?', '*', '[', ']', ':' };
        var sanitized = new string(title.Select(c => invalidChars.Contains(c) ? '-' : c).ToArray());
        return sanitized.Length > 31 ? sanitized[..31] : (sanitized.Length == 0 ? "Report" : sanitized);
    }
}
