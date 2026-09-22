namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities;

public class RemarkConfiguration : IEntityTypeConfiguration<Remark>
{
    public void Configure(EntityTypeBuilder<Remark> builder)
    {
        builder.ToTable("PreBudgetRemark", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("Id").ValueGeneratedOnAdd();
        builder.Property(r => r.DemandId).HasColumnName("DemandId");
        builder.Property(r => r.AppendixId).HasColumnName("AppendixId");
        builder.Property(r => r.FinancialYear).HasColumnName("FinancialYear").HasMaxLength(9).IsRequired();
        builder.Property(r => r.RemarkText).HasColumnName("RemarkText").IsRequired();
        builder.Property(r => r.CreatedByUserId).HasColumnName("CreatedByUserId");
        builder.Property(r => r.CreatedByRoleSnapshot).HasColumnName("CreatedByRoleSnapshot").HasMaxLength(100);
        builder.Property(r => r.CreatedAtUtc).HasColumnName("CreatedAtUtc");
        builder.Property(r => r.IsDeleted).HasColumnName("IsDeleted");

        builder.HasQueryFilter(r => !r.IsDeleted);
        builder.HasIndex(r => new { r.DemandId, r.AppendixId, r.FinancialYear });
    }
}
