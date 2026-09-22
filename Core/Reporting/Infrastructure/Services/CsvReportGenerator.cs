namespace UBIS.Services.Reporting.Infrastructure.Services;

using System.Text;
using UBIS.Services.Reporting.Application.Interfaces;
using UBIS.Services.Reporting.Domain.Entities;

/// <summary>
/// Renders a TabularReport to .csv - the third IReportGenerator, exactly as anticipated by that
/// interface's own doc comment ("a future CSV export would add a third without touching the other
/// two or the controller"). CSV has no cell-merge concept, so a ColumnGroups group header (e.g.
/// "Revenue" spanning 4 sub-columns) is flattened to a single extra header line repeating each
/// group's label once per spanned column, immediately above the normal Columns header row - the
/// closest a flat text format can get to the PDF/Excel merged-header look without inventing a
/// non-standard CSV convention. A column with no group is left blank on that line, matching how a
/// vertically-merged header has "no second label" of its own.
/// </summary>
public class CsvReportGenerator : IReportGenerator
{
    public string ContentType => "text/csv";

    public string FileExtension => "csv";

    public byte[] Generate(TabularReport report)
    {
        report.EnsureValid();

        var sb = new StringBuilder();

        AppendLine(sb, new[] { report.Title });
        if (report.TitleBoxLines is { Count: > 0 })
        {
            foreach (var line in report.TitleBoxLines)
            {
                AppendLine(sb, new[] { line });
            }
        }
        if (!string.IsNullOrEmpty(report.Subtitle))
        {
            AppendLine(sb, new[] { report.Subtitle });
        }
        if (!string.IsNullOrEmpty(report.ParaNo))
        {
            AppendLine(sb, new[] { $"(See Para {report.ParaNo})" });
        }
        if (!string.IsNullOrEmpty(report.UnitNote))
        {
            AppendLine(sb, new[] { report.UnitNote });
        }
        sb.AppendLine();

        if (report.Sections is { Count: > 0 })
        {
            foreach (var reportSection in report.Sections)
            {
                AppendLine(sb, new[] { reportSection.Title });
                if (!string.IsNullOrEmpty(reportSection.ParaNo))
                {
                    AppendLine(sb, new[] { $"(See Para {reportSection.ParaNo})" });
                }
                if (!string.IsNullOrEmpty(reportSection.UnitNote))
                {
                    AppendLine(sb, new[] { reportSection.UnitNote });
                }
                AppendTable(sb, reportSection.Columns, reportSection.ColumnGroups, reportSection.Groups);
                sb.AppendLine();
            }
        }
        else
        {
            AppendTable(sb, report.Columns, report.ColumnGroups, report.Groups);
        }

        if (!string.IsNullOrEmpty(report.Remarks))
        {
            sb.AppendLine();
            AppendLine(sb, new[] { report.Remarks });
        }

        // UTF-8 BOM so Excel (still the most common opener for a .csv on Windows) detects the
        // encoding correctly instead of mangling the Rupee sign / any other non-ASCII cell text.
        var bom = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[bom.Length + body.Length];
        Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
        Buffer.BlockCopy(body, 0, result, bom.Length, body.Length);
        return result;
    }

    /// <summary>Writes one table's ColumnGroups/Columns header + Groups rows/totals - factored out
    /// of Generate() 2026-09-18 so both the single-table and Sections[] call sites share identical
    /// header-flattening logic.</summary>
    private static void AppendTable(StringBuilder sb, List<string> columns, List<ReportColumnGroup>? columnGroups, List<ReportGroup> groups)
    {
        if (columnGroups is { Count: > 0 })
        {
            var groupHeaderCells = new List<string>();
            foreach (var group in columnGroups)
            {
                groupHeaderCells.Add(group.Label ?? "");
                for (var i = 1; i < group.ColumnSpan; i++)
                {
                    groupHeaderCells.Add("");
                }
            }
            AppendLine(sb, groupHeaderCells);
        }

        AppendLine(sb, columns);

        foreach (var group in groups)
        {
            if (!string.IsNullOrEmpty(group.Label))
            {
                var labelRow = new string[columns.Count];
                labelRow[0] = group.Label;
                for (var i = 1; i < labelRow.Length; i++) labelRow[i] = "";
                AppendLine(sb, labelRow);
            }

            foreach (var row in group.Rows)
            {
                // Client requirement 2026-09-18: a null cell means "no data for this combination"
                // rather than a genuine computed zero - CSV (most commonly reopened in Excel, see
                // this class's own doc comment) renders it as "0.00", matching ExcelReportGenerator.
                AppendLine(sb, row.Select(cell => cell ?? "0.00"));
            }

            foreach (var total in group.Totals)
            {
                AppendLine(sb, total.Cells.Select(cell => cell ?? "0.00"));
            }
        }
    }

    private static void AppendLine(StringBuilder sb, IEnumerable<string> cells)
    {
        sb.AppendLine(string.Join(",", cells.Select(Escape)));
    }

    /// <summary>RFC 4180: wrap in quotes (doubling any embedded quote) whenever a cell contains a
    /// comma, quote, or newline - the three characters that would otherwise corrupt the column
    /// split when the file is reopened.</summary>
    private static string Escape(string? cell)
    {
        cell ??= "";
        if (cell.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
        {
            return cell;
        }
        return "\"" + cell.Replace("\"", "\"\"") + "\"";
    }
}
