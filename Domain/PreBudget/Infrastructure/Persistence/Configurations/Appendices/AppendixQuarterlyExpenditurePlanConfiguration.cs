namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;

public class AppendixQuarterlyExpenditurePlanConfiguration : AppendixEntityConfigurationBase<AppendixQuarterlyExpenditurePlan>, IEntityTypeConfiguration<AppendixQuarterlyExpenditurePlan>
{
    public void Configure(EntityTypeBuilder<AppendixQuarterlyExpenditurePlan> builder)
    {
        ConfigureCommon(builder, "Appendix_II_QuarterlyExpenditurePlan");

        builder.Property(e => e.Q1ApprovedQepPrevYear).HasColumnName("Q1ApprovedQepPrevYear").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Q1ActualsPrevYear).HasColumnName("Q1ActualsPrevYear").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Q1ApprovedQep).HasColumnName("Q1ApprovedQep").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Q1Actuals).HasColumnName("Q1Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.RemarksQ1).HasColumnName("RemarksQ1").HasMaxLength(500);
        builder.Property(e => e.Q1HasDeviation).HasColumnName("Q1HasDeviation");
        builder.Property(e => e.Q1MofApprovalDetails).HasColumnName("Q1MofApprovalDetails").HasMaxLength(500);

        builder.Property(e => e.Q2ApprovedQepPrevYear).HasColumnName("Q2ApprovedQepPrevYear").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Q2ActualsPrevYear).HasColumnName("Q2ActualsPrevYear").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Q2ApprovedQep).HasColumnName("Q2ApprovedQep").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Q2Actuals).HasColumnName("Q2Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.RemarksQ2).HasColumnName("RemarksQ2").HasMaxLength(500);
        builder.Property(e => e.Q2HasDeviation).HasColumnName("Q2HasDeviation");
        builder.Property(e => e.Q2MofApprovalDetails).HasColumnName("Q2MofApprovalDetails").HasMaxLength(500);

        builder.Ignore(e => e.TotalApprovedQep);
        builder.Ignore(e => e.TotalActuals);
    }
}
