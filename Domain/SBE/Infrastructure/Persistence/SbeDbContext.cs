namespace UBIS.Services.Sbe.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using UBIS.Services.Sbe.Domain.Entities;
using UBIS.Services.Sbe.Infrastructure.Persistence.Configurations;

/// <summary>
/// DB-first, no EF migrations — same shared UBIS-Dev database every other microservice in this
/// solution points at. SBEData/M_Category/M_Scheme/M_SubScheme/M_UmbScheme/M_MajorHead were
/// already live before this module's own work started; M_SubCategory/M_SBEDemand/SBENotes/
/// M_DemandCeiling/M_ObjectCeiling/PSECategory/PSEDemand/PSECategoryDemand were created by
/// Others/publish-staging/sbe-workstream-1-schema.sql (additive-only, idempotent).
///
/// CRITICAL: every entity configuration added below MUST also be registered in OnModelCreating via
/// ApplyConfiguration — forgetting this compiles fine but produces a runtime 500 ("Invalid object
/// name") the first time that DbSet is queried (this exact mistake has already bitten this repo
/// once — see PreBudgetDbContext's own doc comments/history — do not repeat it here).
/// </summary>
public class SbeDbContext : DbContext
{
    public SbeDbContext(DbContextOptions<SbeDbContext> options) : base(options)
    {
    }

    // Write aggregates.
    public DbSet<SbeData> SbeDataRows { get; set; } = null!;
    public DbSet<SbeNote> SbeNotes { get; set; } = null!;
    public DbSet<SbeDemand> SbeDemands { get; set; } = null!;
    public DbSet<PseCategory> PseCategories { get; set; } = null!;
    public DbSet<PseDemand> PseDemands { get; set; } = null!;
    public DbSet<PseCategoryDemand> PseCategoryDemands { get; set; } = null!;
    public DbSet<DemandCeiling> DemandCeilings { get; set; } = null!;
    public DbSet<ObjectCeiling> ObjectCeilings { get; set; } = null!;

    // Read-only shared reference data.
    public DbSet<SbeCategory> Categories { get; set; } = null!;
    public DbSet<SbeSubCategory> SubCategories { get; set; } = null!;
    public DbSet<SbeScheme> Schemes { get; set; } = null!;
    public DbSet<SbeSubScheme> SubSchemes { get; set; } = null!;
    public DbSet<SbeUmbScheme> UmbSchemes { get; set; } = null!;
    public DbSet<SbeMajorHead> MajorHeads { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new SbeDataConfiguration());
        modelBuilder.ApplyConfiguration(new SbeNoteConfiguration());
        modelBuilder.ApplyConfiguration(new SbeDemandConfiguration());
        modelBuilder.ApplyConfiguration(new PseCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new PseDemandConfiguration());
        modelBuilder.ApplyConfiguration(new PseCategoryDemandConfiguration());
        modelBuilder.ApplyConfiguration(new DemandCeilingConfiguration());
        modelBuilder.ApplyConfiguration(new ObjectCeilingConfiguration());

        modelBuilder.ApplyConfiguration(new SbeCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new SbeSubCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new SbeSchemeConfiguration());
        modelBuilder.ApplyConfiguration(new SbeSubSchemeConfiguration());
        modelBuilder.ApplyConfiguration(new SbeUmbSchemeConfiguration());
        modelBuilder.ApplyConfiguration(new SbeMajorHeadConfiguration());
    }
}
