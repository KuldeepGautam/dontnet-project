namespace UBIS.Services.Sbe.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Sbe.Domain.Entities;

public class SbeMajorHeadConfiguration : IEntityTypeConfiguration<SbeMajorHead>
{
    public void Configure(EntityTypeBuilder<SbeMajorHead> builder)
    {
        builder.ToTable("M_MajorHead", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.MajorHeadId);
        builder.Property(e => e.MajorHeadId).HasColumnName("MajorHeadId");
        builder.Property(e => e.MajorHeadCode).HasColumnName("MajorHeadCode").HasMaxLength(4);
        builder.Property(e => e.MajorHeadName).HasColumnName("MajorHeadName").HasMaxLength(500);
        builder.Property(e => e.HMajorHeadName).HasColumnName("HMajorHeadName").HasMaxLength(500);
        builder.Property(e => e.IsActive).HasColumnName("IsActive");
        builder.Property(e => e.IsDeleted).HasColumnName("IsDeleted");

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
