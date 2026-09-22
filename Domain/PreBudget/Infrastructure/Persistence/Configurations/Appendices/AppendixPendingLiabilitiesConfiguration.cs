namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;

public class AppendixPendingLiabilitiesConfiguration : AppendixEntityConfigurationBase<AppendixPendingLiabilities>, IEntityTypeConfiguration<AppendixPendingLiabilities>
{
    public void Configure(EntityTypeBuilder<AppendixPendingLiabilities> builder)
    {
        ConfigureCommon(builder, "Appendix_VIB_PendingLiabilities");

        builder.Property(e => e.SchemeId).HasColumnName("SchemeId");
        builder.Property(e => e.SubSchemeId).HasColumnName("SubSchemeId");
        builder.Property(e => e.PendingLiabilityAsOnMarch31).HasColumnName("PendingLiabilityAsOnMarch31").HasColumnType("decimal(18,2)");
        builder.Property(e => e.BE).HasColumnName("BE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.EstimatedExpenditure).HasColumnName("EstimatedExpenditure").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Remarks).HasColumnName("Remarks").HasMaxLength(500);
    }
}
