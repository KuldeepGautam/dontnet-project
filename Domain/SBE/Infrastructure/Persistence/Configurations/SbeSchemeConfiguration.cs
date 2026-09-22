namespace UBIS.Services.Sbe.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Sbe.Domain.Entities;

public class SbeSchemeConfiguration : IEntityTypeConfiguration<SbeScheme>
{
    public void Configure(EntityTypeBuilder<SbeScheme> builder)
    {
        builder.ToTable("M_Scheme", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.SchemeId);
        builder.Property(e => e.SchemeId).HasColumnName("SchemeId");
        builder.Property(e => e.DemandId).HasColumnName("DemandId");
        builder.Property(e => e.SchemeName).HasColumnName("SchemeName").HasMaxLength(500);
        builder.Property(e => e.HSchemeName).HasColumnName("HSchemeName").HasMaxLength(1000);
        builder.Property(e => e.IsUmbrella).HasColumnName("IsUmbrella");
        builder.Property(e => e.IsActive).HasColumnName("IsActive");
        builder.Property(e => e.IsDeleted).HasColumnName("IsDeleted");
        builder.Property(e => e.CategoryId).HasColumnName("CategoryId");
        builder.Property(e => e.SchemeSrNo).HasColumnName("SchemeSrNo");
        builder.Property(e => e.PrevSchemeId).HasColumnName("PrevSchemeId");
        builder.Property(e => e.UmbSchemeId).HasColumnName("UmbSchemeId");

        builder.HasQueryFilter(e => !e.IsDeleted);
        builder.HasIndex(e => new { e.DemandId, e.CategoryId });
    }
}
