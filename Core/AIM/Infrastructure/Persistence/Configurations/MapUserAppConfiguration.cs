namespace UBIS.Services.Aim.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Aim.Domain.Entities;

public class MapUserAppConfiguration : IEntityTypeConfiguration<MapUserApp>
{
    public void Configure(EntityTypeBuilder<MapUserApp> builder)
    {
        builder.ToTable("M_MapUserApp", "dbo", t => t.ExcludeFromMigrations());

        builder.HasKey(ma => ma.Id);
        builder.Property(ma => ma.Id).HasColumnName("Id").ValueGeneratedOnAdd();
        builder.Property(ma => ma.UserId).HasColumnName("UserId");
        builder.Property(ma => ma.AppId).HasColumnName("AppId");
        builder.Property(ma => ma.IsActive).HasColumnName("IsActive");
        builder.Property(ma => ma.CreatedAt).HasColumnName("CreatedAt");
        builder.Property(ma => ma.UpdatedAt).HasColumnName("UpdatedAt");

        builder.HasIndex(ma => new { ma.UserId, ma.AppId })
            .IsUnique()
            .HasDatabaseName("UQ_M_MapUserApp_UserId_AppId");

        builder.HasOne(ma => ma.User)
            .WithMany(u => u.MapUserApps)
            .HasForeignKey(ma => ma.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
