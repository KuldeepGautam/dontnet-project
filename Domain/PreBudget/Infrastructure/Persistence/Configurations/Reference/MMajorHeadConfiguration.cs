namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Reference;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Reference;

public class MMajorHeadConfiguration : IEntityTypeConfiguration<MMajorHead>
{
    public void Configure(EntityTypeBuilder<MMajorHead> builder)
    {
        builder.ToTable("M_MajorHead");
        builder.HasKey(e => e.MajorHeadId);
        builder.Property(e => e.MajorHeadId).HasColumnName("MajorHeadId");
        builder.Property(e => e.MajorHeadCode).HasColumnName("MajorHeadCode");
        builder.Property(e => e.MajorHeadName).HasColumnName("MajorHeadName");
        builder.Property(e => e.IsActive).HasColumnName("IsActive");
        builder.Property(e => e.IsDeleted).HasColumnName("IsDeleted");
    }
}
