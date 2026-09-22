namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Reference;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Reference;

public class SbeDataConfiguration : IEntityTypeConfiguration<SbeData>
{
    public void Configure(EntityTypeBuilder<SbeData> builder)
    {
        builder.ToTable("SBEData");
        builder.HasKey(e => e.SBEDataID);
        builder.Property(e => e.SBEDataID).HasColumnName("SBEDataID");
        builder.Property(e => e.DemandId).HasColumnName("DemandId");
        builder.Property(e => e.FinancialYear).HasColumnName("FinancialYear");
        builder.Property(e => e.MajorHeadCode).HasColumnName("MajorHeadCode");
        builder.Property(e => e.NBE_PLan).HasColumnName("NBE_PLan");
        builder.Property(e => e.Actual_plan).HasColumnName("Actual_plan");
        builder.Property(e => e.Spl_SchemeId).HasColumnName("Spl_SchemeId");
        builder.Property(e => e.Exp_Type).HasColumnName("Exp_Type");
        builder.Property(e => e.CategoryId).HasColumnName("CategoryId");
        builder.Property(e => e.SchemeID).HasColumnName("SchemeID");
        builder.Property(e => e.BE_Plan).HasColumnName("BE_Plan");
    }
}
