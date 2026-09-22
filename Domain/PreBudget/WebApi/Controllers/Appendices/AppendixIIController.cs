namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Persistence;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>
/// Appendix II: Quarterly Expenditure Plan (QEP). Enforces the FRS's Deviation Validations
/// (§8.4/prototype review §11.3 of the evaluation doc): if Q1/Q2HasDeviation is set, the matching
/// Remarks field becomes mandatory. MoF Approval Details was dropped from the drawer (client
/// instruction 2026-09-10) and is no longer bound to anything on the UI.
/// </summary>
[ApiController]
[Route("api/appendix-ii")]
[Authorize]
public class AppendixIIController : ControllerBase
{
    private readonly AppendixDataRepository<AppendixQuarterlyExpenditurePlan> _repository;
    private readonly PreBudgetDbContext _db;

    public AppendixIIController(AppendixDataRepository<AppendixQuarterlyExpenditurePlan> repository, PreBudgetDbContext db)
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

    /// <summary>
    /// Q1/Q2 "As per approved QEP" pre-fill, per the client's reference SQL (2026-08-07): resolves
    /// PrevDemandId via M_Demand, then sums dbo.T_QEPData.Total for that prior demand+quarter,
    /// scaled /10000 and rounded to 2 decimals (matches legacy FORMAT(SUM(Total)/10000.0,'0.##')).
    /// Replaces the earlier self-referential version that read Appendix II's own prior-year saved
    /// row instead - wrong per this reference SQL, which re-derives from the QEP-specific legacy
    /// source table via PrevDemandId, not from whatever this appendix itself has on file.
    /// </summary>
    [HttpGet("previous-year-approved-qep")]
    public async Task<IActionResult> GetPreviousYearApprovedQep([FromQuery] int demandId, CancellationToken ct)
    {
        var prevDemandId = await _db.Demands.AsNoTracking()
            .Where(d => d.DemandId == demandId)
            .Select(d => d.PrevDemandId)
            .FirstOrDefaultAsync(ct);

        if (prevDemandId is null)
        {
            return Ok(new { found = false });
        }

        var q1Total = await _db.QepDataRows.AsNoTracking()
            .Where(q => q.DemandId == prevDemandId.Value && q.QuarterCode == 1)
            .SumAsync(q => (decimal?)q.Total, ct) ?? 0m;

        var q2Total = await _db.QepDataRows.AsNoTracking()
            .Where(q => q.DemandId == prevDemandId.Value && q.QuarterCode == 2)
            .SumAsync(q => (decimal?)q.Total, ct) ?? 0m;

        return Ok(new
        {
            found = true,
            q1ApprovedQep = Math.Round(q1Total / 10000m, 2),
            q2ApprovedQep = Math.Round(q2Total / 10000m, 2)
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixQuarterlyExpenditurePlanDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        // Client requirement 2026-08-27: "data can be edited if there is entry, second entry is
        // prohibited" - same one-row-per-Demand+FinancialYear rule as Appendix I-A.
        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear, _ => true, excludeId: null, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "A record for this Demand already exists." });
        }

        var entity = new AppendixQuarterlyExpenditurePlan
        {
            DemandId = request.DemandId,
            FinancialYear = request.FinancialYear
        };

        try
        {
            entity.UpdateFrom(
                request.Q1ApprovedQepPrevYear,
                request.Q1ActualsPrevYear,
                request.Q1ApprovedQep,
                request.Q1Actuals,
                request.RemarksQ1,
                request.Q1HasDeviation,
                request.Q1MofApprovalDetails,
                request.Q2ApprovedQepPrevYear,
                request.Q2ActualsPrevYear,
                request.Q2ApprovedQep,
                request.Q2Actuals,
                request.RemarksQ2,
                request.Q2HasDeviation,
                request.Q2MofApprovalDetails);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Code = "VALIDATION_ERROR", Message = ex.Message });
        }

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(ToDto(saved));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixQuarterlyExpenditurePlanDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

        try
        {
            var updated = await _repository.UpdateAsync(id, userId.Value, entity => entity.UpdateFrom(
                request.Q1ApprovedQepPrevYear,
                request.Q1ActualsPrevYear,
                request.Q1ApprovedQep,
                request.Q1Actuals,
                request.RemarksQ1,
                request.Q1HasDeviation,
                request.Q1MofApprovalDetails,
                request.Q2ApprovedQepPrevYear,
                request.Q2ActualsPrevYear,
                request.Q2ApprovedQep,
                request.Q2Actuals,
                request.RemarksQ2,
                request.Q2HasDeviation,
                request.Q2MofApprovalDetails), ct);
            return Ok(ToDto(updated));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Code = "VALIDATION_ERROR", Message = ex.Message });
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
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

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
        if (userId is null)
        {
            return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });
        }

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

    private static AppendixQuarterlyExpenditurePlanDto ToDto(AppendixQuarterlyExpenditurePlan e) => new()
    {
        Id = e.Id,
        DemandId = e.DemandId,
        FinancialYear = e.FinancialYear,
        Q1ApprovedQepPrevYear = e.Q1ApprovedQepPrevYear,
        Q1ActualsPrevYear = e.Q1ActualsPrevYear,
        Q1ApprovedQep = e.Q1ApprovedQep,
        Q1Actuals = e.Q1Actuals,
        RemarksQ1 = e.RemarksQ1,
        Q1HasDeviation = e.Q1HasDeviation,
        Q1MofApprovalDetails = e.Q1MofApprovalDetails,
        Q2ApprovedQepPrevYear = e.Q2ApprovedQepPrevYear,
        Q2ActualsPrevYear = e.Q2ActualsPrevYear,
        Q2ApprovedQep = e.Q2ApprovedQep,
        Q2Actuals = e.Q2Actuals,
        RemarksQ2 = e.RemarksQ2,
        Q2HasDeviation = e.Q2HasDeviation,
        Q2MofApprovalDetails = e.Q2MofApprovalDetails,
        TotalApprovedQep = e.TotalApprovedQep,
        TotalActuals = e.TotalActuals,
        IsFrozen = e.IsFrozen
    };

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
