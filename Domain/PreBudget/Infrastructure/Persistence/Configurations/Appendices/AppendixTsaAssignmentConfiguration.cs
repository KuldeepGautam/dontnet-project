namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;

public class AppendixTsaAssignmentConfiguration : AppendixEntityConfigurationBase<AppendixTsaAssignment>, IEntityTypeConfiguration<AppendixTsaAssignment>
{
    public void Configure(EntityTypeBuilder<AppendixTsaAssignment> builder)
    {
        ConfigureCommon(builder, "Appendix_IIIA_TsaAssignment");

        builder.Property(e => e.EntityName).HasColumnName("EntityName").IsRequired();
        builder.Property(e => e.BE).HasColumnName("BE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.TsaAssignmentAsOnSept).HasColumnName("TsaAssignmentAsOnSept").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ActualExpenditureUptoSept).HasColumnName("ActualExpenditureUptoSept").HasColumnType("decimal(18,2)");
        builder.Property(e => e.UnspentAssignment).HasColumnName("UnspentAssignment").HasColumnType("decimal(18,2)");
        builder.Property(e => e.DateOfLastAssignment).HasColumnName("DateOfLastAssignment");
        builder.Property(e => e.AmountOfLastAssignment).HasColumnName("AmountOfLastAssignment").HasColumnType("decimal(18,2)");
    }
}
