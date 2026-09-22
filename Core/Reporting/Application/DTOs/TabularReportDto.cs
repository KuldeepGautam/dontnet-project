namespace UBIS.Services.Reporting.Application.DTOs;

/// <summary>Wire contract for POST api/reports/pdf and api/reports/excel — mirrors Domain.Entities.TabularReport exactly (DTO exists so the WebApi surface doesn't depend on the Domain project's own types directly, matching this solution's Dto/Entity separation convention elsewhere).</summary>
public class TabularReportDto
{
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional additional bold, centered, bordered lines under Title - see
    /// Domain.Entities.TabularReport.TitleBoxLines.</summary>
    public List<string>? TitleBoxLines { get; set; }

    public string? Subtitle { get; set; }

    /// <summary>Optional right-aligned line rendered above the table (e.g. "(₹ in crore)") - see
    /// Domain.Entities.TabularReport.UnitNote.</summary>
    public string? UnitNote { get; set; }

    /// <summary>Optional government-circular paragraph reference (e.g. "1.2") - see
    /// Domain.Entities.TabularReport.ParaNo.</summary>
    public string? ParaNo { get; set; }

    /// <summary>Optional italic note rendered below the table - see
    /// Domain.Entities.TabularReport.Remarks.</summary>
    public string? Remarks { get; set; }

    /// <summary>Optional second, top-level header row for a merged/grouped header (e.g. "Revenue"
    /// spanning the BE/RE/Actuals/Actuals-Upto-Sept sub-columns of <see cref="Columns"/>) — a real
    /// column-group, unrelated to <see cref="ReportGroupDto.Label"/>'s full-width ROW-grouping
    /// header. Null/empty preserves every existing caller's single-header-row behavior exactly.
    /// Every entry's <see cref="ReportColumnGroupDto.ColumnSpan"/> must sum to Columns.Count across
    /// the whole list; a column not covered by any group renders as a single cell spanning both
    /// header rows vertically instead (e.g. "S.No."/"Year" next to a grouped "Revenue"/"Capital").</summary>
    public List<ReportColumnGroupDto>? ColumnGroups { get; set; }

    public List<string> Columns { get; set; } = new();

    /// <summary>0-based indexes into <see cref="Columns"/> whose data-row cells should be
    /// right-aligned (numeric/amount columns) instead of the default left alignment. Null/empty
    /// preserves every existing caller's default alignment exactly.</summary>
    public List<int>? RightAlignedColumns { get; set; }

    /// <summary>0-based indexes into <see cref="Columns"/> whose header AND data-row cells should
    /// be center-aligned - see Domain.Entities.TabularReport.CenterAlignedColumns.</summary>
    public List<int>? CenterAlignedColumns { get; set; }

    public List<ReportGroupDto> Groups { get; set; } = new();

    /// <summary>Optional independent sub-tables, each with its own heading and column set - see
    /// Domain.Entities.TabularReport.Sections. When set (non-empty), the top-level Columns/
    /// ColumnGroups/Groups/RightAlignedColumns/CenterAlignedColumns are ignored.</summary>
    public List<ReportSectionDto>? Sections { get; set; }

    /// <summary>File name to suggest to the browser (without extension — each export endpoint appends .pdf/.xlsx).</summary>
    public string FileName { get; set; } = "report";
}

/// <summary>One independent sub-table under a report's shared Title/Subtitle/top-level ParaNo -
/// see Domain.Entities.ReportSection.</summary>
public class ReportSectionDto
{
    public string Title { get; set; } = string.Empty;

    public string? ParaNo { get; set; }

    public string? UnitNote { get; set; }

    public List<ReportColumnGroupDto>? ColumnGroups { get; set; }

    public List<string> Columns { get; set; } = new();

    public List<int>? RightAlignedColumns { get; set; }

    public List<int>? CenterAlignedColumns { get; set; }

    public List<ReportGroupDto> Groups { get; set; } = new();
}

public class ReportColumnGroupDto
{
    /// <summary>Empty/null = this column has no group header - it renders as a single cell
    /// spanning both header rows vertically, using its own Columns[] text.</summary>
    public string? Label { get; set; }

    public int ColumnSpan { get; set; } = 1;
}

public class ReportGroupDto
{
    public string? Label { get; set; }

    /// <summary>A cell may still be a runtime null (<c>null!</c>) to mean "no data for this
    /// combination" as distinct from a genuine computed zero - see Domain.Entities.ReportGroup.Rows'
    /// own doc comment for the per-format rendering convention.</summary>
    public List<string[]> Rows { get; set; } = new();

    public List<ReportTotalRowDto> Totals { get; set; } = new();
}

public class ReportTotalRowDto
{
    public string[] Cells { get; set; } = Array.Empty<string>();
}
