namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;

public class AppendixMinorHeadUserChargesConfiguration : AppendixEntityConfigurationBase<AppendixMinorHeadUserCharges>, IEntityTypeConfiguration<AppendixMinorHeadUserCharges>
{
    public void Configure(EntityTypeBuilder<AppendixMinorHeadUserCharges> builder)
    {
        ConfigureCommon(builder, "Appendix_VIF_MinorHeadUserCharges");

        builder.Property(e => e.MinorHeadCode).HasColumnName("MinorHeadCode").HasMaxLength(9).IsRequired();
        builder.Property(e => e.BriefOnReceipts).HasColumnName("BriefOnReceipts").HasMaxLength(500);
        builder.Property(e => e.PresentStatus).HasColumnName("PresentStatus").HasMaxLength(500);
        builder.Property(e => e.NoOfTransactions).HasColumnName("NoOfTransactions").HasColumnType("decimal(18,2)");
        builder.Property(e => e.RateOfService).HasColumnName("RateOfService").HasMaxLength(250);
        builder.Property(e => e.ReceiptsCollection).HasColumnName("ReceiptsCollection").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ActionTakenPlan).HasColumnName("ActionTakenPlan").HasMaxLength(500);
    }
}
