namespace UBIS.Services.PreBudget.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UBIS.Services.PreBudget.Application.DTOs;
using UBIS.Services.PreBudget.Application.Interfaces;
using UBIS.Services.PreBudget.Domain.Entities;
using UBIS.Services.PreBudget.Infrastructure.Persistence;
using UBIS.Services.PreBudget.Infrastructure.Security;

public class RemarkService : IRemarkService
{
    private readonly PreBudgetDbContext _db;
    private readonly RemarkRoleOptions _roleOptions;

    public RemarkService(PreBudgetDbContext db, IOptions<RemarkRoleOptions> roleOptions)
    {
        _db = db;
        _roleOptions = roleOptions.Value;
    }

    public async Task<IReadOnlyList<RemarkDto>> GetRemarksAsync(int demandId, int appendixId, string financialYear, string callerRoleName, CancellationToken ct)
    {
        EnsureRoleAllowed(callerRoleName);

        var remarks = await _db.Remarks
            .Where(r => r.DemandId == demandId && r.AppendixId == appendixId && r.FinancialYear == financialYear)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(ct);

        return remarks.Select(ToDto).ToList();
    }

    public async Task<RemarkDto> CreateRemarkAsync(CreateRemarkDto request, int userId, string callerRoleName, CancellationToken ct)
    {
        EnsureRoleAllowed(callerRoleName);

        var entity = new Remark
        {
            DemandId = request.DemandId,
            AppendixId = request.AppendixId,
            FinancialYear = request.FinancialYear,
            RemarkText = request.RemarkText,
            CreatedByUserId = userId,
            CreatedByRoleSnapshot = callerRoleName,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Remarks.Add(entity);
        await _db.SaveChangesAsync(ct);

        return ToDto(entity);
    }

    private void EnsureRoleAllowed(string callerRoleName)
    {
        if (!_roleOptions.AllowedRoleNames.Contains(callerRoleName, StringComparer.OrdinalIgnoreCase))
        {
            throw new RemarkAccessDeniedException();
        }
    }

    private static RemarkDto ToDto(Remark r) => new()
    {
        Id = r.Id,
        DemandId = r.DemandId,
        AppendixId = r.AppendixId,
        FinancialYear = r.FinancialYear,
        RemarkText = r.RemarkText,
        CreatedByRoleSnapshot = r.CreatedByRoleSnapshot,
        CreatedAtUtc = r.CreatedAtUtc
    };
}
