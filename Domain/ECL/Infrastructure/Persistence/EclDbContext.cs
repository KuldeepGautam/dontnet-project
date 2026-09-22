namespace UBIS.Services.Ecl.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using UBIS.Services.Ecl.Domain.Entities;
using UBIS.Services.Ecl.Infrastructure.Persistence.Configurations;

/// <summary>
/// DB-first, no EF migrations — same shared UBIS-Dev database every other microservice in this
/// solution points at. dbo.ECL_T_Outlay/dbo.ECL_T_Actuals_Log are DBA/legacy-owned; the two new
/// columns and the two new small tables (M_EclAuthority, ECL_Config) were created by
/// Others/publish-staging/ecl-workstream-1-schema.sql, an additive-only, idempotent script.
///
/// CRITICAL: every entity configuration added below MUST also be registered in OnModelCreating via
/// ApplyConfiguration — forgetting this compiles fine but produces a runtime 500 ("Invalid object
/// name") the first time that DbSet is queried. This exact mistake has already bitten this repo
/// once (see PreBudgetDbContext's doc comments/history) — do not repeat it here.
/// </summary>
public class EclDbContext : DbContext
{
    public EclDbContext(DbContextOptions<EclDbContext> options) : base(options)
    {
    }

    public DbSet<EclSchemeOutlay> SchemeOutlays { get; set; } = null!;
    public DbSet<EclActualsLogEntry> ActualsLog { get; set; } = null!;
    public DbSet<EclApprovalAuthority> ApprovalAuthorities { get; set; } = null!;
    public DbSet<EclAppraiseAuthority> AppraiseAuthorities { get; set; } = null!;
    public DbSet<EclConfig> Configs { get; set; } = null!;
    public DbSet<EclFinanceCommission> FinanceCommissions { get; set; } = null!;

    // Read-only shared reference data.
    public DbSet<EclCategory> Categories { get; set; } = null!;
    public DbSet<EclScheme> Schemes { get; set; } = null!;
    public DbSet<EclDemand> Demands { get; set; } = null!;
    public DbSet<EclUmbScheme> UmbSchemes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new EclSchemeOutlayConfiguration());
        modelBuilder.ApplyConfiguration(new EclActualsLogEntryConfiguration());
        modelBuilder.ApplyConfiguration(new EclApprovalAuthorityConfiguration());
        modelBuilder.ApplyConfiguration(new EclAppraiseAuthorityConfiguration());
        modelBuilder.ApplyConfiguration(new EclConfigConfiguration());
        modelBuilder.ApplyConfiguration(new EclFinanceCommissionConfiguration());
        modelBuilder.ApplyConfiguration(new EclCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new EclSchemeConfiguration());
        modelBuilder.ApplyConfiguration(new EclDemandConfiguration());
        modelBuilder.ApplyConfiguration(new EclUmbSchemeConfiguration());
    }
}
