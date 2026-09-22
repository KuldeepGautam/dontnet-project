namespace UBIS.Services.Aim.Application.Interfaces;

using UBIS.Services.Aim.Domain.Entities.Legacy;

/// <summary>
/// Resolves which <c>M_SBEDemand</c> rows a legacy user is granted access to — either through
/// explicit <c>UserDetails</c> ("Profile Access") mapping, or, when the user is a Statement
/// Owner (<c>M_StmtControl.StmtUserId</c>/<c>StmtUserId1</c>) or has an explicit non-owner
/// Statement/"Profile" assignment (<c>dbo.UserId_StmtId_Mapping</c>, added 2026-07), through
/// automatic cascading access to every Demand row sharing that Statement's financial year.
/// Added 2026-07-10.
/// </summary>
public interface IStatementAccessService
{
    /// <summary>
    /// True if <paramref name="legacyUserId"/> owns (or co-owns) any Statement for
    /// <paramref name="financialYear"/>.
    /// </summary>
    Task<bool> IsStatementOwnerAsync(int legacyUserId, string financialYear, CancellationToken ct = default);

    /// <summary>
    /// Returns the Demand rows this user may access for <paramref name="financialYear"/>:
    /// every matching <c>M_SBEDemand</c> row if the user is a Statement Owner or has an explicit
    /// non-owner Statement assignment (bypasses explicit <c>UserDetails</c> mapping entirely),
    /// otherwise only the rows explicitly granted via <c>UserDetails</c>.
    /// </summary>
    Task<IReadOnlyList<SBEDemand>> GetAccessibleDemandsAsync(int legacyUserId, string financialYear, CancellationToken ct = default);

    /// <summary>
    /// Lists the Statements/"Profiles" (<c>M_StmtControl</c> rows) this user is either the Owner
    /// of or explicitly assigned to via <c>UserId_StmtId_Mapping</c>, for display (e.g. a "My
    /// Assigned Statements" list on the Profile page). Added 2026-07.
    /// </summary>
    Task<IReadOnlyList<StmtControl>> GetAssignedStatementsAsync(int legacyUserId, string financialYear, CancellationToken ct = default);
}
