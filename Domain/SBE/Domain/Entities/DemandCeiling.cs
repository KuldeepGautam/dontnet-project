namespace UBIS.Services.Sbe.Domain.Entities;

/// <summary>
/// FR017's Demand Scheme Ceiling Allocation, per Demand+FinancialYear. Maps the newly-created
/// dbo.M_DemandCeiling. Ceiling semantics (decided 2026-08-21, revisit with client): implemented
/// as a hard maximum — any RE/NBE save that would exceed BeCeiling/ReCeiling/NbeCeiling is
/// blocked, checked on every write in Stage 5, not only at Freeze time. That enforcement lives
/// on SbeData's write path (Stage 3+5), not here — this entity is a Foundation-stage data holder.
/// </summary>
public class DemandCeiling
{
    public int RowId { get; set; }
    public int DemandId { get; set; }
    public string? FinancialYear { get; set; }
    public decimal? BeCeiling { get; set; }
    public decimal? ReCeiling { get; set; }
    public decimal? NbeCeiling { get; set; }

    /// <summary>Capital-expenditure earmark %, separately for BE/RE/NBE.</summary>
    public decimal? CapBePer { get; set; }
    public decimal? CapRePer { get; set; }
    public decimal? CapNbePer { get; set; }

    public decimal? TotSchemeBe { get; set; }
    public decimal? TotSchemeRe { get; set; }
    public decimal? TotSchemeNbe { get; set; }

    /// <summary>SC/ST/NER earmark percentages, separately for BE/RE/NBE (9 columns total).</summary>
    public decimal? ScPerBe { get; set; }
    public decimal? ScPerRe { get; set; }
    public decimal? ScPerNbe { get; set; }
    public decimal? StPerBe { get; set; }
    public decimal? StPerRe { get; set; }
    public decimal? StPerNbe { get; set; }
    public decimal? NerPerBe { get; set; }
    public decimal? NerPerRe { get; set; }
    public decimal? NerPerNbe { get; set; }

    public DateTime EntryDate { get; set; }
    public string UserIp { get; set; } = string.Empty;
    public int UserId { get; set; }
}
