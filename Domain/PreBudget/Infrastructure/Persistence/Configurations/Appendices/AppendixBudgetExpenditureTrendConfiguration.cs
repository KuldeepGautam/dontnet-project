namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;

public class AppendixBudgetExpenditureTrendConfiguration : AppendixEntityConfigurationBase<AppendixBudgetExpenditureTrend>, IEntityTypeConfiguration<AppendixBudgetExpenditureTrend>
{
    public void Configure(EntityTypeBuilder<AppendixBudgetExpenditureTrend> builder)
    {
        ConfigureCommon(builder, "Appendix_I_BudgetExpenditureTrend");

        builder.Property(e => e.RevenueBE).HasColumnName("RevenueBE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.RevenueRE).HasColumnName("RevenueRE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.RevenueActuals).HasColumnName("RevenueActuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.RevenueActualsUptoSept).HasColumnName("RevenueActualsUptoSept").HasColumnType("decimal(18,2)");
        builder.Property(e => e.CapitalBE).HasColumnName("CapitalBE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.CapitalRE).HasColumnName("CapitalRE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.CapitalActuals).HasColumnName("CapitalActuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.CapitalActualsUptoSept).HasColumnName("CapitalActualsUptoSept").HasColumnType("decimal(18,2)");
    }
}
