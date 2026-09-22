namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;

public class AppendixNonTaxRevenueConfiguration : AppendixEntityConfigurationBase<AppendixNonTaxRevenue>, IEntityTypeConfiguration<AppendixNonTaxRevenue>
{
    public void Configure(EntityTypeBuilder<AppendixNonTaxRevenue> builder)
    {
        ConfigureCommon(builder, "Appendix_VI_NonTaxRevenue");

        builder.Property(e => e.ReceiptTypeId).HasColumnName("ReceiptTypeId");
        builder.Property(e => e.ReceiptType).HasColumnName("ReceiptType").HasMaxLength(60);
        builder.Property(e => e.PsuReceiptName).HasColumnName("PsuReceiptName").HasMaxLength(250);
        builder.Property(e => e.Actuals).HasColumnName("Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.BE).HasColumnName("BE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ActualsUptoSept).HasColumnName("ActualsUptoSept").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ProposedBE).HasColumnName("ProposedBE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ProposedCollectionQ3).HasColumnName("ProposedCollectionQ3").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ProposedCollectionQ4).HasColumnName("ProposedCollectionQ4").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Remarks).HasColumnName("Remarks");
    }
}
