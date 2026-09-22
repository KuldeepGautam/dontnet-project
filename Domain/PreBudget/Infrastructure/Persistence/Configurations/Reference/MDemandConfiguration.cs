namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Reference;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Reference;

/// <summary>Renamed 2026-08-14 from dbo.M_Demand once a slim DemandNo-keyed master table was
/// split out - see Others/publish-staging/split-m-demand-master-and-fy.sql. DemandId values are
/// untouched by the rename. Repointed 2026-08-21 from dbo.M_MapDemandFY directly to dbo.vw_Demand
/// (same DemandId/PrevDemandId/DemandNo/FinancialYear columns, unaffected by the view's join) —
/// see Others/publish-staging/demand-workstream-1-master-columns-and-view.sql.</summary>
public class MDemandConfiguration : IEntityTypeConfiguration<MDemand>
{
    public void Configure(EntityTypeBuilder<MDemand> builder)
    {
        builder.ToTable("vw_Demand");
        builder.HasKey(e => e.DemandId);
        builder.Property(e => e.DemandId).HasColumnName("DemandId");
        builder.Property(e => e.PrevDemandId).HasColumnName("PrevDemandId");
        builder.Property(e => e.DemandNo).HasColumnName("DemandNo");
        builder.Property(e => e.FinancialYear).HasColumnName("FinancialYear");
        builder.Property(e => e.DemandName).HasColumnName("DemandName");
    }
}
