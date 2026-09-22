namespace UBIS.Services.Ecl.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Ecl.Domain.Entities;

public class EclAuthorityConfiguration : IEntityTypeConfiguration<EclAuthority>
{
    public void Configure(EntityTypeBuilder<EclAuthority> builder)
    {
        builder.ToTable("M_EclAuthority", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.AuthorityId);
        builder.Property(e => e.AuthorityId).HasColumnName("AuthorityId").ValueGeneratedOnAdd();
        builder.Property(e => e.AuthorityName).HasColumnName("AuthorityName").HasMaxLength(200).IsRequired();
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
