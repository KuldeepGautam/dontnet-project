namespace UBIS.Services.Sbe.Domain.Entities;

/// <summary>
/// Note Part: FR009's SBE Notes screen, FR014's renumber. Maps the newly-created dbo.SBENotes
/// (Others/publish-staging/sbe-workstream-1-schema.sql). English+Hindi para text per
/// Scheme/SubScheme, PrintSequenceNo drives FR014's renumber, PrevSBENoteId is the same
/// year-chain convention as SbeData.SbeDataIdPreviousYear.
/// </summary>
public class SbeNote
{
    public int SbeNoteId { get; set; }
    public int? SbeDataId { get; set; }
    public int? DemandId { get; set; }
    public int? DemandNo { get; set; }
    public string? FinancialYear { get; set; }
    public int? PrintSequenceNo { get; set; }
    public string? ParaNo { get; set; }
    public string? ParaHeading { get; set; }
    public string? HParaHeading { get; set; }
    public string? ParaDetails { get; set; }
    public string? HParaDetails { get; set; }
    public int? SchemeId { get; set; }
    public int? SubSchemeId { get; set; }
    public int? ProgrammeId { get; set; }
    public int? SubprogrammeId { get; set; }
    public DateTime? EntryDate { get; set; }
    public int? PrevSbeNoteId { get; set; }
    public int? UserId { get; set; }
    public string? Ip { get; set; }
}
