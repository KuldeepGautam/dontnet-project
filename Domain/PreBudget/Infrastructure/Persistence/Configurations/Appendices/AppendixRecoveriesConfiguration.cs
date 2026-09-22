namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;

public class AppendixRecoveriesConfiguration : AppendixEntityConfigurationBase<AppendixRecoveries>, IEntityTypeConfiguration<AppendixRecoveries>
{
    public void Configure(EntityTypeBuilder<AppendixRecoveries> builder)
    {
        ConfigureCommon(builder, "Appendix_VIIA_Recoveries");

        builder.Property(e => e.SchemeName).HasColumnName("SchemeName").HasMaxLength(255);
        builder.Property(e => e.MajorHeadId).HasColumnName("MajorHeadId");
        builder.Property(e => e.IsCharged).HasColumnName("IsCharged");
        builder.Property(e => e.Actuals).HasColumnName("Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.BE).HasColumnName("BE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.RE).HasColumnName("RE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.NBE).HasColumnName("NBE").HasColumnType("decimal(18,2)");
    }
}
