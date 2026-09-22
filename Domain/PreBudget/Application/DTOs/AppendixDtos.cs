namespace UBIS.Services.PreBudget.Application.DTOs;

public class AppendixDto
{
    public int AppendixId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? HName { get; set; }
    public int DisplaySequence { get; set; }
}

/// <summary>Combines an Appendix with this Demand's current status — the Select-Demand-and-Appendix screen consumes this shape.</summary>
public class AppendixWithStatusDto
{
    public int AppendixId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public string? ParaNo { get; set; }
    public int DisplaySequence { get; set; }

    public bool IsFrozen { get; set; }
    public bool IsNilSubmitted { get; set; }
    public DateTime? TargetDate { get; set; }

    /// <summary>True once TargetDate has passed and the appendix was never frozen — entry is blocked either way, but the message differs (see PreBudgetMeetingController).</summary>
    public bool IsTargetDateExpired { get; set; }
}
