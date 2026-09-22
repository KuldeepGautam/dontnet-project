namespace UBIS.Services.Ecl.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Ecl.Domain.Entities;

/// <summary>Mapping of dbo.M_Scheme. Mostly read (Category/Scheme dropdowns) — the one write path is
/// EclOutlayRepository.CreateSchemeAsync (added 2026-08-18) for the ECL Master "Add Schemes" screen.</summary>
public class EclSchemeConfiguration : IEntityTypeConfiguration<EclScheme>
{
    public void Configure(EntityTypeBuilder<EclScheme> builder)
    {
        builder.ToTable("M_Scheme", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.SchemeId);
        builder.Property(e => e.SchemeId).HasColumnName("SchemeId");
        builder.Property(e => e.DemandId).HasColumnName("DemandId").IsRequired();
        builder.Property(e => e.SchemeName).HasColumnName("SchemeName").HasMaxLength(250).IsRequired();
        builder.Property(e => e.HSchemeName).HasColumnName("HSchemeName").HasMaxLength(500);
        builder.Property(e => e.IsUmbrella).HasColumnName("IsUmbrella").IsRequired();
        builder.Property(e => e.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(e => e.IsDeleted).HasColumnName("IsDeleted").IsRequired();
        builder.Property(e => e.CategoryId).HasColumnName("CategoryId");
        builder.Property(e => e.SchemeSrNo).HasColumnName("SchemeSrNo");
        builder.Property(e => e.PrevSchemeId).HasColumnName("PrevSchemeId");
        builder.Property(e => e.UmbSchemeId).HasColumnName("UmbSchemeId");
        builder.Property(e => e.Ip).HasColumnName("IP").HasMaxLength(50);
        builder.Property(e => e.UserIdCreatedBy).HasColumnName("UserIdCreatedBy");
        builder.Property(e => e.CreatedOnDate).HasColumnName("CreatedOnDate");

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
