namespace UBIS.Services.Sbe.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Sbe.Domain.Entities;

public class PseDemandConfiguration : IEntityTypeConfiguration<PseDemand>
{
    public void Configure(EntityTypeBuilder<PseDemand> builder)
    {
        builder.ToTable("PSEDemand", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.PseDemandId);
        builder.Property(e => e.PseDemandId).HasColumnName("PSEDemandId").ValueGeneratedOnAdd();
        builder.Property(e => e.FinancialYear).HasColumnName("FinancialYear").HasMaxLength(9);
        builder.Property(e => e.UserId).HasColumnName("UserId");
        builder.Property(e => e.DemandId).HasColumnName("DemandId");
        builder.Property(e => e.UnFreezeUserId).HasColumnName("UnFreezeUserid");
        builder.Property(e => e.TargetDate).HasColumnName("TargetDate");
        builder.Property(e => e.UnFreezeDate).HasColumnName("UnFreezeDate");
        builder.Property(e => e.Status).HasColumnName("Status").HasMaxLength(1);
        builder.Property(e => e.Freez).HasColumnName("Freez").HasMaxLength(1);
        builder.Property(e => e.EntryDate).HasColumnName("EntryDate");
        builder.Property(e => e.Ip).HasColumnName("IP").HasMaxLength(15);

        builder.Ignore(e => e.IsFrozen);
        builder.HasIndex(e => new { e.DemandId, e.FinancialYear }).IsUnique();
    }
}
