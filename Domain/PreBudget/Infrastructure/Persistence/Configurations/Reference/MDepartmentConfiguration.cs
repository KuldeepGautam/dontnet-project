namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Reference;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Reference;

public class MDepartmentConfiguration : IEntityTypeConfiguration<MDepartment>
{
    public void Configure(EntityTypeBuilder<MDepartment> builder)
    {
        builder.ToTable("M_Department");
        builder.HasKey(e => e.DepartmentId);
        builder.Property(e => e.DepartmentId).HasColumnName("DepartmentId").ValueGeneratedNever();
        builder.Property(e => e.MinistryId).HasColumnName("MinistryId");
        builder.Property(e => e.DepartmentCode).HasColumnName("DepartmentCode");
        builder.Property(e => e.DepartmentName).HasColumnName("DepartmentName");
        builder.Property(e => e.HDepartmentName).HasColumnName("HDepartmentName");
        builder.Property(e => e.Active).HasColumnName("Active").IsRequired();
        builder.Property(e => e.Remarks).HasColumnName("Remarks");
        builder.Property(e => e.EntryDate).HasColumnName("EntryDate");
        builder.Property(e => e.PrevDepartmentId).HasColumnName("PrevDepartmentId");
    }
}
