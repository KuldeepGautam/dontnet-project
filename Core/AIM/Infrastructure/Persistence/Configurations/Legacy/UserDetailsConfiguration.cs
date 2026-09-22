namespace UBIS.Services.Aim.Infrastructure.Persistence.Configurations.Legacy;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Aim.Domain.Entities.Legacy;

/// <summary>EF Core mapping for the legacy dbo.UserDetails pass-through. Added 2026-07-10.</summary>
public class UserDetailsConfiguration : IEntityTypeConfiguration<UserDetails>
{
    public void Configure(EntityTypeBuilder<UserDetails> builder)
    {
        builder.ToTable("UserDetails", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(ud => ud.RowId);
        builder.Property(ud => ud.RowId).HasColumnName("RowId").ValueGeneratedNever();
        builder.Property(ud => ud.UserId).HasColumnName("UserId");
        builder.Property(ud => ud.DemandId).HasColumnName("DemandId");
        builder.Property(ud => ud.PrevRowId).HasColumnName("PrevRowId");

        builder.HasIndex(ud => ud.UserId).HasDatabaseName("IX_UserDetails_UserId");
    }
}
