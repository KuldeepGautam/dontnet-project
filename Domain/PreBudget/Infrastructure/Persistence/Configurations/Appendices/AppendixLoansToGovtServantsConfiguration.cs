namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;

public class AppendixLoansToGovtServantsConfiguration : AppendixEntityConfigurationBase<AppendixLoansToGovtServants>, IEntityTypeConfiguration<AppendixLoansToGovtServants>
{
    public void Configure(EntityTypeBuilder<AppendixLoansToGovtServants> builder)
    {
        ConfigureCommon(builder, "Appendix_X_LoansToGovtServants");

        builder.Property(e => e.SubHeadName).HasColumnName("SubHeadName").HasMaxLength(250);
        builder.Property(e => e.ActualsY1).HasColumnName("ActualsY1").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ActualsY2).HasColumnName("ActualsY2").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ActualsY3).HasColumnName("ActualsY3").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ActualsUptoSept).HasColumnName("ActualsUptoSept").HasColumnType("decimal(18,2)");
        builder.Property(e => e.BE).HasColumnName("BE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.RE).HasColumnName("RE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.NBE).HasColumnName("NBE").HasColumnType("decimal(18,2)");
    }
}
