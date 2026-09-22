namespace UBIS.Services.MenuGenerator.Infrastructure.Persistence.DevSeed.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.MenuGenerator.Domain.Entities;

/// <summary>
/// Dev/test-seed-only configuration for User - creates and seeds "M_Users" with a single demo
/// login (password "Demo@123", BCrypt-hashed with the same work factor AIM uses). RoleId 13
/// = Administrator, so the demo user has a full menu to exercise the API against.
/// Never seed a real production password here - this table only exists in the throwaway
/// dev/test database, never in production.
/// </summary>
public class UserSeedConfiguration : IEntityTypeConfiguration<User>
{
    private const string DemoPasswordHash = "$2a$12$zbBVvJq.jV2WJBPJU4dQfOSnvnwwI5gYwaXdLyki.H2qbifpQFdNG";

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("M_Users", "dbo");
        builder.HasKey(u => u.UserId);
        builder.Property(u => u.UserId).HasColumnName("UserId").ValueGeneratedNever();
        builder.Property(u => u.UserName).HasColumnName("UserName").HasMaxLength(50).IsRequired();
        builder.Property(u => u.Password).HasColumnName("Password").HasMaxLength(512).IsRequired();
        builder.Property(u => u.Email).HasColumnName("Email").HasMaxLength(320).IsRequired();
        builder.Property(u => u.RoleId).HasColumnName("RoleId");

        builder.HasData(
            new User
            {
                UserId = 1,
                UserName = "demo",
                Password = DemoPasswordHash,
                Email = "demo@ubis.local",
                RoleId = 13
            });
    }
}
