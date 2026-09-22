namespace UBIS.Services.Aim.Infrastructure.Otp;

using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UBIS.Services.Aim.Application.Interfaces;
using UBIS.Services.Aim.Application.DTOs;
using UBIS.Services.Aim.Infrastructure.Configuration;
using UBIS.Services.Aim.Infrastructure.Persistence;

public class OtpService : IOtpService
{
    private readonly ICacheService _cache;
    private readonly AimDbContext _db;
    private readonly IEmailServiceClient _emailServiceClient;
    private readonly ISmsServiceClient _smsServiceClient;
    private readonly ILogWriterClient _logWriter;
    private readonly OtpOptions _options;
    private readonly MfaOptions _mfaOptions;
    private readonly PasswordResetOtpOptions _passwordResetOtpOptions;
    private readonly AppSettingsReader _appSettingsReader;
    private readonly IMobileProtectionService _mobileProtectionService;

    public OtpService(
        ICacheService cache,
        AimDbContext db,
        IEmailServiceClient emailServiceClient,
        ISmsServiceClient smsServiceClient,
        ILogWriterClient logWriter,
        IOptions<OtpOptions> options,
        IOptions<MfaOptions> mfaOptions,
        IOptions<PasswordResetOtpOptions> passwordResetOtpOptions,
        AppSettingsReader appSettingsReader,
        IMobileProtectionService mobileProtectionService)
    {
        _cache = cache;
        _db = db;
        _emailServiceClient = emailServiceClient;
        _smsServiceClient = smsServiceClient;
        _logWriter = logWriter;
        _options = options.Value;
        _mfaOptions = mfaOptions.Value;
        _passwordResetOtpOptions = passwordResetOtpOptions.Value;
        _appSettingsReader = appSettingsReader;
        _mobileProtectionService = mobileProtectionService;
    }

    public async Task<string> GenerateAndStoreOtpAsync(int userId, OtpPurpose purpose, CancellationToken ct = default)
    {
        var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        // produce 6-digit code
        var code = (BitConverter.ToUInt32(bytes, 0) % 1_000_000).ToString("D6");

        var hashed = HashOtp(code);

        var key = BuildKey(userId, purpose);
        var payload = new OtpCacheEntry { OtpHash = hashed, ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes), Attempts = 0 };
        await _cache.SetAsync(key, payload, TimeSpan.FromMinutes(_options.ExpiryMinutes), cancellationToken: ct);

        await DispatchOtpAsync(userId, purpose, code, ct);

        return code;
    }

    /// <summary>
    /// Hands the freshly-minted OTP to the right channel(s). The plaintext code only ever travels
    /// in this one dispatch call — it must never be included in anything sent to LogWriter.
    /// <see cref="OtpPurpose.PasswordReset"/> (added 2026-07-22, forgot-password dialog) is
    /// multi-channel — Email gated by <see cref="PasswordResetOtpOptions"/>, Mobile gated by
    /// dbo.AppSettings.EnableSms (moved off config 2026-08-07) — dispatches to whichever are
    /// enabled. Every other purpose keeps the original single-channel behavior driven by
    /// <c>Mfa:Channel</c>, unchanged.
    /// </summary>
    private async Task DispatchOtpAsync(int userId, OtpPurpose purpose, string code, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user == null)
        {
            await _logWriter.WarnAsync("OTP generated but user not found for dispatch.", new { UserId = userId, Purpose = purpose.ToString() }, ct);
            return;
        }

        if (purpose == OtpPurpose.PasswordReset)
        {
            if (_passwordResetOtpOptions.EmailEnabled)
            {
                await DispatchEmailOtpAsync(user, purpose, code, ct);
            }

            if (await _appSettingsReader.GetBoolAsync("EnableSms", ct: ct))
            {
                await DispatchSmsOtpAsync(user, userId, purpose, code, ct);
            }

            return;
        }

        if (string.Equals(_mfaOptions.Channel, "Sms", StringComparison.OrdinalIgnoreCase))
        {
            await DispatchSmsOtpAsync(user, userId, purpose, code, ct);
            return;
        }

        await DispatchEmailOtpAsync(user, purpose, code, ct);
    }

    private async Task DispatchSmsOtpAsync(Domain.Entities.User user, int userId, OtpPurpose purpose, string code, CancellationToken ct)
    {
        // Real decrypted number needed here (unlike a masked-display call site) — this is the
        // value actually handed to the SMS gateway.
        var plainMobile = _mobileProtectionService.Unprotect(user.EncryptedMobile);
        if (string.IsNullOrWhiteSpace(plainMobile))
        {
            await _logWriter.WarnAsync("OTP generated but user has no mobile number on file for dispatch.", new { UserId = userId, Purpose = purpose.ToString() }, ct);
            return;
        }

        var smsResult = await _smsServiceClient.SendOtpSmsAsync(plainMobile, code, ct);
        if (!smsResult.IsSuccess)
        {
            await _logWriter.WarnAsync("SMS OTP dispatch failed.", new { UserId = userId, Purpose = purpose.ToString(), smsResult.Error?.Code }, ct);
        }
    }

    private async Task DispatchEmailOtpAsync(Domain.Entities.User user, OtpPurpose purpose, string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            await _logWriter.WarnAsync("OTP generated but user has no email on file for dispatch.", new { UserId = user.UserId, Purpose = purpose.ToString() }, ct);
            return;
        }

        var messageId = Guid.NewGuid();
        try
        {
            await _emailServiceClient.SendOtpEmailAsync(new OtpEmailDispatchRequest
            {
                MessageId = messageId,
                ToEmail = user.Email,
                ToName = user.FullName,
                Otp = code,
                Purpose = purpose.ToString(),
                ExpiryMinutes = _options.ExpiryMinutes,
                RequestedAtUtc = DateTime.UtcNow
            }, ct);

            await _logWriter.InfoAsync("OTP email dispatch requested.", new { MessageId = messageId, UserId = user.UserId, Purpose = purpose.ToString() }, ct);
        }
        catch (Exception ex)
        {
            await _logWriter.ErrorAsync("Failed to publish OTP email dispatch request.", ex, new { MessageId = messageId, UserId = user.UserId, Purpose = purpose.ToString() }, ct);
        }
    }

    public async Task<Result<bool>> VerifyOtpAsync(int userId, OtpPurpose purpose, string suppliedOtp, CancellationToken ct = default)
    {
        var key = BuildKey(userId, purpose);
        var entry = await _cache.GetAsync<OtpCacheEntry>(key, ct);
        if (entry == null) return Result<bool>.Failure("OTP_INVALID", "Invalid or expired OTP");

        if (entry.Attempts >= _options.MaxAttempts)
        {
            await _cache.RemoveAsync(key, ct);
            return Result<bool>.Failure("OTP_ATTEMPTS_EXCEEDED", "OTP attempts exceeded");
        }

        var hash = HashOtp(suppliedOtp);
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hash), Encoding.UTF8.GetBytes(entry.OtpHash)))
        {
            // increment attempts
            entry.Attempts++;
            await _cache.SetAsync(key, entry, TimeSpan.FromMinutes(_options.ExpiryMinutes), cancellationToken: ct);
            return Result<bool>.Failure("OTP_INVALID", "Invalid OTP");
        }

        // success: delete key
        await _cache.RemoveAsync(key, ct);
        return Result<bool>.Success(true);
    }

    private static string HashOtp(string otp)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(otp));
        return Convert.ToHexString(bytes);
    }

    private static string BuildKey(int userId, OtpPurpose purpose) => $"ubis:otp:{purpose}:{userId}";

    private class OtpCacheEntry
    {
        public string OtpHash { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public int Attempts { get; set; }
    }
}

public class OtpOptions
{
    public int ExpiryMinutes { get; set; } = 5;
    public int MaxAttempts { get; set; } = 5;
}
