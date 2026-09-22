namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;

public class AppendixInternalResourcesConfiguration : AppendixEntityConfigurationBase<AppendixInternalResources>, IEntityTypeConfiguration<AppendixInternalResources>
{
    public void Configure(EntityTypeBuilder<AppendixInternalResources> builder)
    {
        ConfigureCommon(builder, "Appendix_VID_InternalResources");

        builder.Property(e => e.NameOfInstitute).HasColumnName("NameOfInstitute").HasMaxLength(200);
        builder.Property(e => e.AutonomousBodyId).HasColumnName("AutonomousBodyId");
        builder.Property(e => e.AsOnMarch31).HasColumnName("AsOnMarch31").HasColumnType("decimal(18,2)");
        builder.Property(e => e.AsOnJune30).HasColumnName("AsOnJune30").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ExpectedNextMarch31).HasColumnName("ExpectedNextMarch31").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ExpectedNextFY).HasColumnName("ExpectedNextFY").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Remarks).HasColumnName("Remarks").HasMaxLength(250);
    }
}
