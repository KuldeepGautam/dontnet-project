namespace UBIS.Services.PreBudget.WebApi.Controllers.Appendices;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UBIS.Services.PreBudget.Application.DTOs.Appendices;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;
using UBIS.Services.PreBudget.Infrastructure.Services;

/// <summary>Appendix X: Loan to Government Servants, etc. (Major Head 7610).</summary>
[ApiController]
[Route("api/appendix-x")]
[Authorize]
public class AppendixXController : ControllerBase
{
    private const string AppendixCode = "X";

    private readonly AppendixDataRepository<AppendixLoansToGovtServants> _repository;
    private readonly AppendixPermissionService _permissionService;

    public AppendixXController(AppendixDataRepository<AppendixLoansToGovtServants> repository, AppendixPermissionService permissionService)
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
    public async Task<IActionResult> CreateRecord([FromBody] SaveAppendixLoansToGovtServantsDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        var permissions = await _permissionService.GetPermissionsAsync(GetRoleNameFromClaims(), AppendixCode, ct);
        if (!permissions.CanCreate) return StatusCode(StatusCodes.Status403Forbidden, new { Code = "FORBIDDEN", Message = "Your role does not have permission to add records to Appendix X." });

        // Client requirement 2026-08-31: "if data is available, on submit, it should check, if
        // data is available in grid then no entry should be made, dialogbox validation must
        // appear, edit mode can edit values." Since 2026-08-31's fix to the fixed sub-head list
        // (only 3 rows: House Building Advances, Motor Cars, Computers - see
        // AppendixXViewModel.FixedSubHeadNames), a second Create for a sub-head that already has
        // a row for this Demand+FinancialYear is never meaningful - Edit is the only way to change
        // it from here on. Authoritative here (not just a UBIS_Web MVC-layer convenience check,
        // per this repo's "server-side enforcement always" convention) - SubHeadName is free text,
        // not an FK with a unique constraint of its own (see BuildAppendixXViewModelAsync's doc
        // comment), so nothing else would have caught this.
        var existing = await _repository.GetByDemandAndYearAsync(request.DemandId, request.FinancialYear, ct);
        var duplicate = existing.FirstOrDefault(e => string.Equals(e.SubHeadName?.Trim(), request.SubHeadName?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (duplicate != null)
        {
            return BadRequest(new
            {
                Code = "DUPLICATE_SUB_HEAD",
                Message = $"A record for '{request.SubHeadName}' already exists for this Demand. Use the Edit button on that row to update it instead."
            });
        }

        var zeroOrBlankError = ValidateNonZeroAmounts(request);
        if (zeroOrBlankError != null)
        {
            return BadRequest(zeroOrBlankError);
        }

        var entity = new AppendixLoansToGovtServants { DemandId = request.DemandId, FinancialYear = request.FinancialYear };
        entity.UpdateFrom(
            request.SubHeadName,
            request.ActualsY1,
            request.ActualsY2,
            request.ActualsY3,
            request.ActualsUptoSept,
            request.BE,
            request.RE,
            request.NBE);

        var saved = await _repository.AddAsync(entity, userId.Value, ct);
        return Ok(ToDto(saved));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] SaveAppendixLoansToGovtServantsDto request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null) return Unauthorized(new { Code = "UNAUTHORIZED", Message = "Token has no user id." });

        var permissions = await _permissionService.GetPermissionsAsync(GetRoleNameFromClaims(), AppendixCode, ct);
        if (!permissions.CanEdit) return StatusCode(StatusCodes.Status403Forbidden, new { Code = "FORBIDDEN", Message = "Your role does not have permission to edit Appendix X records." });

        var zeroOrBlankError = ValidateNonZeroAmounts(request);
        if (zeroOrBlankError != null)
        {
            return BadRequest(zeroOrBlankError);
        }

        try
        {
            var updated = await _repository.UpdateAsync(id, userId.Value, entity => entity.UpdateFrom(
                request.SubHeadName,
                request.ActualsY1,
                request.ActualsY2,
                request.ActualsY3,
                request.ActualsUptoSept,
                request.BE,
                request.RE,
                request.NBE), ct);
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
        if (!permissions.CanDelete) return StatusCode(StatusCodes.Status403Forbidden, new { Code = "FORBIDDEN", Message = "Your role does not have permission to delete Appendix X records." });

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
        if (!permissions.CanSubmit) return StatusCode(StatusCodes.Status403Forbidden, new { Code = "FORBIDDEN", Message = "Your role does not have permission to freeze Appendix X records." });

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

    /// <summary>
    /// Client requirement 2026-08-31: "0 value not allowed for the rows that are updating or
    /// adding" - server-side is the real gate per this repo's convention (the client-side check
    /// in pre-budget-meeting.js's Submit handler is a convenience). Every row reaching Create/
    /// Update already has at least one non-blank field (PreBudgetMeetingController.AppendixX.cs
    /// skips a row with all 7 fields blank before ever calling here), so at this point every
    /// field is required to be present and non-zero.
    /// </summary>
    private static object? ValidateNonZeroAmounts(SaveAppendixLoansToGovtServantsDto request)
    {
        if (request.ActualsY1 is null or 0 || request.ActualsY2 is null or 0 || request.ActualsY3 is null or 0
            || request.ActualsUptoSept is null or 0 || request.BE is null or 0 || request.RE is null or 0
            || request.NBE is null or 0)
        {
            return new { Code = "VALIDATION_FAILED", Message = $"Enter non-zero values for all fields in '{request.SubHeadName}'." };
        }

        return null;
    }

    private static AppendixLoansToGovtServantsDto ToDto(AppendixLoansToGovtServants e) => new()
    {
        Id = e.Id,
        DemandId = e.DemandId,
        FinancialYear = e.FinancialYear,
        SubHeadName = e.SubHeadName,
        ActualsY1 = e.ActualsY1,
        ActualsY2 = e.ActualsY2,
        ActualsY3 = e.ActualsY3,
        ActualsUptoSept = e.ActualsUptoSept,
        BE = e.BE,
        RE = e.RE,
        NBE = e.NBE,
        IsFrozen = e.IsFrozen
    };

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string? GetRoleNameFromClaims() => User?.FindFirst("RoleName")?.Value;
}
