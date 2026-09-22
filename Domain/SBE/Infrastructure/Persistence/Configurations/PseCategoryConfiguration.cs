namespace UBIS.Services.Sbe.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Sbe.Domain.Entities;

public class PseCategoryConfiguration : IEntityTypeConfiguration<PseCategory>
{
    public void Configure(EntityTypeBuilder<PseCategory> builder)
    {
        builder.ToTable("PSECategory", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.PseCategoryId);
        builder.Property(e => e.PseCategoryId).HasColumnName("PSECategoryId").ValueGeneratedOnAdd();
        builder.Property(e => e.FinancialYear).HasColumnName("FinancialYear").HasMaxLength(9);
        builder.Property(e => e.DemandId).HasColumnName("DemandId");
        builder.Property(e => e.DemandNo).HasColumnName("DemandNo");
        builder.Property(e => e.PseCategoryCode).HasColumnName("PSECategoryCode");
        builder.Property(e => e.PseCategoryName).HasColumnName("PSECategoryName").HasMaxLength(50);
        builder.Property(e => e.HPseCategoryName).HasColumnName("HPSECategoryName").HasMaxLength(1000);
        builder.Property(e => e.Active).HasColumnName("Active").HasMaxLength(1);
        builder.Property(e => e.EntryDate).HasColumnName("EntryDate");
        builder.Property(e => e.PrevPseCategoryId).HasColumnName("PrevPSECategoryId");
        builder.Property(e => e.Ip).HasColumnName("IP").HasMaxLength(20);
        builder.Property(e => e.UserId).HasColumnName("UserId");

        builder.HasIndex(e => new { e.DemandId, e.FinancialYear });
    }
}
