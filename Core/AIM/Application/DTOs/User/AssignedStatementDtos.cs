namespace UBIS.Services.Aim.Application.DTOs.User;

/// <summary>One Statement/"Profile" (M_StmtControl row) the caller owns or is assigned to. Added 2026-07.</summary>
public class AssignedStatementDto
{
    public int StmtId { get; set; }

    public string? StmtNo { get; set; }

    public string? StmtName { get; set; }
}

/// <summary>Wrapper for GET /api/users/assigned-statements. Added 2026-07.</summary>
public class AssignedStatementsDto
{
    public List<AssignedStatementDto> Statements { get; set; } = new();
}
