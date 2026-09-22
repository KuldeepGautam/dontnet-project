namespace UBIS.Services.Ecl.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Ecl.Domain.Entities;

/// <summary>Mapping of the new dbo.M_UmbScheme table (added 2026-08-19) — see EclUmbScheme's own
/// doc comment for why this exists separately from dbo.M_Scheme.IsUmbrella.</summary>
public class EclUmbSchemeConfiguration : IEntityTypeConfiguration<EclUmbScheme>
{
    public void Configure(EntityTypeBuilder<EclUmbScheme> builder)
    {
        builder.ToTable("M_UmbScheme", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.UmbSchemeId);
        builder.Property(e => e.UmbSchemeId).HasColumnName("UmbSchemeID").ValueGeneratedOnAdd();
        builder.Property(e => e.DemandId).HasColumnName("DemandId");
        builder.Property(e => e.CategoryId).HasColumnName("CategoryId");
        builder.Property(e => e.SubCategoryId).HasColumnName("SubCategoryId");
        builder.Property(e => e.UmSchemeName).HasColumnName("UmSchemeName").HasMaxLength(500);
        builder.Property(e => e.HUmSchemeName).HasColumnName("HUmSchemeName").HasMaxLength(500);
        builder.Property(e => e.UmbSchemeType).HasColumnName("UmbSchemeType").HasMaxLength(50);
        builder.Property(e => e.Active).HasColumnName("Active").HasMaxLength(1);
        builder.Property(e => e.EntryDate).HasColumnName("EntryDate");
        builder.Property(e => e.PrevUmbSchemeId).HasColumnName("PrevUmbSchemeId");
        builder.Property(e => e.DisplaySeqNo).HasColumnName("DisplaySeqNo");
        builder.Property(e => e.Ip).HasColumnName("IP").HasMaxLength(50);
        builder.Property(e => e.UserId).HasColumnName("UserId");
    }
}
