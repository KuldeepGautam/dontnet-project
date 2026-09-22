namespace UBIS.Services.ReferenceData.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using UBIS.Services.ReferenceData.Application.DTOs;
using UBIS.Services.ReferenceData.Application.Interfaces;
using UBIS.Services.ReferenceData.Domain.Entities;
using UBIS.Services.ReferenceData.Infrastructure.Persistence;

public class ReferenceDataService : IReferenceDataService
{
    private readonly ReferenceDataDbContext _db;

    public ReferenceDataService(ReferenceDataDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SchemeDto>> GetSchemesAsync(int? demandId, CancellationToken ct)
    {
        var query = _db.Schemes.Where(s => s.IsActive);
        if (demandId is not null)
        {
            query = query.Where(s => s.DemandId == demandId.Value);
        }

        var entities = await query.OrderBy(s => s.SchemeName).ToListAsync(ct);
        return entities.Select(ToDto).ToList();
    }

    public async Task<SchemeDto?> GetSchemeAsync(int schemeId, CancellationToken ct)
    {
        var entity = await _db.Schemes.SingleOrDefaultAsync(s => s.SchemeId == schemeId, ct);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<SchemeDto> CreateSchemeAsync(CreateSchemeRequest request, int userId, CancellationToken ct)
    {
        var entity = new Scheme
        {
            DemandId = request.DemandId,
            SchemeName = request.SchemeName,
            HSchemeName = request.HSchemeName,
            IsUmbrella = request.IsUmbrella,
            IsActive = true,
            UserIdCreatedBy = userId,
            CreatedOnDate = DateTime.UtcNow
        };

        _db.Schemes.Add(entity);
        await _db.SaveChangesAsync(ct);

        return ToDto(entity);
    }

    public async Task<IReadOnlyList<SubSchemeDto>> GetSubSchemesAsync(int? schemeId, CancellationToken ct)
    {
        var query = _db.SubSchemes.Where(s => s.IsActive);
        if (schemeId is not null)
        {
            query = query.Where(s => s.SchemeId == schemeId.Value);
        }

        var entities = await query.OrderBy(s => s.SubSchemeName).ToListAsync(ct);
        return entities.Select(ToDto).ToList();
    }

    public async Task<SubSchemeDto> CreateSubSchemeAsync(CreateSubSchemeRequest request, int userId, CancellationToken ct)
    {
        var entity = new SubScheme
        {
            SchemeId = request.SchemeId,
            SubSchemeName = request.SubSchemeName,
            HSubSchemeName = request.HSubSchemeName,
            SubSchemeCode = request.SubSchemeCode,
            IsActive = true,
            UserIdCreatedBy = userId,
            CreatedOnDate = DateTime.UtcNow
        };

        _db.SubSchemes.Add(entity);
        await _db.SaveChangesAsync(ct);

        return ToDto(entity);
    }

    public async Task<IReadOnlyList<MajorHeadDto>> GetMajorHeadsAsync(CancellationToken ct)
    {
        var entities = await _db.MajorHeads.Where(m => m.IsActive).OrderBy(m => m.MajorHeadCode).ToListAsync(ct);
        return entities.Select(ToDto).ToList();
    }

    public async Task<MajorHeadDto> CreateMajorHeadAsync(CreateMajorHeadRequest request, int userId, CancellationToken ct)
    {
        var entity = new MajorHead
        {
            MajorHeadCode = request.MajorHeadCode,
            MajorHeadName = request.MajorHeadName,
            HMajorHeadName = request.HMajorHeadName,
            IsActive = true,
            UserIdCreatedBy = userId,
            CreatedOnDate = DateTime.UtcNow
        };

        _db.MajorHeads.Add(entity);
        await _db.SaveChangesAsync(ct);

        return ToDto(entity);
    }

    public async Task<IReadOnlyList<ObjectHeadDto>> GetObjectHeadsAsync(CancellationToken ct)
    {
        var entities = await _db.ObjectHeads.Where(o => o.IsActive).OrderBy(o => o.ObjectHeadCode).ToListAsync(ct);
        return entities.Select(ToDto).ToList();
    }

    public async Task<ObjectHeadDto> CreateObjectHeadAsync(CreateObjectHeadRequest request, int userId, CancellationToken ct)
    {
        var entity = new ObjectHead
        {
            ObjectHeadCode = request.ObjectHeadCode,
            ObjectHeadName = request.ObjectHeadName,
            IsActive = true,
            UserIdCreatedBy = userId,
            CreatedOnDate = DateTime.UtcNow
        };

        _db.ObjectHeads.Add(entity);
        await _db.SaveChangesAsync(ct);

        return ToDto(entity);
    }

    private static SchemeDto ToDto(Scheme s) => new()
    {
        SchemeId = s.SchemeId,
        DemandId = s.DemandId,
        SchemeName = s.SchemeName,
        HSchemeName = s.HSchemeName,
        IsUmbrella = s.IsUmbrella,
        IsActive = s.IsActive
    };

    private static SubSchemeDto ToDto(SubScheme s) => new()
    {
        SubSchemeId = s.SubSchemeId,
        SchemeId = s.SchemeId,
        SubSchemeName = s.SubSchemeName,
        HSubSchemeName = s.HSubSchemeName,
        SubSchemeCode = s.SubSchemeCode,
        IsActive = s.IsActive
    };

    private static MajorHeadDto ToDto(MajorHead m) => new()
    {
        MajorHeadId = m.MajorHeadId,
        MajorHeadCode = m.MajorHeadCode,
        MajorHeadName = m.MajorHeadName,
        HMajorHeadName = m.HMajorHeadName,
        IsActive = m.IsActive
    };

    private static ObjectHeadDto ToDto(ObjectHead o) => new()
    {
        ObjectHeadId = o.ObjectHeadId,
        ObjectHeadCode = o.ObjectHeadCode,
        ObjectHeadName = o.ObjectHeadName,
        IsActive = o.IsActive
    };
}
