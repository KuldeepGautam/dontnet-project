namespace UBIS.Services.UserProfile.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using UBIS.Services.UserProfile.Application.DTOs;
using UBIS.Services.UserProfile.Application.Interfaces;
using UBIS.Services.UserProfile.Domain.Entities;
using UBIS.Services.UserProfile.Infrastructure.Persistence;

public class UserIpRequestService : IUserIpRequestService
{
    private readonly UserProfileDbContext _db;

    public UserIpRequestService(UserProfileDbContext db)
    {
        _db = db;
    }

    public async Task<IpChangeRequestStatusDto?> GetLatestRequestAsync(int userId, CancellationToken ct = default)
    {
        var latest = await _db.IpRequests.AsNoTracking()
            .Where(r => r.UsersId == userId)
            .OrderByDescending(r => r.RequestDate)
            .FirstOrDefaultAsync(ct);

        return latest == null ? null : ToStatusDto(latest);
    }

    public async Task<List<IpRequestHistoryItemDto>> GetAllRequestsAsync(int userId, CancellationToken ct = default)
    {
        var rows = await _db.IpRequests.AsNoTracking()
            .Where(r => r.UsersId == userId)
            .OrderByDescending(r => r.RequestDate)
            .ToListAsync(ct);

        return rows.Select(r => new IpRequestHistoryItemDto
        {
            RequestNumber = r.RowId,
            IPAddress1 = r.IPadres1,
            IPAddress2 = r.IPadres2,
            RequestDate = r.RequestDate,
            Status = r.ApproveFlag == null ? "Active" : "Completed"
        }).ToList();
    }

    public async Task<IpChangeRequestStatusDto> RaiseIpChangeRequestAsync(
        int userId, RaiseIpChangeRequestDto request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var entity = new UserIpRequest
        {
            UsersId = userId,
            IPadres1 = request.IPAddress1,
            IPadres2 = request.IPAddress2,
            RequestDate = now,
            ApproveFlag = null, // Pending — an admin approval workflow decides Y/N/R later.
            IP = request.ExistingIp,
            UserIdCreatedBy = userId,
            CreatedOnDate = now
        };

        _db.IpRequests.Add(entity);
        await _db.SaveChangesAsync(ct);

        return ToStatusDto(entity);
    }

    private static IpChangeRequestStatusDto ToStatusDto(UserIpRequest entity) => new()
    {
        IPAddress1 = entity.IPadres1,
        IPAddress2 = entity.IPadres2,
        RequestDate = entity.RequestDate,
        ApproveDate = entity.ApproveDate,
        Status = entity.ApproveFlag switch
        {
            null => IpRequestStatus.Pending,
            ApproveFlagValues.Approved => IpRequestStatus.Approved,
            _ => IpRequestStatus.NotApproved
        }
    };
}
