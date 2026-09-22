namespace UBIS.Services.Ecl.Application.Interfaces;

using UBIS.Services.Ecl.Application.DTOs;
using UBIS.Services.Ecl.Domain.Entities;

/// <summary>Bespoke repository for the ECL scheme-outlay workflow — not a generic reused from PreBudget, the shapes are too different.</summary>
public interface IEclOutlayRepository
{
    Task<List<EclSchemeOutlay>> GetByDemandCategorySchemeYearAsync(int? demandId, int? categoryId, int? schemeId, string financialYear, CancellationToken ct);

    Task<EclSchemeOutlay?> GetByIdAsync(int rowId, CancellationToken ct);

    Task<Result<EclSchemeOutlay>> AddAsync(EclSchemeOutlay entity, int userId, CancellationToken ct);

    Task<Result<EclSchemeOutlay>> UpdateAsync(int rowId, int userId, Action<EclSchemeOutlay> applyChanges, CancellationToken ct);

    Task<Result<bool>> DeleteAsync(int rowId, int userId, CancellationToken ct);

    Task<Result<EclSchemeOutlay>> SubmitForApprovalAsync(int rowId, int userId, CancellationToken ct);

    Task<Result<EclSchemeOutlay>> ApproveAsync(int rowId, int approveUserId, string? doeRemarks, CancellationToken ct);

    Task<Result<EclSchemeOutlay>> RejectAsync(int rowId, int approveUserId, string? doeRemarks, CancellationToken ct);

    Task<Result<EclSchemeOutlay>> RequestReapprovalAsync(int rowId, CancellationToken ct);

    Task<Result<EclSchemeOutlay>> RecordActualsAsync(int rowId, decimal?[] actuals, int userId, CancellationToken ct);

    Task<List<EclCategory>> GetCategoriesAsync(string financialYear, CancellationToken ct);

    /// <summary>Schemes scoped by both DemandId and CategoryId — see EclOutlayRepository's doc comment for the exact client-specified query.</summary>
    Task<List<EclScheme>> GetSchemesAsync(int demandId, int categoryId, CancellationToken ct);

    /// <summary>Umbrella-scheme options from dbo.M_UmbScheme, filtered by CategoryId and DemandId —
    /// see EclOutlayRepository's doc comment for the exact client-specified query.</summary>
    Task<List<EclUmbScheme>> GetUmbrellaSchemeOptionsAsync(int categoryId, int demandId, CancellationToken ct);

    /// <summary>Bulk name lookup by SchemeId, spanning any category — see EclOutlayRepository's
    /// doc comment for why this exists separately from the category-scoped GetSchemesAsync.</summary>
    Task<Dictionary<int, string>> GetSchemeNamesByIdsAsync(IEnumerable<int> schemeIds, CancellationToken ct);

    /// <summary>Bulk (SchemeName, SchemeSrNo) lookup by SchemeId — see EclOutlayRepository's doc comment.</summary>
    Task<Dictionary<int, (string SchemeName, int? SchemeSrNo)>> GetSchemeInfoByIdsAsync(IEnumerable<int> schemeIds, CancellationToken ct);

    /// <summary>Bulk (DemandNo, DemandName) lookup by DemandId — see EclOutlayRepository's doc comment.</summary>
    Task<Dictionary<int, (int? DemandNo, string? DemandName)>> GetDemandInfoByIdsAsync(IEnumerable<int> demandIds, CancellationToken ct);

    Task<Result<EclScheme>> CreateSchemeAsync(CreateSchemeRequestDto request, int userId, CancellationToken ct);

    /// <summary>Resolves the stable DemandNo for a per-year DemandId, same pattern as PreBudget's
    /// AppendixDataRepository.ResolveDemandNoAsync. Returns demandId itself as a fallback if no
    /// dbo.M_MapDemandFY row resolves it (same graceful-degradation choice PreBudget makes for the
    /// small number of rows with no matching mapping).</summary>
    Task<int> ResolveDemandNoAsync(int demandId, CancellationToken ct);

    Task<List<EclApprovalAuthority>> GetApprovalAuthoritiesAsync(CancellationToken ct);

    Task<List<EclAppraiseAuthority>> GetAppraiseAuthoritiesAsync(CancellationToken ct);

    /// <summary>Resolves an Approval Authority name to its dbo.M_ECLApprovalAuthority.AuthorityId —
    /// case-insensitive match against an existing (non-deleted) row if one exists, otherwise inserts
    /// a new row with IsDropdownVisible = false (a genuinely new "Others" value the user typed) and
    /// returns its new Id. Null/whitespace name returns null without touching the table.</summary>
    Task<int?> EnsureApprovalAuthorityAsync(string? name, int userId, CancellationToken ct);

    /// <summary>Same as EnsureApprovalAuthorityAsync, against dbo.M_ECLAppraiseAuthority.</summary>
    Task<int?> EnsureAppraiseAuthorityAsync(string? name, int userId, CancellationToken ct);

    Task<EclConfig?> GetConfigAsync(CancellationToken ct);

    /// <summary>Replaces GetConfigAsync as the source for Add Scheme Outlay's selectable years —
    /// see EclFinanceCommission's doc comment.</summary>
    Task<EclFinanceCommission?> GetFinanceCommissionAsync(CancellationToken ct);
}
