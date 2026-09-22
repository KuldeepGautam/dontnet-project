namespace UBIS.Services.Aim.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using UBIS.Services.Aim.Application.Interfaces;
using UBIS.Services.Aim.Infrastructure.Persistence;

public class PasswordHistoryService : IPasswordHistoryService
{
    private readonly AimDbContext _db;

    public PasswordHistoryService(AimDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsPasswordReusedAsync(int userId, string newPlainPassword, CancellationToken ct = default)
    {
        var last3 = await _db.PasswordHistories
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.CreatedAtUtc)
            .Take(3)
            .Select(ph => ph.PasswordHash)
            .ToListAsync(ct);

        foreach (var oldHash in last3)
        {
            if (BCrypt.Net.BCrypt.Verify(newPlainPassword, oldHash))
                return true;
        }

        return false;
    }

    public async Task AddPasswordHistoryAsync(int userId, string passwordHash, CancellationToken ct = default)
    {
        var ph = new UBIS.Services.Aim.Domain.Entities.PasswordHistory
        {
            UserId = userId,
            PasswordHash = passwordHash,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.PasswordHistories.Add(ph);
        await _db.SaveChangesAsync(ct);

        // prune older rows beyond last 3
        var idsToKeep = await _db.PasswordHistories
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(3)
            .Select(p => p.PasswordHistoryId)
            .ToListAsync(ct);

        var toDelete = await _db.PasswordHistories
            .Where(p => p.UserId == userId && !idsToKeep.Contains(p.PasswordHistoryId))
            .ToListAsync(ct);
        if (toDelete.Any())
        {
            _db.PasswordHistories.RemoveRange(toDelete);
            await _db.SaveChangesAsync(ct);
        }
    }
}
