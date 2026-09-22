namespace UBIS.Services.Aim.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Aim.Domain.Entities;

public class MapUserIPAddressConfiguration : IEntityTypeConfiguration<MapUserIPAddress>
{
    public void Configure(EntityTypeBuilder<MapUserIPAddress> builder)
    {
        builder.ToTable("M_MapUserIPAddress", "dbo", t => t.ExcludeFromMigrations());

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("Id").ValueGeneratedOnAdd();
        builder.Property(m => m.UserId).HasColumnName("UserId");
        builder.Property(m => m.IPAddress1).HasColumnName("IPAddress1").HasMaxLength(50);
        builder.Property(m => m.IPAddress2).HasColumnName("IPAddress2").HasMaxLength(50);
        builder.Property(m => m.IPAuthFlag).HasColumnName("IPAuthFlag").IsRequired();
        builder.Property(m => m.UserIdCreatedBy).HasColumnName("UserIdCreatedBy");
        builder.Property(m => m.CreatedOnDate).HasColumnName("CreatedOnDate");
        builder.Property(m => m.UserIdModifyBy).HasColumnName("UserIdModifyBy");
        builder.Property(m => m.ModifiedOnDate).HasColumnName("ModifiedOnDate");

        builder.HasIndex(m => m.UserId).HasDatabaseName("IX_M_MapUserIPAddress_UserId").IsUnique();
    }
}
