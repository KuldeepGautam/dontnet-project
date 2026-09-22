namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;

public class AppendixUserChargesAutonomousBodyConfiguration : AppendixEntityConfigurationBase<AppendixUserChargesAutonomousBody>, IEntityTypeConfiguration<AppendixUserChargesAutonomousBody>
{
    public void Configure(EntityTypeBuilder<AppendixUserChargesAutonomousBody> builder)
    {
        ConfigureCommon(builder, "Appendix_VIG_UserChargesAutonomousBody");

        builder.Property(e => e.AutonomousBodyId).HasColumnName("AutonomousBodyId");
        builder.Property(e => e.BriefOnRevenueSources).HasColumnName("BriefOnRevenueSources").HasMaxLength(500);
        builder.Property(e => e.PresentStatus).HasColumnName("PresentStatus").HasMaxLength(500);
        builder.Property(e => e.ReceiptsCollected).HasColumnName("ReceiptsCollected").HasColumnType("decimal(18,2)");
        builder.Property(e => e.TotalRevenueExpenditure).HasColumnName("TotalRevenueExpenditure").HasColumnType("decimal(18,2)");
        builder.Property(e => e.TotalCapitalExpenditure).HasColumnName("TotalCapitalExpenditure").HasColumnType("decimal(18,2)");
    }
}
