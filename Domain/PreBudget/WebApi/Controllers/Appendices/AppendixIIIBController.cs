namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Persistence;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>Appendix III-B: Status of Scheme Appraisal/Approval during the XVI Finance Commission
/// Cycle. New appendix, 2026-09-09 - see Appendix_Papes_I_to IIIB/Appendix-III-B.html.
///
/// Category and Scheme are real cascading dropdowns (client instruction 2026-09-10): Category from
/// dbo.M_Category (current FY, SerialNo II/IV), Scheme cascading from the chosen Category the same
/// way Appendix III does it (M_Scheme by Demand + CategoryId). The record is keyed on
/// CategoryId + SchemeId; CategoryType / SchemeName are written with the resolved display names so
/// the grid / export / search keep working unchanged.</summary>
[ApiController]
[Route("api/appendix-iiib")]
[Authorize]
public class AppendixIIIBController : ControllerBase
{
    private readonly AppendixDataRepository<AppendixSchemeAppraisalStatus> _repository;
    private readonly PreBudgetDbContext _db;

    public AppendixIIIBController(AppendixDataRepository<AppendixSchemeAppraisalStatus> repository, PreBudgetDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecords([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var records = await _repository.GetByDemandAndYearAsync(demandId, financialYear, ct);
        return Ok(records.Select(ToDto).ToList());
    }

    /// <summary>Category dropdown - client reference query 2026-09-10:
    /// <c>select CategoryId, CategoryName from M_Category where FinancialYear=@fy and SerialNo in ('II','IV')</c>.</summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories([FromQuery] string financialYear, CancellationToken ct)
    {
        var categories = await _db.Categories
            .Where(c => c.IsActive && c.FinancialYear == financialYear
                        && (c.SerialNo == "II" || c.SerialNo == "IV"))
            .Select(c => new { c.CategoryId, c.SerialNo, c.CategoryName })
            .ToListAsync(ct);

        return Ok(categories
            .OrderBy(c => c.SerialNo)
            .Select(c => new AppendixIIIBCategoryOptionDto { CategoryId = c.CategoryId, CategoryName = c.CategoryName }));
    }

    /// <summary>Scheme dropdown, cascading from the selected Category - same shape as Appendix III /
    /// VI-B (M_Scheme by Demand + CategoryId, sorted by SchemeSrNo, displayed via SchemeDisplayFormatter).</summary>
    [HttpGet("schemes")]
    public async Task<IActionResult> GetSchemes([FromQuery] int demandId, [FromQuery] int categoryId, CancellationToken ct)
    {
        var schemes = await _db.Schemes
            .Where(s => s.DemandId == demandId && s.CategoryId == categoryId && s.IsActive && !s.IsDeleted)
            .Select(s => new { s.SchemeId, s.SchemeSrNo, s.SchemeName })
            .ToListAsync(ct);

        return Ok(schemes
            .OrderBy(s => s.SchemeSrNo ?? 0)
            .Select(s => new { s.SchemeId, SchemeName = SchemeDisplayFormatter.Format(s.SchemeSrNo, s.SchemeName) }));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixSchemeAppraisalStatusDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        var resolved = await ResolveCategoryAndSchemeAsync(request, ct);
        if (resolved.Error is not null) return BadRequest(resolved.Error);

        // Single entry per Demand per Category + Scheme combination (client requirement 2026-09-09,
        // now keyed on the chosen ids rather than free text) - same uniqueness convention as every
        // other identifying-field appendix in this module.
        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => e.CategoryId == request.CategoryId && e.SchemeId == request.SchemeId, excludeId: null, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = $"A record for Scheme '{resolved.SchemeName}' under '{resolved.CategoryName}' already exists for this Demand." });
        }

        var entity = new AppendixSchemeAppraisalStatus { DemandId = request.DemandId, FinancialYear = request.FinancialYear };
        entity.UpdateFrom(request.CategoryId, request.SchemeId, resolved.CategoryName, resolved.SchemeName,
            request.StatusOfFreshAppraisalApproval, request.SchemeApprovalValidUpto, request.Remarks);

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(ToDto(saved));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixSchemeAppraisalStatusDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        var resolved = await ResolveCategoryAndSchemeAsync(request, ct);
        if (resolved.Error is not null) return BadRequest(resolved.Error);

        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => e.CategoryId == request.CategoryId && e.SchemeId == request.SchemeId, excludeId: id, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = $"A record for Scheme '{resolved.SchemeName}' under '{resolved.CategoryName}' already exists for this Demand." });
        }

        try
        {
            var updated = await _repository.UpdateAsync(id, userId.Value, entity => entity.UpdateFrom(
                request.CategoryId, request.SchemeId, resolved.CategoryName, resolved.SchemeName,
                request.StatusOfFreshAppraisalApproval, request.SchemeApprovalValidUpto, request.Remarks), ct);
            return Ok(ToDto(updated));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Code = "UPDATE_FAILED", Message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRecord(int id, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        try
        {
            await _repository.DeleteAsync(id, userId.Value, ct);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Code = "DELETE_FAILED", Message = ex.Message });
        }
    }

    [HttpPost("{id:int}/freeze")]
    public async Task<IActionResult> FreezeRecord(int id, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        try
        {
            var frozen = await _repository.FreezeAsync(id, userId.Value, ct);
            return Ok(ToDto(frozen));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Code = "FREEZE_FAILED", Message = ex.Message });
        }
    }

    private sealed record ResolvedCategoryScheme(string CategoryName, string SchemeName, object? Error);

    private async Task<ResolvedCategoryScheme> ResolveCategoryAndSchemeAsync(SaveAppendixSchemeAppraisalStatusDto request, CancellationToken ct)
    {
        if (request.CategoryId is null || request.SchemeId is null)
        {
            return new ResolvedCategoryScheme("", "", new { Code = "VALIDATION_ERROR", Message = "Category and Scheme are both required." });
        }

        var category = await _db.Categories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId.Value, ct);
        if (category is null)
        {
            return new ResolvedCategoryScheme("", "", new { Code = "VALIDATION_ERROR", Message = "The selected Category no longer exists." });
        }

        var scheme = await _db.Schemes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SchemeId == request.SchemeId.Value && s.DemandId == request.DemandId, ct);
        if (scheme is null)
        {
            return new ResolvedCategoryScheme("", "", new { Code = "VALIDATION_ERROR", Message = "The selected Scheme is not valid for this Demand." });
        }

        return new ResolvedCategoryScheme(
            category.CategoryName ?? "",
            SchemeDisplayFormatter.Format(scheme.SchemeSrNo, scheme.SchemeName),
            null);
    }

    private static AppendixSchemeAppraisalStatusDto ToDto(AppendixSchemeAppraisalStatus e) => new()
    {
        Id = e.Id,
        DemandId = e.DemandId,
        FinancialYear = e.FinancialYear,
        CategoryId = e.CategoryId,
        SchemeId = e.SchemeId,
        CategoryType = e.CategoryType,
        SchemeName = e.SchemeName,
        StatusOfFreshAppraisalApproval = e.StatusOfFreshAppraisalApproval,
        SchemeApprovalValidUpto = e.SchemeApprovalValidUpto,
        Remarks = e.Remarks,
        IsFrozen = e.IsFrozen
    };

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
