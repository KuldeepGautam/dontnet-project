namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Reference;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Reference;

public class SbeNbeSummaryByYearConfiguration : IEntityTypeConfiguration<SbeNbeSummaryByYear>
{
    public void Configure(EntityTypeBuilder<SbeNbeSummaryByYear> builder)
    {
        builder.ToTable("SbeNbeSummaryByYear");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("Id");
        builder.Property(e => e.DemandId).HasColumnName("DemandId");
        builder.Property(e => e.FinancialYear).HasColumnName("FinancialYear");
        builder.Property(e => e.SchemeId).HasColumnName("SchemeId");
        builder.Property(e => e.SubSchemeId).HasColumnName("SubSchemeId");
        builder.Property(e => e.NbeTotal).HasColumnName("NbeTotal");
    }
}
