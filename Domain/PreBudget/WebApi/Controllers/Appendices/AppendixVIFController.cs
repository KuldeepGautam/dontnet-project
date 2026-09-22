namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Persistence;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>Appendix VI-F: User Charges of Ministries/Departments, keyed by Minor Head. New
/// appendix, 2026-09-09 - see UBIS-Pre-budget-html/Appendix6f.html.</summary>
[ApiController]
[Route("api/appendix-vif")]
[Authorize]
public class AppendixVIFController : ControllerBase
{
    private readonly AppendixDataRepository<AppendixMinorHeadUserCharges> _repository;
    private readonly PreBudgetDbContext _db;

    public AppendixVIFController(AppendixDataRepository<AppendixMinorHeadUserCharges> repository, PreBudgetDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecords([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var records = await _repository.GetByDemandAndYearAsync(demandId, financialYear, ct);
        return Ok(await ToDtosAsync(records, financialYear, ct));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixMinorHeadUserChargesDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        // Single entry per Demand per Minor Head - same uniqueness convention as every other
        // identifying-field appendix in this module (VI-C/VI-E's own AutonomousBodyId check, etc.).
        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => e.MinorHeadCode == request.MinorHeadCode, excludeId: null, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = $"Data for Minor Head '{request.MinorHeadCode}' already exists for this Demand." });
        }

        var entity = new AppendixMinorHeadUserCharges { DemandId = request.DemandId, FinancialYear = request.FinancialYear };
        entity.UpdateFrom(
            request.MinorHeadCode,
            request.BriefOnReceipts,
            request.PresentStatus,
            request.NoOfTransactions,
            request.RateOfService,
            request.ReceiptsCollection,
            request.ActionTakenPlan);

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(await ToDtoAsync(saved, request.FinancialYear, ct));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixMinorHeadUserChargesDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear,
                e => e.MinorHeadCode == request.MinorHeadCode, excludeId: id, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = $"Data for Minor Head '{request.MinorHeadCode}' already exists for this Demand." });
        }

        try
        {
            var updated = await _repository.UpdateAsync(id, userId.Value, entity => entity.UpdateFrom(
                request.MinorHeadCode,
                request.BriefOnReceipts,
                request.PresentStatus,
                request.NoOfTransactions,
                request.RateOfService,
                request.ReceiptsCollection,
                request.ActionTakenPlan), ct);
            return Ok(await ToDtoAsync(updated, request.FinancialYear, ct));
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
            return Ok(await ToDtoAsync(frozen, frozen.FinancialYear, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Code = "FREEZE_FAILED", Message = ex.Message });
        }
    }

    /// <summary>
    /// AJAX-only: Minor Head autocomplete. Client requirement 2026-09-09: "Instead of dropdown, use
    /// textbox, user type in and after 4 characters, automatic suggestion from [this] list comes" -
    /// per the client's own reference SQL: "select distinct left(HeadOfAccount,9) as minorhead from
    /// DDgnew where FinancialYear=@fy and Demandno=@demandNo order by minorhead". dbo.DDG (mapped by
    /// DdgNewRows) is the already-migrated UBIS-Dev copy of BIMSDemo's DDGNew - confirmed identical
    /// row counts directly against both databases (2026-09-09).
    /// </summary>
    [HttpGet("minor-heads")]
    public async Task<IActionResult> SearchMinorHeads([FromQuery] int demandId, [FromQuery] string financialYear, [FromQuery] string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 4)
        {
            return Ok(new List<MinorHeadSuggestionDto>());
        }

        var demandNo = await _db.Demands.Where(d => d.DemandId == demandId).Select(d => (int?)d.DemandNo).FirstOrDefaultAsync(ct);

        var codes = await _db.DdgNewRows
            .Where(d => d.FinancialYear == financialYear && (demandNo != null ? d.DemandNo == demandNo : d.DemandId == demandId))
            .Where(d => d.HeadOfAccount.Length >= 9 && d.HeadOfAccount.StartsWith(query))
            .Select(d => d.HeadOfAccount.Substring(0, 9))
            .Distinct()
            .OrderBy(c => c)
            .Take(20)
            .ToListAsync(ct);

        return Ok(codes.Select(c => new MinorHeadSuggestionDto { Code = c }));
    }

    /// <summary>
    /// AJAX-only: resolves a Minor Head code's human-readable name once picked/typed, per the
    /// client's own reference SQL: "select * from Actdrx where Financialyear=@fy and
    /// AccountHead=@MinorHead" (dbo.M_MinorHeadName is the migrated UBIS-Dev copy of BIMSDemo's
    /// Actdrx, Major+Minor-Head-level rows only - see prebudget-workstream-migrate-minorheadname.sql).
    /// </summary>
    [HttpGet("minor-head-name")]
    public async Task<IActionResult> GetMinorHeadName([FromQuery] string minorHeadCode, [FromQuery] string financialYear, CancellationToken ct)
    {
        var name = await _db.MinorHeadNames
            .Where(m => m.AccountHead == minorHeadCode && m.FinancialYear == financialYear)
            .Select(m => m.AccountHeadName)
            .FirstOrDefaultAsync(ct);

        return Ok(new MinorHeadNameDto { Found = name != null, Name = name });
    }

    private async Task<AppendixMinorHeadUserChargesDto> ToDtoAsync(AppendixMinorHeadUserCharges e, string financialYear, CancellationToken ct)
    {
        var dtos = await ToDtosAsync(new List<AppendixMinorHeadUserCharges> { e }, financialYear, ct);
        return dtos[0];
    }

    private async Task<List<AppendixMinorHeadUserChargesDto>> ToDtosAsync(List<AppendixMinorHeadUserCharges> entities, string financialYear, CancellationToken ct)
    {
        var codes = entities.Select(e => e.MinorHeadCode).Distinct().ToList();
        var names = await _db.MinorHeadNames
            .Where(m => codes.Contains(m.AccountHead) && m.FinancialYear == financialYear)
            .ToDictionaryAsync(m => m.AccountHead, m => m.AccountHeadName, ct);

        return entities.Select(e => new AppendixMinorHeadUserChargesDto
        {
            Id = e.Id,
            DemandId = e.DemandId,
            FinancialYear = e.FinancialYear,
            MinorHeadCode = e.MinorHeadCode,
            MinorHeadName = names.TryGetValue(e.MinorHeadCode, out var name) ? name : null,
            BriefOnReceipts = e.BriefOnReceipts,
            PresentStatus = e.PresentStatus,
            NoOfTransactions = e.NoOfTransactions,
            RateOfService = e.RateOfService,
            ReceiptsCollection = e.ReceiptsCollection,
            ActionTakenPlan = e.ActionTakenPlan,
            IsFrozen = e.IsFrozen
        }).ToList();
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
