namespace UBIS.Services.Sbe.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Sbe.Domain.Entities;

public class SbeUmbSchemeConfiguration : IEntityTypeConfiguration<SbeUmbScheme>
{
    public void Configure(EntityTypeBuilder<SbeUmbScheme> builder)
    {
        builder.ToTable("M_UmbScheme", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.UmbSchemeId);
        builder.Property(e => e.UmbSchemeId).HasColumnName("UmbSchemeID");
        builder.Property(e => e.DemandId).HasColumnName("DemandId");
        builder.Property(e => e.CategoryId).HasColumnName("CategoryId");
        builder.Property(e => e.SubCategoryId).HasColumnName("SubCategoryId");
        builder.Property(e => e.UmSchemeName).HasColumnName("UmSchemeName").HasMaxLength(500);
        builder.Property(e => e.HUmSchemeName).HasColumnName("HUmSchemeName").HasMaxLength(1000);
        builder.Property(e => e.UmbSchemeType).HasColumnName("UmbSchemeType").HasMaxLength(50);
        builder.Property(e => e.Active).HasColumnName("Active").HasMaxLength(1);
        builder.Property(e => e.EntryDate).HasColumnName("EntryDate");
        builder.Property(e => e.PrevUmbSchemeId).HasColumnName("PrevUmbSchemeId");
        builder.Property(e => e.DisplaySeqNo).HasColumnName("DisplaySeqNo");

        builder.HasIndex(e => new { e.DemandId, e.CategoryId });
    }
}
