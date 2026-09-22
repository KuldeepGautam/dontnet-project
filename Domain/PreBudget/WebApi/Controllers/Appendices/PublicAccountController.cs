namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>Public Account Template: RE/BE for inclusion in Budget Estimates. Field list is provisional per the FRS itself (Open Item OI-05) — see design doc §8.6.</summary>
[ApiController]
[Route("api/appendix-pa")]
[Authorize]
public class PublicAccountController : ControllerBase
{
    private const string AppendixCode = "PA-ReceiptPayment";

    private readonly AppendixDataRepository<PublicAccountReceiptPayment> _repository;
    private readonly AppendixPermissionService _permissionService;

    public PublicAccountController(AppendixDataRepository<PublicAccountReceiptPayment> repository, AppendixPermissionService permissionService)
    {
        _repository = repository;
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecords([FromQuery] int demandId, [FromQuery] string financialYear, CancellationToken ct)
    {
        var records = await _repository.GetByDemandAndYearAsync(demandId, financialYear, ct);
        return Ok(records.Select(ToDto));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecord([FromBody] SavePublicAccountReceiptPaymentDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        var permissions = await _permissionService.GetPermissionsAsync(GetRoleNameFromClaims(), AppendixCode, ct);
        if (!permissions.CanCreate) return StatusCode(StatusCodes.Status403Forbidden, new { Code = "FORBIDDEN", Message = "Your role does not have permission to add records to the Public Account template." });

        // "Each Major Head can have single entry, duplicate entry not allowed" (client testing
        // feedback, 2026-08-24).
        if (await HasDuplicateAsync(request, excludeId: null, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "A record already exists for this Major Head." });
        }

        var entity = new PublicAccountReceiptPayment { DemandId = request.DemandId, FinancialYear = request.FinancialYear };
        entity.UpdateFrom(
            request.MajorHeadId,
            request.ActualReceipt,
            request.ActualPayment,
            request.BalanceAtEndReceipt,
            request.BalanceAtEndPayment,
            request.BEReceipt,
            request.BEPayment,
            request.AdjustmentReceipt,
            request.AdjustmentPayment,
            request.REReceipt,
            request.REPayment,
            request.NBEReceipt,
            request.NBEPayment,
            request.RemarksReceipt,
            request.RemarksPayment);

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(ToDto(saved));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SavePublicAccountReceiptPaymentDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        var permissions = await _permissionService.GetPermissionsAsync(GetRoleNameFromClaims(), AppendixCode, ct);
        if (!permissions.CanEdit) return StatusCode(StatusCodes.Status403Forbidden, new { Code = "FORBIDDEN", Message = "Your role does not have permission to edit Public Account template records." });

        if (await HasDuplicateAsync(request, excludeId: id, ct))
        {
            return BadRequest(new { Code = "DUPLICATE_ENTRY", Message = "A record already exists for this Major Head." });
        }

        try
        {
            var updated = await _repository.UpdateAsync(id, userId.Value, entity => entity.UpdateFrom(
                request.MajorHeadId,
                request.ActualReceipt,
                request.ActualPayment,
                request.BalanceAtEndReceipt,
                request.BalanceAtEndPayment,
                request.BEReceipt,
                request.BEPayment,
                request.AdjustmentReceipt,
                request.AdjustmentPayment,
                request.REReceipt,
                request.REPayment,
                request.NBEReceipt,
                request.NBEPayment,
                request.RemarksReceipt,
                request.RemarksPayment), ct);
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

        var permissions = await _permissionService.GetPermissionsAsync(GetRoleNameFromClaims(), AppendixCode, ct);
        if (!permissions.CanDelete) return StatusCode(StatusCodes.Status403Forbidden, new { Code = "FORBIDDEN", Message = "Your role does not have permission to delete Public Account template records." });

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

        var permissions = await _permissionService.GetPermissionsAsync(GetRoleNameFromClaims(), AppendixCode, ct);
        if (!permissions.CanSubmit) return StatusCode(StatusCodes.Status403Forbidden, new { Code = "FORBIDDEN", Message = "Your role does not have permission to freeze Public Account template records." });

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

    private static PublicAccountReceiptPaymentDto ToDto(PublicAccountReceiptPayment e) => new()
    {
        Id = e.Id,
        DemandId = e.DemandId,
        FinancialYear = e.FinancialYear,
        MajorHeadId = e.MajorHeadId,
        ActualReceipt = e.ActualReceipt,
        ActualPayment = e.ActualPayment,
        BalanceAtEndReceipt = e.BalanceAtEndReceipt,
        BalanceAtEndPayment = e.BalanceAtEndPayment,
        BEReceipt = e.BEReceipt,
        BEPayment = e.BEPayment,
        AdjustmentReceipt = e.AdjustmentReceipt,
        AdjustmentPayment = e.AdjustmentPayment,
        REReceipt = e.REReceipt,
        REPayment = e.REPayment,
        NBEReceipt = e.NBEReceipt,
        NBEPayment = e.NBEPayment,
        RemarksReceipt = e.RemarksReceipt,
        RemarksPayment = e.RemarksPayment,
        IsFrozen = e.IsFrozen
    };

    private async Task<bool> HasDuplicateAsync(SavePublicAccountReceiptPaymentDto request, int? excludeId, CancellationToken ct)
    {
        var existing = await _repository.GetByDemandAndYearAsync(request.DemandId, request.FinancialYear, ct);
        return existing.Any(e => e.MajorHeadId == request.MajorHeadId && (excludeId == null || e.Id != excludeId.Value));
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string? GetRoleNameFromClaims() => User?.FindFirst("RoleName")?.Value;
}
