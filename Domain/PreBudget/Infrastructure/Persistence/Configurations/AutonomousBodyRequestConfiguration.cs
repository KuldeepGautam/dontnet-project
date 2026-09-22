namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities;

public class AutonomousBodyRequestConfiguration : IEntityTypeConfiguration<AutonomousBodyRequest>
{
    public void Configure(EntityTypeBuilder<AutonomousBodyRequest> builder)
    {
        builder.ToTable("M_AutonomousBodyRequest", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(r => r.RequestId);
        builder.Property(r => r.RequestId).HasColumnName("RequestId").ValueGeneratedOnAdd();
        builder.Property(r => r.DemandId).HasColumnName("DemandId");
        builder.Property(r => r.FinancialYear).HasColumnName("FinancialYear").HasMaxLength(9).IsRequired();
        builder.Property(r => r.RequestedName).HasColumnName("RequestedName").HasMaxLength(500).IsRequired();
        builder.Property(r => r.RequestedByUserId).HasColumnName("RequestedByUserId");
        builder.Property(r => r.RequestedAtUtc).HasColumnName("RequestedAtUtc");
        builder.Property(r => r.Status).HasColumnName("Status").HasMaxLength(20).IsRequired();
        builder.Property(r => r.ReviewedByUserId).HasColumnName("ReviewedByUserId");
        builder.Property(r => r.ReviewedAtUtc).HasColumnName("ReviewedAtUtc");
        builder.Property(r => r.ReviewRemarks).HasColumnName("ReviewRemarks").HasMaxLength(500);
        builder.Property(r => r.ApprovedAutonomousBodyId).HasColumnName("ApprovedAutonomousBodyId");

        builder.HasIndex(r => new { r.DemandId, r.Status });
    }
}
