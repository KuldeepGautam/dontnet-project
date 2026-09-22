namespace UBIS.Services.Sbe.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Sbe.Domain.Entities;

public class SbeSubCategoryConfiguration : IEntityTypeConfiguration<SbeSubCategory>
{
    public void Configure(EntityTypeBuilder<SbeSubCategory> builder)
    {
        builder.ToTable("M_SubCategory", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.SubCategoryId);
        builder.Property(e => e.SubCategoryId).HasColumnName("SubCategoryID").ValueGeneratedOnAdd();
        builder.Property(e => e.CategoryId).HasColumnName("CategoryID");
        builder.Property(e => e.SerialNo).HasColumnName("SerialNo").HasMaxLength(10);
        builder.Property(e => e.SubCategoryName).HasColumnName("SubCategoryName").HasMaxLength(100);
        builder.Property(e => e.HSubCategoryName).HasColumnName("HSubCategoryName").HasMaxLength(500);
        builder.Property(e => e.Active).HasColumnName("Active").HasMaxLength(1);
        builder.Property(e => e.Remarks).HasColumnName("Remarks").HasMaxLength(100);
        builder.Property(e => e.EntryDate).HasColumnName("Entrydate");
        builder.Property(e => e.PrevSubCategoryId).HasColumnName("PrevSubCategoryId");
        builder.Property(e => e.Ip).HasColumnName("IP").HasMaxLength(20);
        builder.Property(e => e.UserId).HasColumnName("UserId");

        builder.HasIndex(e => e.CategoryId);
    }
}
