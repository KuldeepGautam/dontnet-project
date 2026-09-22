namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Appendices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities;

/// <summary>
/// Configures the columns every one of the 22 appendix tables shares (AppendixEntityBase) — each
/// concrete IEntityTypeConfiguration calls ConfigureCommon(builder, tableName) first, then adds its
/// own appendix-specific columns/FKs. Avoids repeating the same ~16-column mapping 22 times.
/// </summary>
public abstract class AppendixEntityConfigurationBase<TEntity> where TEntity : AppendixEntityBase
{
    protected void ConfigureCommon(EntityTypeBuilder<TEntity> builder, string tableName)
    {
        builder.ToTable(tableName, "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
        builder.Property(e => e.DemandId).HasColumnName("DemandId");
        builder.Property(e => e.DemandNo).HasColumnName("DemandNo");
        builder.Property(e => e.FinancialYear).HasColumnName("FinancialYear").HasMaxLength(9).IsRequired();
        builder.Property(e => e.IsFrozen).HasColumnName("IsFrozen");
        builder.Property(e => e.FrozenAtUtc).HasColumnName("FrozenAtUtc");
        builder.Property(e => e.FrozenByUserId).HasColumnName("FrozenByUserId");
        builder.Property(e => e.IsDeleted).HasColumnName("IsDeleted");
        builder.Property(e => e.UserIdCreatedBy).HasColumnName("UserIdCreatedBy");
        builder.Property(e => e.CreatedOnDate).HasColumnName("CreatedOnDate");
        builder.Property(e => e.UserIdModifyBy).HasColumnName("UserIdModifyBy");
        builder.Property(e => e.ModifiedOnDate).HasColumnName("ModifiedOnDate");
        builder.Property(e => e.UserIdDeletedBy).HasColumnName("UserIdDeletedBy");
        builder.Property(e => e.DeletedOnDate).HasColumnName("DeletedOnDate");
        builder.Property(e => e.CreatedByIp).HasColumnName("CreatedByIp").HasMaxLength(45);
        builder.Property(e => e.CreatedByUserName).HasColumnName("CreatedByUserName").HasMaxLength(100);
        builder.Property(e => e.ModifiedByIp).HasColumnName("ModifiedByIp").HasMaxLength(45);
        builder.Property(e => e.ModifiedByUserName).HasColumnName("ModifiedByUserName").HasMaxLength(100);
        builder.Property(e => e.DeletedByIp).HasColumnName("DeletedByIp").HasMaxLength(45);
        builder.Property(e => e.DeletedByUserName).HasColumnName("DeletedByUserName").HasMaxLength(100);

        builder.HasQueryFilter(e => !e.IsDeleted);
        builder.HasIndex(e => new { e.DemandId, e.FinancialYear });
        builder.HasIndex(e => new { e.DemandNo, e.FinancialYear });
    }
}
