namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Appendices;

public class AppendixEstablishmentOtherThanABConfiguration : AppendixEntityConfigurationBase<AppendixEstablishmentOtherThanAB>, IEntityTypeConfiguration<AppendixEstablishmentOtherThanAB>
{
    public void Configure(EntityTypeBuilder<AppendixEstablishmentOtherThanAB> builder)
    {
        ConfigureCommon(builder, "Appendix_VC_EstablishmentOtherThanAB");

        builder.Property(e => e.Name).HasColumnName("Name").HasMaxLength(250).IsRequired();
        builder.Property(e => e.Actuals).HasColumnName("Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ActualsUptoSeptPrevYear).HasColumnName("ActualsUptoSeptPrevYear").HasColumnType("decimal(18,2)");
        builder.Property(e => e.BE).HasColumnName("BE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ActualsUptoSept).HasColumnName("ActualsUptoSept").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ProposedRE).HasColumnName("ProposedRE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ProposedNBE).HasColumnName("ProposedNBE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Remarks).HasColumnName("Remarks");
    }
}
