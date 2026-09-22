namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;

public class PublicAccountReceiptPaymentConfiguration : AppendixEntityConfigurationBase<PublicAccountReceiptPayment>, IEntityTypeConfiguration<PublicAccountReceiptPayment>
{
    public void Configure(EntityTypeBuilder<PublicAccountReceiptPayment> builder)
    {
        ConfigureCommon(builder, "Appendix_PA_ReceiptPayment");

        builder.Property(e => e.MajorHeadId).HasColumnName("MajorHeadId");
        builder.Property(e => e.ActualReceipt).HasColumnName("ActualReceipt").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ActualPayment).HasColumnName("ActualPayment").HasColumnType("decimal(18,2)");
        builder.Property(e => e.BalanceAtEndReceipt).HasColumnName("BalanceAtEndReceipt").HasColumnType("decimal(18,2)");
        builder.Property(e => e.BalanceAtEndPayment).HasColumnName("BalanceAtEndPayment").HasColumnType("decimal(18,2)");
        builder.Property(e => e.BEReceipt).HasColumnName("BEReceipt").HasColumnType("decimal(18,2)");
        builder.Property(e => e.BEPayment).HasColumnName("BEPayment").HasColumnType("decimal(18,2)");
        builder.Property(e => e.AdjustmentReceipt).HasColumnName("AdjustmentReceipt").HasColumnType("decimal(18,2)");
        builder.Property(e => e.AdjustmentPayment).HasColumnName("AdjustmentPayment").HasColumnType("decimal(18,2)");
        builder.Property(e => e.REReceipt).HasColumnName("REReceipt").HasColumnType("decimal(18,2)");
        builder.Property(e => e.REPayment).HasColumnName("REPayment").HasColumnType("decimal(18,2)");
        builder.Property(e => e.NBEReceipt).HasColumnName("NBEReceipt").HasColumnType("decimal(18,2)");
        builder.Property(e => e.NBEPayment).HasColumnName("NBEPayment").HasColumnType("decimal(18,2)");
        builder.Property(e => e.RemarksReceipt).HasColumnName("RemarksReceipt");
        builder.Property(e => e.RemarksPayment).HasColumnName("RemarksPayment");
    }
}
