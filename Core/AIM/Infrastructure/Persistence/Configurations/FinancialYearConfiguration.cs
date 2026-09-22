namespace UBIS.Services.Aim.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Aim.Domain.Entities;

public class FinancialYearConfiguration : IEntityTypeConfiguration<FinancialYear>
{
    public void Configure(EntityTypeBuilder<FinancialYear> builder)
    {
        builder.ToTable("M_FinancialYear", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("Id").ValueGeneratedOnAdd();
        builder.Property(f => f.YearRange).HasColumnName("YearRange").HasMaxLength(9);
        builder.Property(f => f.BudgetType).HasColumnName("BudgetType").HasMaxLength(20);
        builder.Property(f => f.IsCurrentYear).HasColumnName("IsCurrentYear");
        builder.Property(f => f.BudgetCycle).HasColumnName("BudgetCycle");
        builder.Property(f => f.AppId).HasColumnName("AppId");
    }
}
