namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;

public class AppendixGrantInAidConfiguration : AppendixEntityConfigurationBase<AppendixGrantInAid>, IEntityTypeConfiguration<AppendixGrantInAid>
{
    public void Configure(EntityTypeBuilder<AppendixGrantInAid> builder)
    {
        ConfigureCommon(builder, "Appendix_VA_GrantInAid");

        builder.Property(e => e.AutonomousBodyId).HasColumnName("AutonomousBodyId");

        builder.Property(e => e.GiaGeneralActuals).HasColumnName("GiaGeneralActuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.GiaGeneralActualsUptoSeptPrevYear).HasColumnName("GiaGeneralActualsUptoSeptPrevYear").HasColumnType("decimal(18,2)");
        builder.Property(e => e.GiaGeneralBE).HasColumnName("GiaGeneralBE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.GiaGeneralActualsUptoSept).HasColumnName("GiaGeneralActualsUptoSept").HasColumnType("decimal(18,2)");
        builder.Property(e => e.GiaGeneralRE).HasColumnName("GiaGeneralRE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.GiaGeneralNBE).HasColumnName("GiaGeneralNBE").HasColumnType("decimal(18,2)");

        builder.Property(e => e.GiaCcaActuals).HasColumnName("GiaCcaActuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.GiaCcaActualsUptoSeptPrevYear).HasColumnName("GiaCcaActualsUptoSeptPrevYear").HasColumnType("decimal(18,2)");
        builder.Property(e => e.GiaCcaBE).HasColumnName("GiaCcaBE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.GiaCcaActualsUptoSept).HasColumnName("GiaCcaActualsUptoSept").HasColumnType("decimal(18,2)");
        builder.Property(e => e.GiaCcaRE).HasColumnName("GiaCcaRE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.GiaCcaNBE).HasColumnName("GiaCcaNBE").HasColumnType("decimal(18,2)");

        builder.Property(e => e.GiaSalaryActuals).HasColumnName("GiaSalaryActuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.GiaSalaryActualsUptoSeptPrevYear).HasColumnName("GiaSalaryActualsUptoSeptPrevYear").HasColumnType("decimal(18,2)");
        builder.Property(e => e.GiaSalaryTotal).HasColumnName("GiaSalaryTotal").HasColumnType("decimal(18,2)");
        builder.Property(e => e.GiaSalaryBE).HasColumnName("GiaSalaryBE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.GiaSalaryActualsUptoSept).HasColumnName("GiaSalaryActualsUptoSept").HasColumnType("decimal(18,2)");
        builder.Property(e => e.GiaSalaryRE).HasColumnName("GiaSalaryRE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.GiaSalaryNBE).HasColumnName("GiaSalaryNBE").HasColumnType("decimal(18,2)");

        builder.HasOne<AutonomousBody>().WithMany().HasForeignKey(e => e.AutonomousBodyId).OnDelete(DeleteBehavior.Restrict);
    }
}
