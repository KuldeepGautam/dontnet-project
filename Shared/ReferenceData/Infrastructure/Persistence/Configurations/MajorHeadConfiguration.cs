namespace UBIS.Services.ReferenceData.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.ReferenceData.Domain.Entities;

public class MajorHeadConfiguration : IEntityTypeConfiguration<MajorHead>
{
    public void Configure(EntityTypeBuilder<MajorHead> builder)
    {
        builder.ToTable("M_MajorHead", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(m => m.MajorHeadId);
        builder.Property(m => m.MajorHeadId).HasColumnName("MajorHeadId").ValueGeneratedOnAdd();
        builder.Property(m => m.MajorHeadCode).HasColumnName("MajorHeadCode").HasMaxLength(4).IsRequired();
        builder.Property(m => m.MajorHeadName).HasColumnName("MajorHeadName").HasMaxLength(250).IsRequired();
        builder.Property(m => m.HMajorHeadName).HasColumnName("HMajorHeadName").HasMaxLength(250);
        builder.Property(m => m.IsActive).HasColumnName("IsActive");
        builder.Property(m => m.IsDeleted).HasColumnName("IsDeleted");
        builder.Property(m => m.UserIdCreatedBy).HasColumnName("UserIdCreatedBy");
        builder.Property(m => m.CreatedOnDate).HasColumnName("CreatedOnDate");
        builder.Property(m => m.UserIdModifyBy).HasColumnName("UserIdModifyBy");
        builder.Property(m => m.ModifiedOnDate).HasColumnName("ModifiedOnDate");
        builder.Property(m => m.UserIdDeletedBy).HasColumnName("UserIdDeletedBy");
        builder.Property(m => m.DeletedOnDate).HasColumnName("DeletedOnDate");

        builder.HasQueryFilter(m => !m.IsDeleted);
        builder.HasIndex(m => m.MajorHeadCode).HasDatabaseName("IX_M_MajorHead_MajorHeadCode");
    }
}
