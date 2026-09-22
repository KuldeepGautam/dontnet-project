namespace UBIS.Services.Sbe.Domain.Entities;

/// <summary>
/// Object-Head-level ceiling (BE/RE/NBE per DemandId+ObjectCode) — read-only per the FRS (BR-02
/// on the "Object Head Ceiling Allocation" tab: "any adjustments... must be executed at the
/// source within Demand Scheme Ceiling Allocation"). Maps the newly-created dbo.M_ObjectCeiling.
/// The legacy BIMSDemo BE_Ceiling1212 column (a one-off/typo'd column) was deliberately not
/// carried forward in the migration script.
/// </summary>
public class ObjectCeiling
{
    public int RowId { get; set; }
    public int DemandId { get; set; }
    public string? FinancialYear { get; set; }
    public int ObjectCode { get; set; }
    public decimal? BeCeiling { get; set; }
    public decimal? ReCeiling { get; set; }
    public decimal? NbeCeiling { get; set; }
    public DateTime EntryDate { get; set; }
    public string UserIp { get; set; } = string.Empty;
    public int UserId { get; set; }
}
