namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities;

public class AppendixConfiguration : IEntityTypeConfiguration<Appendix>
{
    public void Configure(EntityTypeBuilder<Appendix> builder)
    {
        builder.ToTable("M_Appendix", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(a => a.AppendixId);
        builder.Property(a => a.AppendixId).HasColumnName("AppendixId").ValueGeneratedOnAdd();
        builder.Property(a => a.FinancialYear).HasColumnName("FinancialYear").HasMaxLength(9).IsRequired();
        builder.Property(a => a.Code).HasColumnName("Code").HasMaxLength(20).IsRequired();
        builder.Property(a => a.Name).HasColumnName("Name").HasMaxLength(250).IsRequired();
        builder.Property(a => a.HName).HasColumnName("HName").HasMaxLength(500);
        builder.Property(a => a.Remarks).HasColumnName("Remarks");
        builder.Property(a => a.ParaNo).HasColumnName("ParaNo").HasMaxLength(50);
        builder.Property(a => a.DisplaySequence).HasColumnName("DisplaySequence");
        builder.Property(a => a.PrevAppendixId).HasColumnName("PrevAppendixId");
        builder.Property(a => a.IsActive).HasColumnName("IsActive");
        builder.Property(a => a.IsDeleted).HasColumnName("IsDeleted");
        builder.Property(a => a.UserIdCreatedBy).HasColumnName("UserIdCreatedBy");
        builder.Property(a => a.CreatedOnDate).HasColumnName("CreatedOnDate");
        builder.Property(a => a.UserIdModifyBy).HasColumnName("UserIdModifyBy");
        builder.Property(a => a.ModifiedOnDate).HasColumnName("ModifiedOnDate");
        builder.Property(a => a.UserIdDeletedBy).HasColumnName("UserIdDeletedBy");
        builder.Property(a => a.DeletedOnDate).HasColumnName("DeletedOnDate");

        builder.HasQueryFilter(a => !a.IsDeleted);
        builder.HasIndex(a => new { a.FinancialYear, a.Code }).IsUnique();
    }
}
