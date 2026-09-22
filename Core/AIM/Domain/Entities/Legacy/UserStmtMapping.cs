namespace UBIS.Services.Aim.Domain.Entities.Legacy;

/// <summary>
/// Read/write pass-through to the new <c>dbo.UserId_StmtId_Mapping</c> table (added 2026-07,
/// schema in <c>db-scripts/UserId_StmtId_Mapping.md</c>) — grants a regular (non-owner) user
/// explicit access to one or more Statements/"Profiles". Deliberately separate from
/// <see cref="StmtControl.StmtUserId"/>/<see cref="StmtControl.StmtUserId1"/>, which still mean
/// Statement **Owner** and are left untouched. See <see cref="Services.IStatementAccessService"/>.
/// </summary>
public class UserStmtMapping
{
    public int MappingId { get; set; }

    /// <summary>Legacy int UserId (<c>dbo.Users.UserId</c>) being granted access.</summary>
    public int UserId { get; set; }

    /// <summary>The specific per-financial-year <c>M_StmtControl.StmtId</c> row being assigned.</summary>
    public int StmtId { get; set; }

    /// <summary>"Y"/"N" — soft-delete flag; deactivate a mapping rather than remove the row.</summary>
    public string Active { get; set; } = "Y";

    public string? Remarks { get; set; }

    public int? UserIdCreatedBy { get; set; }

    public DateTime? CreatedOnDate { get; set; }

    public int? UserIdModifyBy { get; set; }

    public DateTime? ModifiedOnDate { get; set; }
}
