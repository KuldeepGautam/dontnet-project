namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;

public class AppendixProjectedDemandConfiguration : AppendixEntityConfigurationBase<AppendixProjectedDemand>, IEntityTypeConfiguration<AppendixProjectedDemand>
{
    public void Configure(EntityTypeBuilder<AppendixProjectedDemand> builder)
    {
        ConfigureCommon(builder, "Appendix_IA_ProjectedDemand");

        builder.Property(e => e.PrevYrMinRevenueBE).HasColumnName("PrevYrMinRevenueBE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.PrevYrMinRevenueRE).HasColumnName("PrevYrMinRevenueRE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.PrevYrMinCapitalBE).HasColumnName("PrevYrMinCapitalBE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.PrevYrMinCapitalRE).HasColumnName("PrevYrMinCapitalRE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.CurrYrMinRevenueBE).HasColumnName("CurrYrMinRevenueBE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.CurrYrMinCapitalBE).HasColumnName("CurrYrMinCapitalBE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.CurrYrMinMtefRevenueBE).HasColumnName("CurrYrMinMtefRevenueBE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.CurrYrMinMtefCapitalBE).HasColumnName("CurrYrMinMtefCapitalBE").HasColumnType("decimal(18,2)");

        builder.Ignore(e => e.TotalBE);
    }
}
