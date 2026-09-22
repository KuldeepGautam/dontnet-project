namespace UBIS.Services.Aim.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Aim.Domain.Entities;

public class PasswordHistoryConfiguration : IEntityTypeConfiguration<PasswordHistory>
{
    public void Configure(EntityTypeBuilder<PasswordHistory> builder)
    {
        builder.ToTable("M_PasswordHistory", "dbo", t => t.ExcludeFromMigrations());

        builder.HasKey(ph => ph.PasswordHistoryId);
        builder.Property(ph => ph.PasswordHistoryId).HasColumnName("PasswordHistoryId").ValueGeneratedOnAdd();

        builder.Property(ph => ph.UserId).HasColumnName("UserId").IsRequired();

        builder.Property(ph => ph.PasswordHash)
            .HasColumnName("PasswordHash")
            .HasColumnType("nvarchar(512)")
            .IsRequired();

        builder.Property(ph => ph.CreatedAtUtc)
            .HasColumnName("CreatedAtUtc")
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // Standard audit columns.
        builder.Property(ph => ph.IsActive).HasColumnName("IsActive");
        builder.Property(ph => ph.UserIdCreatedBy).HasColumnName("UserIdCreatedBy");
        builder.Property(ph => ph.CreatedOnDate).HasColumnName("CreatedOnDate");
        builder.Property(ph => ph.UserIdModifyBy).HasColumnName("UserIdModifyBy");
        builder.Property(ph => ph.ModifiedOnDate).HasColumnName("ModifiedOnDate");
        builder.Property(ph => ph.UserIdDeletedBy).HasColumnName("UserIdDeletedBy");
        builder.Property(ph => ph.DeletedOnDate).HasColumnName("DeletedOnDate");

        builder.HasOne(ph => ph.User)
            .WithMany(u => u.PasswordHistories)
            .HasForeignKey(ph => ph.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ph => new { ph.UserId, ph.CreatedAtUtc })
            .HasDatabaseName("IX_M_PasswordHistory_UserId_CreatedAtUtc");
    }
}
