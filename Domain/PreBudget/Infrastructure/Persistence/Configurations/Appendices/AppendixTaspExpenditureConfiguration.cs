namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;

public class AppendixTaspExpenditureConfiguration : AppendixEntityConfigurationBase<AppendixTaspExpenditure>, IEntityTypeConfiguration<AppendixTaspExpenditure>
{
    public void Configure(EntityTypeBuilder<AppendixTaspExpenditure> builder)
    {
        ConfigureCommon(builder, "Appendix_IVB_TaspExpenditure");

        builder.Property(e => e.SchemeId).HasColumnName("SchemeId");
        builder.Property(e => e.SubSchemeId).HasColumnName("SubSchemeId");
        builder.Property(e => e.Actuals).HasColumnName("Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ActualsUptoSeptPrevYear).HasColumnName("ActualsUptoSeptPrevYear").HasColumnType("decimal(18,2)");
        builder.Property(e => e.BE).HasColumnName("BE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ActualsUptoSept).HasColumnName("ActualsUptoSept").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ProposedRE).HasColumnName("ProposedRE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.AddlReSought).HasColumnName("AddlReSought").HasColumnType("decimal(18,2)");
        builder.Property(e => e.BudgetRecommendedRE).HasColumnName("BudgetRecommendedRE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ProposedNBE).HasColumnName("ProposedNBE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.AddlNbeSought).HasColumnName("AddlNbeSought").HasColumnType("decimal(18,2)");
        builder.Property(e => e.BudgetRecommendedNBE).HasColumnName("BudgetRecommendedNBE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.RemarksMinistry).HasColumnName("RemarksMinistry");
        builder.Property(e => e.RemarksBudget).HasColumnName("RemarksBudget");
    }
}
