namespace UBIS.Services.Sbe.Domain.Entities;

/// <summary>FR011's Investment in PSE freeze/status tracking, per Demand+FinancialYear. Maps the newly-created dbo.PSEDemand — same freeze/status shape as SbeDemand, kept separate since it's a distinct physical table with its own lifecycle.</summary>
public class PseDemand
{
    public int PseDemandId { get; set; }
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
            throw new InvalidOperationException($"PseDemand {PseDemandId} is already frozen.");
        }

        Freez = "Y";
    }

    public void Unfreeze(int userId)
    {
        if (Freez != "Y")
        {
            throw new InvalidOperationException($"PseDemand {PseDemandId} is not currently frozen.");
        }

        Freez = "N";
        UnFreezeDate = DateTime.UtcNow;
        UnFreezeUserId = userId;
    }

    public bool IsFrozen => Freez == "Y";
}
