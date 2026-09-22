namespace UBIS.Services.Sbe.Domain.Entities;

/// <summary>
/// Maps dbo.M_MajorHead (shared with PreBudget/Shared.ReferenceData). Kept as SBE's own
/// read-only copy for server-side joins/validation within SBE's own WebApi (matching ECL's
/// established per-microservice-owns-its-read-access convention) — UBIS_Web's new
/// IReferenceDataClient is a separate, presentation-layer concern for dropdowns that don't need
/// to round-trip through SBE at all. Development Head = Major Head directly (confirmed
/// 2026-08-25 via BIMSDemo's view_sbedatadryrun5 definition) — no separate mapping needed for
/// Part-B.
/// </summary>
public class SbeMajorHead
{
    public int MajorHeadId { get; set; }
    public string MajorHeadCode { get; set; } = string.Empty;
    public string MajorHeadName { get; set; } = string.Empty;
    public string? HMajorHeadName { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
}
