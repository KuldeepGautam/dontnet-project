namespace UBIS.Services.Aim.Application.DTOs.User;

/// <summary>One Demand (dbo.M_Demand row) the caller is allowed to access, per their JWT's AIM:Demand:{id} claims. Added 2026-07-23 for the Pre-Budget Meeting module's Demand-selection dropdown.</summary>
public class DemandDto
{
    public int DemandId { get; set; }

    public int DemandNo { get; set; }

    public string DemandName { get; set; } = string.Empty;
}

/// <summary>Wrapper for GET /api/users/demands. Added 2026-07-23.</summary>
public class MyDemandsDto
{
    public List<DemandDto> Demands { get; set; } = new();
}
