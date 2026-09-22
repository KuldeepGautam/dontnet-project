namespace UBIS.Services.Aim.Application.Interfaces;

public interface IPasswordHistoryService
{
    Task<bool> IsPasswordReusedAsync(int userId, string newPlainPassword, CancellationToken ct = default);

    Task AddPasswordHistoryAsync(int userId, string passwordHash, CancellationToken ct = default);
}
