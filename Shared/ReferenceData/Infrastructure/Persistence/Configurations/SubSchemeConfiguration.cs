namespace UBIS.Services.ReferenceData.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.ReferenceData.Domain.Entities;

public class SubSchemeConfiguration : IEntityTypeConfiguration<SubScheme>
{
    public void Configure(EntityTypeBuilder<SubScheme> builder)
    {
        builder.ToTable("M_SubScheme", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(s => s.SubSchemeId);
        builder.Property(s => s.SubSchemeId).HasColumnName("SubSchemeId").ValueGeneratedOnAdd();
        builder.Property(s => s.SchemeId).HasColumnName("SchemeId");
        builder.Property(s => s.SubSchemeName).HasColumnName("SubSchemeName").HasMaxLength(250).IsRequired();
        builder.Property(s => s.HSubSchemeName).HasColumnName("HSubSchemeName").HasMaxLength(500);
        builder.Property(s => s.SubSchemeCode).HasColumnName("SubSchemeCode").HasMaxLength(100);
        builder.Property(s => s.IsActive).HasColumnName("IsActive");
        builder.Property(s => s.IsDeleted).HasColumnName("IsDeleted");
        builder.Property(s => s.UserIdCreatedBy).HasColumnName("UserIdCreatedBy");
        builder.Property(s => s.CreatedOnDate).HasColumnName("CreatedOnDate");
        builder.Property(s => s.UserIdModifyBy).HasColumnName("UserIdModifyBy");
        builder.Property(s => s.ModifiedOnDate).HasColumnName("ModifiedOnDate");
        builder.Property(s => s.UserIdDeletedBy).HasColumnName("UserIdDeletedBy");
        builder.Property(s => s.DeletedOnDate).HasColumnName("DeletedOnDate");

        builder.HasQueryFilter(s => !s.IsDeleted);
        builder.HasIndex(s => s.SchemeId).HasDatabaseName("IX_M_SubScheme_SchemeId");
    }
}
