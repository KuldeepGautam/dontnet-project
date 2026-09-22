namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;

public class AppendixCnaSnaBalanceConfiguration : AppendixEntityConfigurationBase<AppendixCnaSnaBalance>, IEntityTypeConfiguration<AppendixCnaSnaBalance>
{
    public void Configure(EntityTypeBuilder<AppendixCnaSnaBalance> builder)
    {
        ConfigureCommon(builder, "Appendix_III_CnaSnaBalance");

        builder.Property(e => e.CategoryType).HasColumnName("CategoryType").HasMaxLength(30).IsRequired();
        builder.Property(e => e.SchemeId).HasColumnName("SchemeId");
        builder.Property(e => e.SubSchemeId).HasColumnName("SubSchemeId");
        builder.Property(e => e.BE).HasColumnName("BE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.BalanceAsOnAprilOpening).HasColumnName("BalanceAsOnAprilOpening").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ReleasesDuringFY).HasColumnName("ReleasesDuringFY").HasColumnType("decimal(18,2)");
        builder.Property(e => e.BalanceAsOnSeptClosing).HasColumnName("BalanceAsOnSeptClosing").HasColumnType("decimal(18,2)");
        builder.Property(e => e.DateOfLastRelease).HasColumnName("DateOfLastRelease");
        builder.Property(e => e.AmountOfLastRelease).HasColumnName("AmountOfLastRelease").HasColumnType("decimal(18,2)");
        builder.Property(e => e.NotTransferredToSnaAsOnSept).HasColumnName("NotTransferredToSnaAsOnSept").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Remarks).HasColumnName("Remarks");
        builder.Property(e => e.ReasonForExemption).HasColumnName("ReasonForExemption");
    }
}
