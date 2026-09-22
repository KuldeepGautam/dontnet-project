namespace UBIS.Services.ReferenceData.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using UBIS.Services.ReferenceData.Domain.Entities;
using UBIS.Services.ReferenceData.Infrastructure.Persistence.Configurations;

/// <summary>
/// DB-first, no EF migrations — same shared UBIS-Dev database every other microservice in this
/// solution points at. Tables are created via SQL Queries\new-tables\ReferenceData_CreateTables.sql
/// (DBA-owned script), not EF Core migrations.
/// </summary>
public class ReferenceDataDbContext : DbContext
{
    public ReferenceDataDbContext(DbContextOptions<ReferenceDataDbContext> options) : base(options)
    {
    }

    public DbSet<Scheme> Schemes { get; set; } = null!;

    public DbSet<SubScheme> SubSchemes { get; set; } = null!;

    public DbSet<MajorHead> MajorHeads { get; set; } = null!;

    public DbSet<ObjectHead> ObjectHeads { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new SchemeConfiguration());
        modelBuilder.ApplyConfiguration(new SubSchemeConfiguration());
        modelBuilder.ApplyConfiguration(new MajorHeadConfiguration());
        modelBuilder.ApplyConfiguration(new ObjectHeadConfiguration());
    }
}
