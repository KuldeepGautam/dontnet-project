namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Reference;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Reference;

public class AppendixViReceiptTypeConfiguration : IEntityTypeConfiguration<AppendixViReceiptType>
{
    public void Configure(EntityTypeBuilder<AppendixViReceiptType> builder)
    {
        builder.ToTable("M_Appendix_VI_ReceiptType", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
        builder.Property(e => e.Code).HasColumnName("Code").HasMaxLength(10);
        builder.Property(e => e.Name).HasColumnName("Name").HasMaxLength(100);
        builder.Property(e => e.DisplayOrder).HasColumnName("DisplayOrder");
        builder.Property(e => e.IsActive).HasColumnName("IsActive");
        builder.Property(e => e.IsDeleted).HasColumnName("IsDeleted");
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
