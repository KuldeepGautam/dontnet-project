namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Reference;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Reference;

public class MDemandMajorHeadConfiguration : IEntityTypeConfiguration<MDemandMajorHead>
{
    public void Configure(EntityTypeBuilder<MDemandMajorHead> builder)
    {
        builder.ToTable("M_DemandMajorHead", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.DemandMajHeadId);
        builder.Property(e => e.DemandMajHeadId).HasColumnName("DemandMajHeadId");
        builder.Property(e => e.FinancialYear).HasColumnName("FinancialYear");
        builder.Property(e => e.DemandId).HasColumnName("DemandId");
        builder.Property(e => e.DemandNo).HasColumnName("DemandNo");
        builder.Property(e => e.MajorHeadCode).HasColumnName("MajorHeadCode");
        builder.Property(e => e.IsActive).HasColumnName("IsActive");
        builder.Property(e => e.IsDeleted).HasColumnName("IsDeleted");
    }
}
