namespace UBIS.Services.Reporting.Infrastructure.Services;

using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using UBIS.Services.Reporting.Application.Interfaces;
using UBIS.Services.Reporting.Domain.Entities;

/// <summary>
/// Renders a TabularReport to PDF via MigraDoc (built on PdfSharp) — MIT-licensed, no revenue-based
/// restrictions, deliberately chosen over QuestPDF/iText for a government system reused across
/// 50+ future pages where a usage-based commercial license would be a real compliance risk.
/// </summary>
public class PdfReportGenerator : IReportGenerator
{
    public string ContentType => "application/pdf";

    public string FileExtension => "pdf";

    public byte[] Generate(TabularReport report)
    {
        report.EnsureValid();

        var document = new Document();
        document.Info.Title = report.Title;

        var style = document.Styles["Normal"]!;
        style.Font.Name = "Verdana";
        style.Font.Size = 8;

        var section = document.AddSection();
        section.PageSetup.Orientation = Orientation.Landscape;
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.LeftMargin = Unit.FromCentimeter(1);
        section.PageSetup.RightMargin = Unit.FromCentimeter(1);
        section.PageSetup.TopMargin = Unit.FromCentimeter(1);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(1);

        if (report.TitleBoxLines is { Count: > 0 })
        {
            RenderTitleBox(section, report.Title, report.TitleBoxLines);
        }
        else
        {
            var title = section.AddParagraph(report.Title);
            title.Format.Font.Size = 14;
            title.Format.Font.Bold = true;
            title.Format.Alignment = ParagraphAlignment.Center;
            title.Format.SpaceAfter = Unit.FromPoint(2);
        }

        if (!string.IsNullOrEmpty(report.Subtitle))
        {
            var subtitle = section.AddParagraph(report.Subtitle);
            subtitle.Format.Font.Size = 9;
            subtitle.Format.Font.Italic = true;
            subtitle.Format.Alignment = ParagraphAlignment.Center;
            subtitle.Format.SpaceAfter = Unit.FromPoint(8);
        }

        if (!string.IsNullOrEmpty(report.ParaNo))
        {
            var paraNote = section.AddParagraph($"(See Para {report.ParaNo})");
            paraNote.Format.Font.Size = 9;
            paraNote.Format.Alignment = ParagraphAlignment.Center;
            paraNote.Format.SpaceAfter = Unit.FromPoint(8);
        }

        if (!string.IsNullOrEmpty(report.UnitNote))
        {
            var unitNote = section.AddParagraph(report.UnitNote);
            unitNote.Format.Font.Size = 9;
            unitNote.Format.Font.Bold = true;
            unitNote.Format.Alignment = ParagraphAlignment.Right;
            unitNote.Format.SpaceAfter = Unit.FromPoint(4);
        }

        if (report.Sections is { Count: > 0 })
        {
            foreach (var reportSection in report.Sections)
            {
                RenderSectionHeading(section, reportSection.Title, reportSection.ParaNo, reportSection.UnitNote);
                RenderTable(section, reportSection.Columns, reportSection.ColumnGroups, reportSection.Groups,
                    reportSection.RightAlignedColumns, reportSection.CenterAlignedColumns);
            }
        }
        else
        {
            RenderTable(section, report.Columns, report.ColumnGroups, report.Groups,
                report.RightAlignedColumns, report.CenterAlignedColumns);
        }

        if (!string.IsNullOrEmpty(report.Remarks))
        {
            var remarks = section.AddParagraph(report.Remarks);
            remarks.Format.Font.Size = 8;
            remarks.Format.Font.Italic = true;
            remarks.Format.SpaceBefore = Unit.FromPoint(8);
        }

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();

        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream);
        return stream.ToArray();
    }

    /// <summary>Renders Title + TitleBoxLines as a bordered, single-column, bold-centered box -
    /// added 2026-09-18 for Appendix II's circular format ("{DemandNo}. {DemandName}" / "Appendix
    /// II" / "{Appendix full name}", each its own bordered row) in place of the plain unbordered
    /// Title paragraph every other caller gets.</summary>
    private static void RenderTitleBox(MigraDoc.DocumentObjectModel.Section section, string title, List<string> boxLines)
    {
        var table = section.AddTable();
        table.Borders.Width = 0.5;
        table.Borders.Color = Colors.Black;
        table.Format.Font.Size = 11;
        table.Format.SpaceAfter = Unit.FromPoint(8);
        table.AddColumn(Unit.FromCentimeter(27.7));

        foreach (var line in new[] { title }.Concat(boxLines))
        {
            var row = table.AddRow();
            row.Format.Font.Bold = true;
            row.Format.Alignment = ParagraphAlignment.Center;
            row.Cells[0].AddParagraph(line);
        }
    }

    /// <summary>Renders one Sections[] entry's own bold heading + optional "(See Para X)"/unit-note
    /// lines directly above its table - added 2026-09-18 for Appendix III's three-table circular
    /// export (CNA balances / SNA balances / exempted-schemes list under one shared Title).</summary>
    private static void RenderSectionHeading(MigraDoc.DocumentObjectModel.Section section, string title, string? paraNo, string? unitNote)
    {
        var heading = section.AddParagraph(title);
        heading.Format.Font.Size = 10;
        heading.Format.Font.Bold = true;
        heading.Format.Font.Underline = Underline.Single;
        heading.Format.Alignment = ParagraphAlignment.Center;
        heading.Format.SpaceBefore = Unit.FromPoint(10);
        heading.Format.SpaceAfter = Unit.FromPoint(4);

        if (!string.IsNullOrEmpty(paraNo))
        {
            var paraNote = section.AddParagraph($"(See Para {paraNo})");
            paraNote.Format.Font.Size = 9;
            paraNote.Format.Alignment = ParagraphAlignment.Center;
            paraNote.Format.SpaceAfter = Unit.FromPoint(4);
        }

        if (!string.IsNullOrEmpty(unitNote))
        {
            var unitNoteParagraph = section.AddParagraph(unitNote);
            unitNoteParagraph.Format.Font.Size = 9;
            unitNoteParagraph.Format.Font.Bold = true;
            unitNoteParagraph.Format.Alignment = ParagraphAlignment.Right;
            unitNoteParagraph.Format.SpaceAfter = Unit.FromPoint(2);
        }
    }

    /// <summary>Builds and populates one table (either the report's single top-level table, or one
    /// Sections[] entry's own table) - factored out of Generate() 2026-09-18 so both call sites
    /// share identical header-group/alignment/total-row logic.</summary>
    private static void RenderTable(MigraDoc.DocumentObjectModel.Section section, List<string> columns,
        List<ReportColumnGroup>? columnGroups, List<ReportGroup> groups,
        List<int>? rightAlignedColumns, List<int>? centerAlignedColumns)
    {
        var table = section.AddTable();
        table.Borders.Width = 0.4;
        table.Borders.Color = Colors.Gray;
        table.Format.Font.Size = 7.5;

        // Usable width of a landscape A4 page with 1cm margins on both sides: 29.7 - 2 = 27.7cm.
        var columnWidth = Unit.FromCentimeter(27.7 / columns.Count);
        foreach (var _ in columns)
        {
            table.AddColumn(columnWidth);
        }

        // Optional grouped/merged header row above the plain Columns header row - same shape as
        // ExcelReportGenerator's equivalent block: a column not covered by any labeled group
        // vertically spans both header rows instead (MigraDoc has no native rowspan, so this is
        // done by leaving that column's cell in the group row empty/merged-down into the row
        // below via MergeDown, with its actual text placed on the group row itself).
        if (columnGroups is { Count: > 0 })
        {
            var groupHeaderRow = table.AddRow();
            groupHeaderRow.Shading.Color = Colors.LightGray;
            groupHeaderRow.Format.Font.Bold = true;
            groupHeaderRow.HeadingFormat = true;

            var col = 0;
            foreach (var group in columnGroups)
            {
                if (string.IsNullOrEmpty(group.Label))
                {
                    var cell = groupHeaderRow.Cells[col];
                    cell.AddParagraph(columns[col]);
                    cell.Format.Alignment = ParagraphAlignment.Center;
                    cell.VerticalAlignment = MigraDoc.DocumentObjectModel.Tables.VerticalAlignment.Center;
                    cell.MergeDown = 1;
                }
                else
                {
                    var cell = groupHeaderRow.Cells[col];
                    cell.AddParagraph(group.Label);
                    cell.Format.Alignment = ParagraphAlignment.Center;
                    cell.MergeRight = group.ColumnSpan - 1;
                }
                col += group.ColumnSpan;
            }
        }

        var headerRow = table.AddRow();
        headerRow.Shading.Color = Colors.LightGray;
        headerRow.Format.Font.Bold = true;
        headerRow.HeadingFormat = true;
        var skipVerticallyMerged = columnGroups is { Count: > 0 }
            ? BuildVerticalMergeSet(columnGroups)
            : new HashSet<int>();
        for (var i = 0; i < columns.Count; i++)
        {
            if (skipVerticallyMerged.Contains(i)) continue;
            var headerCell = headerRow.Cells[i];
            headerCell.AddParagraph(columns[i]);
            if (centerAlignedColumns?.Contains(i) == true)
            {
                headerCell.Format.Alignment = ParagraphAlignment.Center;
            }
        }

        foreach (var group in groups)
        {
            if (!string.IsNullOrEmpty(group.Label))
            {
                var groupRow = table.AddRow();
                groupRow.Format.Font.Bold = true;
                groupRow.Shading.Color = Colors.WhiteSmoke;
                groupRow.Cells[0].MergeRight = columns.Count - 1;
                groupRow.Cells[0].AddParagraph(group.Label);
            }

            foreach (var dataRow in group.Rows)
            {
                var row = table.AddRow();
                for (var i = 0; i < dataRow.Length; i++)
                {
                    var cell = row.Cells[i];
                    // Client requirement 2026-09-18: a null cell means "no data for this
                    // combination" (e.g. Appendix I-A's proposed-year row has no RE figures at
                    // all) rather than a genuine computed zero - PDF renders it as "..." while
                    // ExcelReportGenerator renders the same null as "0.00" instead. A caller that
                    // wants a literal blank cell must still pass "" explicitly, not null.
                    cell.AddParagraph(dataRow[i] ?? "...");
                    // Client requirement 2026-09-17: numeric figures must be right-aligned even on
                    // a column whose header is centered (e.g. Appendix I/I-A's BE/RE/Total columns)
                    // - RightAlignedColumns wins over CenterAlignedColumns for DATA cells only;
                    // header cells below still center per CenterAlignedColumns, unaffected by this.
                    if (rightAlignedColumns?.Contains(i) == true)
                    {
                        cell.Format.Alignment = ParagraphAlignment.Right;
                    }
                    else if (centerAlignedColumns?.Contains(i) == true)
                    {
                        cell.Format.Alignment = ParagraphAlignment.Center;
                    }
                }
            }

            foreach (var total in group.Totals)
            {
                var row = table.AddRow();
                row.Format.Font.Bold = true;
                row.Borders.Top.Width = 0.75;
                for (var i = 0; i < total.Cells.Length; i++)
                {
                    var cell = row.Cells[i];
                    cell.AddParagraph(total.Cells[i] ?? "...");
                    if (rightAlignedColumns?.Contains(i) == true)
                    {
                        cell.Format.Alignment = ParagraphAlignment.Right;
                    }
                    else if (centerAlignedColumns?.Contains(i) == true)
                    {
                        cell.Format.Alignment = ParagraphAlignment.Center;
                    }
                }
            }
        }
    }

    /// <summary>0-based Columns[] indexes covered by a labeled (real) column group - the
    /// complement is already fully rendered by the vertical-merge group-row cells above and must
    /// NOT get a second, duplicate cell written in the plain Columns header row.</summary>
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
}
