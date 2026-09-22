namespace UBIS.Services.Sbe.Domain.Entities;

/// <summary>Freeze/status tracking for PSE Category data, per Demand+FinancialYear — distinct from PseDemand (open item: whether this needs its own tracking or fully inherits SbeDemand's state, not yet confirmed from the FRS text). Maps the newly-created dbo.PSECategoryDemand.</summary>
public class PseCategoryDemand
{
    public int PseCategoryDemandId { get; set; }
    public string? FinancialYear { get; set; }
    public int? UserId { get; set; }
    public int? DemandId { get; set; }
    public int? UnFreezeUserId { get; set; }
    public DateTime? TargetDate { get; set; }
    public DateTime? UnFreezeDate { get; set; }
    public string? Status { get; set; }
    public string? Freez { get; set; }
    public DateTime? EntryDate { get; set; }
    public string? Ip { get; set; }

    public void Freeze(int userId)
    {
        if (Freez == "Y")
        {
            throw new InvalidOperationException($"PseCategoryDemand {PseCategoryDemandId} is already frozen.");
        }

        Freez = "Y";
    }

    public void Unfreeze(int userId)
    {
        if (Freez != "Y")
        {
            throw new InvalidOperationException($"PseCategoryDemand {PseCategoryDemandId} is not currently frozen.");
        }

        Freez = "N";
        UnFreezeDate = DateTime.UtcNow;
        UnFreezeUserId = userId;
    }

    public bool IsFrozen => Freez == "Y";
}
