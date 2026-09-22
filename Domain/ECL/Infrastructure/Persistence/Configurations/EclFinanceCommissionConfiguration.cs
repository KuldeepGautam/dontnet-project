namespace UBIS.Services.Ecl.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Ecl.Domain.Entities;

/// <summary>Mapping of the new dbo.M_FinanceCommission table (added 2026-08-19) — see
/// EclFinanceCommission's own doc comment for why this replaces dbo.ECL_Config.</summary>
public class EclFinanceCommissionConfiguration : IEntityTypeConfiguration<EclFinanceCommission>
{
    public void Configure(EntityTypeBuilder<EclFinanceCommission> builder)
    {
        builder.ToTable("M_FinanceCommission", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.FinancialCommissionNo);
        builder.Property(e => e.FinancialCommissionNo).HasColumnName("FinancialCommissionNo").ValueGeneratedNever();
        builder.Property(e => e.Period).HasColumnName("Period").HasMaxLength(9).IsRequired();
        builder.Property(e => e.NoYears).HasColumnName("NoYears").IsRequired();
    }
}
