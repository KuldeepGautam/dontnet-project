namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Reference;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Reference;

public class MinorHeadNameConfiguration : IEntityTypeConfiguration<MinorHeadName>
{
    public void Configure(EntityTypeBuilder<MinorHeadName> builder)
    {
        builder.ToTable("M_MinorHeadName", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("Id");
        builder.Property(e => e.AccountHead).HasColumnName("AccountHead").HasMaxLength(9);
        builder.Property(e => e.AccountHeadName).HasColumnName("AccountHeadName");
        builder.Property(e => e.HAccountHeadName).HasColumnName("HAccountHeadName");
        builder.Property(e => e.FinancialYear).HasColumnName("FinancialYear").HasMaxLength(9);
    }
}
