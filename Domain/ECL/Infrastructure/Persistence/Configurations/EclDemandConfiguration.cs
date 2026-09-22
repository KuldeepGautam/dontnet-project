namespace UBIS.Services.Ecl.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Ecl.Domain.Entities;

/// <summary>Read-only mapping of dbo.vw_Demand (repointed 2026-08-21 from dbo.M_MapDemandFY directly
/// — DemandName now comes from the dbo.M_Demand master via the view, see
/// Others/publish-staging/demand-workstream-1-master-columns-and-view.sql), same shape as
/// PreBudget's MDemandConfiguration.</summary>
public class EclDemandConfiguration : IEntityTypeConfiguration<EclDemand>
{
    public void Configure(EntityTypeBuilder<EclDemand> builder)
    {
        builder.ToTable("vw_Demand", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.DemandId);
        builder.Property(e => e.DemandId).HasColumnName("DemandId");
        builder.Property(e => e.DemandNo).HasColumnName("DemandNo");
        builder.Property(e => e.FinancialYear).HasColumnName("FinancialYear");
        builder.Property(e => e.DemandName).HasColumnName("DemandName");
    }
}
