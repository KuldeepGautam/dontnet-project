namespace UBIS.Services.Ecl.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Ecl.Domain.Entities;

public class EclAppraiseAuthorityConfiguration : IEntityTypeConfiguration<EclAppraiseAuthority>
{
    public void Configure(EntityTypeBuilder<EclAppraiseAuthority> builder)
    {
        builder.ToTable("M_ECLAppraiseAuthority", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.AppraiseId);
        builder.Property(e => e.AppraiseId).HasColumnName("AppraiseId").ValueGeneratedOnAdd();
        builder.Property(e => e.AppraiseName).HasColumnName("AppraiseName").HasMaxLength(400).IsRequired();
        builder.Property(e => e.IsDropdownVisible).HasColumnName("IsDropdownVisible").IsRequired();
        builder.Property(e => e.DisplaySequenceNo).HasColumnName("DisplaySequenceNo");
        builder.Property(e => e.UserIdCreatedBy).HasColumnName("UserIdCreatedBy");
        builder.Property(e => e.CreatedOnDate).HasColumnName("CreatedOnDate");
        builder.Property(e => e.UserIdModifyBy).HasColumnName("UserIdModifyBy");
        builder.Property(e => e.ModifiedOnDate).HasColumnName("ModifiedOnDate");
        builder.Property(e => e.IsDeleted).HasColumnName("IsDeleted").IsRequired();

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
