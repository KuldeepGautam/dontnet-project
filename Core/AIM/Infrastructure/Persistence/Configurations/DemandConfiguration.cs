namespace UBIS.Services.Aim.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Aim.Domain.Entities;

/// <summary>
/// Maps only the columns GetMyDemands actually needs — dbo.vw_Demand has ~40 columns total, the
/// rest are simply unmapped/ignored. Renamed 2026-08-14 from dbo.M_Demand (per-year history table,
/// one row per DemandNo+FinancialYear) once a slim dbo.M_Demand (DemandNo-keyed master) was split
/// out - see Others/publish-staging/split-m-demand-master-and-fy.sql. Repointed 2026-08-21 from the
/// underlying dbo.M_MapDemandFY table to dbo.vw_Demand (a view joining M_MapDemandFY to the now-
/// enriched M_Demand master on DemandNo, one row per DemandId/FinancialYear same as before) - see
/// Others/publish-staging/demand-workstream-1-master-columns-and-view.sql. DemandName/HDemandName/
/// IsActive now come from the M_Demand master via the view rather than M_MapDemandFY's own
/// (still-present but no longer authoritative) copies of those columns. Read-only entity - nothing
/// in this codebase writes to it, confirmed before repointing since a multi-table-join view isn't
/// directly updatable.
/// </summary>
public class DemandConfiguration : IEntityTypeConfiguration<Demand>
{
    public void Configure(EntityTypeBuilder<Demand> builder)
    {
        builder.ToTable("vw_Demand", "dbo", t => t.ExcludeFromMigrations());

        builder.HasKey(d => d.DemandId);
        builder.Property(d => d.DemandId).HasColumnName("DemandId").ValueGeneratedOnAdd();
        builder.Property(d => d.FinancialYear).HasColumnName("FinancialYear").HasMaxLength(50);
        builder.Property(d => d.DemandNo).HasColumnName("DemandNo");
        builder.Property(d => d.DemandName).HasColumnName("DemandName").HasMaxLength(500);
        builder.Property(d => d.HDemandName).HasColumnName("HDemandName").HasMaxLength(500);
        builder.Property(d => d.IsActive).HasColumnName("IsActive");
        builder.Property(d => d.DemandType).HasColumnName("DemandType").HasMaxLength(1);

        builder.HasIndex(d => new { d.FinancialYear, d.DemandNo }).HasDatabaseName("IX_M_MapDemandFY_FinancialYear_DemandNo");
    }
}
