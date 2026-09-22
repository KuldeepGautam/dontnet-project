namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;

public class AppendixSchemeAppraisalStatusConfiguration : AppendixEntityConfigurationBase<AppendixSchemeAppraisalStatus>, IEntityTypeConfiguration<AppendixSchemeAppraisalStatus>
{
    public void Configure(EntityTypeBuilder<AppendixSchemeAppraisalStatus> builder)
    {
        ConfigureCommon(builder, "Appendix_IIIB_SchemeAppraisalStatus");

        builder.Property(e => e.CategoryId).HasColumnName("CategoryId");
        builder.Property(e => e.SchemeId).HasColumnName("SchemeId");
        builder.Property(e => e.CategoryType).HasColumnName("CategoryType").HasMaxLength(250).IsRequired();
        builder.Property(e => e.SchemeName).HasColumnName("SchemeName").HasMaxLength(500).IsRequired();
        builder.Property(e => e.StatusOfFreshAppraisalApproval).HasColumnName("StatusOfFreshAppraisalApproval").HasMaxLength(500).IsRequired();
        builder.Property(e => e.SchemeApprovalValidUpto).HasColumnName("SchemeApprovalValidUpto").HasColumnType("date");
        builder.Property(e => e.Remarks).HasColumnName("Remarks").HasMaxLength(500);
    }
}
