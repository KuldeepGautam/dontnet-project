namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Persistence;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>Appendix VI: Non-Tax Revenue / Capital Receipts.
///
/// Re-design 2026-09-10 (Appendix6.html): Receipt Type is a real DB dropdown
/// (dbo.M_Appendix_VI_ReceiptType). The record keeps a ReceiptTypeId FK; ReceiptType stores the
/// resolved name so grid / export / search / dedup keep working (same pattern as Appendix III-B's
/// CategoryId / CategoryType). "Proposed R.E." became "Proposed B.E." (column ProposedRE renamed to
/// ProposedBE) plus two new "Proposed Collection" quarter fields.</summary>
[ApiController]
[Route("api/appendix-vi")]
[Authorize]
public class AppendixVIController : ControllerBase
{
    private readonly AppendixDataRepository<AppendixNonTaxRevenue> _repository;
    private readonly PreBudgetDbContext _db;

    public AppendixVIController(AppendixDataRepository<AppendixNonTaxRevenue> repository, PreBudgetDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecords([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var records = await _repository.GetByDemandAndYearAsync(demandId, financialYear, ct);
        return Ok(records.Select(ToDto));
    }

    /// <summary>Receipt Type dropdown - dbo.M_Appendix_VI_ReceiptType (global, active rows only).</summary>
    [HttpGet("receipt-types")]
    public async Task<IActionResult> GetReceiptTypes(CancellationToken ct)
    {
        var types = await _db.AppendixViReceiptTypes.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder).ThenBy(t => t.Name)
            .Select(t => new AppendixVIReceiptTypeOptionDto { Id = t.Id, Name = t.Name })
            .ToListAsync(ct);
        return Ok(types);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixNonTaxRevenueDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        var resolved = await ResolveReceiptTypeAsync(request, ct);
        if (resolved.Error is not null) return BadRequest(resolved.Error);

        // Client requirement 2026-08-31: "duplicate records for same demand, Receipt type and
        // PSU/Receipt Name cannot enter / save in db" - keyed on ReceiptTypeId + PSU/Receipt Name
        // since Receipt Type is now an id. Wording reproduced verbatim ("existis"/"reciept" typos).
        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => e.ReceiptTypeId == request.ReceiptTypeId
                     && string.Equals(e.PsuReceiptName, request.PsuReceiptName, StringComparison.OrdinalIgnoreCase),
                excludeId: null, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "Record existis for selected reciept type and PSU/Receipt Name." });
        }

        var entity = new AppendixNonTaxRevenue { DemandId = request.DemandId, FinancialYear = request.FinancialYear };
        entity.UpdateFrom(
            request.ReceiptTypeId,
            resolved.Name,
            request.PsuReceiptName,
            request.Actuals,
            request.BE,
            request.ActualsUptoSept,
            request.ProposedBE,
            request.ProposedCollectionQ3,
            request.ProposedCollectionQ4,
            request.Remarks);

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(ToDto(saved));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixNonTaxRevenueDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        var resolved = await ResolveReceiptTypeAsync(request, ct);
        if (resolved.Error is not null) return BadRequest(resolved.Error);

        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => e.ReceiptTypeId == request.ReceiptTypeId
                     && string.Equals(e.PsuReceiptName, request.PsuReceiptName, StringComparison.OrdinalIgnoreCase),
                excludeId: id, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "Record existis for selected reciept type and PSU/Receipt Name." });
        }

        try
        {
            var updated = await _repository.UpdateAsync(id, userId.Value, entity => entity.UpdateFrom(
                request.ReceiptTypeId,
                resolved.Name,
                request.PsuReceiptName,
                request.Actuals,
                request.BE,
                request.ActualsUptoSept,
                request.ProposedBE,
                request.ProposedCollectionQ3,
                request.ProposedCollectionQ4,
                request.Remarks), ct);
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

    private sealed record ResolvedReceiptType(string? Name, object? Error);

    /// <summary>Validates ReceiptTypeId against dbo.M_Appendix_VI_ReceiptType and returns its display
    /// name to persist alongside the id (same pattern as Appendix III-B's ResolveCategoryAndSchemeAsync).</summary>
    private async Task<ResolvedReceiptType> ResolveReceiptTypeAsync(SaveAppendixNonTaxRevenueDto request, CancellationToken ct)
    {
        if (request.ReceiptTypeId is null)
        {
            return new ResolvedReceiptType(null, new { Code = "VALIDATION_ERROR", Message = "Receipt Type is required." });
        }

        var type = await _db.AppendixViReceiptTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.ReceiptTypeId.Value, ct);
        if (type is null)
        {
            return new ResolvedReceiptType(null, new { Code = "VALIDATION_ERROR", Message = "The selected Receipt Type no longer exists." });
        }

        return new ResolvedReceiptType(type.Name, null);
    }

    private static AppendixNonTaxRevenueDto ToDto(AppendixNonTaxRevenue e) => new()
    {
        Id = e.Id,
        DemandId = e.DemandId,
        FinancialYear = e.FinancialYear,
        ReceiptTypeId = e.ReceiptTypeId,
        ReceiptType = e.ReceiptType,
        PsuReceiptName = e.PsuReceiptName,
        Actuals = e.Actuals,
        BE = e.BE,
        ActualsUptoSept = e.ActualsUptoSept,
        ProposedBE = e.ProposedBE,
        ProposedCollectionQ3 = e.ProposedCollectionQ3,
        ProposedCollectionQ4 = e.ProposedCollectionQ4,
        Remarks = e.Remarks,
        IsFrozen = e.IsFrozen
    };

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
