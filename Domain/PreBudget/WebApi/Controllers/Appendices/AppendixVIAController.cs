namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>Appendix VI-A: List of User Charges levied by the Departments/Ministries.</summary>
[ApiController]
[Route("api/appendix-via")]
[Authorize]
public class AppendixVIAController : ControllerBase
{
    private readonly AppendixDataRepository<AppendixUserCharges> _repository;

    public AppendixVIAController(AppendixDataRepository<AppendixUserCharges> repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecords([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var records = await _repository.GetByDemandAndYearAsync(demandId, financialYear, ct);
        return Ok(records.Select(ToDto));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixUserChargesDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        //Implement validation to prevent duplicate entry for "Title of the User Charge:"
        if (await _repository.ExistsMatchingAsync(request.DemandId, request.FinancialYear, e => e.TitleOfCharge == request.TitleOfCharge, excludeId: null, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "Data for this Demand/TitleOfCharge combination already exists for this Demand." });
        }

        var entity = new AppendixUserCharges { DemandId = request.DemandId, FinancialYear = request.FinancialYear };
        entity.UpdateFrom(
            request.TitleOfCharge,
            request.Service,
            request.OrgDept,
            request.RateOfCharge,
            request.UnitOfCollection,
            request.DateOfRateFixation,
            request.FixationStatute,
            request.TotalRevenueY1,
            request.TotalRevenueY2,
            request.TotalRevenueY3,
            request.CompetentAuthority,
            request.PeriodOfFixation,
            request.Salary,
            request.OfficeExpenses,
            request.OtherExpenses,
            request.IsCollectionCostHigher,
            request.IsTransCostHigher,
            request.Remarks);

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(ToDto(saved));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixUserChargesDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        try
        {
            var updated = await _repository.UpdateAsync(id, userId.Value, entity => entity.UpdateFrom(
                request.TitleOfCharge,
                request.Service,
                request.OrgDept,
                request.RateOfCharge,
            request.UnitOfCollection,
                request.DateOfRateFixation,
                request.FixationStatute,
                request.TotalRevenueY1,
                request.TotalRevenueY2,
                request.TotalRevenueY3,
                request.CompetentAuthority,
                request.PeriodOfFixation,
                request.Salary,
                request.OfficeExpenses,
                request.OtherExpenses,
                request.IsCollectionCostHigher,
                request.IsTransCostHigher,
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

    private static AppendixUserChargesDto ToDto(AppendixUserCharges e) => new()
    {
        Id = e.Id,
        DemandId = e.DemandId,
        FinancialYear = e.FinancialYear,
        TitleOfCharge = e.TitleOfCharge,
        Service = e.Service,
        OrgDept = e.OrgDept,
        RateOfCharge = e.RateOfCharge,
        UnitOfCollection = e.UnitOfCollection,
        DateOfRateFixation = e.DateOfRateFixation,
        FixationStatute = e.FixationStatute,
        TotalRevenueY1 = e.TotalRevenueY1,
        TotalRevenueY2 = e.TotalRevenueY2,
        TotalRevenueY3 = e.TotalRevenueY3,
        CompetentAuthority = e.CompetentAuthority,
        PeriodOfFixation = e.PeriodOfFixation,
        Salary = e.Salary,
        OfficeExpenses = e.OfficeExpenses,
        OtherExpenses = e.OtherExpenses,
        IsCollectionCostHigher = e.IsCollectionCostHigher,
        IsTransCostHigher = e.IsTransCostHigher,
        Remarks = e.Remarks,
        IsFrozen = e.IsFrozen
    };

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
