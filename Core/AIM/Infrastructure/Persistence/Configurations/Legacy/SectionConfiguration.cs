namespace UBIS.Services.Aim.Infrastructure.Persistence.Configurations.Legacy;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Aim.Domain.Entities.Legacy;

/// <summary>EF Core mapping for the legacy dbo.Section pass-through. Added 2026-07-10.</summary>
public class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> builder)
    {
        builder.ToTable("Section", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(s => s.SectionCode);
        builder.Property(s => s.SectionCode).HasColumnName("SectionCode").ValueGeneratedNever();
        builder.Property(s => s.SectionName1).HasColumnName("SectionName1");
        builder.Property(s => s.SectionName2).HasColumnName("SectionName2");
    }
}
