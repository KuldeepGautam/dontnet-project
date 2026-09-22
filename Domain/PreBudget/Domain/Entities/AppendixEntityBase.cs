namespace UBIS.Services.PreBudget.Domain.Entities;

/// <summary>
/// Common shape shared by every one of the 22 appendix data-entry tables (confirmed against the
/// real legacy BIMSDemo Temp_* schemas — every one carries DemandID/FinancialYear plus its own data
/// columns plus Freez/EntryDate/UserID/IP audit columns). Row-level freeze (decision 4) plus the
/// current-project's richer CreatedBy/ModifiedBy/DeletedBy audit convention (§2.1 of the design
/// doc), not the old app's leaner EntryDate/UserID/IP.
/// </summary>
public abstract class AppendixEntityBase
{
    public int Id { get; set; }

    public int DemandId { get; set; }

    /// <summary>
    /// Stable business demand identifier (unlike DemandId, doesn't change per financial year) -
    /// added 2026-08-14, replicating the pattern already shipped for dbo.M_AutonomousBody
    /// (AutonomousBodyService.cs): DemandId claims/values go stale across FY rollovers since
    /// dbo.M_MapDemandFY mints a new DemandId every year for the same DemandNo, so
    /// AppendixDataRepository resolves and queries by DemandNo instead. DemandId is kept
    /// (not removed) as the fallback for any row that predates this column or whose DemandId
    /// didn't resolve to a DemandNo at backfill time.
    /// </summary>
    public int? DemandNo { get; set; }

    public string FinancialYear { get; set; } = string.Empty;

    public bool IsFrozen { get; set; }

    public DateTime? FrozenAtUtc { get; set; }

    public int? FrozenByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public int? UserIdCreatedBy { get; set; }

    public DateTime? CreatedOnDate { get; set; }

    public int? UserIdModifyBy { get; set; }

    public DateTime? ModifiedOnDate { get; set; }

    public int? UserIdDeletedBy { get; set; }

    public DateTime? DeletedOnDate { get; set; }

    // -- IP address + username snapshot (added 2026-08-11, Edit/Modify/Delete + audit trail
    // retrofit). UserIdCreatedBy/ModifyBy/DeletedBy above are the numeric FK; these are a
    // denormalized snapshot of the login name AT THE TIME of the action, so a historical audit
    // record still reads correctly even if that user is later renamed or deleted.

    public string? CreatedByIp { get; set; }

    public string? CreatedByUserName { get; set; }

    public string? ModifiedByIp { get; set; }

    public string? ModifiedByUserName { get; set; }

    public string? DeletedByIp { get; set; }

    public string? DeletedByUserName { get; set; }

    /// <summary>
    /// Throws if this record is frozen - the invariant every appendix aggregate enforces before
    /// allowing a modification or delete (moved here 2026-08-07 from AppendixDataRepository&lt;T&gt;
    /// so the rule lives on the aggregate that owns it, not scattered across a generic repository).
    /// </summary>
    public void EnsureNotFrozen(string action)
    {
        if (IsFrozen)
        {
            throw new InvalidOperationException($"{GetType().Name} {Id} is frozen and cannot be {action}.");
        }
    }

    /// <summary>Marks this record frozen (row-level freeze, decision 4) - read-only from this point on per EnsureNotFrozen.</summary>
    public void MarkFrozen(int userId)
    {
        IsFrozen = true;
        FrozenAtUtc = DateTime.UtcNow;
        FrozenByUserId = userId;
    }

    /// <summary>Soft-deletes this record (no physical delete per the project's audit-trail convention).</summary>
    public void MarkDeleted(int userId, string? userName = null, string? ip = null)
    {
        IsDeleted = true;
        UserIdDeletedBy = userId;
        DeletedOnDate = DateTime.UtcNow;
        DeletedByUserName = userName;
        DeletedByIp = ip;
    }

    /// <summary>Stamps creation audit fields - call once, when the record is first added.</summary>
    public void MarkCreated(int userId, string? userName = null, string? ip = null)
    {
        UserIdCreatedBy = userId;
        CreatedOnDate = DateTime.UtcNow;
        CreatedByUserName = userName;
        CreatedByIp = ip;
    }

    /// <summary>Stamps modification audit fields - call whenever an already-created record is updated.</summary>
    public void MarkModified(int userId, string? userName = null, string? ip = null)
    {
        UserIdModifyBy = userId;
        ModifiedOnDate = DateTime.UtcNow;
        ModifiedByUserName = userName;
        ModifiedByIp = ip;
    }
}
