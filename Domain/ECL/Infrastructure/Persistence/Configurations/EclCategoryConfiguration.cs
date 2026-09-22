namespace UBIS.Services.Ecl.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Ecl.Domain.Entities;

/// <summary>Read-only mapping of dbo.M_Category — ECL never writes to this table.</summary>
public class EclCategoryConfiguration : IEntityTypeConfiguration<EclCategory>
{
    public void Configure(EntityTypeBuilder<EclCategory> builder)
    {
        builder.ToTable("M_Category", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.CategoryId);
        builder.Property(e => e.CategoryId).HasColumnName("CategoryId");
        builder.Property(e => e.FinancialYear).HasColumnName("FinancialYear").HasMaxLength(9);
        builder.Property(e => e.SerialNo).HasColumnName("SerialNo").HasMaxLength(10);
        builder.Property(e => e.CategoryName).HasColumnName("CategoryName").HasMaxLength(100);
        builder.Property(e => e.HCategoryName).HasColumnName("HCategoryName").HasMaxLength(100);
        builder.Property(e => e.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(e => e.PrevCategoryId).HasColumnName("PrevCategoryId");
        builder.Property(e => e.IsDeleted).HasColumnName("IsDeleted").IsRequired();

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
