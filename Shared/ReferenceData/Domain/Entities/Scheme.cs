namespace UBIS.Services.ReferenceData.Domain.Entities;

/// <summary>
/// Maps to the new dbo.M_Scheme table. Trimmed from legacy BIMSDemo.M_Scheme's 25 columns down to
/// what's confirmed needed by the Pre-Budget-Meeting appendices (III, IV, IV-A, IV-B, VI-B) —
/// Debt/VOA/CSS/Ceiling-specific columns dropped, add back individually if a future consumer needs
/// one. DemandId references dbo.M_Demand, which lives in the PreBudget/UBIS-Dev database this
/// service also points at, but is owned by AIM/UBIS_Web, not this service.
/// </summary>
public class Scheme
{
    public int SchemeId { get; set; }

    public int DemandId { get; set; }

    public string SchemeName { get; set; } = string.Empty;

    public string? HSchemeName { get; set; }

    public bool IsUmbrella { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public int? UserIdCreatedBy { get; set; }

    public DateTime? CreatedOnDate { get; set; }

    public int? UserIdModifyBy { get; set; }

    public DateTime? ModifiedOnDate { get; set; }

    public int? UserIdDeletedBy { get; set; }

    public DateTime? DeletedOnDate { get; set; }
}
