namespace UBIS.Services.Ecl.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Ecl.Domain.Entities;

/// <summary>Maps dbo.ECL_T_Actuals_Log — write-only audit trail, one row per actuals save. Column shape confirmed 2026-08-18 via INFORMATION_SCHEMA.COLUMNS.</summary>
public class EclActualsLogEntryConfiguration : IEntityTypeConfiguration<EclActualsLogEntry>
{
    public void Configure(EntityTypeBuilder<EclActualsLogEntry> builder)
    {
        builder.ToTable("ECL_T_Actuals_Log", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.DataId);
        builder.Property(e => e.DataId).HasColumnName("DataId").ValueGeneratedOnAdd();

        builder.Property(e => e.RowId).HasColumnName("RowID");
        builder.Property(e => e.FinancialYear).HasColumnName("FinancialYear").HasMaxLength(9);
        builder.Property(e => e.CategoryId).HasColumnName("CategoryId");
        builder.Property(e => e.SubCategoryId).HasColumnName("SubCategoryId");
        builder.Property(e => e.DemandId).HasColumnName("DemandID");
        builder.Property(e => e.SchemeId).HasColumnName("SchemeID");
        builder.Property(e => e.SubSchemeId).HasColumnName("SubSchemeID");

        builder.Property(e => e.Fy1Actuals).HasColumnName("FY1_Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy2Actuals).HasColumnName("FY2_Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy3Actuals).HasColumnName("FY3_Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy4Actuals).HasColumnName("FY4_Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy5Actuals).HasColumnName("FY5_Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy6Actuals).HasColumnName("FY6_Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy7Actuals).HasColumnName("FY7_Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy8Actuals).HasColumnName("FY8_Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy9Actuals).HasColumnName("FY9_Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy10Actuals).HasColumnName("FY10_Actuals").HasColumnType("decimal(18,2)");

        builder.Property(e => e.ActualsUserId).HasColumnName("ActualsUserId");
        builder.Property(e => e.ActualsIp).HasColumnName("ActualsIP").HasMaxLength(16);
        builder.Property(e => e.ActualsEntryDate).HasColumnName("ActualsEntryDate");
        builder.Property(e => e.Action).HasColumnName("Action").HasMaxLength(1);
        builder.Property(e => e.ModifiedDate).HasColumnName("ModifiedDate");
        builder.Property(e => e.ModifiedIp).HasColumnName("ModifiedIP").HasMaxLength(16);

        builder.Property(e => e.IsActive).HasColumnName("IsActive");
        builder.Property(e => e.UserIdCreatedBy).HasColumnName("UserIdCreatedBy");
        builder.Property(e => e.CreatedOnDate).HasColumnName("CreatedOnDate");
        builder.Property(e => e.UserIdModifyBy).HasColumnName("UserIdModifyBy");
        builder.Property(e => e.ModifiedOnDate).HasColumnName("ModifiedOnDate");
        builder.Property(e => e.IsDeleted).HasColumnName("IsDeleted").IsRequired();
        builder.Property(e => e.DeletedByUserId).HasColumnName("DeletedByUserId");
        builder.Property(e => e.DeletedByIp).HasColumnName("DeletedByIP").HasMaxLength(50);
        builder.Property(e => e.DeletedOnDate).HasColumnName("DeletedOnDate");

        builder.HasIndex(e => e.RowId);
    }
}
