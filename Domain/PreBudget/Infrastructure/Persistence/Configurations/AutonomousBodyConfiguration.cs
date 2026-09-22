namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities;

public class AutonomousBodyConfiguration : IEntityTypeConfiguration<AutonomousBody>
{
    public void Configure(EntityTypeBuilder<AutonomousBody> builder)
    {
        builder.ToTable("M_AutonomousBody", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(a => a.AutonomousBodyId);
        builder.Property(a => a.AutonomousBodyId).HasColumnName("AutonomousBodyId").ValueGeneratedOnAdd();
        builder.Property(a => a.FinancialYear).HasColumnName("FinancialYear").HasMaxLength(9).IsRequired();
        builder.Property(a => a.DemandId).HasColumnName("DemandId");
        builder.Property(a => a.DemandNo).HasColumnName("DemandNo");
        builder.Property(a => a.Code).HasColumnName("Code").HasMaxLength(50);
        builder.Property(a => a.Name).HasColumnName("Name").HasMaxLength(500).IsRequired();
        builder.Property(a => a.HName).HasColumnName("HName").HasMaxLength(500);
        builder.Property(a => a.PrevAutonomousBodyId).HasColumnName("PrevAutonomousBodyId");
        builder.Property(a => a.IsActive).HasColumnName("IsActive");
        builder.Property(a => a.IsDeleted).HasColumnName("IsDeleted");
        builder.Property(a => a.UserIdCreatedBy).HasColumnName("UserIdCreatedBy");
        builder.Property(a => a.CreatedOnDate).HasColumnName("CreatedOnDate");
        builder.Property(a => a.UserIdModifyBy).HasColumnName("UserIdModifyBy");
        builder.Property(a => a.ModifiedOnDate).HasColumnName("ModifiedOnDate");
        builder.Property(a => a.UserIdDeletedBy).HasColumnName("UserIdDeletedBy");
        builder.Property(a => a.DeletedOnDate).HasColumnName("DeletedOnDate");

        builder.HasQueryFilter(a => !a.IsDeleted);
        builder.HasIndex(a => new { a.DemandId, a.FinancialYear });
        builder.HasIndex(a => a.DemandNo);
    }
}
