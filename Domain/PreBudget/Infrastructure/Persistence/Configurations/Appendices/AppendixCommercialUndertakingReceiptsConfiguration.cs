namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;

public class AppendixCommercialUndertakingReceiptsConfiguration : AppendixEntityConfigurationBase<AppendixCommercialUndertakingReceipts>, IEntityTypeConfiguration<AppendixCommercialUndertakingReceipts>
{
    public void Configure(EntityTypeBuilder<AppendixCommercialUndertakingReceipts> builder)
    {
        ConfigureCommon(builder, "Appendix_VIIB_CommercialUndertakingReceipts");

        builder.Property(e => e.SchemeId).HasColumnName("SchemeId");
        // Widened from 10 to 50 (2026-08-14, bug found via smoke test): "Revenue Expenditure"/
        // "Revenue Receipts" (dbo.Appendix_VIIB_Transaction_Type's own seed values) are 17-20 chars
        // - the original nvarchar(10) meant no VII-B record with a real transaction type could ever
        // actually save; every attempt failed with a SQL truncation error.
        builder.Property(e => e.TransactionType).HasColumnName("TransactionType").HasMaxLength(50);
        builder.Property(e => e.MajorHeadId).HasColumnName("MajorHeadId");
        builder.Property(e => e.ActualsY2).HasColumnName("ActualsY2").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ActualsY1).HasColumnName("ActualsY1").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ActualsUptoSept).HasColumnName("ActualsUptoSept").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ActualsUptoSeptPrevYear).HasColumnName("ActualsUptoSeptPrevYear").HasColumnType("decimal(18,2)");
        builder.Property(e => e.BE).HasColumnName("BE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.RE).HasColumnName("RE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.IncreasedBE).HasColumnName("IncreasedBE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.NBE).HasColumnName("NBE").HasColumnType("decimal(18,2)");
    }
}
