namespace UBIS.Services.Aim.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Aim.Domain.Entities;

public class UserFinancialYearConfiguration : IEntityTypeConfiguration<UserFinancialYear>
{
    public void Configure(EntityTypeBuilder<UserFinancialYear> builder)
    {
        builder.ToTable("M_MapUserFY", "dbo", t => t.ExcludeFromMigrations());

        builder.HasKey(fy => fy.Id);
        builder.Property(fy => fy.Id).HasColumnName("Id").ValueGeneratedOnAdd();
        builder.Property(fy => fy.UserId).HasColumnName("UserId");
        builder.Property(fy => fy.FinancialYear).HasColumnName("FinancialYear").HasMaxLength(9).IsRequired();
        builder.Property(fy => fy.CreatedAt).HasColumnName("CreatedAt");
        builder.Property(fy => fy.UpdatedAt).HasColumnName("UpdatedAt");

        builder.HasIndex(fy => new { fy.UserId, fy.FinancialYear })
            .IsUnique()
            .HasDatabaseName("UQ_M_MapUserFY_UserId_FinancialYear");

        builder.HasOne(fy => fy.User)
            .WithMany(u => u.UserFinancialYears)
            .HasForeignKey(fy => fy.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
