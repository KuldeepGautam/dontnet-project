namespace UBIS.Services.MenuGenerator.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.MenuGenerator.Domain.Entities;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("M_Role", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(r => r.RoleId);
        builder.Property(r => r.RoleId).HasColumnName("RoleId").ValueGeneratedOnAdd();
        builder.Property(r => r.RoleName).HasColumnName("RoleName");
        builder.Property(r => r.Active).HasColumnName("Active");
    }
}
