namespace UBIS.Services.Sbe.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Sbe.Domain.Entities;

public class SbeSubSchemeConfiguration : IEntityTypeConfiguration<SbeSubScheme>
{
    public void Configure(EntityTypeBuilder<SbeSubScheme> builder)
    {
        builder.ToTable("M_SubScheme", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.SubSchemeId);
        builder.Property(e => e.SubSchemeId).HasColumnName("SubSchemeId");
        builder.Property(e => e.SchemeId).HasColumnName("SchemeId");
        builder.Property(e => e.SubSchemeName).HasColumnName("SubSchemeName").HasMaxLength(500);
        builder.Property(e => e.HSubSchemeName).HasColumnName("HSubSchemeName").HasMaxLength(1000);
        builder.Property(e => e.SubSchemeCode).HasColumnName("SubSchemeCode").HasMaxLength(200);
        builder.Property(e => e.IsActive).HasColumnName("IsActive");
        builder.Property(e => e.IsDeleted).HasColumnName("IsDeleted");
        builder.Property(e => e.PrevSubschemeId).HasColumnName("PrevSubschemeId");
        builder.Property(e => e.SubSchemeSrNo).HasColumnName("SubSchemeSrNo");

        builder.HasQueryFilter(e => !e.IsDeleted);
        builder.HasIndex(e => e.SchemeId);
    }
}
