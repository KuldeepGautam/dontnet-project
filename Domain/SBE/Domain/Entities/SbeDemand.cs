namespace UBIS.Services.Sbe.Domain.Entities;

/// <summary>
/// The central freeze/workflow/permission table per Demand+FinancialYear: FR015 Freeze, FR028
/// Actuals/BE/Category edit permissions, FR002's dashboard status counts. Maps the newly-created
/// dbo.M_SBEDemand (Others/publish-staging/sbe-workstream-1-schema.sql). Carries a parallel
/// DDG-freeze sub-state for FR019's import-from-DDG flow, independent of the main Freeze state.
/// Foundation-stage: Freeze()/Unfreeze() cover the FR015 state transition; the full
/// RequestDdgImport() DDG sub-state machine is deferred to Stage 5.
/// </summary>
public class SbeDemand
{
    public int SbeDemandId { get; set; }
    public string? FinancialYear { get; set; }
    public int? DemandId { get; set; }
    public int? UserId { get; set; }
    public string? Status { get; set; }
    public DateTime? EntryDate { get; set; }
    public DateTime? TargetDate { get; set; }
    public DateTime? UnFreezeDate { get; set; }

    /// <summary>'Y'/'N' — main freeze flag (FR015). Distinct from the DDGFreez sub-state below.</summary>
    public string? Freez { get; set; }
    public DateTime? SbeFreezDate { get; set; }
    public int? FreezUserId { get; set; }
    public int? UnFreezeUserId { get; set; }

    /// <summary>'Y'/'N' — whether Actuals are editable for this Demand+FY (Budget Division grants per FR028).</summary>
    public string? ActualEditFlag { get; set; }

    /// <summary>'Y'/'N' — whether BE is editable for this Demand+FY (normally DDG-sourced/read-only otherwise). Checked on every BE write path in Stage 3, not just the UI.</summary>
    public string? BeEditFlag { get; set; }
    public string? ActualEditFlagDdg { get; set; }
    public DateTime? EditTargetDateDdg { get; set; }
    public string? SchemeCatEditFlag { get; set; }
    public string? AllowNegExpFlag { get; set; }
    public string? AllowNegExpFlagDdg { get; set; }
    public string? AllowNegRecFlag { get; set; }
    public DateTime? EditTargetDate { get; set; }

    // DDG-freeze sub-state (FR019 import-from-DDG) — parallel to, and independent of, the main Freeze state above.
    public DateTime? DdgTargetDate { get; set; }
    public string? DdgFreez { get; set; }
    public DateTime? DdgFreezDate { get; set; }
    public string? ConsumedStatus { get; set; }
    public DateTime? DdgUnFreezeDate { get; set; }
    public int? DdgFreezUserId { get; set; }
    public int? DdgUnFreezeUserId { get; set; }

    /// <summary>Freezes this Demand+FY for the given user (FR015). Illegal while already frozen.</summary>
    public void Freeze(int userId)
    {
        if (Freez == "Y")
        {
            throw new InvalidOperationException($"SbeDemand {SbeDemandId} is already frozen.");
        }

        Freez = "Y";
        SbeFreezDate = DateTime.UtcNow;
        FreezUserId = userId;
    }

    /// <summary>Unfreezes this Demand+FY for the given user. Illegal unless currently frozen.</summary>
    public void Unfreeze(int userId)
    {
        if (Freez != "Y")
        {
            throw new InvalidOperationException($"SbeDemand {SbeDemandId} is not currently frozen.");
        }

        Freez = "N";
        UnFreezeDate = DateTime.UtcNow;
        UnFreezeUserId = userId;
    }

    public bool IsFrozen => Freez == "Y";
}
