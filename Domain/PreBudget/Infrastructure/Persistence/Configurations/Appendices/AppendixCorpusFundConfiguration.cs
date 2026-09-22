namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;

public class AppendixCorpusFundConfiguration : AppendixEntityConfigurationBase<AppendixCorpusFund>, IEntityTypeConfiguration<AppendixCorpusFund>
{
    public void Configure(EntityTypeBuilder<AppendixCorpusFund> builder)
    {
        ConfigureCommon(builder, "Appendix_VIC_CorpusFund");

        builder.Property(e => e.AutonomousBodyId).HasColumnName("AutonomousBodyId");
        builder.Property(e => e.IsPublicAccount).HasColumnName("IsPublicAccount");
        builder.Property(e => e.AccumulatedBalancePrevYear).HasColumnName("AccumulatedBalancePrevYear").HasColumnType("decimal(18,2)");
        builder.Property(e => e.AccumulatedBalance).HasColumnName("AccumulatedBalance").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ActualExpenditureY1).HasColumnName("ActualExpenditureY1").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ActualExpenditureY2).HasColumnName("ActualExpenditureY2").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ActualExpenditureY3).HasColumnName("ActualExpenditureY3").HasColumnType("decimal(18,2)");
        builder.Property(e => e.AllocationInBE).HasColumnName("AllocationInBE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ExpenditureTillSept).HasColumnName("ExpenditureTillSept").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ReasonForCorpusFund).HasColumnName("ReasonForCorpusFund").HasMaxLength(1000);

        builder.HasOne<AutonomousBody>().WithMany().HasForeignKey(e => e.AutonomousBodyId).OnDelete(DeleteBehavior.Restrict);
    }
}
