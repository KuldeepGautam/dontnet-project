namespace UBIS.Services.ReferenceData.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.ReferenceData.Domain.Entities;

public class ObjectHeadConfiguration : IEntityTypeConfiguration<ObjectHead>
{
    public void Configure(EntityTypeBuilder<ObjectHead> builder)
    {
        builder.ToTable("M_ObjectHead", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(o => o.ObjectHeadId);
        builder.Property(o => o.ObjectHeadId).HasColumnName("ObjectHeadId").ValueGeneratedOnAdd();
        builder.Property(o => o.ObjectHeadCode).HasColumnName("ObjectHeadCode").HasMaxLength(10).IsRequired();
        builder.Property(o => o.ObjectHeadName).HasColumnName("ObjectHeadName").HasMaxLength(500).IsRequired();
        builder.Property(o => o.IsActive).HasColumnName("IsActive");
        builder.Property(o => o.IsDeleted).HasColumnName("IsDeleted");
        builder.Property(o => o.UserIdCreatedBy).HasColumnName("UserIdCreatedBy");
        builder.Property(o => o.CreatedOnDate).HasColumnName("CreatedOnDate");
        builder.Property(o => o.UserIdModifyBy).HasColumnName("UserIdModifyBy");
        builder.Property(o => o.ModifiedOnDate).HasColumnName("ModifiedOnDate");
        builder.Property(o => o.UserIdDeletedBy).HasColumnName("UserIdDeletedBy");
        builder.Property(o => o.DeletedOnDate).HasColumnName("DeletedOnDate");

        builder.HasQueryFilter(o => !o.IsDeleted);
    }
}
