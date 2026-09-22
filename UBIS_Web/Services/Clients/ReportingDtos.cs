namespace UBIS.Web.Services.Clients;

/// <summary>Client-side mirror of Core/Reporting's wire contract (Application/DTOs/TabularReportDto.cs). Generic — no knowledge of ECL/PreBudget/any other domain.</summary>
public class TabularReportDto
{
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional additional bold, centered, bordered lines rendered directly under Title,
    /// each its own bordered box row (e.g. "Appendix II" / "Quarterly Expenditure Plan Progress (on
    /// Gross Basis)") - added 2026-09-18 for a circular format where the Demand/Appendix/Appendix-
    /// name header is a bordered 3-line block. Null/empty preserves the plain unbordered Title.</summary>
    public List<string>? TitleBoxLines { get; set; }

    public string? Subtitle { get; set; }

    /// <summary>Optional right-aligned line rendered above the table (e.g. "(₹ in crore)").</summary>
    public string? UnitNote { get; set; }

    /// <summary>Optional government-circular paragraph reference (e.g. "1.2"), rendered as its own
    /// "(See Para {ParaNo})" line.</summary>
    public string? ParaNo { get; set; }

    /// <summary>Optional italic note rendered below the table.</summary>
    public string? Remarks { get; set; }

    /// <summary>Optional second, top-level header row for a merged/grouped header (e.g. "Revenue"
    /// spanning 4 sub-columns of Columns) - mirrors Core/Reporting's own ColumnGroups. Null/empty
    /// keeps the plain single-header-row rendering. A column with no group (Label empty/null)
    /// renders as one cell spanning both header rows vertically, using its own Columns[] text.</summary>
    public List<ReportColumnGroupDto>? ColumnGroups { get; set; }

    public List<string> Columns { get; set; } = new();

    /// <summary>0-based indexes into <see cref="Columns"/> whose data-row cells should be
    /// right-aligned (numeric/amount columns) instead of the default left alignment.</summary>
    public List<int>? RightAlignedColumns { get; set; }

    /// <summary>0-based indexes into <see cref="Columns"/> whose header AND data-row cells should
    /// be center-aligned instead of the default left alignment.</summary>
    public List<int>? CenterAlignedColumns { get; set; }

    public List<ReportGroupDto> Groups { get; set; } = new();

    /// <summary>Optional independent sub-tables, each with its own heading and column set - added
    /// 2026-09-18 for Appendix III's government-circular export (CNA balances / SNA balances /
    /// exempted-schemes list, each with different columns, under one shared Title/Subtitle/ParaNo).
    /// When set (non-empty), the top-level Columns/ColumnGroups/Groups/RightAlignedColumns/
    /// CenterAlignedColumns are ignored - populate either the single-table fields OR Sections, never
    /// both.</summary>
    public List<ReportSectionDto>? Sections { get; set; }

    public string FileName { get; set; } = "report";
}

/// <summary>One independent sub-table under a report's shared Title/Subtitle/top-level ParaNo -
/// see TabularReportDto.Sections' own doc comment.</summary>
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
    public string? Label { get; set; }

    public int ColumnSpan { get; set; } = 1;
}

public class ReportGroupDto
{
    public string? Label { get; set; }

    /// <summary>A cell is typed non-nullable, but may still be a runtime null (<c>null!</c>) to mean
    /// "no data for this combination" (e.g. Appendix I-A's proposed-year row has no RE figures at
    /// all) as distinct from a genuine computed zero - added 2026-09-18. The Reporting service
    /// renders a null cell as "0.00" in Excel/CSV and "..." in PDF. A literal blank cell (not this
    /// placeholder) must still be passed as "" explicitly.</summary>
    public List<string[]> Rows { get; set; } = new();

    public List<ReportTotalRowDto> Totals { get; set; } = new();
}

public class ReportTotalRowDto
{
    public string[] Cells { get; set; } = Array.Empty<string>();
}
