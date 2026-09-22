namespace UBIS.Web.Areas.PreBudgetMeeting.Models;

using UBIS.Web.Services.Clients;

public class AppendixXViewModel : AppendixBaseViewModel
{
    /// <summary>Fixed loan-category rows - not a free-text Sub-Head Name with an Add/Edit/Delete CRUD
    /// grid. Corrected 2026-08-31 (tester feedback, citing the FRS directly): the FRS specifies only
    /// 3 major heads for this appendix - House Building Advances, Advances for Purchase of Motor
    /// Cars, Advances for Purchase of Computers. Supersedes the earlier 2026-08-24 client-screenshot-
    /// based 5-row list (House Building/Motor Cars/Other Motor Conveyances/Computers/Other Advances,
    /// numbered (i)-(iii),(v),(vi) with a skipped (iv)) - that screenshot turned out not to match the
    /// FRS; "Other Motor Conveyances" and "Other Advances" are dropped entirely, not renumbered.</summary>
    public static readonly IReadOnlyList<string> FixedSubHeadNames = new[]
    {
        "(i) House building advances",
        "(ii) Advances for purchase of motor cars",
        "(iii) Advances for Purchase of Computers"
    };

    public List<AppendixLoansToGovtServantsDto> Records { get; set; } = new();

    /// <summary>One row per <see cref="FixedSubHeadNames"/> entry, pre-filled from <see
    /// cref="Records"/> when a saved row already exists for that sub-head (client requirement,
    /// 2026-08-31: "if records exists for ... sub-heads etc. it should enable adding that row"
    /// [sic - "enable editing that row's saved values directly"] "... rows should be blank" for
    /// sub-heads with no saved row yet). Superseded the earlier 2026-08-27 "duplicate additions
    /// allowed"/always-blank decision, which itself was already reversed by the same day's
    /// duplicate-entry rejection in AppendixXController.CreateRecord - this just extends that
    /// reversal to the entry table itself, so a sub-head that already has data can be edited
    /// in-place (Id set -> SaveAppendixX treats it as an Update) instead of forcing the separate
    /// grid Edit button just to reach the same row.</summary>
    public List<SaveAppendixLoansToGovtServantsDto> Rows
    {
        get
        {
            return FixedSubHeadNames.Select(name =>
            {
                var existing = Records.FirstOrDefault(r => string.Equals(r.SubHeadName?.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    return new SaveAppendixLoansToGovtServantsDto { SubHeadName = name };
                }
                return new SaveAppendixLoansToGovtServantsDto
                {
                    Id = existing.Id,
                    SubHeadName = name,
                    ActualsY1 = existing.ActualsY1,
                    ActualsY2 = existing.ActualsY2,
                    ActualsY3 = existing.ActualsY3,
                    ActualsUptoSept = existing.ActualsUptoSept,
                    BE = existing.BE,
                    RE = existing.RE,
                    NBE = existing.NBE
                };
            }).ToList();
        }
    }
}
