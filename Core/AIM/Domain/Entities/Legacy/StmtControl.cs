namespace UBIS.Services.Aim.Domain.Entities.Legacy;

/// <summary>
/// Read-only pass-through to the pre-existing legacy <c>dbo.M_StmtControl</c> table — controls
/// which Statement(s) a user owns. <see cref="StmtUserId"/>/<see cref="StmtUserId1"/> identify
/// the Statement Owner(s) — the same int identity as <see cref="User.UserId"/> directly; see
/// <see cref="Services.IStatementAccessService"/> for how ownership cascades into
/// <see cref="SBEDemand"/> rows. Added 2026-07-10.
/// </summary>
public class StmtControl
{
    public int StmtId { get; set; }

    public string? FinancialYear { get; set; }

    public string? StmtNo { get; set; }

    public string? StmtName { get; set; }

    public string? HStmtName { get; set; }

    public string? HeaderNoteFlag { get; set; }

    public string? FooterNoteFlag { get; set; }

    public int? LayoutId { get; set; }

    public string? LayoutYear { get; set; }

    public string? Regular_Interim { get; set; }

    public int? No_Column { get; set; }

    public string? Caption1 { get; set; }

    public string? Caption2 { get; set; }

    public string? Caption3 { get; set; }

    public int? DisplaySeqNo { get; set; }

    public string? DisplayFigure { get; set; }

    public string? TemplateName { get; set; }

    public DateTime? Entrydate { get; set; }

    public string? StatementType { get; set; }

    public string? OutputType { get; set; }

    public int? PageNo { get; set; }

    public int? StmtDisplaySeq { get; set; }

    /// <summary>Primary Statement Owner (legacy int UserId).</summary>
    public int? StmtUserId { get; set; }

    /// <summary>Secondary/co-owner Statement Owner (legacy int UserId).</summary>
    public int? StmtUserId1 { get; set; }

    public int? PrevStmtId { get; set; }

    public string? IsSonata { get; set; }

    public string? IsSSRS { get; set; }
}
