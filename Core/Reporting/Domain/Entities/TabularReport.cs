namespace UBIS.Services.Reporting.Domain.Entities;

/// <summary>
/// Generic tabular report model — the one shape every consumer (ECL DataAnalysis today, any of the
/// other 50+ pages later) maps its own data into before asking this service for a PDF or Excel
/// export. Deliberately data-shape-agnostic: a "column" is just a header string, a "row" is just an
/// array of already-formatted cell strings. Formatting/aggregation (currency rounding, which columns
/// to show, how to group) stays the caller's responsibility — this service only lays the result out.
/// </summary>
public class TabularReport
{
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional additional bold, centered lines rendered directly under Title, each in its
    /// own bordered box row (e.g. "Appendix II" / "Quarterly Expenditure Plan Progress (on Gross
    /// Basis)") - added 2026-09-18 for a circular format where the Demand/Appendix/Appendix-name
    /// header is a bordered 3-line block rather than a plain Title + italic Subtitle. When set
    /// (non-empty), Title becomes this box's first row (also bordered) instead of the plain
    /// unbordered heading paragraph every other caller gets; Subtitle/ParaNo/UnitNote still render
    /// below the box exactly as before. Null/empty preserves every existing caller's layout
    /// exactly.</summary>
    public List<string>? TitleBoxLines { get; set; }

    public string? Subtitle { get; set; }

    /// <summary>Optional right-aligned line rendered above the table (e.g. "(₹ in crore)") - added
    /// 2026-09-17 for Appendix I's government-circular export format, which pairs a centered
    /// Title/Subtitle with a separate right-aligned unit annotation. Null/empty renders nothing,
    /// preserving every existing caller's layout exactly.</summary>
    public string? UnitNote { get; set; }

    /// <summary>Optional government-circular paragraph reference (e.g. "1.2") - added 2026-09-17.
    /// Rendered as its own centered "(See Para {ParaNo})" line, independent of (and rendered after)
    /// <see cref="Subtitle"/> so existing callers that already use Subtitle for their own text
    /// (e.g. "Demand: X | Financial Year: Y") are unaffected. Null/empty renders nothing. Every
    /// PreBudget appendix export binds this from dbo.M_Appendix.ParaNo, though it may be blank.</summary>
    public string? ParaNo { get; set; }

    /// <summary>Optional italic note rendered below the table (client requirement 2026-09-17: bound
    /// from dbo.M_Appendix.Remarks for whichever appendix is being exported - every PreBudget
    /// appendix export binds this, though it may be blank). Null/empty renders nothing.</summary>
    public string? Remarks { get; set; }

    /// <summary>Optional second, top-level header row for a merged/grouped header - see
    /// ReportColumnGroup's own doc comment. Null/empty means every existing caller's plain
    /// single-header-row rendering is unchanged.</summary>
    public List<ReportColumnGroup>? ColumnGroups { get; set; }

    public List<string> Columns { get; set; } = new();

    /// <summary>0-based indexes into <see cref="Columns"/> whose data-row cells should be
    /// right-aligned (numeric/amount columns) instead of the default left alignment - added
    /// 2026-09-15 for Appendix VI-F's Rate of Service column. Null/empty preserves every existing
    /// caller's default alignment exactly. Header cells are unaffected (headers always render as
    /// bold, left-aligned column titles regardless of the column's own data alignment) - unless
    /// the same index is also in <see cref="CenterAlignedColumns"/>, in which case the header
    /// centers while the data still right-aligns (client requirement 2026-09-17: every numeric
    /// figure must be right-aligned even when its header is centered under a grouped heading like
    /// Appendix I/I-A's Revenue/Capital/Total - RightAlignedColumns always wins over
    /// CenterAlignedColumns for DATA cells specifically).</summary>
    public List<int>? RightAlignedColumns { get; set; }

    /// <summary>0-based indexes into <see cref="Columns"/> whose header AND data-row cells should
    /// be center-aligned instead of the default left alignment - added 2026-09-17 for Appendix I's
    /// government-circular export format (e.g. its Year column, whose value is text-ish rather
    /// than a currency figure). For a numeric/amount column, list the index in BOTH this AND
    /// <see cref="RightAlignedColumns"/> instead: the header centers under its grouped heading
    /// (Revenue/Capital/Total) while the actual figure still right-aligns - RightAlignedColumns
    /// always wins over this one for DATA cells specifically (client requirement 2026-09-17: every
    /// numeric figure must be right-aligned). Null/empty preserves every existing caller's default
    /// alignment.</summary>
    public List<int>? CenterAlignedColumns { get; set; }

    public List<ReportGroup> Groups { get; set; } = new();

    /// <summary>Optional independent sub-tables, each with its own heading and column set - added
    /// 2026-09-18 for Appendix III's government-circular export, which is three genuinely different
    /// tables (CNA balances / SNA balances / exempted-schemes list, each with its own columns) under
    /// one shared Title/Subtitle/ParaNo. When set (non-empty), the top-level Columns/ColumnGroups/
    /// Groups/RightAlignedColumns/CenterAlignedColumns are ignored by every generator - populate
    /// either the top-level single-table fields OR Sections, never both.</summary>
    public List<ReportSection>? Sections { get; set; }

    /// <summary>Every row/total-row cell array must have exactly Columns.Count entries — checked once here rather than trusted at every render site in both generators.</summary>
    public void EnsureValid()
    {
        if (Sections is { Count: > 0 })
        {
            foreach (var section in Sections)
            {
                section.EnsureValid();
            }
            return;
        }

        if (Columns.Count == 0)
        {
            throw new InvalidOperationException("A report needs at least one column.");
        }

        if (ColumnGroups is { Count: > 0 })
        {
            var spanned = ColumnGroups.Sum(g => g.ColumnSpan);
            if (spanned != Columns.Count)
            {
                throw new InvalidOperationException($"ColumnGroups' spans ({spanned}) must add up to Columns.Count ({Columns.Count}).");
            }
        }

        foreach (var group in Groups)
        {
            foreach (var row in group.Rows)
            {
                if (row.Length != Columns.Count)
                {
                    throw new InvalidOperationException($"Row cell count ({row.Length}) does not match column count ({Columns.Count}).");
                }
            }

            foreach (var total in group.Totals)
            {
                if (total.Cells.Length != Columns.Count)
                {
                    throw new InvalidOperationException($"Total row cell count ({total.Cells.Length}) does not match column count ({Columns.Count}).");
                }
            }
        }
    }
}

/// <summary>One independent sub-table under a report's shared Title/Subtitle/top-level ParaNo - see
/// TabularReport.Sections' own doc comment. Title renders as a bold heading directly above this
/// section's own column header row; ParaNo/UnitNote are this section's own optional
/// "(See Para X)"/"(₹ in crore)"-style lines, independent of the report's top-level ones.</summary>
public class ReportSection
{
    public string Title { get; set; } = string.Empty;

    public string? ParaNo { get; set; }

    public string? UnitNote { get; set; }

    public List<ReportColumnGroup>? ColumnGroups { get; set; }

    public List<string> Columns { get; set; } = new();

    public List<int>? RightAlignedColumns { get; set; }

    public List<int>? CenterAlignedColumns { get; set; }

    public List<ReportGroup> Groups { get; set; } = new();

    public void EnsureValid()
    {
        if (Columns.Count == 0)
        {
            throw new InvalidOperationException($"Section '{Title}' needs at least one column.");
        }

        if (ColumnGroups is { Count: > 0 })
        {
            var spanned = ColumnGroups.Sum(g => g.ColumnSpan);
            if (spanned != Columns.Count)
            {
                throw new InvalidOperationException($"Section '{Title}': ColumnGroups' spans ({spanned}) must add up to Columns.Count ({Columns.Count}).");
            }
        }

        foreach (var group in Groups)
        {
            foreach (var row in group.Rows)
            {
                if (row.Length != Columns.Count)
                {
                    throw new InvalidOperationException($"Section '{Title}': row cell count ({row.Length}) does not match column count ({Columns.Count}).");
                }
            }

            foreach (var total in group.Totals)
            {
                if (total.Cells.Length != Columns.Count)
                {
                    throw new InvalidOperationException($"Section '{Title}': total row cell count ({total.Cells.Length}) does not match column count ({Columns.Count}).");
                }
            }
        }
    }
}

/// <summary>One entry in a report's optional grouped/merged header row (e.g. "Revenue" spanning 4
/// sub-columns of BE/RE/Actuals/Actuals-Upto-Sept). Label empty/null = this column has no group -
/// it renders as one cell spanning both header rows vertically, showing its own Columns[] text
/// instead (matching a native HTML rowspan="2" header, e.g. "S.No."/"Year" next to a grouped
/// "Revenue"/"Capital").</summary>
public class ReportColumnGroup
{
    public string? Label { get; set; }

    public int ColumnSpan { get; set; } = 1;
}

/// <summary>One logical group of rows (e.g. one Demand's schemes). Label is rendered as a full-width
/// header row when non-empty; leave it empty for a flat, ungrouped table. A cell in
/// <see cref="Rows"/> is typed non-nullable, but a caller may still put a runtime null there
/// (<c>null!</c>) to mean "no data for this combination" (e.g. Appendix I-A's proposed-year row has
/// no RE figures at all) as distinct from a genuine computed zero - added 2026-09-18. Both
/// generators render a null cell as a visible placeholder rather than leaving it truly blank: the
/// Excel/CSV generators render "0.00" (still a usable number for anyone recalculating in the
/// sheet), the PDF generator renders "...". A caller that wants a literal blank cell (not this
/// placeholder) must still pass "" explicitly, not null.</summary>
public class ReportGroup
{
    public string? Label { get; set; }

    public List<string[]> Rows { get; set; } = new();

    public List<ReportTotalRow> Totals { get; set; } = new();
}

/// <summary>
/// A subtotal/total row rendered after a group's data rows, bolded. Shaped exactly like a data row
/// (Cells.Length must equal Columns.Count) — put the row's label (e.g. "Total Outlay") in Cells[0]
/// the same way a normal row would, rather than as a separate field, so callers don't have to
/// decide whether the label is redundant with Cells[0] or not. A cell may be null - see
/// <see cref="ReportGroup.Rows"/>'s own doc comment for the null/blank-cell convention.
/// </summary>
public class ReportTotalRow
{
    public string[] Cells { get; set; } = Array.Empty<string>();
}
