namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities;

public class DemandAllocationConfiguration : IEntityTypeConfiguration<DemandAllocation>
{
    public void Configure(EntityTypeBuilder<DemandAllocation> builder)
    {
        builder.ToTable("M_DemandAppendixAllocation", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("Id").ValueGeneratedOnAdd();
        builder.Property(s => s.DemandId).HasColumnName("DemandId");
        builder.Property(s => s.AppendixId).HasColumnName("AppendixId");
        builder.Property(s => s.FinancialYear).HasColumnName("FinancialYear").HasMaxLength(9).IsRequired();
        builder.Property(s => s.TargetDate).HasColumnName("TargetDate");
        builder.Property(s => s.NilRemarks).HasColumnName("NilRemarks").HasMaxLength(500);
        builder.Property(s => s.FrozenAtUtc).HasColumnName("FrozenAtUtc");
        builder.Property(s => s.FrozenByUserId).HasColumnName("FrozenByUserId");
        builder.Property(s => s.IsDeleted).HasColumnName("IsDeleted");
        builder.Property(s => s.UserIdCreatedBy).HasColumnName("UserIdCreatedBy");
        builder.Property(s => s.CreatedOnDate).HasColumnName("CreatedOnDate");
        builder.Property(s => s.UserIdModifyBy).HasColumnName("UserIdModifyBy");
        builder.Property(s => s.ModifiedOnDate).HasColumnName("ModifiedOnDate");
        builder.Property(s => s.UserIdDeletedBy).HasColumnName("UserIdDeletedBy");
        builder.Property(s => s.DeletedOnDate).HasColumnName("DeletedOnDate");

        builder.HasQueryFilter(s => !s.IsDeleted);
        builder.HasIndex(s => new { s.DemandId, s.AppendixId, s.FinancialYear }).IsUnique();
    }
}
