namespace UBIS.Services.Aim.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Aim.Domain.Entities;

public class MapUserRoleConfiguration : IEntityTypeConfiguration<MapUserRole>
{
    public void Configure(EntityTypeBuilder<MapUserRole> builder)
    {
        builder.ToTable("M_MapUserRole", "dbo", t => t.ExcludeFromMigrations());

        builder.HasKey(uc => uc.UserRoleId);
        builder.Property(uc => uc.UserRoleId).HasColumnName("UserRoleId").ValueGeneratedOnAdd();
        builder.Property(uc => uc.UserId).HasColumnName("UserId");
        builder.Property(uc => uc.RoleId).HasColumnName("RoleId");
        builder.Property(uc => uc.IsActive).HasColumnName("IsActive");
        builder.Property(uc => uc.CreatedAt).HasColumnName("CreatedAt");
        builder.Property(uc => uc.UpdatedAt).HasColumnName("UpdatedAt");

        builder.HasIndex(uc => uc.UserId)
            .IsUnique()
            .HasDatabaseName("UQ_MapUserRole_UserId");

        builder.HasOne(uc => uc.User)
            .WithMany(u => u.MapUserRoles)
            .HasForeignKey(uc => uc.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(uc => uc.Role)
            .WithMany()
            .HasForeignKey(uc => uc.RoleId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
