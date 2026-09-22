namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;

public class AppendixCorpusFundAbGiaConfiguration : AppendixEntityConfigurationBase<AppendixCorpusFundAbGia>, IEntityTypeConfiguration<AppendixCorpusFundAbGia>
{
    public void Configure(EntityTypeBuilder<AppendixCorpusFundAbGia> builder)
    {
        ConfigureCommon(builder, "Appendix_VIE_CorpusFundAbGia");

        builder.Property(e => e.AutonomousBodyId).HasColumnName("AutonomousBodyId");
        builder.Property(e => e.CorpusFundBalance1).HasColumnName("CorpusFundBalance1").HasColumnType("decimal(18,2)");
        builder.Property(e => e.CorpusFundBalance2).HasColumnName("CorpusFundBalance2").HasColumnType("decimal(18,2)");
        builder.Property(e => e.CorpusFundBankName).HasColumnName("CorpusFundBankName").HasMaxLength(500);
        builder.Property(e => e.CorpusFundReason).HasColumnName("CorpusFundReason").HasMaxLength(1000);
        builder.Property(e => e.GiaRE).HasColumnName("GiaRE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.GiaBE).HasColumnName("GiaBE").HasColumnType("decimal(18,2)");

        builder.HasOne<AutonomousBody>().WithMany().HasForeignKey(e => e.AutonomousBodyId).OnDelete(DeleteBehavior.Restrict);
    }
}
