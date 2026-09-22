namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Reference;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Reference;

public class DdgNewConfiguration : IEntityTypeConfiguration<DdgNew>
{
    public void Configure(EntityTypeBuilder<DdgNew> builder)
    {
        builder.ToTable("DDG");
        builder.HasKey(e => e.ComputerSlno);
        builder.Property(e => e.ComputerSlno).HasColumnName("ComputerSlno");
        builder.Property(e => e.DemandId).HasColumnName("DemandId");
        builder.Property(e => e.HeadOfAccount).HasColumnName("HeadOfAccount");
        builder.Property(e => e.NBE_Plan).HasColumnName("NBE_Plan");
        builder.Property(e => e.Actual_Plan).HasColumnName("Actual_Plan");
        builder.Property(e => e.SchemeId).HasColumnName("SchemeId");
        builder.Property(e => e.FinancialYear).HasColumnName("FinancialYear");
        builder.Property(e => e.DemandNo).HasColumnName("DemandNo");
    }
}
