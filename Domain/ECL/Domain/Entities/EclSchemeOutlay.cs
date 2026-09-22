namespace UBIS.Services.Ecl.Domain.Entities;

/// <summary>
/// Maps to dbo.ECL_T_Outlay. The legacy application that used to write to this table directly is
/// now retired (confirmed 2026-08-18), so ecl-workstream-4-outlay-column-cleanup.sql renamed
/// TotalOutlay -> CentralShare and GrandTotalOutlay -> TotalOutlay (the legacy column names didn't
/// match their actual meaning) and dropped the now-unused MappedSchemeID/SubScheme/
/// InitialTotalOutlay columns along with the superseded additive CentralShare column from
/// ecl-workstream-1-schema.sql. FileTitle remains physically present but still deliberately
/// unmapped (nothing in this app ever needs it).
/// </summary>
public class EclSchemeOutlay
{
    public int RowId { get; set; }

    public string? FinancialYear { get; set; }

    public int? CategoryId { get; set; }

    public int? SubCategoryId { get; set; }

    public int? DemandId { get; set; }

    public int? SchemeId { get; set; }

    /// <summary>Free-text approval authority name — either a name chosen from M_ECLApprovalAuthority or a typed "Others" value; kept for backward-compat/history/reporting alongside ApprovalAuthorityId below (the authoritative reference going forward, added 2026-08-25).</summary>
    public string? ApproveAuth { get; set; }

    /// <summary>FK to dbo.M_ECLApprovalAuthority.AuthorityId (added 2026-08-25) — resolved server-side from ApproveAuth on every save (EclOutlayRepository.EnsureApprovalAuthorityAsync), promoting a genuinely new "Others" value into the master table instead of only ever storing free text.</summary>
    public int? ApprovalAuthorityId { get; set; }

    public decimal? TotalOutlay { get; set; }

    /// <summary>Maps ECL_T_Outlay.Percentage. NEVER trust a client-supplied value — always recomputed server-side by RecomputeCentralSharePercentage() before saving.</summary>
    public decimal? CentralSharePercentage { get; set; }

    /// <summary>Rupee amount of central share. Maps ECL_T_Outlay.CentralShare (renamed 2026-08-18 from the legacy TotalOutlay column, which actually held this value).</summary>
    public decimal? CentralShare { get; set; }

    public decimal? Fy1Outlay { get; set; }
    public decimal? Fy2Outlay { get; set; }
    public decimal? Fy3Outlay { get; set; }
    public decimal? Fy4Outlay { get; set; }
    public decimal? Fy5Outlay { get; set; }
    public decimal? Fy6Outlay { get; set; }
    public decimal? Fy7Outlay { get; set; }
    public decimal? Fy8Outlay { get; set; }
    public decimal? Fy9Outlay { get; set; }
    public decimal? Fy10Outlay { get; set; }

    /// <summary>Real stored server-generated PDF filename (dbo.ECL_T_Outlay.FileName). FileTitle is deliberately not mapped — see class doc comment.</summary>
    public string? FileName { get; set; }

    /// <summary>Entry-screen remarks (dbo.ECL_T_Outlay.UserRemarks) — distinct from DoeRemarks below.</summary>
    public string? UserRemarks { get; set; }

    public int? EntryUserId { get; set; }

    /// <summary>Maps dbo.ECL_T_Outlay.SendToDOE (Y/N) (renamed 2026-08-18 from FreezeByDemand).</summary>
    public string? SendToDoe { get; set; }

    public DateTime? EntryDateDemand { get; set; }

    public string? IpDemand { get; set; }

    /// <summary>Maps dbo.ECL_T_Outlay.ApproveByDOE. Confirmed real values: Y=Approved, N=Pending/Not Approved, R=Rejected.</summary>
    public string? ApprovedByDoe { get; set; }

    /// <summary>Maps dbo.ECL_T_Outlay.ReapproveByDOE (Y/N).</summary>
    public string? ReapprovalByDoe { get; set; }

    public int? ApproveUserId { get; set; }

    public DateTime? ApproveDate { get; set; }

    public string? ApproverIp { get; set; }

    /// <summary>Maps dbo.ECL_T_Outlay.Remarks — the DOE reviewer's remarks, distinct from UserRemarks.</summary>
    public string? DoeRemarks { get; set; }

    public decimal? Fy1Actuals { get; set; }
    public decimal? Fy2Actuals { get; set; }
    public decimal? Fy3Actuals { get; set; }
    public decimal? Fy4Actuals { get; set; }
    public decimal? Fy5Actuals { get; set; }
    public decimal? Fy6Actuals { get; set; }
    public decimal? Fy7Actuals { get; set; }
    public decimal? Fy8Actuals { get; set; }
    public decimal? Fy9Actuals { get; set; }
    public decimal? Fy10Actuals { get; set; }

    public int? ActualsUserId { get; set; }

    public string? ActualsIp { get; set; }

    public DateTime? ActualsEntryDate { get; set; }

    /// <summary>Self-referential lineage to a prior RowID (reapproval/history chain).</summary>
    public int? PrevRowId { get; set; }

    /// <summary>"Requires approval during 16th FC cycle?" (Y/N).</summary>
    public string? Is16Fc { get; set; }

    /// <summary>Free-text appraisal authority name — same free-text-with-"Others" pattern as ApproveAuth, kept for backward-compat/history/reporting alongside AppraisalAuthorityId below.</summary>
    public string? AppraisalAuth { get; set; }

    /// <summary>FK to dbo.M_ECLAppraiseAuthority.AppraiseId (added 2026-08-25) — resolved server-side from AppraisalAuth on every save (EclOutlayRepository.EnsureAppraiseAuthorityAsync), same "Others" promotion behavior as ApprovalAuthorityId.</summary>
    public int? AppraisalAuthorityId { get; set; }

    /// <summary>Maps dbo.ECL_T_Outlay.IsApprised (legacy spelling kept on the column; domain name uses "Appraised"). Y/N.</summary>
    public string? WhetherAppraised { get; set; }

    /// <summary>Maps dbo.ECL_T_Outlay.NotApprisedRem — "if No, provide status" free text.</summary>
    public string? AppraisalStatusRemarks { get; set; }

    /// <summary>
    /// Maps dbo.ECL_T_Outlay.IsApproved — a SEPARATE ministry-side self-declared "whether approved"
    /// flag. NOT the same concept as ApprovedByDoe (the actual DOE workflow decision); kept distinct
    /// per spec.
    /// </summary>
    public string? IsApproved { get; set; }

    public string? NotApprovedRem { get; set; }

    public bool? IsActive { get; set; }

    public int? UserIdCreatedBy { get; set; }
    public DateTime? CreatedOnDate { get; set; }
    public int? UserIdModifyBy { get; set; }
    public DateTime? ModifiedOnDate { get; set; }
    public int? UserIdDeletedBy { get; set; }
    public DateTime? DeletedOnDate { get; set; }

    public bool IsDeleted { get; set; }

    public int? DeletedByUserId { get; set; }

    public string? DeletedByIp { get; set; }

    /// <summary>New column (added 2026-08-18) — mandatory dropdown, format matches FinancialYear (e.g. "2030-2031").</summary>
    public string? SchemeEndYear { get; set; }

    // ---- Domain behavior: the workflow state machine lives HERE, not in the controller/repository ----

    /// <summary>
    /// Recomputes CentralSharePercentage server-side. Must be called before every save — a
    /// client-supplied Percentage value is never trusted.
    /// </summary>
    public void RecomputeCentralSharePercentage()
    {
        CentralSharePercentage = TotalOutlay is > 0
            ? Math.Round((CentralShare ?? 0) / TotalOutlay.Value * 100, 2)
            : 0;
    }

    /// <summary>
    /// Sends this outlay row to DOE for approval (SendToDoe = "Y"). Illegal while a decision is
    /// already pending (SendToDoe == "Y" &amp;&amp; ApprovedByDoe == "N") or once already approved
    /// (ApprovedByDoe == "Y"). Resubmitting after a rejection (ApprovedByDoe == "R") is allowed and
    /// resets ApprovedByDoe back to "N" so it re-enters the normal pending state.
    /// </summary>
    public void SubmitForDoeApproval()
    {
        if (SendToDoe == "Y" && ApprovedByDoe == "N")
        {
            throw new InvalidOperationException($"EclSchemeOutlay {RowId} is already pending DOE approval.");
        }

        if (ApprovedByDoe == "Y")
        {
            throw new InvalidOperationException($"EclSchemeOutlay {RowId} is already approved by DOE.");
        }

        SendToDoe = "Y";
        ApprovedByDoe = "N";
    }

    /// <summary>Approves this outlay row. Only legal while genuinely pending (SendToDoe == "Y" &amp;&amp; ApprovedByDoe == "N"). Also clears ReapprovalByDoe — a fresh decision resolves whatever reapproval cycle led back here, so a re-approved row must not keep showing as "Re-approval Requested" on DataForReapproval.</summary>
    public void ApproveByDoe(int approveUserId, string? approverIp, string? doeRemarks)
    {
        EnsurePending();

        ApprovedByDoe = "Y";
        ApproveUserId = approveUserId;
        ApproveDate = DateTime.UtcNow;
        ApproverIp = approverIp;
        DoeRemarks = doeRemarks;
        ReapprovalByDoe = "N";
    }

    /// <summary>Rejects this outlay row. Only legal while genuinely pending. A rejection reason is mandatory — an audit gap otherwise. Also clears ReapprovalByDoe, same reasoning as ApproveByDoe.</summary>
    public void RejectByDoe(int approveUserId, string? approverIp, string? doeRemarks)
    {
        EnsurePending();

        if (string.IsNullOrWhiteSpace(doeRemarks))
        {
            throw new InvalidOperationException("DOE remarks are required when rejecting an outlay.");
        }

        ApprovedByDoe = "R";
        ApproveUserId = approveUserId;
        ApproveDate = DateTime.UtcNow;
        ApproverIp = approverIp;
        DoeRemarks = doeRemarks;
        ReapprovalByDoe = "N";
    }

    /// <summary>
    /// Requests reapproval of an already-approved outlay. Atomically sets exactly
    /// ReapprovalByDoe = "Y", SendToDoe = "N", ApprovedByDoe = "N" — never applied partially. Only
    /// legal when ApprovedByDoe == "Y". Prior approval metadata (ApproveUserId/ApproveDate/
    /// ApproverIp/DoeRemarks) is deliberately left untouched here for audit — it is only overwritten
    /// when the NEW approval/rejection decision is actually made.
    /// </summary>
    public void RequestReapproval()
    {
        if (ApprovedByDoe != "Y")
        {
            throw new InvalidOperationException($"EclSchemeOutlay {RowId} cannot request reapproval — it is not currently approved.");
        }

        ReapprovalByDoe = "Y";
        SendToDoe = "N";
        ApprovedByDoe = "N";
    }

    /// <summary>Friendly status string for API responses, derived from the raw Y/N/R.</summary>
    public string DoeApprovalStatusDisplay => ApprovedByDoe switch
    {
        "Y" => "Approved",
        "R" => "Rejected",
        _ => "Pending"
    };

    /// <summary>Updates the FY1-10 Actuals grid and stamps the actuals audit columns.</summary>
    public void RecordActuals(decimal?[] actuals, int userId, string? ip)
    {
        if (actuals.Length != 10)
        {
            throw new ArgumentException("Actuals grid must have exactly 10 entries (FY1..FY10).", nameof(actuals));
        }

        Fy1Actuals = actuals[0];
        Fy2Actuals = actuals[1];
        Fy3Actuals = actuals[2];
        Fy4Actuals = actuals[3];
        Fy5Actuals = actuals[4];
        Fy6Actuals = actuals[5];
        Fy7Actuals = actuals[6];
        Fy8Actuals = actuals[7];
        Fy9Actuals = actuals[8];
        Fy10Actuals = actuals[9];

        ActualsUserId = userId;
        ActualsIp = ip;
        ActualsEntryDate = DateTime.UtcNow;
    }

    private void EnsurePending()
    {
        if (SendToDoe != "Y" || ApprovedByDoe != "N")
        {
            throw new InvalidOperationException($"EclSchemeOutlay {RowId} is not currently pending DOE approval.");
        }
    }
}
