namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;

public class AppendixEstablishmentExpenditureConfiguration : AppendixEntityConfigurationBase<AppendixEstablishmentExpenditure>, IEntityTypeConfiguration<AppendixEstablishmentExpenditure>
{
    public void Configure(EntityTypeBuilder<AppendixEstablishmentExpenditure> builder)
    {
        ConfigureCommon(builder, "Appendix_V_EstablishmentExpenditure");

        builder.Property(e => e.Category).HasColumnName("Category").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Actuals).HasColumnName("Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ActualsUptoSeptPrevYear).HasColumnName("ActualsUptoSeptPrevYear").HasColumnType("decimal(18,2)");
        builder.Property(e => e.BE).HasColumnName("BE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ActualsUptoSept).HasColumnName("ActualsUptoSept").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ProposedRE).HasColumnName("ProposedRE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.BudgetRecommendedRE).HasColumnName("BudgetRecommendedRE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ProposedNBE).HasColumnName("ProposedNBE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.BudgetRecommendedNBE).HasColumnName("BudgetRecommendedNBE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.RemarksBudget).HasColumnName("RemarksBudget");
    }
}
