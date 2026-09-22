namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Reference;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Reference;

public class MObjectHeadConfiguration : IEntityTypeConfiguration<MObjectHead>
{
    public void Configure(EntityTypeBuilder<MObjectHead> builder)
    {
        builder.ToTable("M_ObjectHead");
        builder.HasKey(e => e.ObjectHeadId);
        builder.Property(e => e.ObjectHeadId).HasColumnName("ObjectHeadId");
        builder.Property(e => e.ObjectHeadCode).HasColumnName("ObjectHeadCode");
        builder.Property(e => e.ObjectHeadName).HasColumnName("ObjectHeadName");
        builder.Property(e => e.IsActive).HasColumnName("IsActive");
        builder.Property(e => e.IsDeleted).HasColumnName("IsDeleted");
    }
}
