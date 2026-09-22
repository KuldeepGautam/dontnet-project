namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;

public class AppendixUserChargesConfiguration : AppendixEntityConfigurationBase<AppendixUserCharges>, IEntityTypeConfiguration<AppendixUserCharges>
{
    public void Configure(EntityTypeBuilder<AppendixUserCharges> builder)
    {
        ConfigureCommon(builder, "Appendix_VIA_UserCharges");

        builder.Property(e => e.TitleOfCharge).HasColumnName("TitleOfCharge").HasMaxLength(250);
        builder.Property(e => e.Service).HasColumnName("Service").HasMaxLength(250);
        builder.Property(e => e.OrgDept).HasColumnName("OrgDept").HasMaxLength(250);
        builder.Property(e => e.RateOfCharge).HasColumnName("RateOfCharge").HasMaxLength(250);
        builder.Property(e => e.UnitOfCollection).HasColumnName("UnitOfCollection").HasMaxLength(250);
        builder.Property(e => e.DateOfRateFixation).HasColumnName("DateOfRateFixation");
        builder.Property(e => e.FixationStatute).HasColumnName("FixationStatute").HasMaxLength(250);
        builder.Property(e => e.TotalRevenueY1).HasColumnName("TotalRevenueY1").HasColumnType("decimal(18,2)");
        builder.Property(e => e.TotalRevenueY2).HasColumnName("TotalRevenueY2").HasColumnType("decimal(18,2)");
        builder.Property(e => e.TotalRevenueY3).HasColumnName("TotalRevenueY3").HasColumnType("decimal(18,2)");
        builder.Property(e => e.CompetentAuthority).HasColumnName("CompetentAuthority").HasMaxLength(250);
        builder.Property(e => e.PeriodOfFixation).HasColumnName("PeriodOfFixation").HasMaxLength(250);
        builder.Property(e => e.Salary).HasColumnName("Salary").HasColumnType("decimal(18,2)");
        builder.Property(e => e.OfficeExpenses).HasColumnName("OfficeExpenses").HasColumnType("decimal(18,2)");
        builder.Property(e => e.OtherExpenses).HasColumnName("OtherExpenses").HasColumnType("decimal(18,2)");
        builder.Property(e => e.IsCollectionCostHigher).HasColumnName("IsCollectionCostHigher");
        builder.Property(e => e.IsTransCostHigher).HasColumnName("IsTransCostHigher");
        builder.Property(e => e.Remarks).HasColumnName("Remarks");
    }
}
