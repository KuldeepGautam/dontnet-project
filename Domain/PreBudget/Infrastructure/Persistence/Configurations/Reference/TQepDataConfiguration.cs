namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Reference;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Reference;

public class TQepDataConfiguration : IEntityTypeConfiguration<TQepData>
{
    public void Configure(EntityTypeBuilder<TQepData> builder)
    {
        builder.ToTable("T_QEPData");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("Id");
        builder.Property(e => e.DemandId).HasColumnName("DemandId");
        builder.Property(e => e.QuarterCode).HasColumnName("QuarterCode");
        builder.Property(e => e.Total).HasColumnName("Total");
    }
}
