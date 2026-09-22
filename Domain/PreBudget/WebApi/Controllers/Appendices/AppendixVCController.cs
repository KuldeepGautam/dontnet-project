namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Persistence;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>Appendix V-C: Establishment Expenditure - Other than AB.</summary>
[ApiController]
[Route("api/appendix-vc")]
[Authorize]
public class AppendixVCController : ControllerBase
{
    private readonly AppendixDataRepository<AppendixEstablishmentOtherThanAB> _repository;
    private readonly PreBudgetDbContext _db;

    public AppendixVCController(AppendixDataRepository<AppendixEstablishmentOtherThanAB> repository, PreBudgetDbContext db)
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

    [HttpPost]
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixEstablishmentOtherThanABDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        // Client requirement 2026-09-02: "Name is unique for entry, no duplicate name entry is
        // allowed... In first letter Uppercase format" - the stored Name is always the normalized
        // form (whitespace-collapsed, trimmed, first letter capitalized), not whatever raw text was
        // typed, so the uniqueness check and the saved value can never drift apart even if a caller
        // bypasses the client-side on-blur normalization.
        var normalizedName = NormalizeName(request.Name);
        if (await HasDuplicateAsync(request.DemandId, request.FinancialYear, normalizedName, excludeId: null, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "A record with this Name already exists." });
        }

        var entity = new AppendixEstablishmentOtherThanAB { DemandId = request.DemandId, FinancialYear = request.FinancialYear };
        entity.UpdateFrom(
            normalizedName,
            request.Actuals,
            request.ActualsUptoSeptPrevYear,
            request.BE,
            request.ActualsUptoSept,
            request.ProposedRE,
            request.ProposedNBE,
            request.Remarks);

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(ToDto(saved));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixEstablishmentOtherThanABDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        // Name is frozen (readonly) client-side while editing, so this branch normally just
        // re-saves the same Name it was given - still normalized and re-checked here regardless,
        // since server-side validation must never depend on the client having disabled the field.
        var normalizedName = NormalizeName(request.Name);
        if (await HasDuplicateAsync(request.DemandId, request.FinancialYear, normalizedName, excludeId: id, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "A record with this Name already exists." });
        }

        try
        {
            var updated = await _repository.UpdateAsync(id, userId.Value, entity => entity.UpdateFrom(
                normalizedName,
                request.Actuals,
                request.ActualsUptoSeptPrevYear,
                request.BE,
                request.ActualsUptoSept,
                request.ProposedRE,
                request.ProposedNBE,
                request.Remarks), ct);
            return Ok(ToDto(updated));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Code = "UPDATE_FAILED", Message = ex.Message });
        }
    }

    // Whitespace-collapse + trim + capitalize-first-letter, matching the client-side on-blur
    // normalization exactly (see prebudget.js/pre-budget-meeting.js's normalizeVCName) - applied
    // here too so the persisted value is correct even if a caller bypasses the browser entirely.
    private static string NormalizeName(string? name)
    {
        var collapsed = Regex.Replace((name ?? string.Empty).Trim(), @"\s+", " ");
        return collapsed.Length == 0 ? collapsed : char.ToUpperInvariant(collapsed[0]) + collapsed[1..];
    }

    private async Task<bool> HasDuplicateAsync(int demandId, string financialYear, string normalizedName, int? excludeId, CancellationToken ct)
    {
        if (normalizedName.Length == 0)
        {
            return false;
        }
        return await _db.AppendixVCEstablishmentOtherThanABs.AsNoTracking().AnyAsync(e =>
            !e.IsDeleted
            && e.DemandId == demandId
            && e.FinancialYear == financialYear
            && e.Name.Trim().ToLower() == normalizedName.ToLower()
            && (excludeId == null || e.Id != excludeId.Value), ct);
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

    private static AppendixEstablishmentOtherThanABDto ToDto(AppendixEstablishmentOtherThanAB e) => new()
    {
        Id = e.Id,
        DemandId = e.DemandId,
        FinancialYear = e.FinancialYear,
        Name = e.Name,
        Actuals = e.Actuals,
        ActualsUptoSeptPrevYear = e.ActualsUptoSeptPrevYear,
        BE = e.BE,
        ActualsUptoSept = e.ActualsUptoSept,
        ProposedRE = e.ProposedRE,
        ProposedNBE = e.ProposedNBE,
        Remarks = e.Remarks,
        IsFrozen = e.IsFrozen
    };

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
