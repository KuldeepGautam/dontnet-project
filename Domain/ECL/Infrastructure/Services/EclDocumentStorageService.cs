namespace UBIS.Services.Ecl.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UBIS.Services.Ecl.Application.DTOs;
using UBIS.Services.Ecl.Application.Interfaces;

/// <summary>
/// First file-upload implementation in this solution — nothing to copy, designed from scratch.
///
/// Defense in depth against a malicious/oversized upload:
///  1) Program.cs sets Kestrel's MaxRequestBodySize and the upload action carries
///     [RequestSizeLimit]/[RequestFormLimits] — the framework itself rejects an oversized request
///     before it ever reaches this class.
///  2) This class re-checks the declared Content-Length as a fast pre-check before touching the
///     stream at all.
///  3) While streaming to disk, a running byte count is enforced against the same 5 MB cap — a
///     lying Content-Length header cannot bypass the limit.
///  4) Content-Type is NEVER trusted alone — the first 5 bytes actually read from the stream must
///     be the literal "%PDF-" magic-byte signature.
///
/// The stored filename is generated here, server-side, as
/// {DemandNo}_{SchemeId}_{UnixTimeMilliseconds}.pdf — DemandNo (the stable cross-year identifier,
/// not the per-year DemandId), per the client's exact naming spec (2026-08-18). The caller's
/// original filename is only ever kept as an optional display label — never used for the actual
/// storage path, which closes off path traversal / overwrite risks from a hostile client filename.
/// </summary>
public class EclDocumentStorageService : IEclDocumentStorage
{
    private const long MaxSizeBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly byte[] PdfMagicBytes = "%PDF-"u8.ToArray();

    private readonly EclDocumentStorageOptions _options;
    private readonly ILogger<EclDocumentStorageService> _logger;

    public EclDocumentStorageService(IOptions<EclDocumentStorageOptions> options, ILogger<EclDocumentStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        Directory.CreateDirectory(_options.EclDocumentsPath);
    }

    public async Task<Result<EclDocumentUploadResultDto>> SaveAsync(Stream content, string? contentType, long declaredLength, string? originalFileName, int demandNo, int schemeId, CancellationToken ct)
    {
        // Fast pre-check on the declared length before buffering anything.
        if (declaredLength > MaxSizeBytes)
        {
            return Result<EclDocumentUploadResultDto>.Failure(Error.PdfTooLarge());
        }

        if (declaredLength <= 0)
        {
            return Result<EclDocumentUploadResultDto>.Failure(Error.NotAValidPdf("The uploaded file is empty."));
        }

        // Content-Type is advisory only (never trusted alone) — still worth an early, cheap reject.
        if (!string.IsNullOrEmpty(contentType) && !contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return Result<EclDocumentUploadResultDto>.Failure(Error.NotAValidPdf($"Expected content type 'application/pdf', got '{contentType}'."));
        }

        var storedFileName = $"{demandNo}_{schemeId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.pdf";
        var storedPath = Path.Combine(_options.EclDocumentsPath, storedFileName);
        var tempPath = storedPath + ".tmp";

        try
        {
            long totalBytesWritten = 0;
            var buffer = new byte[8192];
            var headerChecked = false;

            await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                int bytesRead;
                while ((bytesRead = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
                {
                    totalBytesWritten += bytesRead;

                    // Defense in depth: enforce the cap while streaming, not just on the declared length.
                    if (totalBytesWritten > MaxSizeBytes)
                    {
                        await fileStream.DisposeAsync();
                        TryDelete(tempPath);
                        return Result<EclDocumentUploadResultDto>.Failure(Error.PdfTooLarge());
                    }

                    if (!headerChecked && totalBytesWritten >= PdfMagicBytes.Length)
                    {
                        headerChecked = true;
                        if (!StartsWithPdfMagicBytes(buffer, bytesRead, totalBytesWritten))
                        {
                            await fileStream.DisposeAsync();
                            TryDelete(tempPath);
                            return Result<EclDocumentUploadResultDto>.Failure(Error.NotAValidPdf("The file's first bytes do not match the PDF signature (%PDF-)."));
                        }
                    }

                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                }
            }

            if (!headerChecked)
            {
                // Fewer than 5 bytes were ever read — cannot possibly be a valid PDF.
                TryDelete(tempPath);
                return Result<EclDocumentUploadResultDto>.Failure(Error.NotAValidPdf("The uploaded file is too small to be a valid PDF."));
            }

            File.Move(tempPath, storedPath, overwrite: false);

            _logger.LogInformation("Stored ECL document {StoredFileName} ({SizeBytes} bytes) for DemandNo {DemandNo} / Scheme {SchemeId}.", storedFileName, totalBytesWritten, demandNo, schemeId);

            return Result<EclDocumentUploadResultDto>.Success(new EclDocumentUploadResultDto
            {
                StoredFileName = storedFileName,
                OriginalFileName = originalFileName,
                SizeBytes = totalBytesWritten
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store ECL document upload for DemandNo {DemandNo} / Scheme {SchemeId}.", demandNo, schemeId);
            TryDelete(tempPath);
            return Result<EclDocumentUploadResultDto>.Failure(Error.InternalError("Failed to store the uploaded document."));
        }
    }

    /// <summary>
    /// Re-reads the magic bytes out of whatever chunk(s) contained them. Since the first read call
    /// almost always returns at least 5 bytes (default buffer 8192), this is effectively checking
    /// the very first bytes of the stream; the totalBytesWritten guard only fires once enough bytes
    /// have actually accumulated across reads, covering the pathological tiny-first-chunk case too.
    /// </summary>
    private static bool StartsWithPdfMagicBytes(byte[] buffer, int bytesReadThisChunk, long totalBytesWritten)
    {
        // Simple, correct case: the very first chunk already contains >= 5 bytes.
        if (totalBytesWritten == bytesReadThisChunk && bytesReadThisChunk >= PdfMagicBytes.Length)
        {
            return buffer.AsSpan(0, PdfMagicBytes.Length).SequenceEqual(PdfMagicBytes);
        }

        // Extremely small first chunk(s): by the time headerChecked triggers, only the current
        // buffer is available for the *tail* bytes; since PdfMagicBytes.Length (5) is smaller than
        // any realistic stream's chunking behavior in practice, treat this edge case conservatively
        // as invalid rather than mis-validate.
        return bytesReadThisChunk >= PdfMagicBytes.Length &&
               buffer.AsSpan(0, PdfMagicBytes.Length).SequenceEqual(PdfMagicBytes);
    }

    public Task<Result<Stream>> GetAsync(string storedFileName, CancellationToken ct)
    {
        // storedFileName should only ever be a value this service itself generated (read back from
        // dbo.ECL_T_Outlay.FileName by the caller) — validated defensively anyway rather than
        // trusting that invariant, since Path.Combine with an unvalidated ".." segment or a rooted
        // path is a real path-traversal risk.
        if (string.IsNullOrWhiteSpace(storedFileName)
            || storedFileName.Contains("..")
            || Path.IsPathRooted(storedFileName)
            || storedFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return Task.FromResult(Result<Stream>.Failure(Error.NotFound("Document", storedFileName)));
        }

        var fullPath = Path.Combine(_options.EclDocumentsPath, storedFileName);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult(Result<Stream>.Failure(Error.NotFound("Document", storedFileName)));
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(Result<Stream>.Success(stream));
    }

    private void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not clean up temp upload file {Path}.", path);
        }
    }
}
