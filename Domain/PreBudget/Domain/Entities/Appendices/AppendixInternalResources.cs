namespace UBIS.Services.PreBudget.Domain.Entities.Appendices;

/// <summary>
/// Appendix VI-D: Available internal resources with Grantee Bodies/Autonomous Institutions. Maps
/// to dbo.AppendixInternalResources (was legacy Temp_VIE_IntResource — its "VIE" name is a leftover
/// naming artifact; its actual columns confirm it's genuinely VI-D data, not VI-E, see design doc
/// §5). NameOfInstitute was reverted to free text 2026-07-30 (was modeled as AutonomousBodyId
/// before any real data existed to reveal the mismatch) but client testing feedback (2026-08-24)
/// explicitly asked for it back as an Autonomous dropdown, same as VI-C/VI-E, per the FRS - re-added
/// AutonomousBodyId as an additive nullable column so existing free-text NameOfInstitute rows aren't
/// broken; NameOfInstitute stays on the entity as a legacy fallback for any pre-existing rows saved
/// before this change.
/// </summary>
public class AppendixInternalResources : AppendixEntityBase
{
    public string? NameOfInstitute { get; set; }
    public int? AutonomousBodyId { get; set; }
    public decimal? AsOnMarch31 { get; set; }
    public decimal? AsOnJune30 { get; set; }
    public decimal? ExpectedNextMarch31 { get; set; }
    public decimal? ExpectedNextFY { get; set; }
    public string? Remarks { get; set; }

    public void UpdateFrom(
        int? autonomousBodyId,
        decimal? asOnMarch31,
        decimal? asOnJune30,
        decimal? expectedNextMarch31,
        decimal? expectedNextFY,
        string? remarks)
    {
        AutonomousBodyId = autonomousBodyId;
        AsOnMarch31 = asOnMarch31;
        AsOnJune30 = asOnJune30;
        ExpectedNextMarch31 = expectedNextMarch31;
        ExpectedNextFY = expectedNextFY;
        Remarks = remarks;
    }
}
