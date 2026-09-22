namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Reference;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Reference;

public class MCategoryConfiguration : IEntityTypeConfiguration<MCategory>
{
    public void Configure(EntityTypeBuilder<MCategory> builder)
    {
        builder.ToTable("M_Category");
        builder.HasKey(e => e.CategoryId);
        builder.Property(e => e.CategoryId).HasColumnName("CategoryId");
        builder.Property(e => e.FinancialYear).HasColumnName("FinancialYear");
        builder.Property(e => e.SerialNo).HasColumnName("SerialNo");
        builder.Property(e => e.CategoryName).HasColumnName("CategoryName");
        builder.Property(e => e.HCategoryName).HasColumnName("HCategoryName");
        builder.Property(e => e.IsActive).HasColumnName("IsActive");
        builder.Property(e => e.PrevCategoryId).HasColumnName("PrevCategoryId");
    }
}
