namespace UBIS.Services.Aim.Domain.Entities.Legacy;

/// <summary>
/// Read-only pass-through to the pre-existing legacy <c>dbo.Section</c> lookup table — the
/// master list of organisational sections a "Section Officer"/"Under Secretary" user
/// (see <see cref="User.SectionUserType"/>/<see cref="User.SectionCode"/>) can belong to.
/// Added 2026-07-10.
/// </summary>
public class Section
{
    public string SectionCode { get; set; } = string.Empty;

    /// <summary>Section name (primary language).</summary>
    public string? SectionName1 { get; set; }

    /// <summary>Section name (secondary/bilingual rendering, e.g. Hindi).</summary>
    public string? SectionName2 { get; set; }
}
